using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Zenject;

public class SC_GameLogic : MonoBehaviour
{
    [SerializeField] private Transform gemsHolder;
    [SerializeField] private TextMeshProUGUI scoreText;
    private float displayScore = 0;
    private GameBoard gameBoard;
    private IBombService bombService;
    private IScoreService scoreService;
    private IGemSpawnService gemSpawnService;
    private IBoardRefillService boardRefillService;
    private IInputService inputService;
    private IHintService hintService;
    private IMatchResolver matchResolver;

    private GlobalEnums.GameState currentState = GlobalEnums.GameState.move;
    public GlobalEnums.GameState CurrentState { get { return currentState; } }
    private SC_Gem lastMovedGemA;
    private SC_Gem lastMovedGemB;

    public IInputService InputService => inputService;
    public SC_Gem LastMovedGemA => lastMovedGemA;
    public SC_Gem LastMovedGemB => lastMovedGemB;
    public Transform GemsHolder => gemsHolder;
    public TextMeshProUGUI ScoreText => scoreText;
    public GameBoard Board => gameBoard;

    [Inject]
    public void Construct(
        GameBoard gameBoard,
        IBombService bombService,
        IScoreService scoreService,
        IGemSpawnService gemSpawnService,
        IBoardRefillService boardRefillService,
        IInputService inputService,
        IHintService hintService,
        IMatchResolver matchResolver)
    {
        this.gameBoard = gameBoard;
        this.bombService = bombService;
        this.scoreService = scoreService;
        this.gemSpawnService = gemSpawnService;
        this.boardRefillService = boardRefillService;
        this.inputService = inputService;
        this.hintService = hintService;
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
        ScoreText.text = displayScore.ToString("0");
        hintService?.Tick(Time.deltaTime, currentState);
    }
    #endregion

    #region Logic
    private void Init()
    {
        gemSpawnService.FillBoard(this, gemsHolder);
    }

    public void StartGame()
    {
        int initialScore = scoreService != null ? scoreService.Score : gameBoard.Score;
        ScoreText.text = initialScore.ToString("0");
    }

    public void RegisterSwap(SC_Gem first, SC_Gem second)
    {
        lastMovedGemA = first;
        lastMovedGemB = second;
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
        if (bomb == null || !bomb.IsBomb)
            return;

        hintService?.RegisterActivity();

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

                if (target.IsBomb && !visitedBombs.Contains(target))
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
