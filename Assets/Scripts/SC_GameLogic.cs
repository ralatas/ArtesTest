using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Zenject;

public class SC_GameLogic : MonoBehaviour
{
    private Dictionary<string, GameObject> unityObjects;
    private float displayScore = 0;
    private GameBoard gameBoard;
    private IBombService bombService;
    private IScoreService scoreService;
    private IBoardRefillService boardRefillService;
    private IInputService inputService;
    private IMatchResolver matchResolver;

    private GlobalEnums.GameState currentState = GlobalEnums.GameState.move;
    public GlobalEnums.GameState CurrentState { get { return currentState; } }
    private SC_Gem lastMovedGemA;
    private SC_Gem lastMovedGemB;

    public IInputService InputService => inputService;
    public SC_Gem LastMovedGemA => lastMovedGemA;
    public SC_Gem LastMovedGemB => lastMovedGemB;

    public const string BombInnerSpriteName = "BombInnerSprite";

    [Inject]
    public void Construct(
        GameBoard gameBoard,
        IBombService bombService,
        IScoreService scoreService,
        IBoardRefillService boardRefillService,
        IInputService inputService,
        IMatchResolver matchResolver)
    {
        this.gameBoard = gameBoard;
        this.bombService = bombService;
        this.scoreService = scoreService;
        this.boardRefillService = boardRefillService;
        this.inputService = inputService;
        this.matchResolver = matchResolver;
    }

    #region MonoBehaviour
    private void Start()
    {
        Init();
        StartGame();
    }

    private void Update()
    {
        float targetScore = scoreService != null ? scoreService.Score : gameBoard.Score;
        displayScore = Mathf.Lerp(displayScore, targetScore, SC_GameVariables.Instance.scoreSpeed * Time.deltaTime);
        unityObjects["Txt_Score"].GetComponent<TextMeshProUGUI>().text = displayScore.ToString("0");
    }
    #endregion

    #region Logic
    private void Init()
    {
        unityObjects = new Dictionary<string, GameObject>();
        GameObject[] _obj = GameObject.FindGameObjectsWithTag("UnityObject");
        foreach (GameObject g in _obj)
            unityObjects.Add(g.name, g);
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
        int initialScore = scoreService != null ? scoreService.Score : gameBoard.Score;
        unityObjects["Txt_Score"].GetComponent<TextMeshProUGUI>().text = initialScore.ToString("0");
    }

    public void SpawnGem(Vector2Int _Position, SC_Gem _GemToSpawn)
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
        SC_Gem prefab = gemInstance.prefabReference != null ? gemInstance.prefabReference : _GemToSpawn;
        if (prefab != null)
        {
            gemInstance.type = prefab.type;
            gemInstance.scoreValue = prefab.scoreValue;
            gemInstance.destroyEffect = prefab.destroyEffect;
            gemInstance.blastSize = prefab.blastSize;
        }

        gemInstance.isBomb = false;
        gemInstance.isRocket = false;
        gemInstance.rocketDirection = GlobalEnums.RocketDirection.None;
        gemInstance.baseType = gemInstance.type;

        // Reset main visual from prefab (in case this was a bomb before)
        var gemSR = gemInstance.GetComponent<SpriteRenderer>();
        SpriteRenderer prefabSR = null;
        if (gemInstance.prefabReference != null)
            prefabSR = gemInstance.prefabReference.GetComponent<SpriteRenderer>();

        if (gemSR != null && prefabSR != null)
        {
            gemSR.sprite = prefabSR.sprite;
            gemSR.color = prefabSR.color;
        }

        // Hide/remove inner bomb sprite if it exists
        Transform innerTransform = gemInstance.transform.Find(BombInnerSpriteName);
        if (innerTransform != null)
        {
            var innerSR = innerTransform.GetComponent<SpriteRenderer>();
            if (innerSR != null)
            {
                innerSR.sprite = null;
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

    public void SetGem(int _X, int _Y, SC_Gem _Gem)
    {
        gameBoard.SetGem(_X, _Y, _Gem);
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
        if (matchResolver != null)
            StartCoroutine(matchResolver.DestroyMatchesCo());
    }

    public void FindAllMatches()
    {
        gameBoard.FindAllMatches();
    }

    public void TriggerBomb(SC_Gem bomb)
    {
        if (bomb == null || !bomb.isBomb)
            return;

        // Royal Match-like behavior: bomb can be activated only when the board is ready for input.
        if (currentState != GlobalEnums.GameState.move)
            return;

        currentState = GlobalEnums.GameState.wait;
        StartCoroutine(TriggerBombCo(bomb));
    }

    private IEnumerator TriggerBombCo(SC_Gem startBomb)
    {
        // Collect chain-reaction bombs and all affected gems.
        var bombsQueue = new Queue<SC_Gem>();
        var visitedBombs = new HashSet<SC_Gem>();
        var destroySet = new HashSet<SC_Gem>();

        bombsQueue.Enqueue(startBomb);
        visitedBombs.Add(startBomb);

        while (bombsQueue.Count > 0)
        {
            SC_Gem bomb = bombsQueue.Dequeue();
            destroySet.Add(bomb);

            foreach (var target in bombService.GetExplosionTargets(bomb, gameBoard))
            {
                if (target == null)
                    continue;

                destroySet.Add(target);

                if (target.isBomb && !visitedBombs.Contains(target))
                {
                    visitedBombs.Add(target);
                    bombsQueue.Enqueue(target);
                }
            }
        }

        // Destroy affected gems (including bombs) and then cascade.
        yield return StartCoroutine(matchResolver.DestroyGemsCo(destroySet));
        StartCascade();
    }

    public void StartCascade()
    {
        StartCoroutine(CascadeAndResolveCo());
    }

    private IEnumerator CascadeAndResolveCo()
    {
        if (boardRefillService == null)
            yield break;

        // Run cascade + refill via dedicated service
        yield return StartCoroutine(boardRefillService.CascadeAndRefillCo());

        // After refill, keep original timing before checking for new matches
        yield return new WaitForSeconds(0.5f);

        gameBoard.FindAllMatches();

        if (gameBoard.CurrentMatches.Count > 0)
        {
            yield return new WaitForSeconds(0.5f);
            DestroyMatches();
        }
        else
        {
            // No more matches.
            yield return new WaitForSeconds(0.5f);
            currentState = GlobalEnums.GameState.move;
            
        }
    }
    #endregion
}
