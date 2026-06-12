using UnityEngine;

public class PickupableItem : MonoBehaviour, IInteractable
{
    public ItemData data;

    private Countertop _currentCountertop;
    public GameObject foodGroupPrefab;

    // [Fix 4] 掉落銷毀計時器
    private float _decayTimer = 0f;
    private const float DECAY_TIME = 10f;

    public void SetParentCountertop(Countertop countertop)
    {
        _currentCountertop = countertop;
    }

    private void Update()
    {
        // [Fix 4] 自動銷毀邏輯
        // 條件：是食材 + 未處理 + 沒人拿 (Parent為null) + 沒放桌上
        if (data != null && data.category == ItemCategory.Ingredient && !data.isProcessed)
        {
            // 檢查是否獨立存在於場景中 (不在玩家手上，也不在桌上)
            // transform.parent == null 通常代表在地板上 (或根目錄)
            // _currentCountertop == null 代表沒被 Countertop 記錄
            if (transform.parent == null && _currentCountertop == null)
            {
                _decayTimer += Time.deltaTime;
                if (_decayTimer >= DECAY_TIME)
                {
                    Debug.Log($"{name} 放置過久，自動銷毀");
                    Destroy(gameObject);
                }
            }
            else
            {
                _decayTimer = 0f; // 只要被拿起來或放桌上，計時重置
            }
        }
    }

    public void OnInteract(PlayerStateMachine player, ItemData heldItemData)
    {
        if (data != null && data.isProcessed && heldItemData != null && heldItemData.isProcessed)
        {
            TryCombineFoodOnGround(player, heldItemData);
        }
    }

    private void TryCombineFoodOnGround(PlayerStateMachine player, ItemData heldItemData)
    {
        if (foodGroupPrefab == null) return;

        ItemData groundItemData = this.data;
        Vector3 groundPosition = transform.position;

        GameObject newGroupObj = Instantiate(foodGroupPrefab, groundPosition, Quaternion.identity);
        newGroupObj.transform.rotation = Quaternion.identity;

        if (newGroupObj.TryGetComponent<Rigidbody>(out Rigidbody rb)) rb.isKinematic = false;

        FoodGroup group = newGroupObj.GetComponent<FoodGroup>();
        group.AddIngredient(groundItemData);
        group.AddIngredient(heldItemData);

        player.DestroyHeldItem();
        Destroy(this.gameObject);
    }

    public void OnPickup(PlayerStateMachine player)
    {
        if (!player.IsHoldingItem && data.canBePickedUp)
        {
            if (_currentCountertop != null)
            {
                _currentCountertop.ClearPlacedObject();
                _currentCountertop = null;
            }

            _decayTimer = 0f; // 拿起時重置銷毀計時
            player.PickupItem(this.gameObject, this.data);
        }
    }
}