using UnityEngine;
using UnityEngine.InputSystem;
using System.Reflection;
using System.Collections.Generic;
using System;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// FPS > PC etkileşimi: Cinemachine varsa vCam Priority değiştirerek geçiş yapar; yoksa kontrolleri aç/kapatır.
/// 2D modda 2D player scriptlerini açar, FPS hareket/bakışı kapatır, crosshair gizler.
/// IInteractable: ray ile tetiklenir.
/// </summary>
public class ComputerInteractSwitch : MonoBehaviour, IInteractable
{
    [Header("Kameralar / Kontroller")]
    [SerializeField] private Camera fpCamera; // Ana Camera (MainCamera tag, Brain burada)
    [SerializeField] private bool refreshCameraOnExit = true; // PC'den çıkarken Camera.main'i tekrar çek
    [SerializeField] private bool refreshCameraAlwaysWhenFPS = true; // FPS modunda her frame Camera.main'i tazele

    [System.Serializable]
    private struct VCamPriority
    {
        public Component vcam;
        public int priorityDefault;
        public int priorityWhenPC;
    }

    [Header("VCam Priority Ayarları")]
    [SerializeField] private VCamPriority fpsVcamPriority = new VCamPriority { priorityDefault = 10, priorityWhenPC = 9 };
    [SerializeField] private VCamPriority pcVcamPriority  = new VCamPriority { priorityDefault = 9, priorityWhenPC = 20 };

    [Tooltip("Hareketi kapatmak için devre dışı bırakılacak scriptler (FirstPersonMovement vb.).")]
    [SerializeField] private MonoBehaviour[] movementScripts;
    [Tooltip("Bakışı kapatmak için devre dışı bırakılacak scriptler (FirstPersonLook vb.).")]
    [SerializeField] private MonoBehaviour[] lookScripts;
    [SerializeField] private Rigidbody fpRigidbody;

