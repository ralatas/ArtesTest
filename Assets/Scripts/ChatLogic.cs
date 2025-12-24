using System;
using System.Collections;
using System.Collections.Generic;
using Lib.SimpleJSON;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChatLogic : MonoBehaviour
{
    [SerializeField] private GirlSO girlSO;
    [SerializeField] private Image girlStageImage;
    [SerializeField] private TextMeshProUGUI chatText;
   // [SerializeField] private ButtonPrefub chatText;

    class DateStage
    {
        public List<string> girlTexts;
        public string question;
        public string answer_good;
        public string answer_bad;
        public string answer_neutral;
    }

    private int activeDate = 0;
    private int activeStage = 0;
    private List<DateStage> DateStages;

    void Awake()
    {
        DateStages = GetDataStage(activeDate);
    }

    void Start()
    {
        initStageView();
    }
    private void initStageView()
    {
        chatText.text = DateStages[activeStage].girlTexts[0];
        if (girlStageImage != null)
            girlStageImage.sprite = girlSO.GirlGameStages[activeStage];

        InstantiateAnswerButtons();
    }
    private void InstantiateAnswerButtons()
    {
        string goodAnswer = DateStages[activeStage].answer_good;
        string badAnswer = DateStages[activeStage].answer_bad;
        string neutralAnswer = DateStages[activeStage].answer_neutral;
        //Instantiate();
    }
    private void UpChangeStage()
    {
        activeStage++;
        initStageView();
    }
    private void DownChangeStage()
    {
        activeStage--;
        initStageView();
    }


    private void UpdateChatForStage()
    {
        
    }

    /// <summary>
    /// Возвращает объект свидания по индексу из массива dates (0-based).
    /// Например, GetDateStageByIndex(1, 2) вернет Dates[1] Stage[1].
    /// </summary>
    private JSONArray GetDateStages(int dateIndex)
    {
        JSONNode chatData = JSON.Parse(girlSO.GirlChat.text);
        if (chatData == null)
            return null;

        JSONArray dates = chatData["dates"] as JSONArray;

        if (dates == null || dateIndex < 0 || dateIndex >= dates.Count)
            return null;

        JSONArray stages = dates[dateIndex]["stages"] as JSONArray;

        if (stages == null)
            return null;

        return stages;
    }
    private List<DateStage> GetDataStage(int dateIndex)
    {
        List<DateStage> dateStages = new List<DateStage>();
        JSONArray stages = GetDateStages(dateIndex);
        if (stages == null) return null;

        for (int i = 0; i < stages.Count; i++)
        {
            JSONNode stageNode = stages[i];
            DateStage dateStage = new DateStage();
            dateStage.question = GetKeyByDateStage(stageNode, "question");
            dateStage.answer_good = GetKeyByDateStage(stageNode, "answer_good");
            dateStage.answer_bad = GetKeyByDateStage(stageNode, "answer_bad");
            dateStage.answer_neutral = GetKeyByDateStage(stageNode, "answer_neutral");
            JSONArray _girlTexts = stageNode["texts"] as JSONArray;
            List<string> girlTexts = new List<string>();
            if (girlTexts != null)
            {
                for (int j = 0; j < _girlTexts.Count; j++)
                {   
                    girlTexts.Add(_girlTexts[j]);
                }
                dateStage.girlTexts = girlTexts;
            }
            dateStages.Add(dateStage);
        }

        return dateStages;
    }
    private string GetKeyByDateStage(JSONNode stageNode, string key)
    {
        string _text = "";

        if (stageNode != null)
        {
            JSONNode text = stageNode[key];
            if(text != null)
            {
                _text = text;
            }

        }
        return _text;
    }
}
