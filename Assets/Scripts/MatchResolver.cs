using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MatchResolver
{
    private readonly SC_GameLogic gameLogic;
    private readonly GameBoard gameBoard;
    private readonly IBombService bombService;

    public MatchResolver(SC_GameLogic gameLogic, GameBoard gameBoard, IBombService bombService)
    {
        this.gameLogic = gameLogic;
        this.gameBoard = gameBoard;
        this.bombService = bombService;
    }

    /// <summary>
    /// Resolves current matches:
    /// - triggers bombs in BombService (bombs inside or adjacent to matches),
    /// - creates exactly one bomb from any 4+ connected match,
    /// - destroys only regular matched gems (bombs are kept for delayed explosion),
    /// - then starts cascading.
    /// </summary>
    public IEnumerator DestroyMatchesCo()
    {
        List<SC_Gem> matches = new List<SC_Gem>(gameBoard.CurrentMatches);
        if (matches.Count == 0)
            yield break;

        // 1) Trigger bombs for this match (bomb in the match or adjacent to it)
        bombService.RegisterMatch(matches, gameBoard);

        // 2) Create a bomb from any 4+ match in this wave (including cascades)
        TryCreateBombInMatches(matches);

        // 3) Destroy only regular matched gems; bombs will explode later
        foreach (SC_Gem gem in matches)
        {
            if (gem != null && gem.isMatch && !gem.isBomb)
            {
                ScoreCheck(gem);
                DestroyMatchedGemsAt(gem.posIndex);
            }
        }

        yield return new WaitForSeconds(0.2f);

        // 4) Start standard cascading. Bombs will explode after all cascades + 1s delay.
        gameLogic.StartCascade();
    }

    /// <summary>
    /// Handles delayed bomb explosions after all cascades and refills:
    /// - waits 1 second,
    /// - explodes all pending bombs (neighbors first, then bombs),
    /// - starts another cascade afterwards.
    /// </summary>
    public IEnumerator HandleBombExplosionsAfterCascadeCo()
    {
        // 1) Wait 1 second AFTER all cascades and refills
        yield return new WaitForSeconds(1.0f);

        var bombsToExplode = bombService.ConsumePendingBombs();
        if (bombsToExplode == null || bombsToExplode.Count == 0)
        {
            // No bombs actually pending – return control to player
            gameLogic.SetState(GlobalEnums.GameState.move);
            yield break;
        }

        // 2) Collect all neighbors to destroy (once), so we do not double-hit same gem
        var neighborsToDestroy = new HashSet<SC_Gem>();

        foreach (var bomb in bombsToExplode)
        {
            if (bomb == null)
                continue;

            foreach (var target in bombService.GetExplosionTargets(bomb, gameBoard))
            {
                if (target != null && target != bomb)
                    neighborsToDestroy.Add(target);
            }
        }

        // 3) Destroy neighbors first
        foreach (var gem in neighborsToDestroy)
        {
            if (gem != null)
            {
                ScoreCheck(gem);
                DestroyMatchedGemsAt(gem.posIndex);
            }
        }

        // 4) Destroy bombs themselves
        foreach (var bomb in bombsToExplode)
        {
            if (bomb != null)
            {
                ScoreCheck(bomb);
                DestroyMatchedGemsAt(bomb.posIndex);
            }
        }

        // 5) Launch standard cascade after explosions
        gameLogic.StartCascade();
    }

    private void ScoreCheck(SC_Gem gemToCheck)
    {
        gameBoard.Score += gemToCheck.scoreValue;
    }

    private void DestroyMatchedGemsAt(Vector2Int pos)
    {
        SC_Gem curGem = gameBoard.GetGem(pos.x, pos.y);
        if (curGem != null)
        {
            Object.Instantiate(curGem.destroyEffect, new Vector2(pos.x, pos.y), Quaternion.identity);

            gameLogic.SetGem(pos.x, pos.y, null);

            if (SC_GemPool.Instance != null)
            {
                SC_GemPool.Instance.Release(curGem);
            }
            else
            {
                Object.Destroy(curGem.gameObject);
            }
        }
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

    private void MakeGemBomb(SC_Gem gem)
    {
        if (gem == null) return;

        SC_Gem bombTemplate = SC_GameVariables.Instance.bomb;

        gem.isBomb = true;
        gem.blastSize = bombTemplate.blastSize;
        gem.destroyEffect = bombTemplate.destroyEffect;
        gem.scoreValue = bombTemplate.scoreValue;

        var bombSR = bombTemplate.GetComponent<SpriteRenderer>();
        var gemSR  = gem.GetComponent<SpriteRenderer>();
        if (gemSR == null || bombSR == null)
        {
            // Fallback: just mark as bomb without visual if something is missing
            gem.isMatch = false;
            return;
        }

        // 1) Remember original sprite (color/type that formed this bomb)
        Sprite originalSprite = gemSR.sprite;

        // 2) Set bomb base sprite
        gemSR.sprite = bombSR.sprite;

        // 3) Setup / create inner sprite for original gem icon
        Transform innerTransform = gem.transform.Find(SC_GameLogic.BombInnerSpriteName);
        SpriteRenderer innerSR;

        if (innerTransform == null)
        {
            GameObject innerObj = new GameObject(SC_GameLogic.BombInnerSpriteName);
            innerObj.transform.SetParent(gem.transform);
            innerObj.transform.localPosition = Vector3.zero;
            innerObj.transform.localRotation = Quaternion.identity;
            innerObj.transform.localScale = Vector3.one * 0.6f; // slightly smaller than bomb

            innerSR = innerObj.AddComponent<SpriteRenderer>();
        }
        else
        {
            innerSR = innerTransform.GetComponent<SpriteRenderer>();
            if (innerSR == null)
                innerSR = innerTransform.gameObject.AddComponent<SpriteRenderer>();

            innerTransform.localPosition = Vector3.zero;
            innerTransform.localRotation = Quaternion.identity;
            innerTransform.localScale = Vector3.one * 0.6f;
        }

        innerSR.sprite = originalSprite;
        innerSR.sortingLayerID = gemSR.sortingLayerID;
        innerSR.sortingOrder   = gemSR.sortingOrder + 1; // draw above bomb base
        innerSR.enabled        = true;

        // Keep baseType as match color; just clear match flag for this turn
        gem.isMatch = false;
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
                // Choose any gem from this group to become a bomb.
                // You can change this selection strategy if needed
                SC_Gem candidate = group[0];

                // Skip if it is already a bomb
                if (candidate.isBomb)
                    continue;

                MakeGemBomb(candidate);

                // This gem should not be destroyed as part of normal match resolution
                currentMatches.Remove(candidate);
                gameBoard.CurrentMatches.Remove(candidate);

                // Only one bomb per resolve step
                return;
            }
        }
    }
}