    [Header("2D Player Kontrol")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private MonoBehaviour[] playerComponentsToToggle;
    [SerializeField] private string[] playerComponentNames;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference interactAction;
    [SerializeField] private InputActionReference exitAction;

    [Header("Proximity Gate")]
    [SerializeField] private bool requireProximity = false;
    [SerializeField] private string proximityTag = "Player";
    [SerializeField] private Collider proximityCollider; // Harici trigger atanabilir
    private bool playerInsideProximity;

    [Header("Cursor / Crosshair")]
    [SerializeField] private bool hideSystemCursorInFP = true;
    [SerializeField] private bool manageCursor = true;
    [SerializeField] private GameObject crosshairOverlay;
    [SerializeField] private string levelCursorTag = "LevelCursor"; // additive sahnede otomatik bulma

    [Header("LevelCursor Buttons")]
    [SerializeField] private string levelCursorButtonTag = "LevelCursor";

    [Header("2D Physics")]
    [SerializeField] private bool pause2DWhenNotFocused = true;
    [SerializeField] private bool restoreSimulationOnDisable = true;

    private bool in2D;
    private GameObject cached2DPlayer;
    private SimulationMode2D originalSimMode = SimulationMode2D.FixedUpdate;
    private LevelCursor2D levelCursor2D;
    private List<Button> levelCursorButtons = new List<Button>();

    private void Awake()
    {
        originalSimMode = Physics2D.simulationMode;
        AttachProximityRelay();
    }

    private void Start()
    {
        RefreshCamera();
        Set2D(false);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
        interactAction?.action?.Enable();
        exitAction?.action?.Enable();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
        interactAction?.action?.Disable();
        exitAction?.action?.Disable();

        if (pause2DWhenNotFocused && restoreSimulationOnDisable)
            Physics2D.simulationMode = originalSimMode;
    }

    private void Update()
    {
        if (!in2D && refreshCameraAlwaysWhenFPS)
            RefreshCamera();

        if (in2D)
        {
            if (ExitPressedThisFrame())
                Set2D(false);
        }
    }

    private bool CanInteractInternal()
    {
        if (requireProximity && !playerInsideProximity)
            return false;
        return true;
    }

    private void Set2D(bool enable)
    {
        in2D = enable;

        ApplyPriority(fpsVcamPriority, enable);
        ApplyPriority(pcVcamPriority, enable);

        ToggleBehaviours(movementScripts, !enable);
        ToggleBehaviours(lookScripts, !enable);
        Toggle2DPlayer(enable);

        if (fpRigidbody != null && enable)
            fpRigidbody.linearVelocity = Vector3.zero;

        if (crosshairOverlay != null)
            crosshairOverlay.SetActive(!enable);

        ResolveCursorAndButtons();
        if (levelCursor2D != null)
            levelCursor2D.SetCursorActive(enable);
        ToggleLevelCursorButtons(enable);

        if (pause2DWhenNotFocused)
            Physics2D.simulationMode = enable ? originalSimMode : SimulationMode2D.Script;

        if (manageCursor)
        {
            if (enable)
            {
                Cursor.lockState = CursorLockMode.Confined;
                Cursor.visible = false;
            }
            else
            {
                if (refreshCameraOnExit)
                    RefreshCamera();

                Cursor.lockState = hideSystemCursorInFP ? CursorLockMode.Locked : CursorLockMode.None;
                Cursor.visible = !hideSystemCursorInFP;
            }
        }
    }

    private void RefreshCamera()
    {
        if (fpCamera == null || !fpCamera.isActiveAndEnabled)
        {
            if (Camera.main != null)
                fpCamera = Camera.main;
        }
    }

    private void ApplyPriority(VCamPriority config, bool pcMode)
    {
        int targetPriority = pcMode ? config.priorityWhenPC : config.priorityDefault;
        SetPriorityValue(config.vcam, targetPriority);
    }

    private void SetPriorityValue(Component cam, int priority)
    {
        if (cam == null)
            return;

        Component target = FindPriorityComponent(cam);
        if (target == null)
        {
            Debug.LogWarning($"ComputerInteractSwitch: Priority alani bulunamadi ({cam.name})");
            return;
        }

        const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
        var type = target.GetType();

        var prop = type.GetProperty("Priority", flags);
        if (prop != null && prop.CanWrite)
        {
            var pType = prop.PropertyType;
            if (pType == typeof(int))
            {
                prop.SetValue(target, priority, null);
                return;
            }
            object val = prop.GetValue(target, null);
            if (TrySetValueMember(val, pType, priority))
            {
                prop.SetValue(target, val, null);
                return;
            }
            var ctor = pType.GetConstructor(new[] { typeof(int) });
            if (ctor != null)
            {
                object boxed = ctor.Invoke(new object[] { priority });
                prop.SetValue(target, boxed, null);
                return;
            }
        }

        var field = type.GetField("Priority", flags);
        if (field != null)
        {
            var fType = field.FieldType;
            if (fType == typeof(int))
            {
                field.SetValue(target, priority);
                return;
            }
            object val = field.GetValue(target);
            if (TrySetValueMember(val, fType, priority))
            {
                field.SetValue(target, val);
                return;
            }
            var ctor = fType.GetConstructor(new[] { typeof(int) });
            if (ctor != null)
            {
                object boxed = ctor.Invoke(new object[] { priority });
                field.SetValue(target, boxed);
                return;
            }
        }

        Debug.LogWarning($"ComputerInteractSwitch: Priority alani atanamadi ({type.Name})");
    }

    private bool TrySetValueMember(object obj, Type type, int priority)
    {
        if (obj == null) return false;
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
        var valProp = type.GetProperty("Value", flags);
        if (valProp != null && valProp.CanWrite && valProp.PropertyType == typeof(int))
        {
            valProp.SetValue(obj, priority, null);
            return true;
        }
        var valField = type.GetField("Value", flags);
        if (valField != null && valField.FieldType == typeof(int))
        {
            valField.SetValue(obj, priority);
            return true;
        }
        return false;
    }

    private Component FindPriorityComponent(Component cam)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
        var t = cam.GetType();
        if (t.GetProperty("Priority", flags) != null || t.GetField("Priority", flags) != null)
            return cam;

        var comps = cam.gameObject.GetComponents<Component>();
        for (int i = 0; i < comps.Length; i++)
        {
            var ct = comps[i].GetType();
            if (ct.GetProperty("Priority", flags) != null || ct.GetField("Priority", flags) != null)
                return comps[i];
        }
        return null;
    }

    private void ToggleBehaviours(MonoBehaviour[] behaviours, bool state)
    {
        if (behaviours == null)
            return;
        for (int i = 0; i < behaviours.Length; i++)
            if (behaviours[i] != null)
                behaviours[i].enabled = state;
    }

    private void Toggle2DPlayer(bool enable)
    {
        GameObject playerObj = cached2DPlayer != null ? cached2DPlayer : FindPlayer();
        if (playerObj == null)
            return;

        cached2DPlayer = playerObj;
        var targets = GetComponentsToToggle(playerObj);
        for (int i = 0; i < targets.Length; i++)
            targets[i].enabled = enable;
    }

    private GameObject FindPlayer()
    {
        if (string.IsNullOrEmpty(playerTag))
            return null;
        return GameObject.FindGameObjectWithTag(playerTag);
    }

