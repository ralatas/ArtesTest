using UnityEngine;

/// <summary>
/// Service responsible for managing and updating game score.
/// Wraps GameBoard.Score so that score-related logic is centralized
/// and can be easily extended (multipliers, combos, etc.).
/// </summary>
public interface IScoreService
{
    int Score { get; }

    /// <summary>
    /// Resets score back to zero.
    /// </summary>
    void ResetScore();

    /// <summary>
    /// Adds score based on the given gem's scoreValue.
    /// </summary>
    void AddGemScore(SC_Gem gem);

    /// <summary>
    /// Adds a raw score value.
    /// </summary>
    void AddScore(int value);
}

public class ScoreService : IScoreService
{
    private readonly GameBoard gameBoard;

    public int Score => gameBoard.Score;

    public ScoreService(GameBoard gameBoard)
    {
        this.gameBoard = gameBoard;
    }

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

    public void AddScore(int value)
    {
        gameBoard.Score += value;
    }
}
