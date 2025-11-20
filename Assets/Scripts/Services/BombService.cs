using System.Collections.Generic;
using UnityEngine;

public class BombService : IBombService
{
    private readonly Queue<SC_Gem> pendingBombs = new Queue<SC_Gem>();

    public bool HasPendingBombs => pendingBombs.Count > 0;

    public void RegisterMatch(IReadOnlyList<SC_Gem> matchedGems, GameBoard board)
    {
        if (matchedGems == null || board == null)
            return;

        var bombsToQueue = new HashSet<SC_Gem>();

        foreach (var gem in matchedGems)
        {
            if (gem == null)
                continue;

            // 1) Bombs that are part of the match itself
            if (gem.isBomb)
                bombsToQueue.Add(gem);

            // 2) Bombs adjacent (orthogonal) to matched gems
            Vector2Int pos = gem.posIndex;
            TryAddAdjacentBomb(pos.x - 1, pos.y, board, bombsToQueue);
            TryAddAdjacentBomb(pos.x + 1, pos.y, board, bombsToQueue);
            TryAddAdjacentBomb(pos.x, pos.y - 1, board, bombsToQueue);
            TryAddAdjacentBomb(pos.x, pos.y + 1, board, bombsToQueue);
        }

        foreach (var bomb in bombsToQueue)
        {
            if (bomb == null)
                continue;

            // avoid duplicates inside the queue
            if (!pendingBombs.Contains(bomb))
                pendingBombs.Enqueue(bomb);
        }
    }

    public IReadOnlyList<SC_Gem> ConsumePendingBombs()
    {
        var result = new List<SC_Gem>();

        while (pendingBombs.Count > 0)
        {
            var bomb = pendingBombs.Dequeue();
            if (bomb != null)
                result.Add(bomb);
        }

        return result;
    }

    public IEnumerable<SC_Gem> GetExplosionTargets(SC_Gem bomb, GameBoard board)
    {
        var result = new HashSet<SC_Gem>();
        if (bomb == null || board == null)
            return result;

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

                var gem = board.GetGem(nx, ny);
                if (gem != null)
                    result.Add(gem);
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

            var gem = board.GetGem(nx, ny);
            if (gem != null)
                result.Add(gem);
        }

        return result;
    }

    private void TryAddAdjacentBomb(int x, int y, GameBoard board, HashSet<SC_Gem> bombs)
    {
        if (x < 0 || x >= board.Width || y < 0 || y >= board.Height)
            return;

        var neighbor = board.GetGem(x, y);
        if (neighbor != null && neighbor.isBomb)
            bombs.Add(neighbor);
    }
}
