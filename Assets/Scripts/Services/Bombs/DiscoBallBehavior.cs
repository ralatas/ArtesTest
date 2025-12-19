using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Disco ball clears all gems of a chosen color.
/// - If swiped with another gem, clears that gem's color.
/// - If tapped, clears the most common color on the board.
/// </summary>
public class DiscoBallBehavior : IBombBehavior
{
    public void MakeBomb(SC_Gem gem)
    {
        if (gem == null)
            return;

        SC_Gem template = SC_GameVariables.Instance.discoBall != null
            ? SC_GameVariables.Instance.discoBall
            : SC_GameVariables.Instance.bomb;

        var templateSR = template != null ? template.GetComponent<SpriteRenderer>() : null;
        var gemSR = gem.GetComponent<SpriteRenderer>();

        gem.type = GlobalEnums.GemType.bomb;
        gem.baseType = GlobalEnums.GemType.bomb;
        gem.bombType = GlobalEnums.BombType.DiscoBall;
        gem.rocketDirection = GlobalEnums.RocketDirection.None;
        gem.discoHasTarget = false;
        gem.discoTargetType = GlobalEnums.GemType.bomb;

        if (template != null)
        {
            gem.blastSize = template.blastSize;
            gem.destroyEffect = template.destroyEffect;
            gem.scoreValue = template.scoreValue;
        }

        if (gemSR != null && templateSR != null)
        {
            gemSR.sprite = templateSR.sprite;
            gemSR.color = templateSR.color;
        }

        gem.isMatch = false;
    }

    public bool CanHandle(SC_Gem bomb)
    {
        return bomb != null && bomb.bombType == GlobalEnums.BombType.DiscoBall;
    }

    public IEnumerable<SC_Gem> GetExplosionTargets(SC_Gem bomb, GameBoard board)
    {
        if (bomb == null || board == null)
            yield break;

        GlobalEnums.GemType targetType = DetermineTargetType(bomb, board);
        if (targetType == GlobalEnums.GemType.bomb)
            yield break;

        int width = board.Width;
        int height = board.Height;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                SC_Gem g = board.GetGem(x, y);
                if (g == null || g == bomb)
                    continue;

                if (g.IsBomb)
                    continue;

                if (g.baseType == targetType)
                    yield return g;
            }
        }
    }

    private GlobalEnums.GemType DetermineTargetType(SC_Gem bomb, GameBoard board)
    {
        if (bomb.discoHasTarget)
            return bomb.discoTargetType;

        // Pick most common non-bomb gem type on board.
        Dictionary<GlobalEnums.GemType, int> counts = new Dictionary<GlobalEnums.GemType, int>();

        for (int x = 0; x < board.Width; x++)
        {
            for (int y = 0; y < board.Height; y++)
            {
                SC_Gem g = board.GetGem(x, y);
                if (g == null || g.IsBomb)
                    continue;

                GlobalEnums.GemType t = g.baseType;
                if (!counts.ContainsKey(t))
                    counts[t] = 0;
                counts[t]++;
            }
        }

        GlobalEnums.GemType bestType = GlobalEnums.GemType.bomb;
        int bestCount = 0;
        foreach (var kvp in counts)
        {
            if (kvp.Value > bestCount)
            {
                bestCount = kvp.Value;
                bestType = kvp.Key;
            }
        }

        bomb.discoHasTarget = bestType != GlobalEnums.GemType.bomb;
        bomb.discoTargetType = bestType;
        return bestType;
    }
}
