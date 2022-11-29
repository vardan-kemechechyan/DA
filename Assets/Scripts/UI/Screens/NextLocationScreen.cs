using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UI;
using System.Linq;

public class NextLocationScreen : UIScreen
{
    [SerializeField] Image locationIcon;
    [SerializeField] ClueRenderer clueRenderer;
    [SerializeField] GameObject clueWord;
    [SerializeField] GameObject clueWordLetterPrefab;

    List<ClueWordLetter> clueWordLetters = new List<ClueWordLetter>();

    GameManager _gameManager;

    public void Init(GameManager gameManager)
    {
        _gameManager = gameManager;
    }

    public override void Open()
    {
        base.Open();

        var location = _gameManager.GetNextLocation();

        locationIcon.sprite = location.icons[1];

        UpdateClueWord();
    }

    public override void Close()
    {
        base.Close();
    }

    public void Continue() 
    {
        _gameManager.NextLocation();
    }

    private void UpdateClueWord()
    {
        foreach (var l in clueWordLetters)
            Destroy(l.gameObject);

        clueWordLetters.Clear();

        var location = _gameManager.Location;
        var level = _gameManager.Level + 1;

        var scale = 1.0f;

        var length = location.clue.Length;
        if (length > 11 && length <= 15) scale = 0.7f;
        else if (length > 15) scale = 0.6f;

        clueWord.transform.localScale = new Vector3(scale, scale, 1);

        var letters = location.clue.ToCharArray();

        foreach (var l in letters)
        {
            var letter = Instantiate(clueWordLetterPrefab, clueWord.transform).GetComponent<ClueWordLetter>();

            clueWordLetters.Add(letter);

            letter.Set(l.ToString());
            letter.gameObject.SetActive(true);
        }
    }
}