using System.Collections;
using UnityEngine;

/// <summary>
/// Basit dÃ¼ÅŸman AI'sÄ±: idle/run/attack animasyonlarÄ± ve dikdÃ¶rtgen gÃ¶rme alanÄ±.
/// </summary>
public class EnemyController : MonoBehaviour
{
    [Header("Referanslar")]
    [SerializeField] private Transform player;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Carpisma")]
    [Tooltip("Ana beden collider'i (player ile beden carpismasini ignore etmek icin).")]
    [SerializeField] private Collider2D bodyCollider;
    [SerializeField] private bool ignorePlayerBodyCollision = false;
    [SerializeField] private bool useKinematicForBlocking = true;
    [Tooltip("Player ile surtunmeden dogan itme/sliding'i engellemek icin temas aninda x hizlarini sifirlar.")]
    [SerializeField] private bool dampenPlayerPush = true;

    [Header("Hareket")]
    public float moveSpeed = 2.5f;

    [Header("Detection Area")]
    public Vector2 detectionBoxSize = new Vector2(6f, 3f);
    public Vector2 detectionBoxOffset = new Vector2(3f, 0f);
    public LayerMask playerLayer;

    [Header("SaldZñrZñ")]
    public float attackCooldown = 1.0f;
    [SerializeField] private bool useAnimationEventsForHitbox = true;
    [SerializeField] private float attackAnimationDuration = 0.5f;
    [SerializeField] private Collider2D attackTrigger;
    [SerializeField] private int playerDamage = 1;
    [SerializeField] private Collider2D attackHitbox;
    [SerializeField] private float hitboxEnableDelay = 0.15f;
    [SerializeField] private float hitboxActiveDuration = 0.25f;
    public string idleBool = "Idle";
    public string runBool = "Run";
    public string attackBool = "IsAttacking";
    [SerializeField] private string attackClipName = "Attack";
    public bool flipSprite = true;
    [SerializeField] private bool lockFacingWhileAttacking = true;
    [SerializeField] private float flipDeadZone = 0.2f;

    [Header("Hasar")]
    [SerializeField] private Collider2D playerAttackCollider;
    [SerializeField] private string playerAttackTag = "PlayerAttack";
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private float knockbackForce = 6f;
    [SerializeField] private float knockbackVerticalBoost = 2f;
    [SerializeField] private bool horizontalOnlyKnockback = true;
    [SerializeField] private float knockbackDuration = 0.2f;
    [SerializeField] private float hitFlashDuration = 0.12f;
    [SerializeField] private Color hitFlashColor = Color.red;
    [SerializeField] private float damageCooldown = 0.1f;
    [Header("VFX")]
    [SerializeField] private GameObject deathEffectPrefab;
    [SerializeField] private float deathEffectLifetime = 2f;
    [SerializeField] private Vector3 deathEffectOffset = Vector3.zero;
    private RigidbodyType2D initialBodyType;
    private float initialGravityScale;
    private bool initialUseFullKinematicContacts;
    private bool bodyStateCaptured;

    private bool isAttacking;
    private bool canAttack = true;
    private bool facingRight = true;
    private int currentHealth;
    private bool isDead;
    private Coroutine flashRoutine;
    private Coroutine knockbackRoutine;
    private float lastDamageTime = float.NegativeInfinity;
    private readonly Collider2D[] attackTriggerHits = new Collider2D[2];
    private ContactFilter2D attackContactFilter;
    private bool attackFilterReady;
    private bool hitboxActive;
    private bool hasHitDuringCurrentSwing;
    private bool shouldMoveTowardsPlayer;
    private bool isKnockedBack;
    private readonly System.Collections.Generic.List<Collider2D> playerColliders = new System.Collections.Generic.List<Collider2D>();

    void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    void Awake()
    {
        EnsureBodyCollider();
        CaptureInitialBodyState();
        ApplyKinematicBlocking();
        ConfigureAttackContactFilter();
        TryFindPlayerByTag();
        EnemyTracker.Instance.RegisterEnemy(this);
        RefreshPlayerColliders();
        IgnoreAllPlayerBodyCollisions();
    }

    void OnValidate()
    {
        EnsureBodyCollider();
        maxHealth = Mathf.Max(1, maxHealth);
        playerDamage = Mathf.Max(1, playerDamage);
        damageCooldown = Mathf.Max(0f, damageCooldown);
        hitboxEnableDelay = Mathf.Max(0f, hitboxEnableDelay);
        hitboxActiveDuration = Mathf.Max(0f, hitboxActiveDuration);
        flipDeadZone = Mathf.Max(0f, flipDeadZone);
        ConfigureAttackContactFilter();
    }

