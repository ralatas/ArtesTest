using System.Collections;
using UnityEngine;

/// <summary>
/// Handles user input for gem swaps: interpreting swipe direction,
/// performing swaps on the board and validating moves (reverting if no match).
/// Extracted from SC_Gem to keep the gem component focused on visuals/state.
/// </summary>
public interface IInputService
{
    /// <summary>
    /// Handles a completed swipe on a gem.
    /// </summary>
    /// <param name="gem">The gem that was swiped.</param>
    /// <param name="firstTouchPosition">World position where swipe started.</param>
    /// <param name="finalTouchPosition">World position where swipe ended.</param>
    void HandleSwipe(SC_Gem gem, Vector2 firstTouchPosition, Vector2 finalTouchPosition);
}

public class InputService : IInputService
{
    private readonly SC_GameLogic gameLogic;

    public InputService(SC_GameLogic gameLogic)
    {
        this.gameLogic = gameLogic;
    }

    public void HandleSwipe(SC_Gem gem, Vector2 firstTouchPosition, Vector2 finalTouchPosition)
    {
        if (gem == null || gameLogic == null)
            return;

        if (gameLogic.CurrentState != GlobalEnums.GameState.move)
            return;

        // Ignore very short swipes
        if (Vector3.Distance(firstTouchPosition, finalTouchPosition) <= 0.5f)
            return;

        // Calculate swipe angle in degrees
        float swipeAngle = Mathf.Atan2(
            finalTouchPosition.y - firstTouchPosition.y,
            finalTouchPosition.x - firstTouchPosition.x
        ) * 180 / Mathf.PI;

        Vector2Int previousPos = gem.posIndex;
        SC_Gem otherGem = null;
        Vector2Int posIndex = gem.posIndex;

        // Decide direction by angle, same logic as before but moved here
        if (swipeAngle < 45 && swipeAngle > -45 && posIndex.x < SC_GameVariables.Instance.rowsSize - 1)
        {
            otherGem = gameLogic.GetGem(posIndex.x + 1, posIndex.y);
            if (otherGem == null) return;

            otherGem.posIndex = new Vector2Int(otherGem.posIndex.x - 1, otherGem.posIndex.y);
            posIndex = new Vector2Int(posIndex.x + 1, posIndex.y);
        }
        else if (swipeAngle > 45 && swipeAngle <= 135 && posIndex.y < SC_GameVariables.Instance.colsSize - 1)
        {
            otherGem = gameLogic.GetGem(posIndex.x, posIndex.y + 1);
            if (otherGem == null) return;

            otherGem.posIndex = new Vector2Int(otherGem.posIndex.x, otherGem.posIndex.y - 1);
            posIndex = new Vector2Int(posIndex.x, posIndex.y + 1);
        }
        else if (swipeAngle < -45 && swipeAngle >= -135 && posIndex.y > 0)
        {
            otherGem = gameLogic.GetGem(posIndex.x, posIndex.y - 1);
            if (otherGem == null) return;

            otherGem.posIndex = new Vector2Int(otherGem.posIndex.x, otherGem.posIndex.y + 1);
            posIndex = new Vector2Int(posIndex.x, posIndex.y - 1);
        }
        else if ((swipeAngle > 135 || swipeAngle < -135) && posIndex.x > 0)
        {
            // Otherwise for swipeAngle > 135 and posIndex.x == 0, GetGem(-1, y) would be called.
            otherGem = gameLogic.GetGem(posIndex.x - 1, posIndex.y);
            if (otherGem == null) return;

            otherGem.posIndex = new Vector2Int(otherGem.posIndex.x + 1, otherGem.posIndex.y);
            posIndex = new Vector2Int(posIndex.x - 1, posIndex.y);
        }

        if (otherGem == null)
            return;

        // Apply new position to the main gem
        gem.posIndex = posIndex;

        // Update board references
        gameLogic.SetGem(gem.posIndex.x, gem.posIndex.y, gem);
        gameLogic.SetGem(otherGem.posIndex.x, otherGem.posIndex.y, otherGem);

        // Remember last swap (for bomb creation etc.)
        gameLogic.RegisterSwap(gem, otherGem);

        // Run move validation coroutine via GameLogic MonoBehaviour
        gameLogic.StartCoroutine(CheckMoveCo(gem, otherGem, previousPos));
    }

    private IEnumerator CheckMoveCo(SC_Gem gem, SC_Gem otherGem, Vector2Int previousPos)
    {
        gameLogic.SetState(GlobalEnums.GameState.wait);

        yield return new WaitForSeconds(.5f);
        gameLogic.FindAllMatches();

        if (otherGem != null)
        {
            if (!gem.isMatch && !otherGem.isMatch)
            {
                // No match — revert swap
                Vector2Int currentPos = gem.posIndex;

                otherGem.posIndex = currentPos;
                gem.posIndex = previousPos;

                gameLogic.SetGem(gem.posIndex.x, gem.posIndex.y, gem);
                gameLogic.SetGem(otherGem.posIndex.x, otherGem.posIndex.y, otherGem);

                yield return new WaitForSeconds(.5f);
                gameLogic.SetState(GlobalEnums.GameState.move);
            }
            else
            {
                // Valid move — destroy matches
                gameLogic.DestroyMatches();
            }
        }
    }
}
