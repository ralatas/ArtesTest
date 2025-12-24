using System.Collections.Generic;
using UnityEngine;
using Lib.SimpleJSON;

[CreateAssetMenu(fileName = "GirlSO", menuName = "ScriptableObjects/GirlSO", order = 1)]
class GirlSO: ScriptableObject
{
    [SerializeField] private string girlName;
    [SerializeField] private Sprite girlIcon;
    [SerializeField] private Sprite girlGameFail;
    [SerializeField] private Sprite girlGameNeutral;
    [SerializeField] private List<Sprite> girlGameStages;
    [SerializeField] private List<Sprite> girlRewardStages;
    [SerializeField] private TextAsset girlChat;

    public string GirlName => girlName;
    public Sprite GirlIcon => girlIcon;
    public Sprite GirlGameFail => girlGameFail;
    public Sprite GirlGameNeutral => girlGameNeutral;
    public List<Sprite>  GirlGameStages => girlGameStages;
    public List<Sprite>  GirlRewardStages => girlRewardStages;
    public TextAsset  GirlChat => girlChat;
}