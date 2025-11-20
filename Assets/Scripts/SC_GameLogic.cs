using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SC_GameLogic : MonoBehaviour
{
    private Dictionary<string, GameObject> unityObjects;
    private int score = 0;
    private float displayScore = 0;
    private GameBoard gameBoard;
    private GlobalEnums.GameState currentState = GlobalEnums.GameState.move;
    public GlobalEnums.GameState CurrentState { get { return currentState; } }
    private SC_Gem lastMovedGemA;
    private SC_Gem lastMovedGemB;
    private IBombService bombService;
    private const string BombInnerSpriteName = "BombInnerSprite";
    #region MonoBehaviour
    private void Awake()
    {
        bombService = new BombService();
        Init();
    }

    private void Start()
    {
        StartGame();
    }

    private void Update()
    {
        displayScore = Mathf.Lerp(displayScore, gameBoard.Score, SC_GameVariables.Instance.scoreSpeed * Time.deltaTime);
        unityObjects["Txt_Score"].GetComponent<TMPro.TextMeshProUGUI>().text = displayScore.ToString("0");
    }
    #endregion

    #region Logic
    private void Init()
    {
        unityObjects = new Dictionary<string, GameObject>();
        GameObject[] _obj = GameObject.FindGameObjectsWithTag("UnityObject");
        foreach (GameObject g in _obj)
            unityObjects.Add(g.name,g);

        gameBoard = new GameBoard(7, 7);
        Setup();
    }
    private void Setup()
    {
        for (int x = 0; x < gameBoard.Width; x++)
            for (int y = 0; y < gameBoard.Height; y++)
            {
                Vector2 _pos = new Vector2(x, y);
                GameObject _bgTile = Instantiate(SC_GameVariables.Instance.bgTilePrefabs, _pos, Quaternion.identity);
                _bgTile.transform.SetParent(unityObjects["GemsHolder"].transform);
                _bgTile.name = "BG Tile - " + x + ", " + y;

                int _gemToUse = Random.Range(0, SC_GameVariables.Instance.gems.Length);

                int iterations = 0;
                while (gameBoard.MatchesAt(new Vector2Int(x, y), SC_GameVariables.Instance.gems[_gemToUse]) && iterations < 100)
                {
                    _gemToUse = Random.Range(0, SC_GameVariables.Instance.gems.Length);
                    iterations++;
                }
                SpawnGem(new Vector2Int(x, y), SC_GameVariables.Instance.gems[_gemToUse]);
            }
    }
    public void StartGame()
    {
        unityObjects["Txt_Score"].GetComponent<TextMeshProUGUI>().text = score.ToString("0");
    }
    private void SpawnGem(Vector2Int _Position, SC_Gem _GemToSpawn)
    {
        SC_Gem gemInstance;
        if (SC_GemPool.Instance != null)
        {
            gemInstance = SC_GemPool.Instance.Get(
                _GemToSpawn,
                _Position,
                this,
                unityObjects["GemsHolder"].transform
            );
        }
        else
        {
            gemInstance = Instantiate(
                _GemToSpawn,
                new Vector3(_Position.x, _Position.y + SC_GameVariables.Instance.dropHeight, 0f),
                Quaternion.identity
            );
            gemInstance.transform.SetParent(unityObjects["GemsHolder"].transform);
            gemInstance.name = "Gem - " + _Position.x + ", " + _Position.y;
            gemInstance.prefabReference = _GemToSpawn;
            gemInstance.SetupGem(this, _Position);
        }

        // Reset bomb state
        gemInstance.isBomb   = false;
        gemInstance.baseType = gemInstance.type;

        // 🔹 Reset main visual from prefab (in case this was a bomb before)
        var gemSR    = gemInstance.GetComponent<SpriteRenderer>();
        SpriteRenderer prefabSR = null;
        if (gemInstance.prefabReference != null)
            prefabSR = gemInstance.prefabReference.GetComponent<SpriteRenderer>();

        if (gemSR != null && prefabSR != null)
        {
            gemSR.sprite = prefabSR.sprite;
            gemSR.color  = prefabSR.color;
        }

        // 🔹 Hide/remove inner bomb sprite if it exists
        Transform innerTransform = gemInstance.transform.Find(BombInnerSpriteName);
        if (innerTransform != null)
        {
            var innerSR = innerTransform.GetComponent<SpriteRenderer>();
            if (innerSR != null)
            {
                innerSR.sprite  = null;
                innerSR.enabled = false;
            }
        }

        gameBoard.SetGem(_Position.x, _Position.y, gemInstance);
    }

    public void RegisterSwap(SC_Gem first, SC_Gem second)
    {
        lastMovedGemA = first;
        lastMovedGemB = second;
    }
    public void SetGem(int _X,int _Y, SC_Gem _Gem)
    {
        gameBoard.SetGem(_X,_Y, _Gem);
    }
    public SC_Gem GetGem(int _X, int _Y)
    {
        return gameBoard.GetGem(_X, _Y);
    }
    public void SetState(GlobalEnums.GameState _CurrentState)
    {
        currentState = _CurrentState;
    }
    public void DestroyMatches()
    {
        StartCoroutine(DestroyMatchesCo());
    }
    private IEnumerator DestroyMatchesCo()
    {
        List<SC_Gem> matches = new List<SC_Gem>(gameBoard.CurrentMatches);
        if (matches.Count == 0)
            yield break;

        // 1) Let BombService know about this match (bombs inside or adjacent)
        bombService.RegisterMatch(matches, gameBoard);

        // 2) Create a bomb from a 4+ match based on the last user move
        TryCreateBombInMatches(matches);

        // 3) Destroy only regular matched gems (bombs are preserved for delayed explosion)
        foreach (SC_Gem gem in matches)
        {
            if (gem != null && gem.isMatch && !gem.isBomb)
            {
                ScoreCheck(gem);
                DestroyMatchedGemsAt(gem.posIndex);
            }
        }

        yield return new WaitForSeconds(0.2f);

        // 4) Trigger normal cascading and refill. Bombs will explode later.
        StartCoroutine(DecreaseRowCo());
    }

    private IEnumerator DecreaseRowCo()
    {
        // Небольшая пауза перед началом каскада, как и раньше
        yield return new WaitForSeconds(.2f);

        for (int x = 0; x < gameBoard.Width; x++)
        {
            int emptyY = -1; // Позиция пустой клетки

            for (int y = 0; y < gameBoard.Height; y++)
            {
                SC_Gem curGem = gameBoard.GetGem(x, y);

                if (curGem == null)
                {
                    // Нашли первую пустую клетку в колонке
                    if (emptyY == -1)
                        emptyY = y;
                }
                else if (emptyY != -1)
                {
                    // Есть пустота ниже — "роняем" гем в самую нижнюю пустую позицию
                    curGem.posIndex = new Vector2Int(x, emptyY);
                    SetGem(x, emptyY, curGem);
                    SetGem(x, y, null);

                    emptyY++;

                    // Делаем каскад "по одному" — задержка между падениями
                    yield return new WaitForSeconds(SC_GameVariables.Instance.cascadeStepDelay);
                }
            }
        }

        StartCoroutine(FilledBoardCo());
    }
    private IEnumerator HandleBombExplosionsAfterCascade()
    {
        // 1) Wait 1 second AFTER all cascades and refills
        yield return new WaitForSeconds(1.0f);

        var bombsToExplode = bombService.ConsumePendingBombs();
        if (bombsToExplode == null || bombsToExplode.Count == 0)
        {
            // No bombs actually pending – just return control to player
            currentState = GlobalEnums.GameState.move;
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
        StartCoroutine(DecreaseRowCo());
    }

    public void ScoreCheck(SC_Gem gemToCheck)
    {
        gameBoard.Score += gemToCheck.scoreValue;
    }
    private void DestroyMatchedGemsAt(Vector2Int _Pos)
    {
        SC_Gem _curGem = gameBoard.GetGem(_Pos.x, _Pos.y);
        if (_curGem != null)
        {
            Instantiate(_curGem.destroyEffect, new Vector2(_Pos.x, _Pos.y), Quaternion.identity);

            SetGem(_Pos.x, _Pos.y, null);

            if (SC_GemPool.Instance != null)
            {
                SC_GemPool.Instance.Release(_curGem);
            }
            else
            {
                Destroy(_curGem.gameObject);
            }
        }
    }


    private IEnumerator FilledBoardCo()
    {
        yield return new WaitForSeconds(0.5f);

        // теперь refill идёт как корутина со stagger-анимацией
        yield return StartCoroutine(RefillBoardCo());

        yield return new WaitForSeconds(0.5f);
        gameBoard.FindAllMatches();

        if (gameBoard.CurrentMatches.Count > 0)
        {
            yield return new WaitForSeconds(0.5f);
            DestroyMatches();
        }
        else
        {
            // No more matches. Check if there are any bombs waiting to explode.
            if (bombService != null && bombService.HasPendingBombs)
            {
                // handle bombs: wait 1 second, then explode them
                yield return StartCoroutine(HandleBombExplosionsAfterCascade());
            }
            else
            {
                yield return new WaitForSeconds(0.5f);
                currentState = GlobalEnums.GameState.move;
            }
        }
    }

    private IEnumerator RefillBoardCo()
    {
        for (int x = 0; x < gameBoard.Width; x++)
        {
            // Идём по колонке сверху вниз: после каскада пустые клетки будут сверху
            for (int y = 0; y < gameBoard.Height; y++)
            {
                SC_Gem _curGem = gameBoard.GetGem(x, y);
                if (_curGem == null)
                {
                    int gemToUse = Random.Range(0, SC_GameVariables.Instance.gems.Length);
                    int iterations = 0;
                    Vector2Int pos = new Vector2Int(x, y);

                    // Анти-матч логика, как в Setup
                    while (gameBoard.MatchesAt(pos, SC_GameVariables.Instance.gems[gemToUse]) && iterations < 100)
                    {
                        gemToUse = Random.Range(0, SC_GameVariables.Instance.gems.Length);
                        iterations++;
                    }

                    // Спавним новый гем чуть выше клетки (dropHeight уже используется в SpawnGem)
                    SpawnGem(pos, SC_GameVariables.Instance.gems[gemToUse]);

                    // 🔹 Stagger: небольшая задержка между спавном камней
                    yield return new WaitForSeconds(SC_GameVariables.Instance.spawnStaggerDelay);
                }
            }
        }

        CheckMisplacedGems();
    }
    private void CheckMisplacedGems()
    {
        List<SC_Gem> foundGems = new List<SC_Gem>();
        foundGems.AddRange(FindObjectsOfType<SC_Gem>());
        for (int x = 0; x < gameBoard.Width; x++)
        {
            for (int y = 0; y < gameBoard.Height; y++)
            {
                SC_Gem _curGem = gameBoard.GetGem(x, y);
                if (foundGems.Contains(_curGem))
                    foundGems.Remove(_curGem);
            }
        }

        foreach (SC_Gem g in foundGems)
            Destroy(g.gameObject);
    }
    public void FindAllMatches()
    {
        gameBoard.FindAllMatches();
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
        Transform innerTransform = gem.transform.Find(BombInnerSpriteName);
        SpriteRenderer innerSR;

        if (innerTransform == null)
        {
            GameObject innerObj = new GameObject(BombInnerSpriteName);
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

    #endregion
}
