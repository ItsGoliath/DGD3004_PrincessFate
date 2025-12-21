using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Renkli pad: dogru renk dizisini girince odul acar. Fokus ac/kapa icerde yonetilir; fokus disindayken butonlar ve canvaslar kilitlenir, cursor kapatilir.
/// </summary>
public class ColorPadController : MonoBehaviour
{
    [Tooltip("Dogru sira. Or: R, G, B, Y veya Red, Green, Blue, Yellow.")]
    [SerializeField] private string[] sequence = new string[] { "R", "G", "B", "Y" };

    [Header("Odul")]
    [Tooltip("Dogru kombinasyon sonrasi acilacak/etkinlesecek odul (disk, kaset vb.).")]
    [SerializeField] private GameObject rewardToEnable;
    [Tooltip("Dogru kombinasyon sonrasi anahtar esyayi otomatik ver.")]
    [SerializeField] private bool grantKeyOnSolve = false;

    [Header("Olaylar")]
    [SerializeField] private UnityEvent onStepCorrect;
    [SerializeField] private UnityEvent onStepWrong;
    [SerializeField] private UnityEvent onSolved;

    [Header("Focus")]
    [SerializeField] private bool requireFocus = true;
    [SerializeField] private LevelCursor2D focusCursor; // focus disinda imlec hareketini kapatmak icin
    [SerializeField] private string cursorObjectTag = "LevelCursor"; // opsiyonel: sahnede tag ile bul
    [SerializeField] private CanvasGroup[] focusCanvasGroups; // focus yokken tiklama blokla
    [SerializeField] private Button[] focusButtons; // opsiyonel, sadece interactable ac/kapa
    [SerializeField] private string focusButtonTag = "LevelCursor"; // tag ile Button bul (additive sahne)

    private bool hasFocus;
    public bool HasFocus => !requireFocus || hasFocus;
    public bool RequireFocus => requireFocus;

    private int currentIndex;
    private bool solved;

    private void Awake()
    {
        ResolveCursor();
        ResolveButtons(forceRefresh: true);
        ApplyFocusState(false);
    }

    private void ResolveCursor()
    {
        if (focusCursor == null && !string.IsNullOrEmpty(cursorObjectTag))
        {
            var tagged = GameObject.FindWithTag(cursorObjectTag);
            if (tagged != null)
                focusCursor = tagged.GetComponent<LevelCursor2D>();
        }

        if (focusCursor == null)
        {
            focusCursor = FindObjectOfType<LevelCursor2D>(true);
        }
    }

    private void ResolveButtons(bool forceRefresh)
    {
        if (!forceRefresh && focusButtons != null && focusButtons.Length > 0)
            return;

        var found = new List<Button>();
        if (!string.IsNullOrEmpty(focusButtonTag))
        {
            Button[] all = Resources.FindObjectsOfTypeAll<Button>(); // inactive dahil
            foreach (var btn in all)
            {
                if (btn == null) continue;
                if (btn.gameObject.CompareTag(focusButtonTag))
                    found.Add(btn);
            }
        }

        if (found.Count > 0)
            focusButtons = found.ToArray();
    }

    /// <summary>
    /// Odak disaridan set edilir (PC gir/cik vb.).
    /// </summary>
    public void SetFocus(bool on)
    {
        hasFocus = on;
        ResolveButtons(forceRefresh: true); // sonradan acilan butonlari da yakala
        ApplyFocusState(on);
    }

    private void ApplyFocusState(bool on)
    {
        if (focusCursor != null)
            focusCursor.SetCursorActive(on);

        if (focusCanvasGroups != null)
        {
            foreach (var cg in focusCanvasGroups)
            {
                if (cg == null) continue;
                cg.interactable = on;
                cg.blocksRaycasts = on;
            }
        }

        if (focusButtons != null)
        {
            foreach (var btn in focusButtons)
            {
                if (btn == null) continue;
                btn.interactable = on;
            }
        }
    }

    /// <summary>Butonlardan cagirilir.</summary>
    public void Submit(string colorId)
    {
        if (requireFocus && !HasFocus)
            return;

        if (solved)
            return;

        if (sequence == null || sequence.Length == 0)
            return;

        if (string.IsNullOrEmpty(colorId))
        {
            ResetProgress();
            return;
        }

        string input = colorId.Trim().ToUpperInvariant();
        string expected = sequence[currentIndex].Trim().ToUpperInvariant();

        if (input == expected)
        {
            currentIndex++;
            onStepCorrect?.Invoke();

            if (currentIndex >= sequence.Length)
            {
                solved = true;
                onSolved?.Invoke();
                UnlockReward();
            }
        }
        else
        {
            ResetProgress();
            onStepWrong?.Invoke();
        }
    }

    public void ResetProgress()
    {
        currentIndex = 0;
        // solved bayragini korur; yeniden acilmasini istemiyorsan cozdukten sonra tekrar submit etmezsin
    }

    private void UnlockReward()
    {
        if (rewardToEnable != null)
            rewardToEnable.SetActive(true);

        if (grantKeyOnSolve)
        {
            var state = KeyItemState.EnsureExists();
            if (state != null)
                state.GrantKeyItem();
        }
    }
}
