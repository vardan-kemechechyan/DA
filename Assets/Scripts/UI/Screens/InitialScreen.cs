using UnityEngine;
using UnityEngine.UI;
using UI;
using System;
using System.Collections.Generic;
using System.Linq;

public class InitialScreen : UIScreen
{
    [SerializeField] GameObject menu;
    [SerializeField] GameObject tutorial;

    GameManager _gameManager;

    public void Init(GameManager gameManager)
    {
        _gameManager = gameManager;
    }

    public override void Open()
    {
        base.Open();

        menu.SetActive(true);
        tutorial.SetActive(false);

        _gameManager.PlayMenuMusic(true);
    }

    public void Play()
    {
        if (!tutorial.activeInHierarchy)
        {
            menu.SetActive(false);
            tutorial.SetActive(true);
        }
        else
        {
            _gameManager.SetGameState("Play");
            _gameManager.PlayLocationMusic(true);
        }
    }
}