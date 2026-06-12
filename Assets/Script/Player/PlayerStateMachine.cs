using UnityEngine;
using UnityEngine.InputSystem;
using Animancer;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(AnimancerComponent))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerStateMachine : MonoBehaviour
{
    public IPlayerState CurrentState { get; private set; }
    public PlayerIdleState IdleState { get; private set; }
    public PlayerMoveState MoveState { get; private set; }
    public PlayerDashState DashState { get; private set; }
    public PlayerThrowState ThrowState { get; private set; }

    public Rigidbody Rb { get; private set; }
    public Animator Animator { get; private set; }
    public AnimancerComponent Animancer { get; private set; }

    [Header("Team Settings")]
    public int TeamID = 0;

    [Header("Visuals")]
    [Tooltip("����ҫ��������I")]
    public Transform modelContainer;

    [Header("Movement")]
    public float MoveSpeed = 5f;
    [Header("Rotation")]
    public float RotationSpeed = 10f;

    [Header("Dash")]
    public float DashSpeed = 15f;
    public float MinDashTime = 0.2f;
    public float MaxDashTime = 0.5f;
    public float DashCooldown = 1.0f;
    private float _lastDashTime = -10f;

    [Header("Throw")]
    public float MaxThrowChargeTime = 1.5f;
    public float MinThrowForce = 5f;
    public float MaxThrowForce = 20f;
    public float ThrowAngle = -30f;
    public float ThrowSlowMoveSpeed = 2f;
    public float ThrowSlowRotationSpeed = 5f;
    public float ThrowCooldown = 0.5f;
    private float _lastThrowTime = -10f;

    [Header("Respawn")]
    [Tooltip("低於此 Y 高度判定為掉出地圖,觸發重生。需設在地板高度以下、下方任何地形以上")]
    public float fallThresholdY = 5f;
    [HideInInspector] public Vector3 RespawnPoint;

    [Header("Animation (Animancer)")]
    public ClipTransition IdleAnimation;
    public ClipTransition MoveAnimation;
    public ClipTransition DashAnimation;

    [Header("Interaction")]
    public Transform HandHoldPoint;
    public PlayerInteractor interactor;

    public GameObject HeldItem { get; private set; }
    public bool IsHoldingItem { get; private set; } = false;
    private ItemData _heldItemData;

    public Vector2 MoveInput { get; private set; }
    public bool ReadyToDash { get; private set; } = true;
    public bool IsDashPressed { get; private set; } = false;
    public bool IsInteractPressed { get; private set; } = false;

    private PlayerInput _playerInput;
    private InputAction _moveAction;
    private InputAction _dashAction;
    private InputAction _interactAction;
    private InputAction _pickupThrowAction;

    private void Awake()
    {
        Rb = GetComponent<Rigidbody>();
        Animator = GetComponent<Animator>();
        Animancer = GetComponent<AnimancerComponent>();
        _playerInput = GetComponent<PlayerInput>();

        IdleState = new PlayerIdleState();
        MoveState = new PlayerMoveState();
        DashState = new PlayerDashState();
        ThrowState = new PlayerThrowState();

        Rb.mass = 50f;
        Rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezeRotationY;
        Rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        // [Fix] 預設重生點為起始位置,避免 spawn 指派未跑時重生到原點 (0,0,0)
        RespawnPoint = transform.position;

        if (_playerInput != null)
        {
            _moveAction = _playerInput.actions["Move"];
            _dashAction = _playerInput.actions["Dash"];
            _interactAction = _playerInput.actions["Interact"];
            _pickupThrowAction = _playerInput.actions["PickupThrow"];
        }
    }

    private void OnEnable()
    {
        if (_moveAction != null) { _moveAction.performed += OnMoveAction; _moveAction.canceled += OnMoveAction; }
        if (_dashAction != null) { _dashAction.performed += OnDashAction; _dashAction.canceled += OnDashAction; }
        if (_interactAction != null) { _interactAction.performed += OnInteractAction; _interactAction.canceled += OnInteractAction; }
        if (_pickupThrowAction != null) { _pickupThrowAction.performed += OnPickupThrowAction; _pickupThrowAction.canceled += OnPickupThrowAction; }
    }

    private void OnDisable()
    {
        if (_moveAction != null) { _moveAction.performed -= OnMoveAction; _moveAction.canceled -= OnMoveAction; }
        if (_dashAction != null) { _dashAction.performed -= OnDashAction; _dashAction.canceled -= OnDashAction; }
        if (_interactAction != null) { _interactAction.performed -= OnInteractAction; _interactAction.canceled -= OnInteractAction; }
        if (_pickupThrowAction != null) { _pickupThrowAction.performed -= OnPickupThrowAction; _pickupThrowAction.canceled -= OnPickupThrowAction; }
    }

    private void Update()
    {
        // [Fix 1] �C���Ȱ��޿�
        if (GameFlowManager.Instance != null && !GameFlowManager.Instance.IsGameRunning)
        {
            // �j��^�� Idle ���A
            if (CurrentState != IdleState) ChangeState(IdleState);
            return;
        }
        CurrentState?.Update();
    }

    private void FixedUpdate()
    {
        // [Fix 1] �C���Ȱ�����w���z
        if (GameFlowManager.Instance != null && !GameFlowManager.Instance.IsGameRunning)
        {
            Rb.linearVelocity = Vector3.zero;
            Rb.angularVelocity = Vector3.zero;
            return;
        }
        // [Fix] 掉落重生:玩家掉出薄地板/掉進缺口時拉回出生點,避免永久墜落出地圖
        if (transform.position.y < fallThresholdY)
        {
            Respawn();
            return;
        }

        CurrentState?.FixedUpdate();
    }

    private void Start() { ChangeState(IdleState); }
    public void ChangeState(IPlayerState newState) { CurrentState?.Exit(); CurrentState = newState; CurrentState.Enter(this); }

    // [Fix] 把玩家瞬移回出生點並清空速度
    public void Respawn()
    {
        Rb.position = RespawnPoint;
        Rb.rotation = Quaternion.identity;
        Rb.linearVelocity = Vector3.zero;
        Rb.angularVelocity = Vector3.zero;
        transform.position = RespawnPoint;
        ChangeState(IdleState);
    }

    // --- ��J�B�z ---
    private void OnMoveAction(InputAction.CallbackContext ctx)
    {
        MoveInput = ctx.ReadValue<Vector2>();
    }

    private void OnDashAction(InputAction.CallbackContext ctx)
    {
        IsDashPressed = ctx.ReadValueAsButton();
        if (ctx.canceled) ReadyToDash = true;
    }

    private void OnInteractAction(InputAction.CallbackContext ctx)
    {
        IsInteractPressed = ctx.ReadValueAsButton();
        if (ctx.performed)
        {
            // [Fix 1] �Ȱ��ɸT���
            if (GameFlowManager.Instance != null && !GameFlowManager.Instance.IsGameRunning) return;
            CurrentState?.OnInteract();
        }
    }

    private void OnPickupThrowAction(InputAction.CallbackContext ctx)
    {
        // [Fix 1] �Ȱ��ɸT����
        if (GameFlowManager.Instance != null && !GameFlowManager.Instance.IsGameRunning) return;

        if (ctx.performed) CurrentState?.OnPickupThrowPressed();
        else if (ctx.canceled) CurrentState?.OnPickupThrowReleased();
    }

    // --- ���U��k ---
    public bool CanDash() => Time.time >= _lastDashTime + DashCooldown;
    public void UpdateDashTime() => _lastDashTime = Time.time;
    public bool CanThrow() => Time.time >= _lastThrowTime + ThrowCooldown;

    public void DispatchInteract()
    {
        IInteractable target = interactor.GetClosestInteractable();
        target?.OnInteract(this, _heldItemData);
    }

    public void DispatchPickupThrowPressed()
    {
        if (IsHoldingItem) { if (CanThrow()) ChangeState(ThrowState); }
        else { IInteractable target = interactor.GetClosestInteractable(); target?.OnPickup(this); }
    }

    public void PickupItem(GameObject itemObj, ItemData data)
    {
        if (IsHoldingItem) return;

        HeldItem = itemObj;
        _heldItemData = data;
        IsHoldingItem = true;

        if (HeldItem.TryGetComponent<ThrownItem>(out ThrownItem thrownItemComponent)) Destroy(thrownItemComponent);

        HeldItem.transform.SetParent(HandHoldPoint);
        HeldItem.transform.localPosition = Vector3.zero;

        // �����޿�GFoodGroup �j���k�s�A���q���~�̳]�w����
        FoodGroup group = HeldItem.GetComponent<FoodGroup>();
        if (group != null)
        {
            if (group.groupItemData != null && group.groupItemData.heldRotation != Vector3.zero)
                HeldItem.transform.localRotation = Quaternion.Euler(group.groupItemData.heldRotation);
            else
                HeldItem.transform.localRotation = Quaternion.identity;
        }
        else
        {
            if (data != null) HeldItem.transform.localRotation = Quaternion.Euler(data.heldRotation);
            else HeldItem.transform.localRotation = Quaternion.identity;
        }

        foreach (var rb in HeldItem.GetComponentsInChildren<Rigidbody>())
        {
            rb.isKinematic = true;
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
        }
        foreach (var col in HeldItem.GetComponentsInChildren<Collider>()) col.enabled = false;
    }

    public void DestroyHeldItem()
    {
        if (!IsHoldingItem) return;
        Destroy(HeldItem);
        HeldItem = null;
        _heldItemData = null;
        IsHoldingItem = false;
    }

    public void ForceDropItem()
    {
        HeldItem = null;
        _heldItemData = null;
        IsHoldingItem = false;
    }

    public void ThrowHeldItem(float chargeTime)
    {
        if (!IsHoldingItem) return;

        _lastThrowTime = Time.time;
        GameObject itemToThrow = HeldItem;
        ItemData itemData = _heldItemData;

        HeldItem = null;
        _heldItemData = null;
        IsHoldingItem = false;

        itemToThrow.transform.SetParent(null);

        Vector3 throwOrigin = transform.position + Vector3.up * 1.0f + transform.forward * 0.8f;
        itemToThrow.transform.position = throwOrigin;

        FoodGroup group = itemToThrow.GetComponent<FoodGroup>();
        if (group != null)
        {
            if (group.groupItemData != null && group.groupItemData.placedRotation != Vector3.zero)
                itemToThrow.transform.rotation = Quaternion.Euler(group.groupItemData.placedRotation);
            else
                itemToThrow.transform.rotation = Quaternion.identity;
        }
        else
        {
            if (itemData != null) itemToThrow.transform.rotation = Quaternion.Euler(itemData.placedRotation);
            else itemToThrow.transform.rotation = Quaternion.identity;
        }

        ThrownItem thrownItemMarker = itemToThrow.AddComponent<ThrownItem>();
        thrownItemMarker.data = itemData;

        Collider[] allColliders = itemToThrow.GetComponentsInChildren<Collider>();
        Collider[] playerColliders = GetComponentsInChildren<Collider>();

        foreach (var pCol in playerColliders)
        {
            foreach (var iCol in allColliders) Physics.IgnoreCollision(pCol, iCol, true);
        }
        StartCoroutine(ResetCollision(playerColliders, allColliders, 0.5f));

        foreach (var col in allColliders) col.enabled = true;

        if (itemToThrow.TryGetComponent<Rigidbody>(out Rigidbody itemRb))
        {
            itemRb.isKinematic = false;
            itemRb.mass = 1f;
            itemRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            foreach (var rb in itemToThrow.GetComponentsInChildren<Rigidbody>())
            {
                rb.isKinematic = false;
                rb.mass = 1f;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            }

            float chargePercent = Mathf.Clamp01(chargeTime / MaxThrowChargeTime);
            float force = Mathf.Lerp(MinThrowForce, MaxThrowForce, chargePercent);

            Vector3 flatForward = transform.forward;
            flatForward.y = 0;
            flatForward.Normalize();

            Quaternion throwRotation = Quaternion.LookRotation(flatForward) * Quaternion.Euler(ThrowAngle, 0, 0);
            Vector3 throwDirection = throwRotation * Vector3.forward;

            itemToThrow.transform.position += flatForward * 0.2f;

            itemRb.AddForce(throwDirection.normalized * force, ForceMode.Impulse);
        }
    }

    private IEnumerator ResetCollision(Collider[] pCols, Collider[] iCols, float delay)
    {
        yield return new WaitForSeconds(delay);
        foreach (var pCol in pCols)
        {
            if (pCol == null) continue;
            foreach (var iCol in iCols)
            {
                if (iCol != null) Physics.IgnoreCollision(pCol, iCol, false);
            }
        }
    }

    public ItemData GetHeldItemData() { return _heldItemData; }
    public void SetReadyToDash(bool isReady) { ReadyToDash = isReady; }
}