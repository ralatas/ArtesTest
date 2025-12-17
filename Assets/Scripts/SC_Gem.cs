using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SC_Gem : MonoBehaviour
{
    [HideInInspector]
    public Vector2Int posIndex;

    [HideInInspector]
    public SC_Gem prefabReference;

    [HideInInspector]
    public GlobalEnums.GemType baseType; // the group color used for matching (for regular gems: same as type).

    [HideInInspector]
    public bool isBomb = false;
    [HideInInspector]
    public bool isRocket = false;
    [HideInInspector]
    public GlobalEnums.RocketDirection rocketDirection = GlobalEnums.RocketDirection.None;

    private Vector2 firstTouchPosition;
    private Vector2 finalTouchPosition;
    private bool mousePressed;

    public GlobalEnums.GemType type;
    public bool isMatch = false;
    public GameObject destroyEffect;
    public int scoreValue = 10;

    public int blastSize = 1;
    private SC_GameLogic scGameLogic;

    private void Update()
    {
        // Smooth movement towards logical board position
        if (Vector2.Distance(transform.position, posIndex) > 0.01f)
        {
            transform.position = Vector2.MoveTowards(
                transform.position,
                posIndex,
                SC_GameVariables.Instance.gemSpeed * Time.deltaTime
            );
        }
        else
        {
            transform.position = new Vector3(posIndex.x, posIndex.y, 0);
            scGameLogic.SetGem(posIndex.x, posIndex.y, this);
        }

        // Handle mouse/touch release
        if (mousePressed && Input.GetMouseButtonUp(0))
        {
            mousePressed = false;
            if (scGameLogic != null && scGameLogic.CurrentState == GlobalEnums.GameState.move)
            {
                finalTouchPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                HandleSwipe();
            }
        }
    }

    public void SetupGem(SC_GameLogic _ScGameLogic, Vector2Int _Position)
    {
        posIndex = _Position;
        scGameLogic = _ScGameLogic;
    }

    private void OnMouseDown()
    {
        if (scGameLogic != null && scGameLogic.CurrentState == GlobalEnums.GameState.move)
        {
            firstTouchPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePressed = true;
        }
    }
    private void OnMouseUp()
    {
        if (scGameLogic == null)
            return;

        if (scGameLogic.CurrentState != GlobalEnums.GameState.move)
            return;

        // Tap-to-explode for bombs (no swipe)
        if (isBomb)
        {
            Vector2 releasePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            float dx = Mathf.Abs(releasePos.x - firstTouchPosition.x);
            float dy = Mathf.Abs(releasePos.y - firstTouchPosition.y);

            if (dx < SC_GameVariables.Instance.swipeResist && dy < SC_GameVariables.Instance.swipeResist)
            {
                scGameLogic.TriggerBomb(this);
            }
        }
    }

    /// <summary>
    /// Delegates swipe handling to the input service.
    /// </summary>
    private void HandleSwipe()
    {
        if (scGameLogic == null || scGameLogic.InputService == null)
            return;

        // Ignore tiny swipes
        if (Vector3.Distance(firstTouchPosition, finalTouchPosition) <= 0.5f)
            return;

        scGameLogic.InputService.HandleSwipe(this, firstTouchPosition, finalTouchPosition);
    }
}