    void OnEnable()
    {
        TryFindPlayerByTag();
        ResetHealth();
    }

    void OnDisable()
    {
        if (EnemyTracker.Instance != null)
            EnemyTracker.Instance.UnregisterEnemy(this);
    }

    private void ResetHealth()
    {
        currentHealth = Mathf.Max(1, maxHealth);
        isDead = false;
        flashRoutine = null;
        lastDamageTime = float.NegativeInfinity;
        hasHitDuringCurrentSwing = false;
        SetAttackHitboxActive(false);
        isKnockedBack = false;
        if (knockbackRoutine != null) { StopCoroutine(knockbackRoutine); knockbackRoutine = null; }
        if (rb != null) rb.linearVelocity = Vector2.zero;
        RestoreBodyState();
    }

    void Update()
    {
        if (player == null)
            TryFindPlayerByTag();

        if (player == null || isDead)
            return;

        if (transform.position.y <= fallDeathY)
        {
            Die();
            return;
        }

        bool playerInSight = CheckPlayerInDetectionBox();
        float deltaX = player.position.x - transform.position.x;
        float fullDistance = Vector2.Distance(transform.position, player.position);

        if (!playerInSight || fullDistance > detectionBoxSize.x * 1.5f)
        {
            if (!isAttacking)
                SetState(idle: true, run: false);
            shouldMoveTowardsPlayer = false;
            return;
        }

        OrientTowardsPlayer();

        bool playerInFront = facingRight ? deltaX >= 0f : deltaX <= 0f;
        bool playerInAttackZone = CheckPlayerInAttackTrigger();

        if (playerInFront && playerInAttackZone && canAttack)
        {
            StartCoroutine(AttackRoutine());
            shouldMoveTowardsPlayer = false;
        }
        else if (!isAttacking)
        {
            shouldMoveTowardsPlayer = true;
        }
        else
        {
            shouldMoveTowardsPlayer = false;
        }

        if (hitboxActive)
        {
            Collider2D dmgCol = GetDamageCollider();
            if (dmgCol != null)
                AttemptHitFromHitbox(dmgCol);
        }
    }

    void FixedUpdate()
    {
        if (shouldMoveTowardsPlayer && !isAttacking && !isDead && !isKnockedBack && player != null)
            MoveTowardsPlayer();
    }

    private bool CheckPlayerInDetectionBox()
    {
        Vector2 center = (Vector2)transform.position + GetDetectionOffset();
        Collider2D hit = Physics2D.OverlapBox(center, detectionBoxSize, 0f, playerLayer);
        return hit != null && hit.transform == player;
    }

    private void MoveTowardsPlayer()
    {
        SetState(idle: false, run: true);

        Vector2 direction = new Vector2(player.position.x - transform.position.x, 0f).normalized;
        rb.MovePosition(rb.position + direction * moveSpeed * Time.fixedDeltaTime);
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;
        canAttack = false;
        rb.linearVelocity = Vector2.zero;
        SetState(idle: false, run: false);

        if (animator != null)
            animator.SetBool(attackBool, true);

        float attackDuration = GetAttackClipDuration();

        if (useAnimationEventsForHitbox)
        {
            // Hitbox'i animasyon event'leri acip kapatacak; burada sadece animasyonun bitmesini bekliyoruz.
            hasHitDuringCurrentSwing = false;
            if (attackDuration > 0f)
                yield return new WaitForSeconds(attackDuration);
        }
        else
        {
            yield return HandleAttackDamageWindow();
        }

        if (animator != null)
            animator.SetBool(attackBool, false);

        isAttacking = false;
        SetState(idle: true, run: false);

        if (attackCooldown > 0f)
            yield return new WaitForSeconds(attackCooldown);

        canAttack = true;
    }

    private IEnumerator HandleAttackDamageWindow()
    {
        Collider2D damageCollider = GetDamageCollider();
        if (damageCollider == null)
        {
            DealDamageToPlayer(transform.position, requireTriggerCheck: true);
            yield break;
        }

        hasHitDuringCurrentSwing = false;

        if (hitboxEnableDelay > 0f)
            yield return new WaitForSeconds(hitboxEnableDelay);

        SetAttackHitboxActive(true);
        float remaining = hitboxActiveDuration;

        while (remaining > 0f)
        {
            AttemptHitFromHitbox(damageCollider);
            remaining -= Time.deltaTime;
            yield return null;
        }

        SetAttackHitboxActive(false);

        // final check in case overlap happened on last frame
        AttemptHitFromHitbox(damageCollider);
    }

