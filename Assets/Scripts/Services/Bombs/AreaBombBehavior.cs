using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Default bomb that clears neighbors plus a small cross.
/// </summary>
public class AreaBombBehavior : IBombBehavior
{
    public void MakeBomb(SC_Gem gem)
    {
        if (gem == null)
            return;

        SC_Gem bombTemplate = SC_GameVariables.Instance.bomb;

        gem.type = GlobalEnums.GemType.bomb;
        gem.baseType = GlobalEnums.GemType.bomb;
        gem.bombType = GlobalEnums.BombType.Area;
        gem.rocketDirection = GlobalEnums.RocketDirection.None;
        gem.blastSize = bombTemplate.blastSize;
        gem.destroyEffect = bombTemplate.destroyEffect;
        gem.scoreValue = bombTemplate.scoreValue;

        var bombSR = bombTemplate.GetComponent<SpriteRenderer>();
        var gemSR = gem.GetComponent<SpriteRenderer>();
        if (gemSR == null || bombSR == null)
        {
            // Fallback: just mark as bomb without visual if something is missing
            gem.isMatch = false;
            return;
        }

        // Set bomb base sprite to detach from source gem type.
        gemSR.sprite = bombSR.sprite;
        gemSR.color = bombSR.color;

        gem.isMatch = false;
    }

    public bool CanHandle(SC_Gem bomb)
    {
        // Fallback handler for any non-rocket bomb type.
        return bomb != null && bomb.bombType == GlobalEnums.BombType.Area;
    }

    public IEnumerable<SC_Gem> GetExplosionTargets(SC_Gem bomb, GameBoard board)
    {
        if (bomb == null || board == null)
            yield break;

        Vector2Int center = bomb.posIndex;

        // 1) 8 neighbors around the bomb
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0)
                    continue;

                int nx = center.x + dx;
                int ny = center.y + dy;

                if (nx < 0 || nx >= board.Width || ny < 0 || ny >= board.Height)
                    continue;

                SC_Gem g = board.GetGem(nx, ny);
                if (g != null)
                    yield return g;
            }
        }

        // 2) Cross at distance 2 (x±2, y) and (x, y±2)
        int[,] offsets2 =
        {
            { -2,  0 },
            {  2,  0 },
            {  0, -2 },
            {  0,  2 }
        };

        for (int i = 0; i < offsets2.GetLength(0); i++)
        {
            int nx = center.x + offsets2[i, 0];
            int ny = center.y + offsets2[i, 1];

            if (nx < 0 || nx >= board.Width || ny < 0 || ny >= board.Height)
                continue;

            SC_Gem g = board.GetGem(nx, ny);
            if (g != null)
                yield return g;
        }
    }
}
