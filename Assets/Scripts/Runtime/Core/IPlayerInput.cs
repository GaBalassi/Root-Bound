namespace Rootbound.Core
{
    /// <summary>
    /// Astrazione degli input di gioco per supportare tastiera/gamepad e test automatici.
    /// </summary>
    public interface IPlayerInput
    {
        float MoveX { get; }
        float MoveY { get; }
        bool JumpPressed { get; }
        bool JumpHeld { get; }
        bool DashPressed { get; }
        bool AttackPressed { get; }
        bool AttackHeld { get; }
        bool AttackReleased { get; }
        bool RootPressed { get; }
        bool InteractPressed { get; }
        bool RestartPressed { get; }
        bool VillagePressed { get; }
        bool PausePressed { get; }
    }
}
