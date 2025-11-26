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

        if (currentMatches.Count > 0)
            currentMatches = currentMatches.Distinct().ToList();

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

