using UnityEngine;

public class PlayerThrowState : IPlayerState
{
    private PlayerStateMachine _machine;
    private float _chargeTimer;

    // [Optimize] 移除自定義移動變數，改用 Root Motion 邏輯

    public void Enter(PlayerStateMachine stateMachine)
    {
        _machine = stateMachine;
        _chargeTimer = 0f;
        _machine.Rb.angularVelocity = Vector3.zero;

        // [Optimize] 啟用 Root Motion，讓角色可以像 MoveState 一樣移動
        _machine.Animator.applyRootMotion = true;

        // 這裡假設你有一個 "ThrowCharge" 的動畫
        // 如果這個動畫只有上半身，或者它是混合樹，效果會最好
        // _machine.Animancer.Play(_machine.ThrowChargeAnimation);

        // 如果你的 ThrowCharge 動畫會鎖死下半身，那你可能需要在這裡
        // 使用 Animancer 的 Layer 來混合動畫 (Base: Move, Upper: Throw)
        // 簡單解法：如果 ThrowCharge 是全身動畫且站定不動，那就把 applyRootMotion 設為 false，
        // 然後手動寫移動邏輯 (如下方的 FixedUpdate 修正版)
    }

    public void Update()
    {
        _chargeTimer += Time.deltaTime;
        if (_chargeTimer > _machine.MaxThrowChargeTime)
        {
            _chargeTimer = _machine.MaxThrowChargeTime;
        }
    }

    public void FixedUpdate()
    {
        // [Optimize] 讓 ThrowState 擁有與 MoveState 相同的移動手感
        // 即使我們在集氣，也允許玩家慢速移動調整位置

        Vector3 moveInputVector = new Vector3(_machine.MoveInput.x, 0f, _machine.MoveInput.y);

        if (moveInputVector.sqrMagnitude > 0.01f)
        {
            Vector3 moveDirection = moveInputVector.normalized;

            // 旋轉
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            _machine.transform.rotation = Quaternion.Slerp(
                _machine.transform.rotation,
                targetRotation,
                _machine.ThrowSlowRotationSpeed * Time.fixedDeltaTime
            );

            // 位移：如果你開啟了 Root Motion 且動畫有位移，這裡可以註解掉
            // 如果動畫是站立的，則手動位移 (通常集氣時移動會變慢)
            // 這裡使用 Velocity 確保物理流暢
            Vector3 targetVelocity = moveDirection * _machine.ThrowSlowMoveSpeed;

            // 混合原本的 Y 軸速度 (重力)
            targetVelocity.y = _machine.Rb.linearVelocity.y;

            _machine.Rb.linearVelocity = targetVelocity;
        }
        else
        {
            // 沒輸入時停止水平移動
            _machine.Rb.linearVelocity = new Vector3(0, _machine.Rb.linearVelocity.y, 0);
        }
    }

    public void Exit()
    {
        // 離開時不歸零速度，讓慣性延續
    }

    public void OnInteract() { }
    public void OnPickupThrowPressed() { }

    public void OnPickupThrowReleased()
    {
        _machine.ThrowHeldItem(_chargeTimer);

        if (_machine.MoveInput.sqrMagnitude > 0.01f)
        {
            _machine.ChangeState(_machine.MoveState);
        }
        else
        {
            _machine.ChangeState(_machine.IdleState);
        }
    }
}