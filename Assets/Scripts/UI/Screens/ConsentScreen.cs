using System.Collections;
using UnityEngine;
using UI;
using System;

public class ConsentScreen : UIScreen
{
    GameManager _gameManager;

    public void Init(GameManager gameManager)
    {
        _gameManager = gameManager;
    }

    public void Accept(bool accept) 
    {
        _gameManager.AcceptConsent(accept);
    }
}
