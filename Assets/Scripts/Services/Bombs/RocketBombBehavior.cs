using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Clears an entire row or column depending on rocket orientation.
/// Also responsible for configuring a gem into a rocket bomb.
/// </summary>
public class RocketBombBehavior : IBombBehavior
{
    public void MakeBomb(SC_Gem gem, GlobalEnums.RocketDirection direction)
    {
        if (gem == null || (direction != GlobalEnums.RocketDirection.Vertical && direction != GlobalEnums.RocketDirection.Horizontal))
            return;

        SC_Gem rocketTemplate = GetRocketTemplate(direction);
        var templateSR = rocketTemplate != null ? rocketTemplate.GetComponent<SpriteRenderer>() : null;
        var gemSR = gem.GetComponent<SpriteRenderer>();

        gem.type = GlobalEnums.GemType.bomb;
        gem.baseType = GlobalEnums.GemType.bomb;
        gem.bombType = GlobalEnums.BombType.Rocket;
        gem.rocketDirection = direction;
        if (rocketTemplate != null)
        {
            gem.blastSize = rocketTemplate.blastSize;
            gem.destroyEffect = rocketTemplate.destroyEffect;
            gem.scoreValue = rocketTemplate.scoreValue;
        }

        if (gemSR != null && templateSR != null)
        {
            gemSR.sprite = templateSR.sprite;
            gemSR.color = templateSR.color;
        }

        Transform innerTransform = gem.transform.Find(SC_GameLogic.BombInnerSpriteName);
        if (innerTransform != null)
        {
            var innerSR = innerTransform.GetComponent<SpriteRenderer>();
            if (innerSR != null)
            {
                innerSR.sprite = null;
                innerSR.enabled = false;
            }
        }

        gem.isMatch = false;
    }

    public bool CanHandle(SC_Gem bomb)
    {
        return bomb != null
            && bomb.bombType == GlobalEnums.BombType.Rocket
            && bomb.rocketDirection != GlobalEnums.RocketDirection.None;
    }

    public IEnumerable<SC_Gem> GetExplosionTargets(SC_Gem bomb, GameBoard board)
    {
        if (bomb == null || board == null)
            yield break;

        if (bomb.rocketDirection == GlobalEnums.RocketDirection.Vertical)
        {
            int x = bomb.posIndex.x;
            for (int y = 0; y < board.Height; y++)
            {
                SC_Gem g = board.GetGem(x, y);
                if (g != null && g != bomb)
                    yield return g;
            }
        }
        else if (bomb.rocketDirection == GlobalEnums.RocketDirection.Horizontal)
        {
            int y = bomb.posIndex.y;
            for (int x = 0; x < board.Width; x++)
            {
                SC_Gem g = board.GetGem(x, y);
                if (g != null && g != bomb)
                    yield return g;
            }
        }
    }

    private SC_Gem GetRocketTemplate(GlobalEnums.RocketDirection direction)
    {
        if (direction == GlobalEnums.RocketDirection.Vertical && SC_GameVariables.Instance.rocketVertical != null)
            return SC_GameVariables.Instance.rocketVertical;
        if (direction == GlobalEnums.RocketDirection.Horizontal && SC_GameVariables.Instance.rocketHorizontal != null)
            return SC_GameVariables.Instance.rocketHorizontal;
        return SC_GameVariables.Instance.bomb;
    }
}
