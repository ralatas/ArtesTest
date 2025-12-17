using System.Collections.Generic;

public interface IBombService
{
    /// <summary>
    /// Returns all gems affected by the given bomb.
    /// Pattern:
    /// - all 8 neighbors around the bomb;
    /// - plus cross positions at distance 2 (x±2, y and x, y±2).
    /// </summary>
    IEnumerable<SC_Gem> GetExplosionTargets(SC_Gem bomb, GameBoard board);
}
