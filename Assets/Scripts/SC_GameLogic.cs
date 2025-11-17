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

    #region MonoBehaviour
    private void Awake()
    {
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

        // Обычный гем по умолчанию — не бомба
        gemInstance.isBomb = false;
        gemInstance.baseType = gemInstance.type;

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

        // 🔹 Шаг 1. Создаём бомбу, если был матч 4+ от последнего хода
        TryCreateBombFromLastMove(matches);

        // Обновляем список после возможного удаления кандидата-бомбы
        List<SC_Gem> bombsToExplode = new List<SC_Gem>();
        foreach (SC_Gem g in matches)
        {
            if (g != null && g.isBomb && g.isMatch)
                bombsToExplode.Add(g);
        }

        // 🔹 Случай без бомб: обычное уничтожение и каскад
        if (bombsToExplode.Count == 0)
        {
            foreach (SC_Gem gem in matches)
            {
                if (gem != null && gem.isMatch)
                {
                    ScoreCheck(gem);
                    DestroyMatchedGemsAt(gem.posIndex);
                }
            }

            yield return new WaitForSeconds(0.2f);
            StartCoroutine(DecreaseRowCo());
            yield break;
        }

        // 🔹 Случай с бомбами: сначала соседи, потом сами бомбы

        // Собираем всех, кого нужно уничтожить как "соседей" — это:
        // - все обычные матч-гемы
        // - соседние к бомбам в радиусе blastSize
        HashSet<SC_Gem> neighborGroup = new HashSet<SC_Gem>();

        foreach (SC_Gem gem in matches)
        {
            if (gem != null && gem.isMatch && !gem.isBomb)
                neighborGroup.Add(gem);
        }

        foreach (SC_Gem bomb in bombsToExplode)
        {
            List<SC_Gem> neighbors = GetBombNeighbors(bomb);
            foreach (SC_Gem n in neighbors)
            {
                if (n != null && n != bomb)
                    neighborGroup.Add(n);
            }
        }

        // Фаза 1: ждём задержку и уничтожаем соседей
        yield return new WaitForSeconds(SC_GameVariables.Instance.bombNeighborDelay);

        foreach (SC_Gem gem in neighborGroup)
        {
            if (gem != null)
            {
                ScoreCheck(gem);
                DestroyMatchedGemsAt(gem.posIndex);
            }
        }

        // Фаза 2: ждём задержку и уничтожаем сами бомбы
        yield return new WaitForSeconds(SC_GameVariables.Instance.bombDestroyDelay);

        foreach (SC_Gem bomb in bombsToExplode)
        {
            if (bomb != null)
            {
                ScoreCheck(bomb);
                DestroyMatchedGemsAt(bomb.posIndex);
            }
        }

        // Только после уничтожения бомб запускаем каскад
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
            yield return new WaitForSeconds(0.5f);
            currentState = GlobalEnums.GameState.move;
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

        // Цветовая группа сохраняется
        // gem.baseType уже = цвету матча
        // при желании можно ещё поменять визуал
        var bombSR = bombTemplate.GetComponent<SpriteRenderer>();
        var gemSR = gem.GetComponent<SpriteRenderer>();
        if (bombSR != null && gemSR != null)
        {
            gemSR.sprite = bombSR.sprite;
        }

        // Сбросим флаг матчей на этом ходу
        gem.isMatch = false;
    }
    private void TryCreateBombFromLastMove(List<SC_Gem> currentMatches)
    {
        SC_Gem candidate = FindBombCandidate(lastMovedGemA, currentMatches);
        if (candidate == null)
            candidate = FindBombCandidate(lastMovedGemB, currentMatches);

        if (candidate == null)
            return;

        MakeGemBomb(candidate);

        // Убираем из списка уничтожаемых
        currentMatches.Remove(candidate);
        gameBoard.CurrentMatches.Remove(candidate);
    }

    private SC_Gem FindBombCandidate(SC_Gem gem, List<SC_Gem> currentMatches)
    {
        if (gem == null) return null;
        if (!gem.isMatch) return null;
        if (gem.isBomb) return null; // уже бомба

        // Берём связную группу матча вокруг этой фишки
        List<SC_Gem> group = GetConnectedMatchGroupFrom(gem);
        if (group.Count >= 4)
        {
            return gem;
        }

        return null;
    }
    private List<SC_Gem> GetBombNeighbors(SC_Gem bomb)
    {
        List<SC_Gem> result = new List<SC_Gem>();
        if (bomb == null) return result;

        Vector2Int center = bomb.posIndex;

        // 1) Все ближайшие соседи (8 направлений вокруг бомбы)
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0)
                    continue; // пропускаем саму бомбу

                int nx = center.x + dx;
                int ny = center.y + dy;

                if (nx < 0 || nx >= gameBoard.Width || ny < 0 || ny >= gameBoard.Height)
                    continue;

                SC_Gem g = gameBoard.GetGem(nx, ny);
                if (g != null && !result.Contains(g))
                    result.Add(g);
            }
        }

        // 2) Элементы "через одну" по прямым (крест на 2 клетки)
        // слева и справа
        int[,] offsets2 =
        {
            { -2,  0 },
            {  2,  0 },
            {  0, -2 },
            {  0,  2 }
        };

        for (int i = 0; i < offsets2.GetLength(0); i++)
        {
            int nx = center.x + offsets2[i, 0];
            int ny = center.y + offsets2[i, 1];

            if (nx < 0 || nx >= gameBoard.Width || ny < 0 || ny >= gameBoard.Height)
                continue;

            SC_Gem g = gameBoard.GetGem(nx, ny);
            if (g != null && !result.Contains(g))
                result.Add(g);
        }

        return result;
    }

    #endregion
}
