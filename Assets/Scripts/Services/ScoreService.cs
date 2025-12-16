using UnityEngine;
/// <summary>
/// Реализация сервиса счёта: хранит и изменяет GameBoard.Score через единый интерфейс.
/// </summary>

public class ScoreService : IScoreService
{
    private readonly GameBoard gameBoard;

    public int Score => gameBoard.Score;

    public ScoreService(GameBoard gameBoard)
    {
        this.gameBoard = gameBoard;
    }
    /// <summary>
    /// Сбрасывает счёт в ноль.
    /// </summary>
    public void ResetScore()
    {
        gameBoard.Score = 0;
    }

    public void AddGemScore(SC_Gem gem)
    {
        if (gem == null)
            return;

        gameBoard.Score += gem.scoreValue;
    }

    /// <summary>
    /// Добавляет указанное количество очков напрямую.
    /// </summary>
    public void AddScore(int value)
    {
        gameBoard.Score += value;
    }
}
