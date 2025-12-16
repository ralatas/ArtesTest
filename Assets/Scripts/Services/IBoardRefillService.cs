using System.Collections;

/// <summary>
/// Сервис отвечает за каскад (падение гемов) и рефилл (спавн новых гемов).
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