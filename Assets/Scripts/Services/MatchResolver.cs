using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MatchResolver : IMatchResolver
{
    private readonly SC_GameLogic gameLogic;
    private readonly GameBoard gameBoard;
    private readonly AreaBombBehavior areaBombBehavior = new AreaBombBehavior();
    private readonly DiscoBallBehavior discoBallBehavior = new DiscoBallBehavior();
    private readonly RocketBombBehavior rocketBombBehavior = new RocketBombBehavior();
    private readonly HelicopterBombBehavior helicopterBombBehavior = new HelicopterBombBehavior();

    public MatchResolver(SC_GameLogic gameLogic, GameBoard gameBoard)
    {
        this.gameLogic = gameLogic;
        this.gameBoard = gameBoard;
    }

    /// <summary>
    /// Resolves current matches:
    /// - creates exactly one bomb from any 4+ connected match,
    /// - destroys only regular matched gems (bombs stay for manual trigger or chain reaction),
    /// - then starts cascading.
    /// </summary>
    public IEnumerator DestroyMatchesCo()
    {
        List<SC_Gem> matches = new List<SC_Gem>(gameBoard.CurrentMatches);
        if (matches.Count == 0)
            yield break;

        // 2) Create a bomb from any 4+ match in this wave (including cascades).
        bool helicopterCreated = helicopterBombBehavior.TryCreateHelicopterBomb(matches, gameBoard, gameLogic);
        if (!helicopterCreated)
            TryCreateBombInMatches(matches);

        // 3) Destroy only regular matched gems; bombs will explode later.
        foreach (SC_Gem gem in matches)
        {
            if (gem != null && gem.isMatch && !gem.IsBomb)
                DestroyMatchedGemsAt(gem.posIndex);
        }

        yield return new WaitForSeconds(0.2f);

        // 4) Start standard cascading. Bombs will explode after all cascades + 1s delay.
        gameLogic.StartCascade();
    }

    private void ScoreCheck(SC_Gem gemToCheck)
    {
        gameBoard.Score += gemToCheck.scoreValue;
    }

    /// <summary>
    /// Destroys an explicit set of gems (used for bomb explosions).
    /// This method does NOT create new bombs from 4+ matches.
    /// </summary>
    public IEnumerator DestroyGemsCo(IReadOnlyCollection<SC_Gem> gemsToDestroy)
    {
        if (gemsToDestroy == null || gemsToDestroy.Count == 0)
            yield break;

        foreach (var gem in gemsToDestroy)
        {
            if (gem == null)
                continue;

            DestroyGemAt(gem.posIndex, allowBombDestroy: true);
            yield return new WaitForSeconds(0.02f);
        }
    }

    private void DestroyGemAt(Vector2Int pos, bool allowBombDestroy)
    {
        SC_Gem curGem = gameBoard.GetGem(pos.x, pos.y);
        if (curGem == null)
            return;

        if (curGem.IsBomb && !allowBombDestroy)
            return;

        ScoreCheck(curGem);
        Transform parent = gameLogic != null ? gameLogic.GemsHolder : null;
        Vector3 localPos = new Vector3(pos.x, pos.y, 0f);

        if (curGem.destroyEffect != null)
        {
            GameObject effect = Object.Instantiate(curGem.destroyEffect, parent);
            effect.transform.localPosition = localPos;
            effect.transform.localRotation = Quaternion.identity;
        }
        gameBoard.SetGem(pos.x, pos.y, null);

        if (SC_GemPool.Instance != null)
            SC_GemPool.Instance.Release(curGem);
        else
            Object.Destroy(curGem.gameObject);
    }

    private void DestroyMatchedGemsAt(Vector2Int pos)
    {
        DestroyGemAt(pos, allowBombDestroy: false);
    }

    private List<SC_Gem> GetConnectedMatchGroupFrom(SC_Gem startGem)
    {
        List<SC_Gem> result = new List<SC_Gem>();
        if (startGem == null) return result;
        if (!startGem.isMatch) return result;

        GlobalEnums.GemType matchType = startGem.baseType;
        bool[,] visited = new bool[gameBoard.Width, gameBoard.Height];

        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        queue.Enqueue(startGem.posIndex);
        visited[startGem.posIndex.x, startGem.posIndex.y] = true;

        int[] dx = { 1, -1, 0, 0 };
        int[] dy = { 0, 0, 1, -1 };

        while (queue.Count > 0)
        {
            Vector2Int pos = queue.Dequeue();
            SC_Gem gem = gameBoard.GetGem(pos.x, pos.y);

            if (gem == null || !gem.isMatch) continue;
            if (gem.baseType != matchType) continue;

            result.Add(gem);

            for (int i = 0; i < 4; i++)
            {
                int nx = pos.x + dx[i];
                int ny = pos.y + dy[i];

                if (nx < 0 || nx >= gameBoard.Width || ny < 0 || ny >= gameBoard.Height)
                    continue;

                if (visited[nx, ny]) continue;

                SC_Gem neighbor = gameBoard.GetGem(nx, ny);
                if (neighbor != null && neighbor.isMatch && neighbor.baseType == matchType)
                {
                    visited[nx, ny] = true;
                    queue.Enqueue(new Vector2Int(nx, ny));
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Creates exactly one bomb from the current matches
    /// if there is any connected group of 4+ gems of the same baseType.
    /// Works for both user-initiated matches and cascade/refill matches.
    /// </summary>
    private void TryCreateBombInMatches(List<SC_Gem> currentMatches)
    {
        if (currentMatches == null || currentMatches.Count == 0)
            return;

        // We use this set to not re-process the same gems
        HashSet<SC_Gem> visited = new HashSet<SC_Gem>();

        foreach (SC_Gem gem in currentMatches)
        {
            if (gem == null || visited.Contains(gem))
                continue;

            // Take a connected group starting from this gem
            List<SC_Gem> group = GetConnectedMatchGroupFrom(gem);
            foreach (var g in group)
                visited.Add(g);

            // We only care about groups of size >= 4
            if (group.Count >= 4)
            {
                // Prefer the swapped gem (if part of the group) so the bomb appears where the move happened.
                SC_Gem candidate = null;
                if (gameLogic.LastMovedGemA != null && group.Contains(gameLogic.LastMovedGemA))
                    candidate = gameLogic.LastMovedGemA;
                else if (gameLogic.LastMovedGemB != null && group.Contains(gameLogic.LastMovedGemB))
                    candidate = gameLogic.LastMovedGemB;
                else
                    candidate = group[0];

                // Skip if it is already a bomb
                if (candidate.IsBomb)
                    continue;

                GlobalEnums.RocketDirection rocketDir = DetermineLineDirection(group);
                bool isLine = rocketDir != GlobalEnums.RocketDirection.None;
                bool createDisco = group.Count >= 5 && isLine;
                bool createRocket = group.Count == 4 && isLine;

                if (createDisco)
                    discoBallBehavior.MakeBomb(candidate);
                else if (createRocket)
                    rocketBombBehavior.MakeBomb(candidate, rocketDir);
                else
                    areaBombBehavior.MakeBomb(candidate);

                // This gem should not be destroyed as part of normal match resolution
                currentMatches.Remove(candidate);
                gameBoard.CurrentMatches.Remove(candidate);

                // Only one bomb per resolve step
                return;
            }
        }
    }
    private GlobalEnums.RocketDirection DetermineLineDirection(List<SC_Gem> group)
    {
        if (group == null || group.Count == 0)
            return GlobalEnums.RocketDirection.None;

        bool sameX = true;
        bool sameY = true;
        int x0 = group[0].posIndex.x;
        int y0 = group[0].posIndex.y;

        foreach (var g in group)
        {
            if (g.posIndex.x != x0) sameX = false;
            if (g.posIndex.y != y0) sameY = false;
        }

        if (sameY) return GlobalEnums.RocketDirection.Vertical;   // horizontal match -> vertical blast
        if (sameX) return GlobalEnums.RocketDirection.Horizontal; // vertical match   -> horizontal blast
        return GlobalEnums.RocketDirection.None;
    }
}
