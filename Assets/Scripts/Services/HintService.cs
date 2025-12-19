using System.Collections.Generic;
using UnityEngine;

public class HintService : IHintService
{
    private readonly GameBoard gameBoard;
    private readonly float hintDelaySeconds;

    private float idleTimer;
    private List<(SC_Gem gem, Color original)> highlighted = new List<(SC_Gem, Color)>();

    public HintService(GameBoard gameBoard, float hintDelaySeconds = 5f)
    {
        this.gameBoard = gameBoard;
        this.hintDelaySeconds = hintDelaySeconds;
    }

    public void Tick(float deltaTime, GlobalEnums.GameState state)
    {
        
        if (state != GlobalEnums.GameState.move)
        {
            ClearHint();
            idleTimer = 0f;
            return;
        }

        idleTimer += deltaTime;
        if (idleTimer >= hintDelaySeconds && highlighted.Count == 0)
        {
            ShowHint();
            idleTimer = 0f; // restart timer so we don't spam highlights
        }
    }

    public void RegisterActivity()
    {
        idleTimer = 0f;
        ClearHint();
    }

    public void ClearHint()
    {
        foreach (var entry in highlighted)
        {
            if (entry.gem == null)
                continue;

            var sr = entry.gem.GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.color = entry.original;
        }

        highlighted.Clear();
    }

    private void ShowHint()
    {
        ClearHint();
        var best = FindBestMove();
        if (best == null)
            return;

        HighlightGems(best.Value.gems);
    }

    private void HighlightGems(List<SC_Gem> gems)
    {
        if (gems == null)
            return;

        foreach (var gem in gems)
        {
            if (gem == null)
                continue;
            var sr = gem.GetComponent<SpriteRenderer>();
            if (sr == null)
                continue;
            highlighted.Add((gem, sr.color));

            sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 0.5f);
        }
    }

    private (List<SC_Gem> gems, int priority)? FindBestMove()
    {
        (List<SC_Gem>, int)? best = null;

        for (int x = 0; x < gameBoard.Width; x++)
        {
            for (int y = 0; y < gameBoard.Height; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                // Right neighbor
                EvaluateMove(pos, new Vector2Int(x + 1, y), ref best);
                // Up neighbor
                EvaluateMove(pos, new Vector2Int(x, y + 1), ref best);
            }
        }

        return best;
    }

    private void EvaluateMove(Vector2Int aPos, Vector2Int bPos, ref (List<SC_Gem>, int)? best)
    {
        if (bPos.x >= gameBoard.Width || bPos.y >= gameBoard.Height)
            return;

        SC_Gem a = gameBoard.GetGem(aPos.x, aPos.y);
        SC_Gem b = gameBoard.GetGem(bPos.x, bPos.y);

        if (a == null || b == null)
            return;

        // swap temporarily
        gameBoard.SetGem(aPos.x, aPos.y, b);
        gameBoard.SetGem(bPos.x, bPos.y, a);

        int priority = GetMovePriority(aPos, bPos, out List<SC_Gem> matchGems);

        // revert
        gameBoard.SetGem(aPos.x, aPos.y, a);
        gameBoard.SetGem(bPos.x, bPos.y, b);

        if (priority == 0)
            return;

        if (best == null || priority < best.Value.Item2)
            best = (matchGems, priority);
    }

    private int GetMovePriority(Vector2Int aPos, Vector2Int bPos, out List<SC_Gem> matchGems)
    {
        List<SC_Gem> aLine = GetMatchLine(aPos);
        List<SC_Gem> bLine = GetMatchLine(bPos);

        matchGems = aLine.Count >= bLine.Count ? aLine : bLine;

        int maxLen = matchGems.Count;

        if (maxLen < 3)
            return 0; // not a valid match

        if (maxLen >= 5)
            return 1; // disco

        if (maxLen == 4)
            return 2; // bomb/rocket

        return 3; // normal
    }

    private List<SC_Gem> GetMatchLine(Vector2Int pos)
    {
        SC_Gem center = gameBoard.GetGem(pos.x, pos.y);
        if (center == null || center.IsBomb)
            return new List<SC_Gem>();
        GlobalEnums.GemType type = center.baseType;

        // horizontal
        List<SC_Gem> horiz = new List<SC_Gem> { center };
        for (int dx = pos.x - 1; dx >= 0; dx--)
        {
            SC_Gem g = gameBoard.GetGem(dx, pos.y);
            if (g == null || g.baseType != type)
                break;
            horiz.Add(g);
        }
        for (int dx = pos.x + 1; dx < gameBoard.Width; dx++)
        {
            SC_Gem g = gameBoard.GetGem(dx, pos.y);
            if (g == null || g.baseType != type)
                break;
            horiz.Add(g);
        }

        // vertical
        List<SC_Gem> vert = new List<SC_Gem> { center };
        for (int dy = pos.y - 1; dy >= 0; dy--)
        {
            SC_Gem g = gameBoard.GetGem(pos.x, dy);
            if (g == null || g.baseType != type)
                break;
            vert.Add(g);
        }
        for (int dy = pos.y + 1; dy < gameBoard.Height; dy++)
        {
            SC_Gem g = gameBoard.GetGem(pos.x, dy);
            if (g == null || g.baseType != type)
                break;
            vert.Add(g);
        }

        return horiz.Count >= vert.Count ? horiz : vert;
    }
}
