using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lib.SimpleJSON;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChatLogic : MonoBehaviour
{
    [SerializeField] private GirlSO girlSO;
    [SerializeField] private Image girlStageImage;
    [SerializeField] private TextMeshProUGUI chatText;
    [SerializeField] private GameObject answersContainer;
    [SerializeField] private Button buttonPrefub;

    private Button buttonGood;
    private Button buttonBad;
    private Button buttonNeutral;

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

    async void Start()
    {
        InstantiateAnswerButtons();
        await initStageView();
    }
    private async Task initStageView()
    {  
        ChangeButtonVisibility(false);
        buttonGood.GetComponentInChildren<TextMeshProUGUI>().text = DateStages[activeStage].answer_good;
        buttonBad.GetComponentInChildren<TextMeshProUGUI>().text = DateStages[activeStage].answer_bad;
        buttonNeutral.GetComponentInChildren<TextMeshProUGUI>().text = DateStages[activeStage].answer_neutral;
        
        girlStageImage.sprite = girlSO.GirlGameStages[activeStage];
        await WriteText();
    }
    private async Task WriteText()
    {
        chatText.text = DateStages[activeStage].girlTexts[0];
        await Task.Delay(4000);
        chatText.text = DateStages[activeStage].girlTexts[1];
        await Task.Delay(4000);
        chatText.text = DateStages[activeStage].question;
        ChangeButtonVisibility(true);
    }
    private void InstantiateAnswerButtons()
    {
        buttonGood = Instantiate(buttonPrefub, answersContainer.transform) ;
        buttonGood.onClick.AddListener(async() => await UpChangeStage());

        buttonBad = Instantiate(buttonPrefub, answersContainer.transform);
        buttonBad.onClick.AddListener(async() => await DownChangeStage());
        
        buttonNeutral = Instantiate(buttonPrefub, answersContainer.transform);
        buttonNeutral.onClick.AddListener(async() => await RepeatStage());
       
        ChangeButtonVisibility(false);
    }

    private void ChangeButtonVisibility(bool visibility)
    {
        buttonGood.gameObject.SetActive(visibility);
        buttonBad.gameObject.SetActive(visibility);
        buttonNeutral.gameObject.SetActive(visibility);
    }
    private async Task UpChangeStage()
    {
        chatText.text = "Я дара это слышать";
        await Task.Delay(2000);
        if (activeStage < DateStages.Count)
        {
            activeStage++;
            await initStageView();
        } else
        {
            Debug.Log("Все стадии пройдены!");
        }
        
    }
    private async Task RepeatStage()
    {
        girlStageImage.sprite = girlSO.GirlGameNeutral;
        chatText.text = "Ты уверен в своем выборе? Попробуй еще раз!";
        await Task.Delay(2000);
        girlStageImage.sprite = girlSO.GirlGameStages[activeStage];
        chatText.text = DateStages[activeStage].question;
    }
    private async Task DownChangeStage()
    {
        girlStageImage.sprite = girlSO.GirlGameFail;
        chatText.text = "Мне не понравился твой ответ...";
        await Task.Delay(2000);
        if (activeStage > 0) activeStage--;
        girlStageImage.sprite = girlSO.GirlGameStages[activeStage];
        chatText.text = DateStages[activeStage].question;
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
