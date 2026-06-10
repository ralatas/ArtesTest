using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuView : MonoBehaviour
{
    [SerializeField] private Button playButton;
    private void OnEnable()
    {
        playButton.onClick.AddListener(GoToGame);
    }

    // Update is called once per frame
    private void OnDisable()
    {
        playButton.onClick.RemoveAllListeners();
    }

    void GoToGame()
    {
        SceneManager.LoadSceneAsync("Scene_Game");
    }
}
