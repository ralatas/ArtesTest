using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Helicopter bomb: spawns from a square match and blasts orthogonal neighbors.
/// </summary>
public class HelicopterBombBehavior : IBombBehavior
{
    public bool TryCreateHelicopterBomb(List<SC_Gem> currentMatches, GameBoard board, SC_GameLogic gameLogic)
    {
        if (currentMatches == null || currentMatches.Count == 0 || board == null || gameLogic == null)
            return false;

        HashSet<SC_Gem> matchSet = new HashSet<SC_Gem>(currentMatches);

        for (int x = 0; x < board.Width - 1; x++)
        {
            for (int y = 0; y < board.Height - 1; y++)
            {
                List<SC_Gem> square = GetSquareGroup(board, x, y);
                if (square == null || square.Count == 0)
                    continue;

                if (!square.TrueForAll(matchSet.Contains))
                    continue;

                GlobalEnums.GemType matchType = GetGemMatchType(square[0]);
                SC_Gem adjacent = FindAdjacentSameType(board, square, matchType);
                if (adjacent != null && !matchSet.Contains(adjacent))
                {
                    adjacent.isMatch = true;
                    currentMatches.Add(adjacent);
                    board.CurrentMatches.Add(adjacent);
                    matchSet.Add(adjacent);
                }

                List<SC_Gem> fullGroup = new List<SC_Gem>(square);
                if (adjacent != null)
                    fullGroup.Add(adjacent);

                SC_Gem candidate = PickBombOrigin(fullGroup, gameLogic);
                if (candidate == null || candidate.IsBomb)
                    continue;

                MakeBomb(candidate);
                candidate.isMatch = false;

                currentMatches.Remove(candidate);
                board.CurrentMatches.Remove(candidate);
                return true;
            }
        }

        return false;
    }

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

        foreach (var target in CrossNeighbors(bomb, board))
            yield return target;

    }

    private IEnumerable<SC_Gem> CrossNeighbors(SC_Gem bomb, GameBoard board)
    {
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

    public SC_Gem FindPriorityBlocker(GameBoard board, SC_Gem bomb)
    {
        SC_Gem best = null;
        int bestPriority = int.MaxValue;

        for (int x = 0; x < board.Width; x++)
        {
            for (int y = 0; y < board.Height; y++)
            {
                SC_Gem candidate = board.GetGem(x, y);
                if (candidate == null || candidate == bomb)
                    continue;

                int priority = EvaluateRemoval(board, x, y);
                if (priority > 0 && priority < bestPriority)
                {
                    bestPriority = priority;
                    best = candidate;
                }
            }
        }

        return best;
    }

    // Lower priority number means better (disco=1, bomb=2, match=3)
    private int EvaluateRemoval(GameBoard board, int x, int y)
    {
        // simulate removing gem at (x,y) and letting the column drop by 1
        SC_Gem[] column = new SC_Gem[board.Height];
        for (int i = 0; i < board.Height; i++)
            column[i] = board.GetGem(x, i);

        for (int i = y; i < board.Height - 1; i++)
            column[i] = column[i + 1];
        column[board.Height - 1] = null;

        SC_Gem fallen = column[y];
        if (fallen == null || fallen.IsBomb)
            return 0;

        GlobalEnums.GemType type = fallen.baseType;

        int horiz = 1;
        horiz += CountDirection(board, x, y, -1, 0, type, column);
        horiz += CountDirection(board, x, y, 1, 0, type, column);

        int vert = 1;
        vert += CountDirection(board, x, y, 0, -1, type, column);
        vert += CountDirection(board, x, y, 0, 1, type, column);

        int maxLen = Mathf.Max(horiz, vert);
        if (maxLen < 3)
            return 0;
        if (maxLen >= 5)
            return 1;
        if (maxLen == 4)
            return 2;
        return 3;
    }

    private int CountDirection(GameBoard board, int startX, int startY, int dx, int dy, GlobalEnums.GemType type, SC_Gem[] simulatedColumn)
    {
        int count = 0;
        int x = startX + dx;
        int y = startY + dy;
        while (x >= 0 && x < board.Width && y >= 0 && y < board.Height)
        {
            SC_Gem g = (x == startX) ? simulatedColumn[y] : board.GetGem(x, y);
            if (g == null || g.IsBomb || g.baseType != type)
                break;
            count++;
            x += dx;
            y += dy;
        }
        return count;
    }

    private List<SC_Gem> GetSquareGroup(GameBoard board, int startX, int startY)
    {
        if (startX < 0 || startY < 0 || startX >= board.Width - 1 || startY >= board.Height - 1)
            return null;

        SC_Gem g00 = board.GetGem(startX, startY);
        SC_Gem g10 = board.GetGem(startX + 1, startY);
        SC_Gem g01 = board.GetGem(startX, startY + 1);
        SC_Gem g11 = board.GetGem(startX + 1, startY + 1);

        if (g00 == null || g10 == null || g01 == null || g11 == null)
            return null;

        if (g00.IsBomb || g10.IsBomb || g01.IsBomb || g11.IsBomb)
            return null;

        GlobalEnums.GemType matchType = GetGemMatchType(g00);
        if (GetGemMatchType(g10) != matchType || GetGemMatchType(g01) != matchType || GetGemMatchType(g11) != matchType)
            return null;

        return new List<SC_Gem> { g00, g10, g01, g11 };
    }

    private SC_Gem FindAdjacentSameType(GameBoard board, IEnumerable<SC_Gem> square, GlobalEnums.GemType matchType)
    {
        HashSet<SC_Gem> squareSet = new HashSet<SC_Gem>(square);
        Vector2Int[] dirs =
        {
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1)
        };

        foreach (var gem in square)
        {
            Vector2Int pos = gem.posIndex;
            foreach (var dir in dirs)
            {
                int nx = pos.x + dir.x;
                int ny = pos.y + dir.y;

                if (nx < 0 || ny < 0 || nx >= board.Width || ny >= board.Height)
                    continue;

                SC_Gem neighbor = board.GetGem(nx, ny);
                if (neighbor == null || neighbor.IsBomb || squareSet.Contains(neighbor))
                    continue;

                if (GetGemMatchType(neighbor) == matchType)
                    return neighbor;
            }
        }

        return null;
    }

    private SC_Gem PickBombOrigin(List<SC_Gem> group, SC_GameLogic gameLogic)
    {
        if (group == null || group.Count == 0 || gameLogic == null)
            return null;

        if (gameLogic.LastMovedGemA != null && group.Contains(gameLogic.LastMovedGemA))
            return gameLogic.LastMovedGemA;

        if (gameLogic.LastMovedGemB != null && group.Contains(gameLogic.LastMovedGemB))
            return gameLogic.LastMovedGemB;

        return group[0];
    }

    private GlobalEnums.GemType GetGemMatchType(SC_Gem gem)
    {
        return gem.baseType != 0 ? gem.baseType : gem.type;
    }
}
