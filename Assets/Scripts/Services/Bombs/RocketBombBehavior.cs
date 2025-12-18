using System.Collections.Generic;

/// <summary>
/// Clears an entire row or column depending on rocket orientation.
/// </summary>
public class RocketBombBehavior : IBombBehavior
{
    public bool CanHandle(SC_Gem bomb)
    {
        return bomb != null && bomb.isRocket && bomb.rocketDirection != GlobalEnums.RocketDirection.None;
    }

    public IEnumerable<SC_Gem> GetExplosionTargets(SC_Gem bomb, GameBoard board)
    {
        if (bomb == null || board == null)
            yield break;

        if (bomb.rocketDirection == GlobalEnums.RocketDirection.Vertical)
        {
            int x = bomb.posIndex.x;
            for (int y = 0; y < board.Height; y++)
            {
                SC_Gem g = board.GetGem(x, y);
                if (g != null && g != bomb)
                    yield return g;
            }
        }
        else if (bomb.rocketDirection == GlobalEnums.RocketDirection.Horizontal)
        {
            int y = bomb.posIndex.y;
            for (int x = 0; x < board.Width; x++)
            {
                SC_Gem g = board.GetGem(x, y);
                if (g != null && g != bomb)
                    yield return g;
            }
        }
    }
}
