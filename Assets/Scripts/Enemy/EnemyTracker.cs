using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sahnedeki düşmanların durumunu takip eder. EnemyController Awake/OnEnable'da kaydolmalı.
/// </summary>
public class EnemyTracker : MonoBehaviour
{
    private static EnemyTracker instance;

    private readonly HashSet<EnemyController> enemies = new HashSet<EnemyController>();

    public static EnemyTracker Instance
    {
        get
        {
            if (instance == null)
                instance = FindObjectOfType<EnemyTracker>();
            return instance;
        }
    }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    public void RegisterEnemy(EnemyController enemy)
    {
        if (enemy != null)
            enemies.Add(enemy);
    }

    public void UnregisterEnemy(EnemyController enemy)
    {
        if (enemy != null)
            enemies.Remove(enemy);
    }

    public bool AreAllEnemiesDefeated()
    {
        foreach (var enemy in enemies)
        {
            if (enemy != null && !enemy.IsDead())
                return false;
        }
        return true;
    }
}
