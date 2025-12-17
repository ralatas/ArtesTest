using UnityEngine;

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
