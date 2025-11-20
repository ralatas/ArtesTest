using System.Collections.Generic;

public interface IBombService
{
    /// <summary>
    /// Analyze a resolved match and enqueue bombs that should be triggered.
    /// Bombs are triggered if:
    /// - they are part of the match, or
    /// - they are adjacent (orthogonal) to any matched gem.
    /// </summary>
    void RegisterMatch(IReadOnlyList<SC_Gem> matchedGems, GameBoard board);

    /// <summary>
    /// Returns true if there are bombs waiting to explode.
    /// </summary>
    bool HasPendingBombs { get; }

    /// <summary>
    /// Returns and clears the current queue of pending bombs.
    /// </summary>
    IReadOnlyList<SC_Gem> ConsumePendingBombs();

    /// <summary>
    /// Returns all gems that should be destroyed by the given bomb.
    /// Follows the required pattern:
    /// - all 8 neighbors around the bomb;
    /// - plus cross positions at distance 2 (x±2, y and x, y±2).
    /// </summary>
    IEnumerable<SC_Gem> GetExplosionTargets(SC_Gem bomb, GameBoard board);
}
