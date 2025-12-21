using UnityEngine;

/// <summary>
/// Animator'i child objede olan karakterler icin animasyon eventlerini PlayerAttack'a iletir.
/// Animator bulunan child objeye ekleyin, target olarak parent'taki PlayerAttack'i verin.
/// </summary>
public class AttackAnimationRelay : MonoBehaviour
{
    [SerializeField] private PlayerAttack playerAttack;

    public void AnimationEvent_PlayerHitboxOn()
    {
        playerAttack?.AnimationEvent_PlayerHitboxOn();
    }

    public void AnimationEvent_PlayerHitboxOff()
    {
        playerAttack?.AnimationEvent_PlayerHitboxOff();
    }

    void Reset()
    {
        if (playerAttack == null)
            playerAttack = GetComponentInParent<PlayerAttack>();
    }
}
