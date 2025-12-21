using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Plays the slash animation and toggles separate left/right weapon colliders.
/// </summary>
public class PlayerAttack : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer characterSprite;
    [SerializeField] private Collider2D rightWeaponCollider;
    [SerializeField] private Collider2D leftWeaponCollider;
    [SerializeField] private AudioSource attackAudioSource;
    [SerializeField] private AudioClip attackSfx;

    [Header("Settings")]
    [SerializeField] private float attackCooldown = 0.1f;
    [SerializeField] private string attackTriggerName = "SlashTrigger";

    [Header("Input Actions")]
    [SerializeField] private InputActionReference attackAction;

    private bool isAttacking;
    private bool coolingDown;
    private int attackTriggerHash;
    private Player playerComponent;

    void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        CacheCharacterSprite();
        DisableAllWeaponColliders();

        if (!string.IsNullOrEmpty(attackTriggerName))
            attackTriggerHash = Animator.StringToHash(attackTriggerName);
    }

    void OnValidate()
    {
        attackCooldown = Mathf.Max(0f, attackCooldown);
        CacheCharacterSprite();

        if (!Application.isPlaying)
            DisableAllWeaponColliders();
    }

    void OnEnable()
    {
        attackAction?.action?.Enable();
    }

    void OnDisable()
    {
        attackAction?.action?.Disable();
        DisableAllWeaponColliders();
    }

    void Update()
    {
        if (AttackPressedThisFrame())
            TryAttack();
    }

    private void TryAttack()
    {
        if (isAttacking || coolingDown)
            return;

        StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;

        if (animator != null)
            animator.SetTrigger(attackTriggerHash);

        PlayAttackSound();
        DisableAllWeaponColliders();

        isAttacking = false;
        coolingDown = true;

        if (attackCooldown > 0f)
            yield return new WaitForSeconds(attackCooldown);

        coolingDown = false;
    }

    // Animasyon event'lerinden cagirmak icin
    public void AnimationEvent_PlayerHitboxOn()
    {
        Collider2D activeCollider = GetColliderForCurrentFacing();
        EnableWeaponCollider(activeCollider);
    }

    public void AnimationEvent_PlayerHitboxOff()
    {
        DisableAllWeaponColliders();
    }

    private void EnableWeaponCollider(Collider2D targetCollider)
    {
        DisableAllWeaponColliders();
        if (targetCollider != null)
            targetCollider.enabled = true;
    }

    private void DisableAllWeaponColliders()
    {
        if (rightWeaponCollider != null)
            rightWeaponCollider.enabled = false;
        if (leftWeaponCollider != null)
            leftWeaponCollider.enabled = false;
    }

    private Collider2D GetColliderForCurrentFacing()
    {
        return IsFacingLeft() ? leftWeaponCollider : rightWeaponCollider;
    }

    private bool IsFacingLeft()
    {
        if (characterSprite != null)
            return characterSprite.flipX;

        if (playerComponent == null)
            playerComponent = GetComponent<Player>();

        if (playerComponent != null && playerComponent.sprite != null)
            return playerComponent.sprite.flipX;

        return transform.localScale.x < 0f;
    }

    private bool HasAnimatorParameter(string name, AnimatorControllerParameterType type)
    {
        if (animator == null)
            return false;

        foreach (var param in animator.parameters)
        {
            if (param.type == type && param.name == name)
                return true;
        }
        return false;
    }

    private bool AttackPressedThisFrame()
    {
        if (attackAction != null && attackAction.action != null)
            return attackAction.action.triggered;

        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            return true;

        Gamepad gp = Gamepad.current;
        if (gp != null && (gp.rightTrigger.wasPressedThisFrame || gp.leftShoulder.wasPressedThisFrame))
            return true;

        return false;
    }

    private void CacheCharacterSprite()
    {
        if (characterSprite != null)
            return;

        if (playerComponent == null)
            playerComponent = GetComponent<Player>();

        if (playerComponent != null && playerComponent.sprite != null)
        {
            characterSprite = playerComponent.sprite;
            return;
        }

        if (animator != null)
        {
            SpriteRenderer animSprite = animator.GetComponent<SpriteRenderer>();
            if (animSprite != null)
            {
                characterSprite = animSprite;
                return;
            }
        }

        characterSprite = GetComponentInChildren<SpriteRenderer>();
    }

    private void PlayAttackSound()
    {
        if (attackAudioSource == null || attackSfx == null)
            return;

        attackAudioSource.PlayOneShot(attackSfx);
    }
}
