using UnityEngine;

public class PlayerItemHolder : MonoBehaviour
{
    public Transform handSocket;
    public Animator animator;

    public float dropForwardOffest = 0.8f;
    public float dropHeightOffest = 0.1f;

    public GameObject HeldItem { get; private set; }
    public bool IsHolding => HeldItem != null;

    public void Hold(GameObject item)
    {
        if (item == null) return;
        
        HeldItem = item;

        item.transform.SetParent(handSocket);
        item.transform.localPosition = Vector3.zero;
        item.transform.localRotation = Quaternion.identity;

        var itemRb = item.GetComponent<Rigidbody>();
        if (itemRb != null) itemRb.isKinematic = true;

        var itemCol = item.GetComponent<Collider>();
        if (itemCol != null) itemCol.isTrigger = true;


    }
}
