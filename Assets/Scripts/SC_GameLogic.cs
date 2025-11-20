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
    private MatchResolver matchResolver;
    public const string BombInnerSpriteName = "BombInnerSprite";
    #region MonoBehaviour
    private void Awake()
    {
        bombService = new BombService();
        Init();
        matchResolver = new MatchResolver(this, gameBoard, bombService);
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
    public void StartCascade()
    {
        StartCoroutine(DecreaseRowCo());
    }
    public void SetState(GlobalEnums.GameState _CurrentState)
    {
        currentState = _CurrentState;
    }
    public void DestroyMatches()
    {
        if (matchResolver != null) StartCoroutine(matchResolver.DestroyMatchesCo());
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
            if (bombService != null && bombService.HasPendingBombs && matchResolver != null)
            {
                // handle bombs: wait 1 second, then explode them via MatchResolver
                yield return StartCoroutine(matchResolver.HandleBombExplosionsAfterCascadeCo());
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
    #endregion
}
