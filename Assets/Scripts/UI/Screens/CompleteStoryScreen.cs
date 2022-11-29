using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UI;

public class CompleteStoryScreen : UIScreen
{
    [SerializeField] AppReview appReview;

    GameManager _gameManager;

    public void Init(GameManager gameManager)
    {
        _gameManager = gameManager;
    }

    public override void Open() 
    {
        base.Open();

        _gameManager.PlayMenuMusic(true);
    }

    public void Continue(bool openStore) 
    {
        _gameManager.State = GameState.None;
        _gameManager.State = GameState.Start;

        if (openStore)
        {
            AnalyticEvents.ReportEvent("no_levels_feedback");
            Application.OpenURL(appReview.GetStoreUrl());
        }    
    }
}
