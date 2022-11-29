using System;
using UnityEngine;
using UnityEngine.UI;
using UI;

public class LevelFailedScreen : UIScreen
{
    [SerializeField] Button restartButton;
    [SerializeField] Text failedMessage;

    GameManager _gameManager;

    public void Init(GameManager gameManager)
    {
        _gameManager = gameManager;
    }

    public override void Open()
    {
        base.Open();

        isSkipLevel = false;

        restartButton.interactable = true;
    }

    bool isSkipLevel;

    public void Skip() 
    {
        if (isSkipLevel)
            return;

        isSkipLevel = true;

        Advertisement.Show("Interstitial_restart", success =>
        {
            Invoke("SkipLevel", 0.25f);
        });
    }

    private void SkipLevel() 
    {
        _gameManager.LeaveGame();
    }

    public void FailedMessage(string text) 
    {
        failedMessage.text = text;
    }

    private void OnRewarded()
    {
        _gameManager.NextLevel();
    }

    private void OnRewardedFailed()
    {
        _gameManager.Restart();
    }
}

