using UnityEngine;

public class PlayerIdleState : IPlayerState
{
    private PlayerStateMachine _machine;

    public void Enter(PlayerStateMachine stateMachine)
    {
        _machine = stateMachine;

        // 確保 Root Motion 關閉
        _machine.Animator.applyRootMotion = false;

        // 播放待機動畫
        _machine.Animancer.Play(_machine.IdleAnimation);

        // 強制停止水平移動 (保留重力)
        Vector3 currentVel = _machine.Rb.linearVelocity;
        _machine.Rb.linearVelocity = new Vector3(0, currentVel.y, 0);
        _machine.Rb.angularVelocity = Vector3.zero;
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
        // 偵測移動輸入
        if (_machine.MoveInput.sqrMagnitude > 0.01f)
        {
            _machine.ChangeState(_machine.MoveState);
            return;
        }

        // 持續確保不會滑行 (物理引擎有時候會有殘餘動量)
        Vector3 currentVel = _machine.Rb.linearVelocity;
        if (Mathf.Abs(currentVel.x) > 0.01f || Mathf.Abs(currentVel.z) > 0.01f)
        {
            _machine.Rb.linearVelocity = new Vector3(0, currentVel.y, 0);
        }
    }

    public void Exit() { }

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