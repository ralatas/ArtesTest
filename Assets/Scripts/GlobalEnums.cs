using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlobalEnums : MonoBehaviour
{
    public enum GemType { blue, green, red, yellow, purple, bomb };
    public enum RocketDirection { None, Horizontal, Vertical }
    public enum BombType { None, Area, Rocket, DiscoBall, Helicopter }
    public enum GameState { wait, move }
}
