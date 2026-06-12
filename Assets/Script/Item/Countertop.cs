using UnityEngine;

public class Countertop : MonoBehaviour, IInteractable
{
    [Header("Settings")]
    public StationType stationType;
    public Transform placementPoint;

    [Header("State")]
    public GameObject PlacedObject;
    public ItemData PlacedItemData;

    public GameObject foodGroupPrefab;

    public void OnInteract(PlayerStateMachine player, ItemData heldItemData)
    {
        if (heldItemData != null)
        {
            if (PlacedObject == null) PlaceItem(player);
            else
            {
                bool isTableItemProcessed = PlacedItemData != null && PlacedItemData.isProcessed;
                bool isHeldItemProcessed = heldItemData.isProcessed;
                if (isTableItemProcessed && isHeldItemProcessed)
                {
                    TryCombineFood(player, heldItemData);
                    return;
                }
                if (PlacedObject.TryGetComponent<IInteractable>(out IInteractable interactable))
                {
                    if (interactable is KitchenTool tool) tool.CurrentStation = this.stationType;
                    interactable.OnInteract(player, heldItemData);
                }
            }
        }
        else
        {
            if (PlacedObject == null) return;
            KitchenTool tool = PlacedObject.GetComponent<KitchenTool>();
            bool isTool = tool != null;
            bool canPickup = PlacedItemData.canBePickedUp;
            bool hasInteraction = false;
            if (isTool)
            {
                tool.CurrentStation = this.stationType;
                hasInteraction = tool.CanInteract();
            }
            if (canPickup && !hasInteraction)
            {
                if (PlacedObject.TryGetComponent<IInteractable>(out IInteractable interactable)) interactable.OnPickup(player);
            }
            else if (canPickup && hasInteraction)
            {
                if (PlacedObject.TryGetComponent<IInteractable>(out IInteractable interactable)) interactable.OnInteract(player, null);
            }
            else if (!canPickup && hasInteraction)
            {
                if (PlacedObject.TryGetComponent<IInteractable>(out IInteractable interactable)) interactable.OnInteract(player, null);
            }
        }
    }

    public void OnPickup(PlayerStateMachine player)
    {
        if (PlacedObject != null && PlacedObject.TryGetComponent<IInteractable>(out IInteractable interactable)) interactable.OnPickup(player);
    }

    private void TryCombineFood(PlayerStateMachine player, ItemData heldItemData)
    {
        FoodGroup group = null;

        if (PlacedObject.TryGetComponent<FoodGroup>(out FoodGroup existingGroup))
        {
            group = existingGroup;
            group.AddIngredient(heldItemData);
            player.DestroyHeldItem();
        }
        else
        {
            ItemData tableItemData = PlacedItemData;
            GameObject oldObject = PlacedObject;
            GameObject newGroupObj = Instantiate(foodGroupPrefab, placementPoint);
            newGroupObj.transform.localPosition = Vector3.zero;
            newGroupObj.transform.localRotation = Quaternion.identity;

            if (newGroupObj.TryGetComponent<Rigidbody>(out Rigidbody rb)) rb.isKinematic = true;

            group = newGroupObj.GetComponent<FoodGroup>();
            group.SetParentCountertop(this);
            group.AddIngredient(tableItemData);
            group.AddIngredient(heldItemData);

            PlacedObject = newGroupObj;
            PlacedItemData = group.groupItemData != null ? group.groupItemData : tableItemData;

            Destroy(oldObject);
            player.DestroyHeldItem();
        }
    }

    private void PlaceItem(PlayerStateMachine player)
    {
        PlacedObject = player.HeldItem;
        PlacedItemData = player.GetHeldItemData();

        PlacedObject.transform.SetParent(placementPoint);
        PlacedObject.transform.localPosition = Vector3.zero;

        if (PlacedObject.TryGetComponent<FoodGroup>(out FoodGroup group))
        {
            PlacedObject.transform.localRotation = Quaternion.identity;
            group.SetParentCountertop(this);
        }
        else
        {
            if (PlacedItemData != null) PlacedObject.transform.localRotation = Quaternion.Euler(PlacedItemData.placedRotation);
            else PlacedObject.transform.localRotation = Quaternion.identity;

            if (PlacedObject.TryGetComponent<KitchenTool>(out KitchenTool tool))
            {
                tool.CurrentStation = this.stationType;
                tool.SetParentCountertop(this);
            }
            else if (PlacedObject.TryGetComponent<PickupableItem>(out PickupableItem item))
            {
                item.SetParentCountertop(this);
            }
        }

        if (PlacedObject.TryGetComponent<Rigidbody>(out Rigidbody rb)) rb.isKinematic = true;
        if (PlacedObject.TryGetComponent<Collider>(out Collider col)) col.enabled = true;

        player.ForceDropItem();
    }

    public void ClearPlacedObject() { PlacedObject = null; PlacedItemData = null; }
}