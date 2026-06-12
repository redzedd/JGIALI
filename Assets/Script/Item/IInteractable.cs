public interface IInteractable
{
    void OnInteract(PlayerStateMachine player, ItemData heldItemData);
    void OnPickup(PlayerStateMachine player);
}