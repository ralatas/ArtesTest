/// <summary>
/// Сервис централизует всю работу со счётом (очки) игры.
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