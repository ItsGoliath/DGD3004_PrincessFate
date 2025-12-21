using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Disk envanteri: disk kimliklerini ve baðlý sahne/level adlarýný tutar, sahneler arasý taþýr.
/// Son alýnan diski de hatýrlar.
/// </summary>
public class DiskInventory : MonoBehaviour
{
    public static DiskInventory Instance { get; private set; }

    // DiskId -> hedef seviye adý
    private static Dictionary<string, string> savedDisks = new Dictionary<string, string>();
    private static string lastDiskId;
    private static string lastLevelName;

    [SerializeField] private bool dontDestroyOnLoad = true;

    /// <summary>
    /// Sahneye eklenmemiþse otomatik oluþturur ve döndürür.
    /// </summary>
    public static DiskInventory EnsureExists()
    {
        if (Instance != null)
            return Instance;

        var go = new GameObject("DiskInventory");
        var inv = go.AddComponent<DiskInventory>();
        inv.dontDestroyOnLoad = true;
        Instance = inv; // Awake beklemeden kur.
        Debug.Log("DiskInventory: Auto-created singleton");
        return Instance;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (dontDestroyOnLoad)
            DontDestroyOnLoad(gameObject);
    }

    public void AddDisk(string diskId, string levelName)
    {
        if (string.IsNullOrEmpty(diskId))
        {
            Debug.LogWarning("DiskInventory: diskId boþ, eklenmedi");
            return;
        }
        savedDisks[diskId] = levelName ?? string.Empty;
        lastDiskId = diskId;
        lastLevelName = levelName ?? string.Empty;
        Debug.Log($"DiskInventory: Added disk '{diskId}' -> level '{lastLevelName}'");
    }

    public bool HasDisk(string diskId)
    {
        if (string.IsNullOrEmpty(diskId))
            return false;
        return savedDisks.ContainsKey(diskId);
    }

    public bool TryGetLevel(string diskId, out string levelName)
    {
        if (string.IsNullOrEmpty(diskId))
        {
            levelName = string.Empty;
            return false;
        }
        return savedDisks.TryGetValue(diskId, out levelName);
    }

    /// <summary>
    /// Son alýnan diskin id/level bilgisi.
    /// </summary>
    public bool TryGetLastDisk(out string diskId, out string levelName)
    {
        diskId = lastDiskId;
        levelName = lastLevelName;
        bool ok = !string.IsNullOrEmpty(diskId) && !string.IsNullOrEmpty(levelName);
        Debug.Log($"DiskInventory: TryGetLastDisk ok={ok}, disk='{diskId}', level='{levelName}'");
        return ok;
    }

    /// <summary>
    /// Belirli diski siler; son disk ise hafýzayý temizler.
    /// </summary>
    public bool RemoveDisk(string diskId)
    {
        if (string.IsNullOrEmpty(diskId))
            return false;

        bool removed = savedDisks.Remove(diskId);
        if (removed && diskId == lastDiskId)
        {
            lastDiskId = string.Empty;
            lastLevelName = string.Empty;
        }
        Debug.Log($"DiskInventory: Remove '{diskId}', removed={removed}");
        return removed;
    }

    public void ClearAll()
    {
        savedDisks.Clear();
        lastDiskId = string.Empty;
        lastLevelName = string.Empty;
    }
}
