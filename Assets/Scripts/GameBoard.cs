using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameBoard
{
    #region Variables

    private int height = 0;
    public int Height { get { return height; } }

    private int width = 0;
    public int Width { get { return width; } }
  
    private SC_Gem[,] allGems;
  //  public Gem[,] AllGems { get { return allGems; } }

    private int score = 0;
    public int Score 
    {
        get { return score; }
        set { score = value; }
    }

    private List<SC_Gem> currentMatches = new List<SC_Gem>();
    public List<SC_Gem> CurrentMatches { get { return currentMatches; } }
    #endregion

    public GameBoard(int _Width, int _Height)
    {
        height = _Height;
        width = _Width;
        allGems = new SC_Gem[width, height];
    }

    private GlobalEnums.GemType GetMatchType(SC_Gem gem)
    {
        // Fallback: if baseType is not set, use type
        return gem.baseType != 0 ? gem.baseType : gem.type;
    }

    public bool MatchesAt(Vector2Int positionToCheck, SC_Gem gemToCheck)
    {
        if (gemToCheck == null)
            return false;

        // Bombs never participate in color matches
        if (gemToCheck.IsBomb)
            return false;

        int x = positionToCheck.x;
        int y = positionToCheck.y;

        GlobalEnums.GemType matchType = GetMatchType(gemToCheck);

        bool IsSameMatchType(int cx, int cy)
        {
            if (cx < 0 || cx >= width || cy < 0 || cy >= height)
                return false;

            SC_Gem g = allGems[cx, cy];
            return g != null && GetMatchType(g) == matchType;
        }

        foreach (var offsets in MatchTriplets)
        {
            int x1 = x + offsets[0].x;
            int y1 = y + offsets[0].y;
            int x2 = x + offsets[1].x;
            int y2 = y + offsets[1].y;

            if (IsSameMatchType(x1, y1) && IsSameMatchType(x2, y2))
                return true;
        }

        // Square (2x2) match that includes the tested position
        Vector2Int[] squareOffsets =
        {
            new Vector2Int(0, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, -1),
            new Vector2Int(-1, -1)
        };

        foreach (var offset in squareOffsets)
        {
            int startX = x + offset.x;
            int startY = y + offset.y;

            if (startX < 0 || startY < 0 || startX + 1 >= width || startY + 1 >= height)
                continue;

            if (FormsSquareMatch(startX, startY, positionToCheck, gemToCheck, matchType))
                return true;
        }

        return false;
    }

    public void SetGem(int _X, int _Y, SC_Gem _Gem)
    {
        allGems[_X, _Y] = _Gem;
    }
    public SC_Gem GetGem(int _X,int _Y)
    {
       return allGems[_X, _Y];
    }

    public void FindAllMatches()
    {
        currentMatches.Clear();

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                SC_Gem currentGem = allGems[x, y];
                if (currentGem != null)
                {
                    if (currentGem.IsBomb)
                        continue;
                }
                if (currentGem != null)
                {
                    if (x > 0 && x < width - 1)
                    {
                        SC_Gem leftGem = allGems[x - 1, y];
                        SC_Gem rightGem = allGems[x + 1, y];
                        //checking no empty spots
                        if (leftGem != null && rightGem != null)
                        {
                            //Match
                            if (GetMatchType(leftGem) == GetMatchType(currentGem) && 
                                GetMatchType(rightGem) == GetMatchType(currentGem))
                            {
                                currentGem.isMatch = true;
                                leftGem.isMatch = true;
                                rightGem.isMatch = true;
                                currentMatches.Add(currentGem);
                                currentMatches.Add(leftGem);
                                currentMatches.Add(rightGem);
                            }
                        }
                    }

                    if (y > 0 && y < height - 1)
                    {
                        SC_Gem aboveGem = allGems[x, y - 1];
                        SC_Gem bellowGem = allGems[x, y + 1];
                        //checking no empty spots
                        if (aboveGem != null && bellowGem != null)
                        {
                            //Match
                            if (GetMatchType(aboveGem) == GetMatchType(currentGem) &&
                                GetMatchType(bellowGem) == GetMatchType(currentGem))
                            {
                                currentGem.isMatch = true;
                                aboveGem.isMatch = true;
                                bellowGem.isMatch = true;
                                currentMatches.Add(currentGem);
                                currentMatches.Add(aboveGem);
                                currentMatches.Add(bellowGem);
                            }
                        }
                    }
                }
            }

        FindSquareMatches();

        if (currentMatches.Count > 0)
            currentMatches = currentMatches.Distinct().ToList();

    }

    private void FindSquareMatches()
    {
        for (int x = 0; x < width - 1; x++)
        {
            for (int y = 0; y < height - 1; y++)
            {
                SC_Gem g00 = allGems[x, y];
                SC_Gem g10 = allGems[x + 1, y];
                SC_Gem g01 = allGems[x, y + 1];
                SC_Gem g11 = allGems[x + 1, y + 1];

                if (g00 == null || g10 == null || g01 == null || g11 == null)
                    continue;

                if (g00.IsBomb || g10.IsBomb || g01.IsBomb || g11.IsBomb)
                    continue;

                GlobalEnums.GemType matchType = GetMatchType(g00);
                if (GetMatchType(g10) != matchType || GetMatchType(g01) != matchType || GetMatchType(g11) != matchType)
                    continue;

                SC_Gem[] square = { g00, g10, g01, g11 };
                foreach (var gem in square)
                {
                    gem.isMatch = true;
                    currentMatches.Add(gem);
                }

                SC_Gem adjacent = FindAdjacentSameType(square, matchType);
                if (adjacent != null)
                {
                    adjacent.isMatch = true;
                    currentMatches.Add(adjacent);
                }
            }
        }
    }

    private SC_Gem FindAdjacentSameType(IEnumerable<SC_Gem> square, GlobalEnums.GemType matchType)
    {
        HashSet<SC_Gem> squareSet = new HashSet<SC_Gem>(square);
        Vector2Int[] dirs =
        {
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1)
        };

        foreach (var gem in square)
        {
            Vector2Int pos = gem.posIndex;
            foreach (var dir in dirs)
            {
                int nx = pos.x + dir.x;
                int ny = pos.y + dir.y;

                if (nx < 0 || ny < 0 || nx >= width || ny >= height)
                    continue;

                SC_Gem neighbor = allGems[nx, ny];
                if (neighbor == null || neighbor.IsBomb || squareSet.Contains(neighbor))
                    continue;

                if (GetMatchType(neighbor) == matchType)
                    return neighbor;
            }
        }

        return null;
    }

    private bool FormsSquareMatch(int startX, int startY, Vector2Int testedPos, SC_Gem testedGem, GlobalEnums.GemType matchType)
    {
        for (int dx = 0; dx <= 1; dx++)
        {
            for (int dy = 0; dy <= 1; dy++)
            {
                int cx = startX + dx;
                int cy = startY + dy;

                SC_Gem g = (testedPos.x == cx && testedPos.y == cy)
                    ? testedGem
                    : allGems[cx, cy];

                if (g == null || g.IsBomb)
                    return false;

                if (GetMatchType(g) != matchType)
                    return false;
            }
        }

        return true;
    }

    public void MarkBombArea(Vector2Int bombPos, int _BlastSize)
    {
        string _print = "";
        for (int x = bombPos.x - _BlastSize; x <= bombPos.x + _BlastSize; x++)
        {
            for (int y = bombPos.y - _BlastSize; y <= bombPos.y + _BlastSize; y++)
            {
                if (x >= 0 && x < width && y >= 0 && y < height)
                {
                    if (allGems[x, y] != null)
                    {
                        _print += "(" + x + "," + y + ")" + System.Environment.NewLine;
                        allGems[x, y].isMatch = true;
                        currentMatches.Add(allGems[x, y]);
                    }
                }
            }
        }
        currentMatches = currentMatches.Distinct().ToList();
    }
    public static readonly Vector2Int[][] MatchTriplets = new Vector2Int[][]
    {
        // Horizontal triplets
        new [] { new Vector2Int(-1, 0), new Vector2Int(-2, 0) },
        new [] { new Vector2Int(-1, 0), new Vector2Int(+1, 0) },
        new [] { new Vector2Int(+1, 0), new Vector2Int(+2, 0) },

        // Vertical triplets
        new [] { new Vector2Int(0, -1), new Vector2Int(0, -2) },
        new [] { new Vector2Int(0, -1), new Vector2Int(0, +1) },
        new [] { new Vector2Int(0, +1), new Vector2Int(0, +2) },
    };
}
