using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UI;
using System.Collections.Generic;

public class SkinUnlockedScreen : UIScreen
{
    [SerializeField] Button claimRewardButton;
    [SerializeField] Button noThanksButton;

    [SerializeField] Image rarityHeader;
    [SerializeField] Text rarityText;
    [SerializeField] Color[] rarityHeaderColors;

    [SerializeField] GameObject clueOutline;
    [SerializeField] ParticleSystem VictoryParticle;

    [SerializeField] Image skinSilhouette;
    [SerializeField] Image skinFillImage;
    [SerializeField] GameObject noMoreSkins;

    [SerializeField] Transform unlockedSkinHolder;

    Configuration.Skin rewardSkin;

    GameObject currentSkinObject;

    GameManager _gameManager;
    GameData _gameData;

    public void Init(GameManager gameManager, GameData gameData)
    {
        _gameManager = gameManager;
        _gameData = gameData;
    }

    public override void Open()
    {
        base.Open();

        Advertisement.SkipInterstitial(false);

        noMoreSkins.SetActive(false);

        ShowUnlockedSkin();

        VictoryParticle.Play();

        claimRewardButton.interactable = false;
        noThanksButton.gameObject.SetActive(false);
        claimRewardButton.gameObject.SetActive(false);

        CancelInvoke("UpdateRewardButton");
        InvokeRepeating("UpdateRewardButton", 0, 1);

        Invoke("ShowNoThanks", 2.0f);
    }

    private void OnDisable()
    {
        Advertisement.OnRewarded -= OnRewarded;
        Advertisement.OnRewardedFailed -= OnRewardedFailed;
    }

    private void OnRewarded(Advertisement.Placement placement)
    {
        Advertisement.OnRewarded -= OnRewarded;
        Advertisement.OnRewardedFailed -= OnRewardedFailed;

        Advertisement.SkipInterstitial(true);

        _gameManager.UnlockAndSelectTheSkin(rewardSkin);

        var parameters = new Dictionary<string, object>();
        parameters.Add("skin", rewardSkin.id);

        AnalyticEvents.ReportEvent("Reward_skin", parameters);

        StopCoroutine("NextLevelCoroutine");
        StartCoroutine("NextLevelCoroutine", true);
    }

    private void OnRewardedFailed(Advertisement.Placement placement)
    {
        Advertisement.OnRewarded -= OnRewarded;
        Advertisement.OnRewardedFailed -= OnRewardedFailed;

        StopCoroutine("NextLevelCoroutine");
        StartCoroutine("NextLevelCoroutine", false);
    }

    public void Continue(bool bonus)
    {
        noThanksButton.interactable = false;

        CancelInvoke("UpdateRewardButton");

        if (rewardSkin != null && bonus)
        {
            Advertisement.OnRewarded -= OnRewarded;
            Advertisement.OnRewardedFailed -= OnRewardedFailed;
            Advertisement.OnRewarded += OnRewarded;
            Advertisement.OnRewardedFailed += OnRewardedFailed;

            Advertisement.Show("Reward_Skin");
        }
        else
        {
            StopCoroutine("NextLevelCoroutine");
            StartCoroutine("NextLevelCoroutine", false);
        }
    }

    void ShowUnlockedSkin()
    {
        rewardSkin = _gameManager.UnlockSkin;
        _gameManager.UnlockSkin = null;

        _gameData.ExploreSkin(rewardSkin.id);

        rarityHeader.gameObject.SetActive(rewardSkin != null);

        if (rewardSkin != null)
        {
            rarityHeader.color = rarityHeaderColors[(int)rewardSkin.rarity];
            rarityText.text = $"{rewardSkin.rarity.ToString().ToUpper()} SKIN";

            currentSkinObject = Instantiate(rewardSkin.prefab, unlockedSkinHolder);
            currentSkinObject.GetComponent<Animator>().Play("Victory");
        }
        else 
        {
            //rarityText.text = "";
            noMoreSkins.SetActive(true);
            noThanksButton.gameObject.SetActive(false);
        }
    }

    private void UpdateRewardButton()
    {
        claimRewardButton.gameObject.SetActive(true);
        claimRewardButton.interactable = Advertisement.GetPlacement("Reward_Skin").IsReady();
    }

    private void ShowNoThanks()
    {
        noThanksButton.gameObject.SetActive(true);
        noThanksButton.interactable = true;
    }

    WaitForSeconds nextLevelDelay = new WaitForSeconds(1.0f);

    IEnumerator NextLevelCoroutine(bool getNewSkin)
    {
        claimRewardButton.interactable = false;

        yield return nextLevelDelay;

        Advertisement.OnRewarded -= OnRewarded;
        Advertisement.OnRewardedFailed -= OnRewardedFailed;

        Destroy(currentSkinObject);

        _gameManager.NextLevel();
    }
}
