using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GirlSO", menuName = "ScriptableObjects/GirlSO", order = 1)]
class GirlSO: ScriptableObject
{
    [SerializeField] private string girlName;
    [SerializeField] private Sprite girlIcon;
    [SerializeField] private Sprite girlGamelFail;
    [SerializeField] private List<Sprite> girlGameStages;
    [SerializeField] private List<Sprite> girlRewardStages;

    public string GirlName => girlName;
    public Sprite GirlIcon => girlIcon;
    public Sprite GirlGamelFail => girlGamelFail;
    public List<Sprite>  GirlGameStages => girlGameStages;
    public List<Sprite>  GirlRewardStages => girlRewardStages;
}