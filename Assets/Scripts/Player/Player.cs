using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class Player : MonoBehaviour
{
    [Header("Hareket")]
    public float walkSpeed = 6f;
    public float acceleration = 60f;
    public float deceleration = 70f;
    public bool airControl = true;

    [Header("Ziplama")]
    public float jumpForce = 14f;
    public float coyoteTime = 0.1f;
    public float jumpBuffer = 0.1f;
    public int extraAirJumps = 1;

    [Header("Duvar Temasi")]
    [Range(0f, 1f)] public float wallNormalThreshold = 0.9f;

    [Header("Wall Slide / Jump")]
    public bool unlockWallJump = true;
    public float wallSlideSpeed = 2f;
    public Vector2 wallJumpPower = new Vector2(10f, 12f);
    public float jumpCutMultiplier = 0.5f;
    public float wallJumpCooldown = 0.15f;

    [Header("Wall Anim Tag")]
    public string slideWallTag = "StickWall";

    [Header("Zemin / Duvar Kontrol")]
    public string groundTag = "Ground";
    public string wallTag = "Wall";
    [Range(0f, 1f)] public float groundNormalThreshold = 0.55f;

    [Header("Gorsel (Opsiyonel)")]
    public SpriteRenderer sprite;
    public Animator animator;
    [Header("Animasyon Parametreleri")]
    public string jumpTriggerName = "Jump";
    public string slideTriggerName = "Slide";

    [Header("Sesler")]
    [SerializeField] private AudioSource walkAudioSource;
    [SerializeField] private AudioSource sfxAudioSource;
    [SerializeField] private AudioClip footstepClip;
    [SerializeField] private AudioClip jumpClip;
    [SerializeField] private AudioClip hurtClip;

    [Header("Saglik")]
    public int maxHealth = 3;
    public float damageCooldown = 0.4f;
    public float hitKnockbackForce = 10f;
    public float hitKnockbackVerticalBoost = 6f;
    public float hitFlashDuration = 0.12f;
    public Color hitFlashColor = Color.white;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference jumpAction;

    private readonly HashSet<Collider2D> groundedColliders = new HashSet<Collider2D>();
    private readonly HashSet<Collider2D> leftWallColliders = new HashSet<Collider2D>();
    private readonly HashSet<Collider2D> rightWallColliders = new HashSet<Collider2D>();
    private readonly HashSet<Collider2D> slideWallColliders = new HashSet<Collider2D>();

    private Rigidbody2D rb;
    private float inputX;
    private bool isGrounded;
    private bool isTouchingLeftWall;
    private bool isTouchingRightWall;
    private float coyoteCounter;
    private float jumpBufferCounter;
    private int airJumpsRemaining;
    private bool movementLocked;
    private bool hasParamSpeed;
    private bool hasParamIsGrounded;
    private bool hasParamYVelocity;
    private bool hasParamIsRunning;
    private bool hasParamIsSticky;
    private bool hasParamIsFalling;
    private bool hasParamIsJumping;
    private float defaultGravityScale;
    private int jumpTriggerHash;
    private bool hasJumpTrigger;
    private int slideTriggerHash;
    private bool hasSlideTrigger;
    private bool hasSlideBool;
    private bool wallJumpReady;
    private bool isWallSliding;
    private bool wasWallSliding;
    private bool mustLeaveWallAfterJump;
    private float lastWallJumpTime;
    private int lastWallDir;
    private int currentHealth;
    private float lastDamageTime = float.NegativeInfinity;
    private Coroutine hitFlashRoutine;
    private bool isDead;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        defaultGravityScale = rb.gravityScale;
        if (sprite == null) sprite = GetComponentInChildren<SpriteRenderer>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        CacheAnimatorParameters();
        if (animator != null)
        {
            if (!string.IsNullOrEmpty(jumpTriggerName))
                jumpTriggerHash = Animator.StringToHash(jumpTriggerName);

            if (!string.IsNullOrEmpty(slideTriggerName))
                slideTriggerHash = Animator.StringToHash(slideTriggerName);
        }
        airJumpsRemaining = extraAirJumps;
        currentHealth = Mathf.Max(1, maxHealth);
        lastDamageTime = float.NegativeInfinity;
        isDead = false;
    }

    void OnEnable()
    {
        EnableInputActions();
    }

    void OnDisable()
    {
        DisableInputActions();
    }

    void Update()
    {
        if (isDead)
            return;

        Vector2 move = ReadMoveInput();
        inputX = move.x;

        if (JumpPressedThisFrame())
            jumpBufferCounter = jumpBuffer;

        if (jumpBufferCounter > 0f)
            jumpBufferCounter -= Time.deltaTime;

        bool wasGrounded = isGrounded;
        isGrounded = groundedColliders.Count > 0;
        isTouchingLeftWall = leftWallColliders.Count > 0;
        isTouchingRightWall = rightWallColliders.Count > 0;

        if (isGrounded)
        {
            coyoteCounter = coyoteTime;
            airJumpsRemaining = extraAirJumps;
            mustLeaveWallAfterJump = false;
            lastWallDir = 0;
            if (hasParamIsJumping)
                animator.SetBool("IsJumping", false);
        }
        else
        {
            coyoteCounter -= Time.deltaTime;
        }

        CheckWallSlide();

        if (sprite != null && Mathf.Abs(inputX) > 0.01f)
            sprite.flipX = inputX < 0f;

        if (JumpReleasedThisFrame() && rb.linearVelocity.y > 0f)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);

        if (animator != null)
        {
            if (hasParamSpeed)
                animator.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x));

            if (hasParamIsGrounded)
                animator.SetBool("IsGrounded", isGrounded);

            if (hasParamYVelocity)
                animator.SetFloat("YVelocity", rb.linearVelocity.y);

            if (hasParamIsRunning)
            {
                bool isRunning = Mathf.Abs(rb.linearVelocity.x) > 0.1f;
                animator.SetBool("IsRunning", isRunning);
            }

            if (hasParamIsSticky)
                animator.SetBool("IsStickyWall", isWallSliding && IsOnSlideWall());

            bool falling = !isGrounded && rb.linearVelocity.y < -0.1f;
            if (hasParamIsFalling)
                animator.SetBool("IsFalling", falling);

            if (hasParamIsJumping)
            {
                if (!isGrounded && rb.linearVelocity.y > 0.1f)
                    animator.SetBool("IsJumping", true);
                else if (isGrounded || falling)
                    animator.SetBool("IsJumping", false);
            }
        }
    }

    void FixedUpdate()
    {
        if (isDead)
            return;

        if (movementLocked)
            return;

        float targetSpeed = inputX * walkSpeed;
        float speedDif = targetSpeed - rb.linearVelocity.x;
        float accelRate = Mathf.Abs(targetSpeed) > 0.01f ? acceleration : deceleration;
        if (!isGrounded && !airControl) accelRate = 0f;

        float movement = speedDif * accelRate * Time.fixedDeltaTime;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x + movement, rb.linearVelocity.y);

        bool jumpQueued = jumpBufferCounter > 0f;
        if (jumpQueued)
        {
            if (unlockWallJump && isWallSliding)
            {
                PerformWallJump();
            }
            else if (coyoteCounter > 0f)
            {
                PerformJump(true);
            }
            else if (airJumpsRemaining > 0)
            {
                airJumpsRemaining--;
                PerformJump(false);
            }
        }

        if (!isGrounded)
        {
            if (isTouchingLeftWall && inputX < 0f && rb.linearVelocity.x < 0f)
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            }
            else if (isTouchingRightWall && inputX > 0f && rb.linearVelocity.x > 0f)
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            }
        }

        if (unlockWallJump && isWallSliding)
        {
            float y = rb.linearVelocity.y;
            if (y < -wallSlideSpeed)
                y = -wallSlideSpeed;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, y);
        }

        bool shouldPlayFootsteps = isGrounded && Mathf.Abs(rb.linearVelocity.x) > 0.1f && Mathf.Abs(inputX) > 0.1f;
        HandleFootstepAudio(shouldPlayFootsteps);
    }

    private void CheckWallSlide()
    {
        if (!unlockWallJump)
        {
            isWallSliding = false;
            wasWallSliding = false;
            return;
        }

        bool pushingWall = (isTouchingRightWall && inputX > 0f) || (isTouchingLeftWall && inputX < 0f);
        float vy = rb.linearVelocity.y;
        bool verticalOk = vy <= 0.05f || wasWallSliding; // ilk temasta titremeyi azalt
        if (IsOnSlideWall() && (isTouchingLeftWall || isTouchingRightWall) && !isGrounded && verticalOk && pushingWall)
        {
            isWallSliding = true;
            wallJumpReady = true;
        }
        else
        {
            isWallSliding = false;
        }

        bool startedSlide = isWallSliding && !wasWallSliding;
        bool endedSlide = !isWallSliding && wasWallSliding;

        if (animator != null)
        {
            if (startedSlide && hasSlideTrigger)
            {
                animator.ResetTrigger(slideTriggerHash);
                animator.SetTrigger(slideTriggerHash);
            }
            if (startedSlide && hasSlideBool)
            {
                animator.SetBool(slideTriggerName, true);
            }
            if (endedSlide && hasSlideBool)
            {
                animator.SetBool(slideTriggerName, false);
            }
            if (hasParamIsSticky && endedSlide)
            {
                animator.SetBool("IsStickyWall", isWallSliding && IsOnSlideWall());
            }
        }

        wasWallSliding = isWallSliding;
    }

    private void PerformWallJump()
    {
        int wallSide = isTouchingRightWall ? 1 : -1;   // duvar tarafi
        float wallDir = -wallSide;                     // firlatma yonu (duvardan disari)

        if (Time.time < lastWallJumpTime + wallJumpCooldown && wallSide == lastWallDir)
        {
            jumpBufferCounter = 0f;
            return;
        }

        leftWallColliders.Clear();
        rightWallColliders.Clear();
        slideWallColliders.Clear();

        rb.linearVelocity = Vector2.zero;
        Vector2 force = new Vector2(wallJumpPower.x * wallDir, wallJumpPower.y);
        rb.AddForce(force, ForceMode2D.Impulse);

        jumpBufferCounter = 0f;
        coyoteCounter = 0f;
        isWallSliding = false;
        wallJumpReady = false;
        mustLeaveWallAfterJump = true;
        lastWallJumpTime = Time.time;
        lastWallDir = wallSide; // artik duvar tarafini sakliyoruz
    }

    private void PerformJump(bool fromGround)
    {
        bool wallContact = !isGrounded && HasWallContact();
        if (wallContact && !wallJumpReady)
        {
            return;
        }

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        PlaySfx(jumpClip);

        if (hasParamIsJumping)
            animator.SetBool("IsJumping", true);

        if (hasJumpTrigger)
        {
            animator.ResetTrigger(jumpTriggerHash);
            animator.SetTrigger(jumpTriggerHash);
        }

        if (fromGround)
        {
            groundedColliders.Clear();
            isGrounded = false;
        }

        jumpBufferCounter = 0f;
        coyoteCounter = 0f;

        if (wallContact)
            wallJumpReady = false;
    }

    public void SetMovementLock(bool locked)
    {
        if (movementLocked == locked)
            return;

        movementLocked = locked;
        if (locked)
        {
            inputX = 0f;
            rb.linearVelocity = Vector2.zero;
            jumpBufferCounter = 0f;
            HandleFootstepAudio(false);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        EvaluateCollision(collision);
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        EvaluateCollision(collision);
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        groundedColliders.Remove(collision.collider);
        leftWallColliders.Remove(collision.collider);
        rightWallColliders.Remove(collision.collider);
        slideWallColliders.Remove(collision.collider);
        if (!HasWallContact())
        {
            wallJumpReady = false;
        }
    }

    private void EvaluateCollision(Collision2D collision)
    {
        bool canBeGround = string.IsNullOrEmpty(groundTag) || collision.collider.CompareTag(groundTag);

        if (canBeGround)
        {
            foreach (ContactPoint2D contact in collision.contacts)
            {
                if (contact.normal.y >= groundNormalThreshold)
                {
                    groundedColliders.Add(collision.collider);
                    leftWallColliders.Remove(collision.collider);
                    rightWallColliders.Remove(collision.collider);
                    return;
                }
            }
        }

        bool canBeWall = (string.IsNullOrEmpty(wallTag) || collision.collider.CompareTag(wallTag))
            || MatchesSlideWall(collision.collider);
        if (!canBeWall)
        {
            leftWallColliders.Remove(collision.collider);
            rightWallColliders.Remove(collision.collider);
            slideWallColliders.Remove(collision.collider);
            return;
        }

        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (contact.normal.x >= wallNormalThreshold)
            {
                int side = -1; // duvar sol tarafta
                if (mustLeaveWallAfterJump && lastWallDir == side)
                    return;
                if (mustLeaveWallAfterJump && lastWallDir != side)
                    mustLeaveWallAfterJump = false;

                leftWallColliders.Add(collision.collider);
                rightWallColliders.Remove(collision.collider);
                if (MatchesSlideWall(collision.collider))
                    slideWallColliders.Add(collision.collider);

                wallJumpReady = true;
                return;
            }

            if (contact.normal.x <= -wallNormalThreshold)
            {
                int side = 1; // duvar sag tarafta
                if (mustLeaveWallAfterJump && lastWallDir == side)
                    return;
                if (mustLeaveWallAfterJump && lastWallDir != side)
                    mustLeaveWallAfterJump = false;

                rightWallColliders.Add(collision.collider);
                leftWallColliders.Remove(collision.collider);
                if (MatchesSlideWall(collision.collider))
                    slideWallColliders.Add(collision.collider);

                wallJumpReady = true;
                return;
            }
        }

        leftWallColliders.Remove(collision.collider);
        rightWallColliders.Remove(collision.collider);
    }

        private bool MatchesSlideWall(Collider2D col)
    {
        if (col == null) return false;
        if (string.IsNullOrEmpty(slideWallTag)) return false;
        // CompareTag hatasi vermesin diye dogrudan string karsilastirma
        return col.tag == slideWallTag;
    }

    private void CacheAnimatorParameters()
    {
        if (animator == null)
            return;

        hasParamSpeed = false;
        hasParamIsGrounded = false;
        hasParamYVelocity = false;
        hasParamIsRunning = false;
        hasParamIsSticky = false;
        hasParamIsFalling = false;
        hasParamIsJumping = false;
        hasJumpTrigger = false;
        hasSlideTrigger = false;
        hasSlideBool = false;
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            switch (param.name)
            {
                case "Speed":
                    hasParamSpeed = true;
                    break;
                case "IsGrounded":
                    hasParamIsGrounded = true;
                    break;
                case "YVelocity":
                    hasParamYVelocity = true;
                    break;
                case "IsRunning":
                    hasParamIsRunning = true;
                    break;
                case "IsStickyWall":
                    hasParamIsSticky = true;
                    break;
                case "IsFalling":
                    hasParamIsFalling = true;
                    break;
                case "IsJumping":
                    hasParamIsJumping = true;
                    break;
                default:
                    if (!string.IsNullOrEmpty(jumpTriggerName) &&
                        param.type == AnimatorControllerParameterType.Trigger &&
                        param.name == jumpTriggerName)
                        hasJumpTrigger = true;

                    if (!string.IsNullOrEmpty(slideTriggerName) && param.name == slideTriggerName)
                    {
                        if (param.type == AnimatorControllerParameterType.Trigger)
                            hasSlideTrigger = true;
                        else if (param.type == AnimatorControllerParameterType.Bool)
                            hasSlideBool = true;
                    }
                    break;
            }
        }
    }

    private bool HasWallContact()
    {
        return leftWallColliders.Count > 0 || rightWallColliders.Count > 0;
    }

    private bool IsOnSlideWall()
    {
        return slideWallColliders.Count > 0 || string.IsNullOrEmpty(slideWallTag);
    }

    private Vector2 ReadMoveInput()
    {
        if (moveAction != null && moveAction.action != null)
            return Vector2.ClampMagnitude(moveAction.action.ReadValue<Vector2>(), 1f);

        Vector2 move = Vector2.zero;

        Keyboard kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)
                move.x -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed)
                move.x += 1f;
        }

        Gamepad gp = Gamepad.current;
        if (gp != null)
        {
            Vector2 stick = gp.leftStick.ReadValue();
            move += stick;
        }

        return Vector2.ClampMagnitude(move, 1f);
    }

    private bool JumpPressedThisFrame()
    {
        if (jumpAction != null && jumpAction.action != null)
            return jumpAction.action.triggered;

        Keyboard kb = Keyboard.current;
        if (kb != null && kb.spaceKey.wasPressedThisFrame)
            return true;

        Gamepad gp = Gamepad.current;
        if (gp != null && gp.buttonSouth.wasPressedThisFrame)
            return true;

        return false;
    }

    private bool JumpReleasedThisFrame()
    {
        if (jumpAction != null && jumpAction.action != null)
            return jumpAction.action.WasReleasedThisFrame();

        Keyboard kb = Keyboard.current;
        if (kb != null && kb.spaceKey.wasReleasedThisFrame)
            return true;

        Gamepad gp = Gamepad.current;
        if (gp != null && gp.buttonSouth.wasReleasedThisFrame)
            return true;

        return false;
    }

    public void TakeDamage(int amount, Vector2 sourcePosition)
    {
        if (isDead)
            return;

        if (Time.time < lastDamageTime + damageCooldown)
            return;

        int damage = Mathf.Max(1, amount);
        lastDamageTime = Time.time;
        currentHealth = Mathf.Max(0, currentHealth - damage);

        ApplyHitKnockback(sourcePosition);
        PlayHitFlash();
        PlaySfx(hurtClip);
        if (currentHealth <= 0)
        {
            HandleDeath();
        }
    }

    private void ApplyHitKnockback(Vector2 sourcePosition)
    {
        if (rb == null)
            return;

        Vector2 direction = ((Vector2)transform.position - sourcePosition).normalized;
        if (direction.sqrMagnitude < 0.0001f)
            direction = sprite != null && sprite.flipX ? Vector2.right : Vector2.left;

        float horizontalSign = direction.x >= 0f ? 1f : -1f;
        Vector2 knockVelocity = new Vector2(horizontalSign * hitKnockbackForce, hitKnockbackVerticalBoost);
        rb.linearVelocity = knockVelocity;
    }

    private void PlayHitFlash()
    {
        if (sprite == null)
            return;

        if (hitFlashRoutine != null)
            StopCoroutine(hitFlashRoutine);

        hitFlashRoutine = StartCoroutine(HitFlashRoutine());
    }

    private IEnumerator HitFlashRoutine()
    {
        Color originalColor = sprite.color;
        sprite.color = hitFlashColor;

        if (hitFlashDuration > 0f)
            yield return new WaitForSeconds(hitFlashDuration);
        else
            yield return null;

        sprite.color = originalColor;
        hitFlashRoutine = null;
    }

    public bool IsDead()
    {
        return isDead;
    }

    public int CurrentHealth => currentHealth;

    public int MaxHealth => maxHealth;

    public void ForceKill()
    {
        if (isDead)
            return;

        currentHealth = 0;
        HandleDeath();
    }

    private void HandleDeath()
    {
        if (isDead)
            return;

        isDead = true;
        SetMovementLock(true);
        if (rb != null)
            rb.linearVelocity = Vector2.zero;
        HandleFootstepAudio(false);
    }

    private void HandleFootstepAudio(bool shouldPlay)
    {
        if (walkAudioSource == null || footstepClip == null)
            return;

        walkAudioSource.loop = true;
        if (walkAudioSource.clip != footstepClip)
            walkAudioSource.clip = footstepClip;

        if (shouldPlay)
        {
            if (!walkAudioSource.isPlaying)
                walkAudioSource.Play();
        }
        else if (walkAudioSource.isPlaying)
        {
            walkAudioSource.Stop();
        }
    }

    private void PlaySfx(AudioClip clip)
    {
        if (clip == null || sfxAudioSource == null)
            return;

        sfxAudioSource.PlayOneShot(clip);
    }

    private void EnableInputActions()
    {
        moveAction?.action?.Enable();
        jumpAction?.action?.Enable();
    }

    private void DisableInputActions()
    {
        moveAction?.action?.Disable();
        jumpAction?.action?.Disable();
    }
}

