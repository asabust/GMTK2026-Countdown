using System;
using System.Collections.Generic;
using System.IO;
using Game.Runtime.Core;
using Game.Runtime.Core.Attributes;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Serialization;
using EventHandler = Game.Runtime.Core.EventHandler;


public class GameManager : Singleton<GameManager>
{

    [SceneName] public string firstGameScene;
    [SceneName] public string titleScene;
    public GamePhase CurrentPhase { get; private set; }

    public void SetGamePhase(GamePhase newPhase)
    {
        if (CurrentPhase == newPhase) return;
        CurrentPhase = newPhase;
        EventHandler.CallGamePhaseChangedEvent(newPhase);

        if (newPhase != GamePhase.Gameplay)
        {
            UIManager.Instance?.Close<GameHUDPanel>();
        }
    }

    public bool IsGameplay => CurrentPhase == GamePhase.Gameplay;
    
    private void Start()
    {
        GameTitle(); //从标题界面开始
        //StartNewGame();
    }

    private void OnEnable()
    {
        EventHandler.AfterSceneLoadEvent += OnAfterSceneLoad;
    }

    private void OnDisable()
    {
        EventHandler.AfterSceneLoadEvent -= OnAfterSceneLoad;
    }

    /// <summary>
    /// 开始新游戏
    /// </summary>
    public void StartNewGame()
    {
        NumberResource.Instance?.ResetForNewRun();
        SetGamePhase(GamePhase.Gameplay);
        TransitionManager.Instance.TransitionTo(firstGameScene);
    }

    /// <summary>
    /// 游戏开始界面
    /// </summary>
    public void GameTitle()
    {
        SetGamePhase(GamePhase.GameTitle);
        TransitionManager.Instance.Transition(string.Empty, titleScene);
    }


    /// <summary>
    /// 退出游戏
    /// </summary>
    public void QuitGame()
    {
        Application.Quit();
    }

    private void OnAfterSceneLoad(string sceneName)
    {
        if (CurrentPhase == GamePhase.Gameplay && sceneName == firstGameScene)
        {
            UIManager.Instance?.Open<GameHUDPanel>();
        }
    }
}
