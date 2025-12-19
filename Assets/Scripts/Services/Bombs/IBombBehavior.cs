using System.Collections.Generic;

/// <summary>
/// Strategy for defining explosion targets for a specific bomb type.
/// </summary>
public interface IBombBehavior
{
    /// <summary>
    /// Whether this behavior can handle the provided bomb instance.
    /// </summary>
    bool CanHandle(SC_Gem bomb);

    /// <summary>
    /// Returns all gems affected by the bomb.
    /// </summary>
    IEnumerable<SC_Gem> GetExplosionTargets(SC_Gem bomb, GameBoard board);

}
