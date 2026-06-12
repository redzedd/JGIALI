using System.Collections.Generic;
using UnityEngine;

public class KitchenTool : MonoBehaviour, IInteractable
{
    public KitchenToolData data;
    public Collider catchTrigger;

    [Header("UI & Visuals")]
    public SimpleProgressBar progressBar;
    public Transform visualContainer;

    [Header("State")]
    public StationType CurrentStation = StationType.None;
    private Countertop _currentCountertop;

    [SerializeField] private List<ItemData> _contents = new List<ItemData>();

    private bool _isProcessing = false;
    private float _processTimer = 0f;
    private RecipeData _currentRecipe;
    private PlayerStateMachine _interactingPlayer;

    private ItemData _processedOutput;
    private List<GameObject> _spawnedVisuals = new List<GameObject>();

    private void Awake()
    {
        if (visualContainer == null)
        {
            visualContainer = new GameObject("VisualContainer").transform;
            visualContainer.SetParent(transform);
            visualContainer.localPosition = Vector3.zero;
            visualContainer.localRotation = Quaternion.identity;
        }
    }

    public void SetParentCountertop(Countertop countertop) { _currentCountertop = countertop; }
    public bool HasContents() => _contents.Count > 0;

    public void ClearContents()
    {
        _contents.Clear();
        _processedOutput = null;
        ResetProcessing();
        ClearVisuals();
        Debug.Log($"{name} 內容物已清空");
    }

    private void ClearVisuals()
    {
        foreach (var vis in _spawnedVisuals) if (vis != null) Destroy(vis);
        _spawnedVisuals.Clear();
    }

    private void AddVisual(ItemData item)
    {
        if (item.visualPrefab == null) return;
        Vector3 offset = new Vector3(0, 0.05f * _spawnedVisuals.Count, 0);
        GameObject visObj = Instantiate(item.visualPrefab, visualContainer);
        visObj.transform.localPosition = offset;
        visObj.transform.localRotation = Quaternion.Euler(item.inToolRotation);
        DestroyPhysics(visObj);
        _spawnedVisuals.Add(visObj);
    }

    private void DestroyPhysics(GameObject obj)
    {
        foreach (var rb in obj.GetComponentsInChildren<Rigidbody>()) Destroy(rb);
        foreach (var col in obj.GetComponentsInChildren<Collider>()) Destroy(col);
        if (obj.TryGetComponent<PickupableItem>(out var p)) Destroy(p);
    }

    private void Update()
    {
        if (_isProcessing && _currentRecipe != null)
        {
            if (_currentRecipe.processType == CookingProcessType.Automatic) ProcessAutomaticCooking();
            else if (_currentRecipe.processType == CookingProcessType.Manual) ProcessManualCooking();
        }
    }

    private void ProcessAutomaticCooking()
    {
        _processTimer += Time.deltaTime;
        UpdateUI();
        if (_interactingPlayer != null && _interactingPlayer.IsInteractPressed)
        {
            if (Vector3.Distance(transform.position, _interactingPlayer.transform.position) < 2.0f)
            {
                ResetProcessing();
                return;
            }
        }
        if (_processTimer >= _currentRecipe.processDuration) CompleteCooking();
    }

    private void ProcessManualCooking()
    {
        if (_interactingPlayer == null) { ResetProcessing(); return; }
        bool isHoldingKey = _interactingPlayer.IsInteractPressed;
        bool isMoving = _interactingPlayer.MoveInput.sqrMagnitude > 0.1f;
        bool isDashing = _interactingPlayer.CurrentState is PlayerDashState;
        float dist = Vector3.Distance(transform.position, _interactingPlayer.transform.position);
        bool isClose = dist < 3.0f;

        if (!isHoldingKey || isMoving || isDashing || !isClose)
        {
            ResetProcessing();
            return;
        }
        _processTimer += Time.deltaTime;
        UpdateUI();
        if (_processTimer >= _currentRecipe.processDuration) CompleteCooking();
    }

    private void CompleteCooking()
    {
        _isProcessing = false;
        _processTimer = 0f;
        if (progressBar) progressBar.Hide();
        _contents.Clear();
        ClearVisuals();
        _processedOutput = _currentRecipe.outputItem;
        AddVisual(_processedOutput);
        Debug.Log($"烹飪完成! 獲得 {_processedOutput.name}");
    }

    private void ResetProcessing()
    {
        _isProcessing = false;
        _processTimer = 0f;
        _currentRecipe = null;
        _interactingPlayer = null;
        if (progressBar) progressBar.Hide();
    }

    private void UpdateUI()
    {
        if (progressBar && _currentRecipe != null)
            progressBar.UpdateProgress(_processTimer, _currentRecipe.processDuration);
    }

