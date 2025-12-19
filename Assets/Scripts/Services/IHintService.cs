using UnityEngine;

public interface IHintService
{
    /// <summary>
    /// Resets inactivity timer (call on any player action).
    /// </summary>
    void RegisterActivity();

    /// <summary>
    /// Per-frame tick to drive hint timing.
    /// </summary>
    void Tick(float deltaTime, GlobalEnums.GameState state);

    /// <summary>
    /// Clears any active hint visuals (e.g., when the board changes).
    /// </summary>
    void ClearHint();
}
