using System.Collections.Generic;

public interface IBombService
{
    /// <summary>
    /// Returns all gems affected by the given bomb, using the matching bomb behavior.
    /// </summary>
    IEnumerable<SC_Gem> GetExplosionTargets(SC_Gem bomb, GameBoard board);
}
