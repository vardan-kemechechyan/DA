using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UI;
using System.Linq;
using System;

public class BonusCaseScreen : UIScreen
{
    [SerializeField] GameObject bonusCellPrefab;
    [SerializeField] Transform content;
    [SerializeField] Transform cellsTransform;
    [SerializeField] GameObject keyPrefab;
    [SerializeField] Button claimRewardButton;
    [SerializeField] Button continueButton;
    [SerializeField] Button noThanksButton;
    [SerializeField] CoinsCounter money;

    [SerializeField] Image bestCaseIcon;
    [SerializeField] RawImage bestCaseSkin;
    [SerializeField] Text bestCaseLabel;

    [SerializeField] int bonusCases = 9;

    List<GameObject> keyItems = new List<GameObject>();
    List<BonusCellsListItem> cells = new List<BonusCellsListItem>();

    SkinRenderer skinRenderer;

    Configuration config;

    GameManager _gameManager;

    public void Init(GameManager gameManager)
    {
        _gameManager = gameManager;
    }

    public override void Open()
    {
        base.Open();

        claimRewardButton.gameObject.SetActive(false);
        noThanksButton.gameObject.SetActive(false);
        continueButton.gameObject.SetActive(false);

        config = _gameManager.Config;

        claimRewardButton.onClick.AddListener(() => 
        {
            StopCoroutine("ShowGetMoreKeysCoroutine");

            claimRewardButton.gameObject.SetActive(false);
            noThanksButton.gameObject.SetActive(false);

            Advertisement.OnRewarded -= OnRewarded;
            Advertisement.OnRewardedFailed -= OnRewardedFailed;
            Advertisement.OnRewarded += OnRewarded;
            Advertisement.OnRewardedFailed += OnRewardedFailed;

            Advertisement.Show("Reward_Keys");
        });

        continueButton.onClick.AddListener(() =>
        {
            _gameManager.CloseBonusGame();
        });

        noThanksButton.onClick.AddListener(() =>
        {
            _gameManager.CloseBonusGame();
        });

        LoadCells();

        foreach (var item in keyItems)
            Destroy(item.gameObject);

        keyItems.Clear();

        for (int i = 0; i < config.locationKeysCount; i++)
        {
            var item = Instantiate(keyPrefab, keyPrefab.transform.parent);
            
            keyItems.Add(item);
            item.SetActive(true);
        }

        UpdateKeysCount(_gameManager.RemainingKeys);

        money.SetCounter(_gameManager.Money);

        CancelInvoke("UpdateRewardButton");
        InvokeRepeating("UpdateRewardButton", 0, 1);
    }

    private void OnDisable()
    {
        Advertisement.OnRewarded -= OnRewarded;
        Advertisement.OnRewardedFailed -= OnRewardedFailed;

        claimRewardButton.onClick.RemoveAllListeners();
        noThanksButton.onClick.RemoveAllListeners();

        if (skinRenderer != null)
            Destroy(skinRenderer.gameObject);
    }

    public void UpdateMoneyCounter() 
    {
        money.UpdateCounter(_gameManager.Money);
    }

    private void UpdateRewardButton()
    {
        claimRewardButton.interactable = Advertisement.GetPlacement("Reward_Keys").IsReady();
    }

    public void ShowGetMoreKeys()
    {
        StopCoroutine("ShowGetMoreKeysCoroutine");
        StartCoroutine("ShowGetMoreKeysCoroutine");
    }

    WaitForSeconds skipDelay = new WaitForSeconds(1.5f);

    IEnumerator ShowGetMoreKeysCoroutine()
    {
        claimRewardButton.gameObject.SetActive(true);
        yield return skipDelay;
        noThanksButton.gameObject.SetActive(true);
    }

    private void OnRewarded(Advertisement.Placement placement)
    {
        Advertisement.OnRewarded -= OnRewarded;
        Advertisement.OnRewardedFailed -= OnRewardedFailed;

        Advertisement.SkipInterstitial(true);

        _gameManager.GetBonusKeys();

        UpdateKeysCount(_gameManager.RemainingKeys);

        AnalyticEvents.ReportEvent("Reward_Keys");
    }

    private void OnRewardedFailed(Advertisement.Placement placement)
    {
        Advertisement.OnRewarded -= OnRewarded;
        Advertisement.OnRewardedFailed -= OnRewardedFailed;

        claimRewardButton.gameObject.SetActive(true);
        noThanksButton.gameObject.SetActive(true);

        _gameManager.CloseBonusGame();
    }

    private void LoadCells() 
    {
        // Adjust cells
        var cellsScale = 0.8f;

        if (Camera.main.aspect <= 0.5f)
        {
            cellsScale = 0.9f;
        }
        else if (Camera.main.aspect > 0.5f)  // 16:9
        {
            cellsScale = 0.8f;
        }

        cellsTransform.localScale = new Vector3(cellsScale, cellsScale, 1.0f);

        cells.Clear();

        var cases = _gameManager.GetBonusCases(bonusCases);

        // Show skin or money case if no skins left
        if (cases.Any(x => x.type == Configuration.BonusCase.Type.Skin))
            ShowBestCase(cases.Where(x => x.type == Configuration.BonusCase.Type.Skin).OrderBy(x => x.money).Last());
        else
            ShowBestCase(cases.OrderBy(x => x.money).Last());

        for (int i = 0; i < bonusCases; i++) 
        {
            var cell = Instantiate(bonusCellPrefab, content.transform).GetComponent<BonusCellsListItem>();

            cell.Init(_gameManager);

            cell.id = i;
            cell.Bonus = cases[i];

            cells.Add(cell);

            cell.gameObject.SetActive(true);
        }
    }

    public void UpdateKeysCount(int count)
    {
        foreach (var item in keyItems)
            item.SetActive(false);

        for (int i = 0; i < count; i++)
            keyItems[i].SetActive(true);
    }

    public void ShowBestCase(Configuration.BonusCase bonusCase) 
    {
        bestCaseIcon.gameObject.SetActive(bonusCase.type != Configuration.BonusCase.Type.Skin);
        bestCaseSkin.gameObject.SetActive(bonusCase.type == Configuration.BonusCase.Type.Skin);

        switch (bonusCase.type)
        {
            case Configuration.BonusCase.Type.Money:
                bestCaseIcon.sprite = config.bonusCaseIcons[(int)bonusCase.type];
                bestCaseLabel.text = $"{bonusCase.money}";
                break;
            case Configuration.BonusCase.Type.Skin:
                bestCaseLabel.text = $"";

                if (skinRenderer != null)
                    Destroy(skinRenderer.gameObject);

                skinRenderer = Instantiate(config.skinRenderer).GetComponent<SkinRenderer>();

                var position = skinRenderer.transform.position;
                position.x = 100;
                position.y = 10;

                skinRenderer.transform.position = position;

                var renderTexture = new RenderTexture(256, 256, 16, RenderTextureFormat.ARGB32);

                bestCaseSkin.texture = renderTexture;
                skinRenderer.Setup(renderTexture, bonusCase.skin.prefab, false);
                break;
        }
    }

    public void Continue() 
    {
        claimRewardButton.gameObject.SetActive(false);
        noThanksButton.gameObject.SetActive(false);

        continueButton.gameObject.SetActive(true);
    }
}