    private MonoBehaviour[] GetComponentsToToggle(GameObject playerObj)
    {
        if (playerComponentsToToggle != null && playerComponentsToToggle.Length > 0)
        {
            var list = new List<MonoBehaviour>();
            foreach (var template in playerComponentsToToggle)
            {
                if (template == null) continue;
                var mb = playerObj.GetComponent(template.GetType()) as MonoBehaviour;
                if (mb != null) list.Add(mb);
            }
            return list.ToArray();
        }

        if (playerComponentNames != null && playerComponentNames.Length > 0)
        {
            var list = new List<MonoBehaviour>();
            var all = playerObj.GetComponents<MonoBehaviour>();
            foreach (var mb in all)
            {
                string typeName = mb.GetType().Name;
                for (int j = 0; j < playerComponentNames.Length; j++)
                {
                    if (string.Equals(typeName, playerComponentNames[j], StringComparison.Ordinal))
                    {
                        list.Add(mb);
                        break;
                    }
                }
            }
            return list.ToArray();
        }

        return playerObj.GetComponents<MonoBehaviour>();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Additive)
            cached2DPlayer = FindPlayer();

        // Additive sahne geldiğinde LevelCursor2D sahneye eklenmiş olabilir; 2D moddaysak yeniden aç.
        if (in2D)
        {
            ResolveCursorAndButtons();
            if (levelCursor2D != null)
                levelCursor2D.SetCursorActive(true);
            ToggleLevelCursorButtons(true);
        }
    }

    private void OnSceneUnloaded(Scene scene)
    {
        if (cached2DPlayer != null && cached2DPlayer.scene == scene)
            cached2DPlayer = null;
    }

    private bool InteractPressedThisFrame()
    {
        if (interactAction != null && interactAction.action != null)
            return interactAction.action.triggered;
        return false;
    }

    private bool ExitPressedThisFrame()
    {
        if (exitAction != null && exitAction.action != null)
            return exitAction.action.triggered;
        return false;
    }

    private void AttachProximityRelay()
    {
        if (proximityCollider == null)
            return;
        var relay = proximityCollider.gameObject.GetComponent<ProximityRelay>();
        if (relay == null)
            relay = proximityCollider.gameObject.AddComponent<ProximityRelay>();
        relay.Init(this, proximityTag);
    }

    private void ProximityEnter(Collider other)
    {
        playerInsideProximity = true;
    }

    private void ProximityExit(Collider other)
    {
        playerInsideProximity = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!requireProximity) return;
        if (!other.CompareTag(proximityTag)) return;
        playerInsideProximity = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!requireProximity) return;
        if (!other.CompareTag(proximityTag)) return;
        playerInsideProximity = false;
    }

    // IInteractable
    public bool CanInteract(GameObject interactor)
    {
        return !in2D && CanInteractInternal();
    }

    public void Interact(GameObject interactor)
    {
        if (!in2D)
            Set2D(true);
    }

    public void Highlight(bool on, GameObject interactor)
    {
        // ozel highlight yok
    }

    private void ResolveCursorAndButtons()
    {
        if (levelCursor2D == null)
        {
            if (!string.IsNullOrEmpty(levelCursorTag))
            {
                var tagged = GameObject.FindWithTag(levelCursorTag);
                if (tagged != null)
                    levelCursor2D = tagged.GetComponent<LevelCursor2D>();
            }
            if (levelCursor2D == null)
                levelCursor2D = FindObjectOfType<LevelCursor2D>(true);
        }

        levelCursorButtons.Clear();
        if (!string.IsNullOrEmpty(levelCursorButtonTag))
        {
            var allButtons = Resources.FindObjectsOfTypeAll<Button>(); // inactive dahil
            foreach (var btn in allButtons)
            {
                if (btn == null) continue;
                if (btn.gameObject.CompareTag(levelCursorButtonTag))
                    levelCursorButtons.Add(btn);
            }
        }
    }

    private void ToggleLevelCursorButtons(bool enable)
    {
        if (levelCursorButtons == null) return;
        foreach (var btn in levelCursorButtons)
        {
            if (btn == null) continue;
            btn.interactable = enable;
        }
    }

    private class ProximityRelay : MonoBehaviour
    {
        private ComputerInteractSwitch owner;
        private string tagFilter;
        private bool initialized;

        public void Init(ComputerInteractSwitch target, string tag)
        {
            owner = target;
            tagFilter = tag;
            initialized = true;
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!initialized) return;
            if (!other.CompareTag(tagFilter)) return;
            owner.ProximityEnter(other);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!initialized) return;
            if (!other.CompareTag(tagFilter)) return;
            owner.ProximityExit(other);
        }
    }
}

