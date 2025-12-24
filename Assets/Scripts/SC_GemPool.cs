using System.Collections.Generic;
using UnityEngine;

public class SC_GemPool : MonoBehaviour
{
    public static SC_GemPool Instance { get; private set; }

    // Key — prefab, value — queue of free instances of this type
    private readonly Dictionary<SC_Gem, Queue<SC_Gem>> pool =
        new Dictionary<SC_Gem, Queue<SC_Gem>>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        // Can be removed if persistence across scenes is not needed
        DontDestroyOnLoad(gameObject);
    }

    public SC_Gem Get(SC_Gem prefab, Vector2Int boardPos, SC_GameLogic gameLogic, Transform parent)
    {
        if (!pool.TryGetValue(prefab, out Queue<SC_Gem> queue))
        {
            queue = new Queue<SC_Gem>();
            pool[prefab] = queue;
        }

        SC_Gem gem;

        if (queue.Count > 0)
        {
            gem = queue.Dequeue();
            gem.gameObject.SetActive(true);
        }
        else
        {
            // Initial creation
            gem = Instantiate(
                prefab,
                new Vector3(boardPos.x, boardPos.y + SC_GameVariables.Instance.dropHeight, 0f),
                Quaternion.identity
            );

            gem.prefabReference = prefab;
        }

        // If prefabReference is empty — update it
        if (gem.prefabReference == null)
            gem.prefabReference = prefab;

        // Basic setup
        gem.transform.SetParent(parent, worldPositionStays: false);
        gem.name = $"Gem - {boardPos.x}, {boardPos.y}";
        gem.posIndex = boardPos;

        gem.transform.localPosition = new Vector3(
            boardPos.x,
            boardPos.y + SC_GameVariables.Instance.dropHeight,
            0f
        );

        gem.isMatch = false;
        gem.SetupGem(gameLogic, boardPos);

        return gem;
    }

    public void Release(SC_Gem gem)
    {
        if (gem == null)
            return;

        SC_Gem prefab = gem.prefabReference;

        // Если нет ссылки на префаб — лучше удалить, чем ломать словарь
        if (prefab == null)
        {
            Destroy(gem.gameObject);
            return;
        }

        if (!pool.TryGetValue(prefab, out Queue<SC_Gem> queue))
        {
            queue = new Queue<SC_Gem>();
            pool[prefab] = queue;
        }

        gem.isMatch = false;
        gem.bombType = GlobalEnums.BombType.None;
        gem.rocketDirection = GlobalEnums.RocketDirection.None;
        gem.discoHasTarget = false;
        gem.discoTargetType = GlobalEnums.GemType.bomb;

        // Reset position off-board so it doesn't interfere
        gem.transform.position = new Vector3(-10f, -10f, 0f);

        gem.gameObject.SetActive(false);
        queue.Enqueue(gem);
    }
}