    public bool CanInteract()
    {
        if (_processedOutput != null) return true;
        RecipeData readyRecipe = GetReadyRecipe();
        if (readyRecipe != null && !_isProcessing) return true;
        if (_isProcessing && _currentRecipe != null && _currentRecipe.processType == CookingProcessType.Manual) return true;
        return false;
    }

    public void OnInteract(PlayerStateMachine player, ItemData heldItemData)
    {
        if (heldItemData != null)
        {
            if (_contents.Count >= data.maxIngredientCount) return;
            bool isValidCombination = LevelManager.Instance.currentLevelConfig.IsValidIngredientForTool(heldItemData, _contents, this.data);
            if (isValidCombination)
            {
                AddIngredient(heldItemData);
                player.DestroyHeldItem();
            }
        }
        else
        {
            if (_processedOutput != null)
            {
                if (_processedOutput.itemPrefab != null)
                {
                    // [Fix 3] 生成成品時，強制歸零位置與旋轉，確保拿在手上是正確角度 (由 PickupItem 的 heldRotation 決定)
                    GameObject outputObj = Instantiate(_processedOutput.itemPrefab, Vector3.zero, Quaternion.identity);
                    _processedOutput = null;
                    ClearVisuals();
                    player.PickupItem(outputObj, _currentRecipe != null ? _currentRecipe.outputItem : null);
                }
                return;
            }

            if (_isProcessing && _currentRecipe.processType == CookingProcessType.Automatic)
            {
                _interactingPlayer = player;
                return;
            }

            RecipeData readyRecipe = GetReadyRecipe();
            if (readyRecipe != null && !_isProcessing)
            {
                StartProcessing(readyRecipe, player);
                return;
            }

            if (_isProcessing && _currentRecipe.processType == CookingProcessType.Manual)
            {
                _interactingPlayer = player;
            }
        }
    }

    private void StartProcessing(RecipeData recipe, PlayerStateMachine player)
    {
        _currentRecipe = recipe;
        _isProcessing = true;
        _processTimer = 0f;
        _interactingPlayer = player;
        Debug.Log($"開始烹飪 ({recipe.processType}): {recipe.name}");
    }

    public void OnPickup(PlayerStateMachine player)
    {
        if (!data.canBePickedUp)
        {
            if (_contents.Count > 0 && !player.IsHoldingItem)
            {
                ItemData lastItem = _contents[_contents.Count - 1];
                if (lastItem.itemPrefab != null)
                {
                    _contents.RemoveAt(_contents.Count - 1);
                    if (_spawnedVisuals.Count > 0)
                    {
                        GameObject lastVisual = _spawnedVisuals[_spawnedVisuals.Count - 1];
                        _spawnedVisuals.RemoveAt(_spawnedVisuals.Count - 1);
                        Destroy(lastVisual);
                    }
                    // [Fix 3] 拿出原料時也歸零
                    GameObject newItem = Instantiate(lastItem.itemPrefab, Vector3.zero, Quaternion.identity);
                    player.PickupItem(newItem, lastItem);
                }
            }
            return;
        }

        if (!player.IsHoldingItem && data.canBePickedUp)
        {
            if (_currentCountertop != null)
            {
                _currentCountertop.ClearPlacedObject();
                _currentCountertop = null;
            }
            CurrentStation = StationType.None;
            ResetProcessing();
            player.PickupItem(this.gameObject, this.data);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (catchTrigger != null && other != catchTrigger && !IsChildCollider(other, catchTrigger)) return;
        if (other.TryGetComponent<ThrownItem>(out ThrownItem thrownItem))
        {
            bool isValid = LevelManager.Instance.currentLevelConfig.IsValidIngredientForTool(thrownItem.data, _contents, this.data);
            if (isValid && _contents.Count < data.maxIngredientCount)
            {
                AddIngredient(thrownItem.data);
                Destroy(other.gameObject);
            }
        }
    }

    private RecipeData GetReadyRecipe()
    {
        foreach (var recipe in LevelManager.Instance.currentLevelConfig.availableRecipes)
        {
            bool toolMatch = (recipe.requiredTool == this.data);
            bool stationMatch = (recipe.requiredStation == StationType.None || recipe.requiredStation == this.CurrentStation);
            if (toolMatch && stationMatch)
            {
                if (LevelManager.Instance.currentLevelConfig.IsRecipeReadyToCook(recipe, _contents)) return recipe;
            }
        }
        return null;
    }

    private bool IsChildCollider(Collider col, Collider parentCol) { return col.transform.IsChildOf(parentCol.transform); }
    private void AddIngredient(ItemData ingredient) { _contents.Add(ingredient); AddVisual(ingredient); }
}