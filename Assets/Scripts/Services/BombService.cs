using System.Collections.Generic;
using UnityEngine;

public class BombService : IBombService
{
    public IEnumerable<SC_Gem> GetExplosionTargets(SC_Gem bomb, GameBoard board)
    {
        if (bomb == null || board == null)
            yield break;

        // Rocket: clear full row/column based on orientation.
        if (bomb.isRocket && bomb.rocketDirection != GlobalEnums.RocketDirection.None)
        {
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
            yield break;
        }

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
