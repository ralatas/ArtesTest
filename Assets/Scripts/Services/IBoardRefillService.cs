using System.Collections;

/// <summary>
/// Handles cascades (falling gems) and board refill (spawning new gems).
/// Extracted from SC_GameLogic to keep game logic cleaner and closer to SRP.
/// </summary>
public interface IBoardRefillService
{
    /// <summary>
    /// Runs full cascade + refill sequence once.
    /// </summary>
    IEnumerator CascadeAndRefillCo();
}