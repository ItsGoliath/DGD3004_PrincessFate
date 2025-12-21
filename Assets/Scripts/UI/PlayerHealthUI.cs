using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Oyuncu canını kafatası/ikon listesi üzerinden gösterir.
/// </summary>
public class PlayerHealthUI : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField] private List<GameObject> lifeIcons = new List<GameObject>();
    [SerializeField] private string playerTag = "Player";

    private int lastHealth = -1;

    void Start()
    {
        if (player == null)
            FindPlayer();

        UpdateIcons(force: true);
    }

    void OnValidate()
    {
        if (!Application.isPlaying)
            return;
        UpdateIcons(force: true);
    }

    void Update()
    {
        if (player == null)
        {
            FindPlayer();
            if (player == null)
                return;
        }

        if (player.CurrentHealth != lastHealth)
            UpdateIcons(force: true);
    }

    private void FindPlayer()
    {
        if (string.IsNullOrEmpty(playerTag))
            return;

        GameObject found = GameObject.FindGameObjectWithTag(playerTag);
        if (found != null)
            player = found.GetComponent<Player>();
    }

    private void UpdateIcons(bool force)
    {
        if (player == null)
            return;

        int currentHealth = Mathf.Clamp(player.CurrentHealth, 0, lifeIcons.Count);
        for (int i = 0; i < lifeIcons.Count; i++)
        {
            if (lifeIcons[i] != null)
                lifeIcons[i].SetActive(i < currentHealth);
        }

        lastHealth = currentHealth;
    }
}
