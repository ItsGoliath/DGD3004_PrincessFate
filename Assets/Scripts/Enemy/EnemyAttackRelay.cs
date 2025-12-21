using UnityEngine;

/// <summary>
/// Enemy Animator child objede ise animasyon eventlerini EnemyController'a iletir.
/// Animator bulunan child objeye ekleyin, target olarak parent'taki EnemyController'i verin.
/// </summary>
public class EnemyAttackRelay : MonoBehaviour
{
    [SerializeField] private EnemyController enemyController;

    public void AnimationEvent_EnableHitbox()
    {
        enemyController?.AnimationEvent_EnableHitbox();
    }

    public void AnimationEvent_DisableHitbox()
    {
        enemyController?.AnimationEvent_DisableHitbox();
    }

    void Reset()
    {
        if (enemyController == null)
            enemyController = GetComponentInParent<EnemyController>();
    }
}
