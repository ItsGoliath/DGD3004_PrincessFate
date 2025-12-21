using UnityEngine;

public interface IInteractable
{
    bool CanInteract(GameObject interactor);
    void Interact(GameObject interactor);
    void Highlight(bool on, GameObject interactor);
}
