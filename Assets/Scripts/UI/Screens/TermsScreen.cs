using UnityEngine;
using UI;
using System;

public class TermsScreen : UIScreen
{
    GameManager _gameManager;

    public void Init(GameManager gameManager)
    {
        _gameManager = gameManager;
    }

    public void Accept()
    {
        _gameManager.AcceptTerms();
    }
}
