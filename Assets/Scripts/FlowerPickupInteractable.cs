using UnityEngine;

public class FlowerPickupInteractable : MonoBehaviour, IInteractable
{
    public bool CanInteract(PlayerInteractor interactor)
    {
        if (interactor == null) return false;
        if (interactor.itemHolder == null) return false;

        return !interactor.itemHolder.IsHolding;
    }

    public void Interact(PlayerInteractor interactor)
    {
        if (!CanInteract(interactor)) return;
        interactor.itemHolder.Hold(gameObject);
    }
}
