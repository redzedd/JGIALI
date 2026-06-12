public interface IPlayerState
{
    void Enter(PlayerStateMachine stateMachine);
    void Update();
    void FixedUpdate();
    void Exit();

    void OnInteract();
    void OnPickupThrowPressed();
    void OnPickupThrowReleased();
}