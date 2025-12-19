using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles cascades (falling gems) and board refill.
/// Keeps board update logic outside of SC_GameLogic.
/// </summary>
public class BoardRefillService : IBoardRefillService
{
    private readonly GameBoard gameBoard;
    private readonly IGemSpawnService gemSpawnService;
    private readonly SC_GameLogic gameLogic;

    public BoardRefillService(GameBoard gameBoard, IGemSpawnService gemSpawnService, SC_GameLogic gameLogic)
    {
        this.gameBoard = gameBoard;
        this.gemSpawnService = gemSpawnService;
        this.gameLogic = gameLogic;
    }

    public IEnumerator CascadeAndRefillCo()
    {
        // Small delay before the cascade to keep the original feel
        if (SC_GameVariables.Instance.cascadeStartDelay > 0f)
            yield return new WaitForSeconds(SC_GameVariables.Instance.cascadeStartDelay);

        yield return CascadeGemsCo();
        yield return RefillBoardCo();
        CleanupMisplacedGems();
    }

    /// <summary>
    /// Moves existing gems down to fill empty cells.
    /// Only updates logical positions (posIndex and GameBoard).
    /// Visual movement is handled by SC_Gem via Lerp.
    /// </summary>
    private IEnumerator CascadeGemsCo()
    {
        float stepDelay = SC_GameVariables.Instance.cascadeStepDelay;

        for (int x = 0; x < gameBoard.Width; x++)
        {
            int emptyRow = -1;

            // Scan from bottom to top in each column
            for (int y = 0; y < gameBoard.Height; y++)
            {
                SC_Gem currentGem = gameBoard.GetGem(x, y);

                if (currentGem == null)
                {
                    if (emptyRow == -1)
                        emptyRow = y;
                }
                else if (emptyRow != -1)
                {
                    gameBoard.SetGem(x, emptyRow, currentGem);
                    gameBoard.SetGem(x, y, null);

                    currentGem.posIndex = new Vector2Int(x, emptyRow);
                    emptyRow++;

                    if (stepDelay > 0f)
                        yield return new WaitForSeconds(stepDelay);
                }
            }
        }
    }

    /// <summary>
    /// Refills all empty cells with new gems.
    /// Each new gem is picked so that it does not create an immediate match.
    /// </summary>
    private IEnumerator RefillBoardCo()
    {
        float spawnDelay = SC_GameVariables.Instance.spawnStaggerDelay;

        for (int x = 0; x < gameBoard.Width; x++)
        {
            for (int y = 0; y < gameBoard.Height; y++)
            {
                SC_Gem currentGem = gameBoard.GetGem(x, y);
                if (currentGem != null)
                    continue;

                Vector2Int pos = new Vector2Int(x, y);
                int gemIndex = PickGemIndexWithoutMatch(pos);

                Transform parent = gameLogic.GemsHolder;
                gemSpawnService.SpawnGem(pos, SC_GameVariables.Instance.gems[gemIndex], gameLogic, parent);

                if (spawnDelay > 0f)
                    yield return new WaitForSeconds(spawnDelay);
            }
        }

        CleanupMisplacedGems();
    }

    /// <summary>
    /// Picks a random gem index that does not form a match at the given position.
    /// </summary>
    private int PickGemIndexWithoutMatch(Vector2Int pos)
    {
        int gemIndex = Random.Range(0, SC_GameVariables.Instance.gems.Length);
        int iterations = 0;
        const int maxIterations = 100;

        while (gameBoard.MatchesAt(pos, SC_GameVariables.Instance.gems[gemIndex]) &&
               iterations++ < maxIterations)
        {
            gemIndex = Random.Range(0, SC_GameVariables.Instance.gems.Length);
        }

        return gemIndex;
    }

    /// <summary>
    /// Removes any SC_Gem that is not referenced by GameBoard.
    /// Prevents visual leftovers after cascades and refills.
    /// </summary>
    private void CleanupMisplacedGems()
    {
        List<SC_Gem> foundGems = new List<SC_Gem>();
        foundGems.AddRange(Object.FindObjectsOfType<SC_Gem>());

        for (int x = 0; x < gameBoard.Width; x++)
        {
            for (int y = 0; y < gameBoard.Height; y++)
            {
                SC_Gem curGem = gameBoard.GetGem(x, y);
                if (curGem != null && foundGems.Contains(curGem))
                    foundGems.Remove(curGem);
            }
        }

        foreach (SC_Gem g in foundGems)
        {
            if (g != null)
                Object.Destroy(g.gameObject);
        }
    }
}
