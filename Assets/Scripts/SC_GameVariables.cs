using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SC_GameVariables : MonoBehaviour
{
    public GameObject bgTilePrefabs;
    public SC_Gem bomb;
    public SC_Gem[] gems;
    public float bonusAmount = 0.5f;
    public float bombChance = 2f;
    public SC_Gem rocketHorizontal;
    public SC_Gem rocketVertical;
    public SC_Gem discoBall;
    public int dropHeight = 0;
    public float gemSpeed;
    public float scoreSpeed = 5;
    public float bombNeighborDelay = 0.2f; // delay before destroying neighbors
    public float bombDestroyDelay = 0.2f;  // delay before destroying the bomb itself

    [Header("Cascade Timings")]
    [Tooltip("Delay before cascade starts after matches/bombs")]
    public float cascadeStartDelay = 0.15f;

    [Tooltip("Delay between cascade steps inside a column")]
    public float cascadeStepDelay = 0.05f;

    [Tooltip("Delay between spawning new gems")]
    public float spawnStaggerDelay = 0.04f;

    [Tooltip("Minimum swipe distance required to register a swipe")]
    public float swipeResist = 1f;

    [HideInInspector]
    public int rowsSize = 7;
    [HideInInspector]
    public int colsSize = 7;

    #region Singleton

    static SC_GameVariables instance;
    public static SC_GameVariables Instance
    {
        get
        {
            if (instance == null)
                instance = GameObject.Find("SC_GameVariables").GetComponent<SC_GameVariables>();

            return instance;
        }
    }

    #endregion
}
