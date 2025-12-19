/// <summary>
/// Strategy for configuring a gem into a specific bomb variant.
/// </summary>
public interface IBombCreationBehavior
{
    /// <summary>
    /// Applies all visuals and stats to turn the gem into the bomb handled by this behavior.
    /// </summary>
    void MakeBomb(SC_Gem gem, GlobalEnums.RocketDirection direction);
}
