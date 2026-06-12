using UnityEngine;

public class PlayerMoveState : IPlayerState
{
    private PlayerStateMachine _machine;

    public void Enter(PlayerStateMachine stateMachine)
    {
        _machine = stateMachine;

        // [重點] 關閉 Root Motion，完全由程式碼控制位移
        // 這樣即使動畫本身有位移，也不會影響 Transform
        _machine.Animator.applyRootMotion = false;

        // 播放跑步動畫 (需確保動畫本身是 Loop 的)
        _machine.Animancer.Play(_machine.MoveAnimation);
    }

    public void Update()
    {
        bool canDash = true;
        if (_machine.IsHoldingItem)
        {
            ItemData data = _machine.GetHeldItemData();
            if (data != null) canDash = data.canDashWhileHolding;
        }

        if (_machine.IsDashPressed && _machine.ReadyToDash && canDash && _machine.CanDash())
        {
            _machine.SetReadyToDash(false);
            _machine.ChangeState(_machine.DashState);
            return;
        }
    }

    public void FixedUpdate()
    {
        Vector3 moveInputVector = new Vector3(_machine.MoveInput.x, 0f, _machine.MoveInput.y);

        // 如果沒有輸入，切換回 Idle
        if (moveInputVector.sqrMagnitude <= 0.01f)
        {
            _machine.ChangeState(_machine.IdleState);
            return;
        }

        Vector3 moveDirection = moveInputVector.normalized;

        // 1. 處理旋轉 (保持不變)
        Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
        _machine.transform.rotation = Quaternion.Slerp(
            _machine.transform.rotation,
            targetRotation,
            _machine.RotationSpeed * Time.fixedDeltaTime
        );

        // 2. 處理移動 (改用 Velocity)
        // 計算目標速度
        Vector3 targetVelocity = moveDirection * _machine.MoveSpeed;

        // [關鍵] 保留原本的 Y 軸速度 (重力)
        // 如果直接設為 new Vector3(x, 0, z)，角色在空中會浮著掉不下來
        targetVelocity.y = _machine.Rb.linearVelocity.y;

        // 應用速度
        _machine.Rb.linearVelocity = targetVelocity;
    }

    public void Exit()
    {
        // 離開移動狀態時，將水平速度歸零 (保留垂直重力)
        // 這樣可以讓停止感更乾脆，不會滑行
        Vector3 currentVel = _machine.Rb.linearVelocity;
        _machine.Rb.linearVelocity = new Vector3(0, currentVel.y, 0);
    }

    public void OnInteract()
    {
        _machine.DispatchInteract();
    }

    public void OnPickupThrowPressed()
    {
        _machine.DispatchPickupThrowPressed();
    }

    public void OnPickupThrowReleased()
    {
    }
}