using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Basit kağıt parçası envanteri. Sahne boyunca tek Instance.
/// Kod saklamaz; sadece toplanan parça kimliklerini tutar.
/// </summary>
public class PaperInventory : MonoBehaviour
{
    public static PaperInventory Instance { get; private set; }

    private readonly HashSet<string> collected = new HashSet<string>();
    private readonly HashSet<string> placed = new HashSet<string>();

    [Tooltip("Sahne değişiminde yok olmasın.")]
    [SerializeField] private bool dontDestroyOnLoad = true;

    void Awake()
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

    public void AddPiece(string id)
    {
        if (string.IsNullOrEmpty(id))
            return;
        collected.Add(id);
    }

    public bool HasPiece(string id)
    {
        if (string.IsNullOrEmpty(id))
            return false;
        return collected.Contains(id);
    }

    public bool IsPlaced(string id)
    {
        if (string.IsNullOrEmpty(id))
            return false;
        return placed.Contains(id);
    }

    public void MarkPlaced(string id)
    {
        if (string.IsNullOrEmpty(id))
            return;
        placed.Add(id);
    }

    public void ResetPlacement()
    {
        placed.Clear();
    }
}
