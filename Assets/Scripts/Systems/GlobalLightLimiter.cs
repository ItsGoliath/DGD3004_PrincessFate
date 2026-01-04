using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Tek sahnede (ör. 3D hub) bir kez ekle. Additive yüklenen sahnelerde
/// aynı Unity layer + blend style için birden fazla Global Light2D varsa
/// ilkini bırakır, diğerlerini kapatır (konsol uyarılarını keser).
/// </summary>
public class GlobalLightLimiter : MonoBehaviour
{
    [SerializeField] private bool runOnSceneLoaded = true;
    [SerializeField] private bool runOnAwake = true;
    [SerializeField] private bool logActions = true;

    private void Awake()
    {
        if (runOnAwake)
            Resolve();

        if (runOnSceneLoaded)
            SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (runOnSceneLoaded)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Resolve();
    }

    public void Resolve()
    {
        Light2D[] lights = FindObjectsOfType<Light2D>(true);
        var seen = new Dictionary<(int unityLayer, int blend), Light2D>();
        int disabledCount = 0;

        foreach (var l in lights)
        {
            if (l == null || l.lightType != Light2D.LightType.Global || !l.enabled)
                continue;

            var key = (l.gameObject.layer, l.blendStyleIndex);

            if (!seen.ContainsKey(key))
            {
                seen[key] = l;
                continue;
            }

            l.enabled = false;
            disabledCount++;
            if (logActions)
                Debug.LogWarning($"GlobalLightLimiter: Disabled duplicate Global Light2D on unityLayer {l.gameObject.layer} blend {l.blendStyleIndex} : {l.name}", l);
        }

        if (logActions)
            Debug.Log($"GlobalLightLimiter: Active global lights kept {seen.Count}, disabled {disabledCount}");
    }
}
