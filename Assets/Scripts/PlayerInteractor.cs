using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractor : MonoBehaviour
{
    public float interactRadius = 1.2f;

    public LayerMask interactLayer; //interactable 레이어 적용

    public PlayerItemHolder itemHolder;

    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current == null) return;
        if (!Keyboard.current.fKey.wasPressedThisFrame) return;

        if(itemHolder != null && itemHolder.IsHolding)
        {
            itemHolder.Drop();
            return;
        }

        IInteractable target = FindBestInteractable();
        if (target == null) return;

        if (target.CanInteract(this))
        {
            target.Interact(this);
        }
    }

    private IInteractable FindBestInteractable()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, interactRadius, interactLayer);

        float bestDist = float.MaxValue;

        IInteractable best = null;

        foreach(var h in hits)
        {
            var interactable = h.GetComponentInParent<IInteractable>();
            if (interactable == null) continue;

            float d = Vector3.Distance(transform.position, h.ClosestPoint(transform.position));
            if (d < bestDist)
            {
                bestDist = d;
                best = interactable;
            }
        }

        return best;
    }
}
