using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardRefillService : IBoardRefillService
{
    private readonly GameBoard gameBoard;
    private readonly SC_GameLogic gameLogic;

    /// <summary>
    /// Конструктор сервиса каскада и рефилла доски.
    /// Сохраняет ссылки на GameBoard и SC_GameLogic.
    /// </summary>
    public BoardRefillService(GameBoard gameBoard, SC_GameLogic gameLogic)
    {
        this.gameBoard = gameBoard;
        this.gameLogic = gameLogic;
    }

    /// <summary>
    /// Запускает полный цикл: таймаут → каскад существующих гемов → пауза → рефилл пустых клеток.
    /// </summary>
    public IEnumerator CascadeAndRefillCo()
    {
        // A short pause before the start of the cascade, as before.
        yield return new WaitForSeconds(.2f);

        // 1) Cascade existing gems down into empty spaces.
        yield return CascadeExistingGemsCo();

        // 2) Small delay between cascade and refill (keeps the original "feel").
        yield return new WaitForSeconds(0.5f);

        // 3) Refill the board with new gems and clean up misplaced ones.
        yield return RefillBoardInternalCo();
    }

    /// <summary>
    /// Заставляет существующие гемы падать вниз в пустые клетки (каскад).
    /// Логика извлечена из SC_GameLogic.DecreaseRowCo.
    /// </summary>
    private IEnumerator CascadeExistingGemsCo()
    {
        for (int x = 0; x < gameBoard.Width; x++)
        {
            int emptyY = -1; // Position of an empty cell

            for (int y = 0; y < gameBoard.Height; y++)
            {
                SC_Gem curGem = gameBoard.GetGem(x, y);

                if (curGem == null)
                {
                    // Found the first empty cell in the column
                    if (emptyY == -1)
                        emptyY = y;
                }
                else if (emptyY != -1)
                {
                    // There is a void below — we "drop" the gem into the lowest empty position
                    curGem.posIndex = new Vector2Int(x, emptyY);
                    gameLogic.SetGem(x, emptyY, curGem);
                    gameLogic.SetGem(x, y, null);

                    emptyY++;

                    // Making a cascade "one at a time" — the delay between drops
                    yield return new WaitForSeconds(SC_GameVariables.Instance.cascadeStepDelay);
                }
            }
        }
    }

    /// <summary>
    /// Создает новые гемы во всех пустых клетках, а затем удаляет любые неправильно размещенные гемы.
    /// Логика извлечена из SC_GameLogic.RefillBoardCo + CheckMisplacedGems.
    /// </summary>
    private IEnumerator RefillBoardInternalCo()
    {
        for (int x = 0; x < gameBoard.Width; x++)
        {
            for (int y = 0; y < gameBoard.Height; y++)
            {
                SC_Gem curGem = gameBoard.GetGem(x, y);
                if (curGem == null)
                {
                    int gemToUse = Random.Range(0, SC_GameVariables.Instance.gems.Length);
                    int iterations = 0;
                    Vector2Int pos = new Vector2Int(x, y);

                    // Block match logic
                    while (gameBoard.MatchesAt(pos, SC_GameVariables.Instance.gems[gemToUse]) && iterations < 100)
                    {
                        gemToUse = Random.Range(0, SC_GameVariables.Instance.gems.Length);
                        iterations++;
                    }

                    // Spawn a new gem into the cell (SpawnGem already handles dropHeight and visuals)
                    gameLogic.SpawnGem(pos, SC_GameVariables.Instance.gems[gemToUse]);

                    // Stagger: a small delay between spawning gems
                    yield return new WaitForSeconds(SC_GameVariables.Instance.spawnStaggerDelay);
                }
            }
        }

        // Cleanup: destroy any gems that are no longer referenced by the board.
        CleanupMisplacedGems();
    }
    /// <summary>
    /// Удаляет «лишние» гемы на сцене, которые больше не присутствуют в ссылках GameBoard.
    /// Нужно для уборки объектов, потерявших привязку к сетке.
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
                if (foundGems.Contains(curGem))
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
