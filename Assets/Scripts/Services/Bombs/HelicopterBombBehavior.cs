using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Helicopter bomb: spawns from a square match and blasts orthogonal neighbors.
/// </summary>
public class HelicopterBombBehavior : IBombBehavior
{
    public void MakeBomb(SC_Gem gem)
    {
        if (gem == null)
            return;

        SC_Gem template = SC_GameVariables.Instance.helicopter != null
            ? SC_GameVariables.Instance.helicopter
            : SC_GameVariables.Instance.bomb;

        var templateSR = template != null ? template.GetComponent<SpriteRenderer>() : null;
        var gemSR = gem.GetComponent<SpriteRenderer>();

        gem.type = GlobalEnums.GemType.bomb;
        gem.baseType = GlobalEnums.GemType.bomb;
        gem.bombType = GlobalEnums.BombType.Helicopter;
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
        return bomb != null && bomb.bombType == GlobalEnums.BombType.Helicopter;
    }

    public IEnumerable<SC_Gem> GetExplosionTargets(SC_Gem bomb, GameBoard board)
    {
        if (bomb == null || board == null)
            yield break;

        int x = bomb.posIndex.x;
        int y = bomb.posIndex.y;

        Vector2Int[] offsets =
        {
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1),
        };

        foreach (var offset in offsets)
        {
            int nx = x + offset.x;
            int ny = y + offset.y;

            if (nx < 0 || ny < 0 || nx >= board.Width || ny >= board.Height)
                continue;

            SC_Gem target = board.GetGem(nx, ny);
            if (target != null)
                yield return target;
        }
    }
}
