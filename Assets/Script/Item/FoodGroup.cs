using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(BoxCollider))]
public class FoodGroup : MonoBehaviour, IInteractable
{
    [Header("State")]
    public List<ItemData> ingredients = new List<ItemData>();
    public ItemData groupItemData;

    private Transform _visualContainer;
    private Countertop _currentCountertop;
    private BoxCollider _boxCollider;

    private void Awake()
    {
        _visualContainer = new GameObject("Visuals").transform;
        _visualContainer.SetParent(transform);
        _visualContainer.localPosition = Vector3.zero;
        _visualContainer.localRotation = Quaternion.identity;
        _visualContainer.localScale = Vector3.one;

        _boxCollider = GetComponent<BoxCollider>();
    }

    public void SetParentCountertop(Countertop countertop)
    {
        _currentCountertop = countertop;
    }

    public void AddIngredient(ItemData item)
    {
        if (!item.isProcessed) return;
        ingredients.Add(item);
        RefreshVisuals();
    }

    public void RefreshVisuals()
    {
        // 1. 清除舊模型
        foreach (Transform child in _visualContainer) Destroy(child.gameObject);

        // 2. 排序
        ingredients.Sort((a, b) => a.assemblySortOrder.CompareTo(b.assemblySortOrder));

        // 3. 堆疊
        Vector3 currentMountPoint = Vector3.zero;
        List<GameObject> spawnedVisuals = new List<GameObject>();
        GameObject topBunToSpawn = null;
        ItemData topBunItemData = null;

        if (_boxCollider) _boxCollider.enabled = false;

        foreach (var item in ingredients)
        {
            if (item.visualPrefab == null) continue;

            if (item.topBunVisual != null)
            {
                topBunToSpawn = item.topBunVisual;
                topBunItemData = item;
            }

            GameObject visualObj = Instantiate(item.visualPrefab, _visualContainer);
            StripPhysics(visualObj);

            // [Fix] 子物件負責 "放置旋轉"
            visualObj.transform.localRotation = Quaternion.Euler(item.placedRotation);

            if (visualObj.TryGetComponent<IngredientVisual>(out IngredientVisual visualData))
            {
                Vector3 offset = Vector3.zero;
                if (visualData.bottomPoint != null)
                {
                    // 計算 BottomPoint 在 Container 空間的向量
                    Vector3 bottomPosInContainer = visualObj.transform.localRotation * Vector3.Scale(visualData.bottomPoint.localPosition, visualObj.transform.localScale);
                    offset = -bottomPosInContainer;
                }

                visualObj.transform.localPosition = currentMountPoint + offset;

                if (visualData.topPoint != null)
                {
                    Vector3 topPosRelative = visualObj.transform.localRotation * Vector3.Scale(visualData.topPoint.localPosition, visualObj.transform.localScale);
                    currentMountPoint = visualObj.transform.localPosition + topPosRelative;
                }
            }
            else
            {
                visualObj.transform.localPosition = currentMountPoint;
            }

            spawnedVisuals.Add(visualObj);
        }

        // 生成上蓋
        if (topBunToSpawn != null)
        {
            GameObject topBun = Instantiate(topBunToSpawn, _visualContainer);
            StripPhysics(topBun);

            if (topBunItemData != null)
                topBun.transform.localRotation = Quaternion.Euler(topBunItemData.placedRotation);
            else
                topBun.transform.localRotation = Quaternion.identity;

            topBun.transform.localPosition = currentMountPoint;
            spawnedVisuals.Add(topBun);
        }

        if (_boxCollider) _boxCollider.enabled = true;
        RecalculateBounds(spawnedVisuals);
    }

    private void StripPhysics(GameObject obj)
    {
        foreach (var rb in obj.GetComponentsInChildren<Rigidbody>()) DestroyImmediate(rb);
        foreach (var col in obj.GetComponentsInChildren<Collider>()) DestroyImmediate(col);
        if (obj.TryGetComponent<PickupableItem>(out var pickup)) DestroyImmediate(pickup);
        if (obj.TryGetComponent<KitchenTool>(out var tool)) DestroyImmediate(tool);
        if (obj.TryGetComponent<FoodGroup>(out var group)) DestroyImmediate(group);
    }

    private void RecalculateBounds(List<GameObject> visuals)
    {
        if (visuals.Count == 0) return;

        Bounds combinedBounds = new Bounds(visuals[0].transform.position, Vector3.zero);
        bool hasBounds = false;

        foreach (var vis in visuals)
        {
            Renderer[] renderers = vis.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                if (!hasBounds) { combinedBounds = r.bounds; hasBounds = true; }
                else { combinedBounds.Encapsulate(r.bounds); }
            }
        }

        if (hasBounds)
        {
            _boxCollider.center = transform.InverseTransformPoint(combinedBounds.center);
            _boxCollider.size = combinedBounds.size;
        }
    }

    public void OnInteract(PlayerStateMachine player, ItemData heldItemData)
    {
        if (heldItemData != null && heldItemData.isProcessed)
        {
            AddIngredient(heldItemData);
            player.DestroyHeldItem();
        }
    }

    public void OnPickup(PlayerStateMachine player)
    {
        if (!player.IsHoldingItem)
        {
            if (_currentCountertop != null)
            {
                _currentCountertop.ClearPlacedObject();
                _currentCountertop = null;
            }

            ItemData pickupData = groupItemData != null ? groupItemData : ingredients[0];
            player.PickupItem(this.gameObject, pickupData);
        }
    }
}