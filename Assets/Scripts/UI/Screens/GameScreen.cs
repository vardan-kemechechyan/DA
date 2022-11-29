using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UI;

public class GameScreen : UIScreen
{
    [SerializeField] bool enableTestButton;
    [SerializeField] Button testButton;
    [SerializeField] Text tutorialText;
    [SerializeField] GameObject tutorial;
    [SerializeField] Animator tutorialAnimator;

    [SerializeField] ParticleSystem[] giftLevelConfetti;
    [SerializeField] GameObject giftLevelTitle;
    [SerializeField] GameObject progressBar;
    [SerializeField] GameObject progressItemPrefab;

    [SerializeField] Text floatingMessage;
    [SerializeField] Animator floatingMessageAnimator;

    [SerializeField] Animation keyCounterAnimation;

    List<Image> progressItems = new List<Image>();

    [SerializeField] Color[] progressItemColors;

    [SerializeField] RectTransform progressRectTransform;
    [SerializeField] ContentSizeFitter sizeFitter;
    [SerializeField] HorizontalLayoutGroup layoutGroup;

    [SerializeField] CursorTrail swipeRenderer;

    [SerializeField] GameObject keyCounter;
    [SerializeField] GameObject keyPrefab;
    [SerializeField] Sprite[] keySprites;

    List<Image> keyItems = new List<Image>();

    SwipeDirection tutorialDirection;

    public bool IsTutorialOpen() 
    {
        return tutorial.activeInHierarchy;
    }

    GameManager _gameManager;

    public void Init(GameManager gameManager)
    {
        _gameManager = gameManager;
    }

    public override void Open()
    {
        base.Open();

        testButton.enabled = enableTestButton;

        giftLevelTitle.SetActive(false);
        progressBar.SetActive(false);

        if (_gameManager.IsGiftLevel)
        {
            giftLevelTitle.SetActive(true);

            foreach (var p in giftLevelConfetti)
                p.Play();
        }
        else
        {
            progressBar.SetActive(true);
        }

        floatingMessage.transform.localScale = new Vector3(0, 0, 1);
        floatingMessage.gameObject.SetActive(false);

        _gameManager.PlayLocationMusic(true);

        FillProgressBar();

        Advertisement.HideAll();

        foreach (var item in keyItems)
            Destroy(item.gameObject);

        keyItems.Clear();

        for (int i = 0; i < _gameManager.Config.locationKeysCount; i++)
        {
            var item = Instantiate(keyPrefab, keyPrefab.transform.parent).GetComponent<Image>();
            item.gameObject.SetActive(true);

            keyItems.Add(item);
        }

        if (tutorialAnimator.gameObject.activeInHierarchy)
            tutorialAnimator.Play(tutorialDirection.ToString());
    }

    private void OnDisable()
    {
        keyCounterAnimation.Stop();
        keyCounterAnimation.gameObject.SetActive(false);
    }

    public void FillProgressBar() 
    {
        foreach (var i in progressItems)
            Destroy(i.gameObject);

        progressItems.Clear();

        int obstacles = _gameManager.GetLevelObstaclesCount();

        for (int i = 0; i < obstacles; i++) 
        {
            var item = Instantiate(progressItemPrefab, progressBar.transform).GetComponent<Image>();
            item.gameObject.SetActive(true);

            progressItems.Add(item);
        }

        var scale = 1.0f;

        if (obstacles > 6 && obstacles <= 9)
            scale = 0.8f;
        else if (obstacles > 9)
            scale = 0.7f;

        progressRectTransform.localScale = new Vector2(scale, scale);

        UpdateProgressBar(0);
    }

    public void UpdateProgressBar(int count) 
    {
        for (int i = 0; i < progressItems.Count; i++)
        {
            if (count > i) progressItems[i].color = progressItemColors[1];
            else progressItems[i].color = progressItemColors[0];
        }
    }

    public void ShowTutorial(SwipeDirection direction, string text) 
    {
        tutorialDirection = direction;

        if (!string.IsNullOrEmpty(text))
            tutorialText.text = text;
        else
            tutorialText.text = "";

        tutorial.SetActive(true);

        if(tutorialAnimator.gameObject.activeInHierarchy)
            tutorialAnimator.Play(tutorialDirection.ToString());
    }

    public void ShowFloatingMessage(string text) 
    {
        if (_gameManager.IsGiftLevel)
            return;

        floatingMessage.gameObject.SetActive(true);
        floatingMessage.text = text;
        floatingMessageAnimator.Play("Message", -1, 0);
    }

    public void CloseTutorial()
    {
        tutorial.SetActive(false);
    }

    public void UpdateKeysCount(int count)
    {
        foreach (var item in keyItems)
            item.sprite = keySprites[0];

        for (int i = 0; i < count; i++)
        {
            if (keyItems.Count == 0)
                break;

            keyItems[i].sprite = keySprites[1];
        }

        if (count > 0)
        {
            keyCounterAnimation.gameObject.SetActive(true);
            keyCounterAnimation.Play();
        }
    }

    public void DisableKeys()
    {
        foreach (var item in keyItems)
            item.gameObject.SetActive(false);
    }
}
