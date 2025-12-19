using UnityEngine;

public class GemSpawnService : IGemSpawnService
{
    private readonly GameBoard gameBoard;

    public GemSpawnService(GameBoard gameBoard)
    {
        this.gameBoard = gameBoard;
    }

    public SC_Gem SpawnGem(Vector2Int position, SC_Gem prefab, SC_GameLogic owner, Transform parent)
    {
        if (prefab == null)
            return null;

        SC_Gem gemInstance;
        if (SC_GemPool.Instance != null)
        {
            gemInstance = SC_GemPool.Instance.Get(prefab, position, owner, parent);
        }
        else
        {
            gemInstance = Object.Instantiate(
                prefab,
                new Vector3(position.x, position.y + SC_GameVariables.Instance.dropHeight, 0f),
                Quaternion.identity
            );
            gemInstance.transform.SetParent(parent);
            gemInstance.name = $"Gem - {position.x}, {position.y}";
            gemInstance.prefabReference = prefab;
            gemInstance.SetupGem(owner, position);
        }

        SC_Gem template = gemInstance.prefabReference != null ? gemInstance.prefabReference : prefab;
        if (template != null)
        {
            gemInstance.type = template.type;
            gemInstance.scoreValue = template.scoreValue;
            gemInstance.destroyEffect = template.destroyEffect;
            gemInstance.blastSize = template.blastSize;
        }

        gemInstance.bombType = GlobalEnums.BombType.None;
        gemInstance.rocketDirection = GlobalEnums.RocketDirection.None;
        gemInstance.baseType = gemInstance.type;

        ResetVisualsFromTemplate(gemInstance);

        gameBoard.SetGem(position.x, position.y, gemInstance);
        return gemInstance;
    }

    public void FillBoard(SC_GameLogic owner, Transform holder)
    {
        if (owner == null || holder == null)
            return;

        for (int x = 0; x < gameBoard.Width; x++)
        {
            for (int y = 0; y < gameBoard.Height; y++)
            {
                Vector2 pos = new Vector2(x, y);
                GameObject bgTile = Object.Instantiate(SC_GameVariables.Instance.bgTilePrefabs, pos, Quaternion.identity);
                bgTile.transform.SetParent(holder);
                bgTile.name = $"BG Tile - {x}, {y}";

                Vector2Int gridPos = new Vector2Int(x, y);
                int gemIndex = PickGemIndexWithoutMatch(gridPos);
                SpawnGem(gridPos, SC_GameVariables.Instance.gems[gemIndex], owner, holder);
            }
        }
    }

    private int PickGemIndexWithoutMatch(Vector2Int pos)
    {
        int gemIndex = Random.Range(0, SC_GameVariables.Instance.gems.Length);
        int iterations = 0;
        const int maxIterations = 100;

        while (gameBoard.MatchesAt(pos, SC_GameVariables.Instance.gems[gemIndex]) && iterations++ < maxIterations)
        {
            gemIndex = Random.Range(0, SC_GameVariables.Instance.gems.Length);
        }

        return gemIndex;
    }

    private void ResetVisualsFromTemplate(SC_Gem gemInstance)
    {
        if (gemInstance == null)
            return;

        var gemSR = gemInstance.GetComponent<SpriteRenderer>();
        SpriteRenderer prefabSR = null;
        if (gemInstance.prefabReference != null)
            prefabSR = gemInstance.prefabReference.GetComponent<SpriteRenderer>();

        if (gemSR != null && prefabSR != null)
        {
            gemSR.sprite = prefabSR.sprite;
            gemSR.color = prefabSR.color;
        }

        // No inner sprite cleanup needed; bombs now rely solely on their main sprite.
    }
}
