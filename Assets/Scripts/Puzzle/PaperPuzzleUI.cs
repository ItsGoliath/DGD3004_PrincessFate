using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Kağıt parçalarını kenardaki butonlardan tıklayıp slotlara yerleştirir.
/// Envanter kontrolü yok; butonlar her zaman aktif. Parça kimliği slot kimliği ile eşleşmezse, sıradaki boş slota yerleştirir.
/// </summary>
public class PaperPuzzleUI : MonoBehaviour
{
    [System.Serializable]
    public class PieceButton
    {
        public string pieceId;
        public Button button;
        public Image buttonIcon;
    }

    [System.Serializable]
    public class PieceSlot
    {
        public string pieceId;
        public Image slotImage;   // hedefte gösterilecek sprite
        public Sprite pieceSprite;
    }

    [SerializeField] private PieceButton[] pieceButtons;
    [SerializeField] private PieceSlot[] pieceSlots;
    [SerializeField] private GameObject completionBanner;

    void OnEnable()
    {
        WireButtons();
        RefreshButtons();
        RefreshSlots();
        UpdateCompletion();
    }

    private void WireButtons()
    {
        if (pieceButtons == null)
            return;

        for (int i = 0; i < pieceButtons.Length; i++)
        {
            int index = i;
            if (pieceButtons[i].button != null)
            {
                pieceButtons[i].button.onClick.RemoveAllListeners();
                pieceButtons[i].button.onClick.AddListener(() => PlacePieceFromButton(index));
            }
        }
    }

    private void PlacePieceFromButton(int buttonIndex)
    {
        if (pieceButtons == null || buttonIndex < 0 || buttonIndex >= pieceButtons.Length)
            return;

        var btn = pieceButtons[buttonIndex];
        string id = btn.pieceId;

        // Önce eşleşen slotu ara
        for (int i = 0; i < pieceSlots.Length; i++)
        {
            if (!string.IsNullOrEmpty(pieceSlots[i].pieceId) && pieceSlots[i].pieceId == id)
            {
                ApplySlot(i);
                RefreshButtons();
                UpdateCompletion();
                return;
            }
        }

        // Eşleşme yoksa sıradaki boş slota yerleştir
        for (int i = 0; i < pieceSlots.Length; i++)
        {
            var slot = pieceSlots[i];
            if (slot.slotImage != null && !slot.slotImage.enabled)
            {
                ApplySlot(i);
                RefreshButtons();
                UpdateCompletion();
                return;
            }
        }

        Debug.LogWarning("PaperPuzzleUI: Uygun slot bulunamadı, lütfen pieceId/slotId eşlemesini kontrol et.");
    }

    private void ApplySlot(int slotIndex)
    {
        if (pieceSlots == null || slotIndex < 0 || slotIndex >= pieceSlots.Length)
            return;

        var slot = pieceSlots[slotIndex];
        if (slot.slotImage != null)
        {
            slot.slotImage.sprite = slot.pieceSprite;
            slot.slotImage.enabled = true;
        }
    }

    private void RefreshButtons()
    {
        if (pieceButtons == null)
            return;

        for (int i = 0; i < pieceButtons.Length; i++)
        {
            var btn = pieceButtons[i];
            if (btn.button != null)
                btn.button.interactable = true; // her zaman aktif

            if (btn.buttonIcon != null)
            {
                btn.buttonIcon.color = Color.white;
            }
        }
    }

    private void RefreshSlots()
    {
        if (pieceSlots == null)
            return;

        for (int i = 0; i < pieceSlots.Length; i++)
        {
            var slot = pieceSlots[i];
            if (slot.slotImage != null)
            {
                slot.slotImage.enabled = false;
                slot.slotImage.sprite = null;
            }
        }
    }

    private void UpdateCompletion()
    {
        bool allPlaced = true;
        if (pieceSlots != null && pieceSlots.Length > 0)
        {
            for (int i = 0; i < pieceSlots.Length; i++)
            {
                if (pieceSlots[i].slotImage == null || !pieceSlots[i].slotImage.enabled)
                {
                    allPlaced = false;
                    break;
                }
            }
        }

        if (completionBanner != null)
            completionBanner.SetActive(allPlaced);
    }
}
