using UnityEngine;

public class TreeInteractable : MonoBehaviour, IInteractable
{
    public GameObject flowerPrefab;

    public bool CanInteract(PlayerInteractor interactor)
    {
        if (interactor == null) return false;
        if (interactor.itemHolder == null) return false;
        return !interactor.itemHolder.IsHolding; //현재 holding 상태가 아니어야 다른 interactable 오브젝트와 interact 가능하다.
    }

    public void Interact(PlayerInteractor interactor)
    {
        if (!CanInteract(interactor)) return;
        GameObject flower = Instantiate(flowerPrefab);
        interactor.itemHolder.Hold(flower);
    }
}
