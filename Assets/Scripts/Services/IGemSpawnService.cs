using UnityEngine;

public interface IGemSpawnService
{
    /// <summary>
    /// Spawns (or fetches from pool) a gem at the given board position, resets its state, and registers it on the board.
    /// </summary>
    SC_Gem SpawnGem(Vector2Int position, SC_Gem prefab, SC_GameLogic owner, Transform parent);

    /// <summary>
    /// Fills the entire board with initial gems avoiding immediate matches.
    /// Also instantiates background tiles.
    /// </summary>
    void FillBoard(SC_GameLogic owner, Transform holder);
}
