using UnityEngine;

public class IngredientBox : MonoBehaviour, IInteractable
{
    public IngredientBoxData data;

    public void OnInteract(PlayerStateMachine player, ItemData heldItemData)
    {
        // 按 'E' 對食材箱互動沒作用
    }

    public void OnPickup(PlayerStateMachine player)
    {
        // 按 'G' 嘗試 "撿取"
        if (player.IsHoldingItem) return;

        if (data.itemToSpawnPrefab != null)
        {
            GameObject newItemObj = Instantiate(data.itemToSpawnPrefab, player.HandHoldPoint.position, player.HandHoldPoint.rotation);
            if (newItemObj.TryGetComponent<PickupableItem>(out PickupableItem item))
            {
                player.PickupItem(newItemObj, item.data);
            }
            else
            {
                Destroy(newItemObj); // 生成了錯誤的東西，銷毀它
            }
        }
    }
}