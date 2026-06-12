using UnityEngine;

public class PlayerDashState : IPlayerState
{
    private PlayerStateMachine _machine;
    private Vector3 _dashDirection;
    private float _dashTimer;
    private bool _isCommittedToMinDash;

    // [Optimize] �Ĩ릱�u�A���t���ܤƧ�۵M (�i��)
    // �p�G�S���]�w�A�N�νu�ʻ���
    private float _currentSpeed;

    public void Enter(PlayerStateMachine stateMachine)
    {
        _machine = stateMachine;
        _machine.Animator.applyRootMotion = false; // Dash �q�`�ѵ{�����ަ첾����n����Z��

        _machine.UpdateDashTime();

        // ��w�Ĩ��V�����e���ʿ�J��V�A�p�G�S��J�h�V�e
        if (_machine.MoveInput.sqrMagnitude > 0.1f)
        {
            Vector3 inputDir = new Vector3(_machine.MoveInput.x, 0f, _machine.MoveInput.y).normalized;
            _dashDirection = inputDir;
            // �ߧY��V�Ĩ��V
            _machine.transform.rotation = Quaternion.LookRotation(_dashDirection);
        }
        else
        {
            _dashDirection = _machine.transform.forward;
        }

        _dashTimer = 0f;
        _isCommittedToMinDash = false;

        _machine.Animancer.Play(_machine.DashAnimation);

        // [Optimize] �����@�������z�o�t��
        _currentSpeed = _machine.DashSpeed;
    }

    public void Update()
    {
        _dashTimer += Time.deltaTime;

        if (_dashTimer >= _machine.MaxDashTime)
        {
            TransitionToNextState();
            return;
        }

        if (!_machine.IsDashPressed)
        {
            if (_dashTimer < _machine.MinDashTime)
                _isCommittedToMinDash = true;
            else
            {
                TransitionToNextState();
                return;
            }
        }

        if (_isCommittedToMinDash && _dashTimer >= _machine.MinDashTime)
        {
            TransitionToNextState();
            return;
        }
    }

    public void FixedUpdate()
    {
        // [Optimize] �ϥ� Velocity �Ӥ��O MovePosition
        // �o�������z������۵M�a�B�z�I���A���|�� "�d��" ���Pı
        // [Fix] 保留垂直速度(重力),避免 dash 衝出平台邊緣時水平「飄」出去而不下墜
        Vector3 dashVelocity = _dashDirection * _currentSpeed;
        dashVelocity.y = _machine.Rb.linearVelocity.y;
        _machine.Rb.linearVelocity = dashVelocity;
    }

    public void Exit()
    {
        // [Optimize] ���n�����k�s�t�סA�O�d�@�I�D�����L��� Move/Idle ��ƶ�
        // �p�G�����k�s�A��P�|ı�o "�y" �@�U
        // _machine.Rb.linearVelocity = Vector3.zero; 

        // ���ӥN���A�ڭ����t�צ۵M�I��浹�U�@�Ӫ��A�B�z
        // �Ϊ̫O�d�����t��
        // [Fix] 同樣保留重力,避免結束 dash 的那一影格把垂直速度歸零
        Vector3 exitVelocity = _dashDirection * _machine.MoveSpeed;
        exitVelocity.y = _machine.Rb.linearVelocity.y;
        _machine.Rb.linearVelocity = exitVelocity;

        _machine.Rb.angularVelocity = Vector3.zero;
    }

    private void TransitionToNextState()
    {
        if (_machine.MoveInput.sqrMagnitude > 0.01f)
            _machine.ChangeState(_machine.MoveState);
        else
            _machine.ChangeState(_machine.IdleState);
    }

    public void OnInteract() { }
    public void OnPickupThrowPressed() { }
    public void OnPickupThrowReleased() { }
}