    private float GetAttackClipDuration()
    {
        if (animator != null)
        {
            var ctrl = animator.runtimeAnimatorController;
            if (ctrl != null)
            {
                foreach (var clip in ctrl.animationClips)
                {
                    if (clip == null)
                        continue;

                    if (!string.IsNullOrEmpty(attackClipName) && clip.name == attackClipName)
                        return Mathf.Max(0f, clip.length);
                }
            }
        }

        return Mathf.Max(0f, attackAnimationDuration);
    }

    private void SetAttackHitboxActive(bool active)
    {
        hitboxActive = active;
        if (active)
            hasHitDuringCurrentSwing = false;

        // Oncelik: attackHitbox atanmis ise onu ac/kapat, yoksa attackTrigger'i kullan
        Collider2D target = attackHitbox != null ? attackHitbox : attackTrigger;
        if (target != null)
            target.enabled = active;
    }

    // Animasyon event'lerinden cagirmak icin
    public void AnimationEvent_EnableHitbox()
    {
        SetAttackHitboxActive(true);
    }

    public void AnimationEvent_DisableHitbox()
    {
        SetAttackHitboxActive(false);
    }

    private void AttemptHitFromHitbox(Collider2D damageCollider)
    {
        if (!hitboxActive || damageCollider == null || hasHitDuringCurrentSwing)
            return;

        if (!attackFilterReady)
            ConfigureAttackContactFilter();

        int hitCount = damageCollider.Overlap(attackContactFilter, attackTriggerHits);
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = attackTriggerHits[i];
            if (hit == null || hit.transform != player)
                continue;

            if (DealDamageToPlayer(hit.bounds.center, requireTriggerCheck: false))
            {
                hasHitDuringCurrentSwing = true;
                SetAttackHitboxActive(false);
            }

            break;
        }
    }

    private bool DealDamageToPlayer(Vector2 damageOrigin, bool requireTriggerCheck)
    {
        if (player == null || isDead)
            return false;

        if (requireTriggerCheck && !CheckPlayerInAttackTrigger())
            return false;

        Player playerComponent = player.GetComponent<Player>();
        if (playerComponent == null)
            return false;

        playerComponent.TakeDamage(playerDamage, damageOrigin);
        return true;
    }

    private Collider2D GetDamageCollider()
    {
        return attackHitbox != null ? attackHitbox : attackTrigger;
    }

    private void TryFindPlayerByTag()
    {
        if (player != null || string.IsNullOrEmpty(playerTag))
            return;

        GameObject found = GameObject.FindGameObjectWithTag(playerTag);
        if (found != null)
            player = found.transform;

        RefreshPlayerColliders();
        IgnoreAllPlayerBodyCollisions();
    }

    public bool IsDead()
    {
        return isDead;
    }

    private void SetState(bool idle, bool run)
    {
        if (animator == null)
            return;

        animator.SetBool(idleBool, idle);
        animator.SetBool(runBool, run);
    }

    private void OrientTowardsPlayer()
    {
        if (!flipSprite || spriteRenderer == null || player == null)
            return;

        if (lockFacingWhileAttacking && isAttacking)
            return;

        float deltaX = player.position.x - transform.position.x;
        if (Mathf.Abs(deltaX) <= flipDeadZone)
            return;

        facingRight = deltaX >= 0f;

        Vector3 scale = spriteRenderer.transform.localScale;
        float magnitude = Mathf.Abs(scale.x);
        scale.x = facingRight ? magnitude : -magnitude;
        spriteRenderer.transform.localScale = scale;
    }

    void OnDrawGizmosSelected()
    {
        Vector3 origin = transform.position;
        Vector2 center = (Vector2)origin + GetDetectionOffset();

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(center, detectionBoxSize);
        Gizmos.DrawWireSphere(center, 0.05f);

        if (attackTrigger != null)
        {
            Gizmos.color = Color.yellow;
            DrawColliderGizmo(attackTrigger);
        }

        if (attackHitbox != null)
        {
            Gizmos.color = Color.cyan;
            DrawColliderGizmo(attackHitbox);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        IgnoreBodyPush(other);
        CancelPlayerPush(other);
        HandleDamageCollision(other);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        IgnoreBodyPush(collision.collider);
        CancelPlayerPush(collision.collider);
        HandleDamageCollision(collision.collider);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        IgnoreBodyPush(collision.collider);
        CancelPlayerPush(collision.collider);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        IgnoreBodyPush(other);
        CancelPlayerPush(other);
    }

    private Vector2 GetDetectionOffset()
    {
        float offsetX = detectionBoxOffset.x;

        if (flipSprite)
        {
            offsetX *= facingRight ? 1f : -1f;
        }

        return new Vector2(offsetX, detectionBoxOffset.y);
    }

    private void HandleDamageCollision(Collider2D other)
    {
        if (other == null || isDead)
            return;

        if (!IsDamageSource(other))
            return;

        TryReceiveHit(other.bounds.center);
    }

    private bool IsDamageSource(Collider2D other)
    {
        if (playerAttackCollider != null && other == playerAttackCollider)
            return true;

        if (!string.IsNullOrEmpty(playerAttackTag) && other.CompareTag(playerAttackTag))
            return true;

        return false;
    }

    private void IgnoreBodyPush(Collider2D other)
    {
        if (!ignorePlayerBodyCollision || bodyCollider == null || other == null)
            return;

        if (string.IsNullOrEmpty(playerTag) || !other.CompareTag(playerTag))
            return;

        Physics2D.IgnoreCollision(bodyCollider, other, true);
    }

    private void CancelPlayerPush(Collider2D other)
    {
        if (!dampenPlayerPush || other == null || rb == null)
            return;

        if (string.IsNullOrEmpty(playerTag) || !other.CompareTag(playerTag))
            return;

        // Enemy yatay hizini sifirla
        Vector2 v = rb.linearVelocity;
        v.x = 0f;
        rb.linearVelocity = v;

        // Player yatay hizini da sifirla
        Rigidbody2D playerRb = other.attachedRigidbody;
        if (playerRb != null)
        {
            Vector2 pv = playerRb.linearVelocity;
            pv.x = 0f;
            playerRb.linearVelocity = pv;
        }
    }

    private void ApplyKinematicBlocking()
    {
        if (!useKinematicForBlocking || rb == null)
            return;

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.useFullKinematicContacts = true;
        rb.freezeRotation = true;
    }

    private void EnsureBodyCollider()
    {
        if (bodyCollider != null)
            return;

        Collider2D[] cols = GetComponentsInChildren<Collider2D>(includeInactive: true);
        for (int i = 0; i < cols.Length; i++)
        {
            if (cols[i] != null && !cols[i].isTrigger)
            {
                bodyCollider = cols[i];
                return;
            }
        }

        if (cols.Length > 0)
            bodyCollider = cols[0];
    }

    private void RefreshPlayerColliders()
    {
        playerColliders.Clear();
        if (player == null)
            return;

        player.GetComponentsInChildren(true, playerColliders);
    }

    private void IgnoreAllPlayerBodyCollisions()
    {
        if (!ignorePlayerBodyCollision || bodyCollider == null)
            return;

        if (playerColliders.Count == 0)
            RefreshPlayerColliders();

        for (int i = 0; i < playerColliders.Count; i++)
        {
            Collider2D col = playerColliders[i];
            if (col != null)
                Physics2D.IgnoreCollision(bodyCollider, col, true);
        }
    }

    private void TryReceiveHit(Vector2 sourcePosition)
    {
        if (damageCooldown > 0f && Time.time < lastDamageTime + damageCooldown)
            return;

        lastDamageTime = Time.time;
        ReceiveHit(sourcePosition);
    }

    private void ReceiveHit(Vector2 sourcePosition)
    {
        if (isDead)
            return;

        currentHealth = Mathf.Max(0, currentHealth - 1);
        ApplyKnockback(sourcePosition);
        PlayHitFlash();

        if (currentHealth <= 0)
            Die();
    }
    private void ApplyKnockback(Vector2 sourcePosition)
    {
        if (rb == null)
            return;

        Vector2 direction = ((Vector2)transform.position - sourcePosition).normalized;
        if (direction.sqrMagnitude < 0.0001f)
            direction = facingRight ? Vector2.right : Vector2.left;

        float horizontalSign = Mathf.Sign(direction.x);
        if (Mathf.Abs(horizontalSign) < 0.01f)
            horizontalSign = facingRight ? 1f : -1f;

        float knockY = horizontalOnlyKnockback ? 0f : knockbackVerticalBoost;
        Vector2 knockVelocity = new Vector2(horizontalSign * knockbackForce, knockY);

        if (knockbackRoutine != null)
            StopCoroutine(knockbackRoutine);

        shouldMoveTowardsPlayer = false;
        isKnockedBack = true;
        BeginKnockbackBodyState();
        knockbackRoutine = StartCoroutine(ForceKnockback(knockVelocity));
    }

    private IEnumerator ForceKnockback(Vector2 velocity)
    {
        rb.linearVelocity = velocity;
        float remaining = Mathf.Max(0f, knockbackDuration);

        while (remaining > 0f)
        {
            rb.linearVelocity = velocity;
            remaining -= Time.deltaTime;
            yield return null;
        }

        rb.linearVelocity = Vector2.zero;
        isKnockedBack = false;
        knockbackRoutine = null;
        RestoreBodyState();
    }

    private void CaptureInitialBodyState()
    {
        if (rb == null || bodyStateCaptured)
            return;

        initialBodyType = rb.bodyType;
        initialGravityScale = rb.gravityScale;
        initialUseFullKinematicContacts = rb.useFullKinematicContacts;
        bodyStateCaptured = true;
    }

    private void BeginKnockbackBodyState()
    {
        if (rb == null)
            return;

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.useFullKinematicContacts = false;
        float gravity = initialGravityScale;
        if (gravity < 0.1f)
            gravity = 1f;
        rb.gravityScale = gravity;
        rb.freezeRotation = true;
    }

    private void RestoreBodyState()
    {
        if (rb == null || !bodyStateCaptured)
            return;

        rb.bodyType = initialBodyType;
        rb.useFullKinematicContacts = initialUseFullKinematicContacts;
        rb.gravityScale = initialGravityScale;
        rb.freezeRotation = true;
    }

    private void PlayHitFlash()
    {
        if (spriteRenderer == null)
            return;

        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(HitFlashRoutine());
    }

    private IEnumerator HitFlashRoutine()
    {
        Color originalColor = spriteRenderer.color;
        spriteRenderer.color = hitFlashColor;
        yield return new WaitForSeconds(hitFlashDuration);
        spriteRenderer.color = originalColor;
        flashRoutine = null;
    }

    private void Die()
    {
        if (isDead)
            return;

        isDead = true;
        StopAllCoroutines();
        flashRoutine = null;
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        if (animator != null)
        {
            animator.SetBool(attackBool, false);
            animator.SetBool(runBool, false);
            animator.SetBool(idleBool, false);
        }

        SpawnDeathEffect();
        Destroy(gameObject);
        if (EnemyTracker.Instance != null)
            EnemyTracker.Instance.UnregisterEnemy(this);
    }

    private bool CheckPlayerInAttackTrigger()
    {
        if (attackTrigger == null || player == null)
            return false;

        if (!attackFilterReady)
            ConfigureAttackContactFilter();

        int hitCount = attackTrigger.Overlap(attackContactFilter, attackTriggerHits);
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = attackTriggerHits[i];
            if (hit == null)
                continue;

            if (hit.transform == player)
                return true;
        }

        return false;
    }

    private void ConfigureAttackContactFilter()
    {
        attackContactFilter = new ContactFilter2D();
        attackContactFilter.useLayerMask = true;
        attackContactFilter.layerMask = playerLayer.value == 0 ? Physics2D.AllLayers : playerLayer;
        attackContactFilter.useTriggers = true;
        attackFilterReady = true;
    }

    private void DrawColliderGizmo(Collider2D collider)
    {
        if (collider == null)
            return;

        Matrix4x4 previous = Gizmos.matrix;
        Gizmos.matrix = collider.transform.localToWorldMatrix;

        switch (collider)
        {
            case BoxCollider2D box:
                Gizmos.DrawWireCube(box.offset, box.size);
                break;
            case CircleCollider2D circle:
                Gizmos.DrawWireSphere(circle.offset, circle.radius);
                break;
            case CapsuleCollider2D capsule:
                Gizmos.DrawWireCube(capsule.offset, capsule.size);
                break;
            default:
                Gizmos.matrix = previous;
                Bounds bounds = collider.bounds;
                Gizmos.DrawWireCube(bounds.center, bounds.size);
                Gizmos.matrix = previous;
                return;
        }

        Gizmos.matrix = previous;
    }
    [Header("Other")]
    [SerializeField] private float fallDeathY = -20f;
    private void SpawnDeathEffect()
    {
        if (deathEffectPrefab == null)
            return;

        GameObject fx = Instantiate(deathEffectPrefab, transform.position + deathEffectOffset, Quaternion.identity);
        if (deathEffectLifetime > 0f)
            Destroy(fx, deathEffectLifetime);
    }
}
