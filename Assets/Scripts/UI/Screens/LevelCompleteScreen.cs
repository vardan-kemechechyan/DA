using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UI;
using System.Collections.Generic;

public class LevelCompleteScreen : UIScreen
{
    [SerializeField] Text moneyText;
    [SerializeField] Text tutorialCompleteText;
    [SerializeField] Button claimRewardButton;
    [SerializeField] Button noThanksButton;
    [SerializeField] Text claimButtonText;
    [SerializeField] Button continueTutorialButton;
    [SerializeField] Text continueButton;
    [SerializeField] Slider bonusMultiplier;
    [SerializeField] float bonusBarSpeed = 1.0f;
    [SerializeField] ClueRenderer clueRenderer;
    [SerializeField] GameObject clueOutline;
    [SerializeField] GameObject money;
    [SerializeField] GameObject clueWord;
    [SerializeField] GameObject clueWordLetterPrefab;
    [SerializeField] Image jackpot;
    [SerializeField] Animation jackpotMoneyAnimation;
    [SerializeField] CoinsCounter coinsCounter;
    [SerializeField] Text jackpotAmount;
    [SerializeField] Image skinImage;
    [SerializeField] Image skinFill;
    [SerializeField] GameObject skinsCommingSoon;
    [SerializeField] Text skinProgress;
    [SerializeField] GameObject unknownSkin;

    [SerializeField] Color clueLetterUnlocked;
    [SerializeField] Color clueLetterCurrent;
    [SerializeField] Color clueLetterLocked;

    List<ClueWordLetter> clueWordLetters = new List<ClueWordLetter>();

    float[] bonusPointerPositions = new float[] { 0.16f, 0.379f, 0.597f, 0.82f, 1.0f };
    int[] bonusPointerMultipliers = new int[] { 3, 4, 5, 4, 3 };

    bool invertBonusBar;

    int rewardValue;

    int reward;
    private int Reward
    {
        get => reward;
        set
        {
            moneyText.text = value.ToString();
            reward = value;
        }
    }

    string currentLetter;

    int jackpotMoney;

    Coroutine specialEffectCoroutine;
    Vector3 silhouetteOriginalScale;

    float skinProgressValue;

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

        silhouetteOriginalScale = skinImage.transform.localScale;

        Advertisement.SkipInterstitial(false);

        jackpotMoney = 0;

        jackpot.gameObject.SetActive(false);
        jackpotMoneyAnimation.gameObject.SetActive(false);
        skinImage.gameObject.SetActive(false);
        skinFill.gameObject.SetActive(false);
        skinsCommingSoon.SetActive(false);
        skinProgress.gameObject.SetActive(false);
        unknownSkin.SetActive(false);
        clueOutline.gameObject.SetActive(false);

        claimRewardButton.interactable = false;
        noThanksButton.gameObject.SetActive(false);

        CancelInvoke("UpdateRewardButton");
        InvokeRepeating("UpdateRewardButton", 0, 1);

        StartCoroutine("FloatingBonusBar");

        money.SetActive(true);

        UpdateMoneyCounter();

        if (_gameManager.IsInitialLevel())
        {
            bonusMultiplier.transform.parent.gameObject.SetActive(false);
            claimRewardButton.gameObject.SetActive(false);

            continueTutorialButton.gameObject.SetActive(true);

            tutorialCompleteText.gameObject.SetActive(true);
            money.SetActive(false);
        }
        else
        {
            bonusMultiplier.transform.parent.gameObject.SetActive(true);
            claimRewardButton.gameObject.SetActive(true);

            UpdateSkinProgress();

            Invoke("ShowNoThanks", 2.0f);

            continueTutorialButton.gameObject.SetActive(false);

            tutorialCompleteText.gameObject.SetActive(false);

            clueWord.SetActive(!_gameManager.IsGiftLevel);

            if (!_gameManager.IsGiftLevel)
                UpdateClueWord();
        }

        _gameManager.PlayLocationMusic(false);
    }

    private void OnDisable()
    {
        Advertisement.OnRewarded -= OnRewarded;
        Advertisement.OnRewardedFailed -= OnRewardedFailed;
    }

    public override void Close()
    {
        base.Close();

        StopCoroutine("FloatingBonusBar");
    }

    public void UpdateMoneyCounter()
    {
        coinsCounter.UpdateCounter(_gameManager.Money);
    }

    WaitForSeconds tutorialClueDelay = new WaitForSeconds(3.0f);

    private void ShowNoThanks()
    {
        noThanksButton.gameObject.SetActive(true);
        noThanksButton.interactable = true;
    }

    public void ShowClue()
    {
        StopCoroutine("ShowClueCoroutine");
        StartCoroutine("ShowClueCoroutine");
    }

    IEnumerator ShowClueCoroutine()
    {
        clueOutline.gameObject.SetActive(true);

        yield return tutorialClueDelay;
    }

    private void UpdateRewardButton()
    {
        claimRewardButton.interactable = Advertisement.GetPlacement("Reward_ClaimX").IsReady();
    }

    public void SetLevelReward(int reward)
    {
        rewardValue = reward;
        Reward = rewardValue;
    }

    public void GetReward()
    {
        AnalyticEvents.ReportEvent("Reward_claim");

        CancelInvoke("UpdateRewardButton");
        StopCoroutine("FloatingBonusBar");

        Continue(true);
    }

    private int GetBonusValue()
    {
        for (int i = 0; i < bonusPointerPositions.Length; i++)
            if (bonusMultiplier.value <= bonusPointerPositions[i])
                return bonusPointerMultipliers[i];

        return 1;
    }

    public void Continue(bool bonus)
    {
        noThanksButton.interactable = false;

        CancelInvoke("UpdateRewardButton");
        StopCoroutine("FloatingBonusBar");

        moneyText.transform.parent.gameObject.SetActive(false);

        if (bonus)
        {
            Advertisement.OnRewarded -= OnRewarded;
            Advertisement.OnRewardedFailed -= OnRewardedFailed;
            Advertisement.OnRewarded += OnRewarded;
            Advertisement.OnRewardedFailed += OnRewardedFailed;

            Advertisement.Show("Reward_ClaimX");
        }
        else
        {
            jackpotMoneyAnimation.Play("JackpotAmount");
            _gameManager.updateMoneyView = false;
            _gameManager.Money += jackpotMoney;

            _gameManager.Money += rewardValue;

            Reward = rewardValue;

            StopCoroutine("NextLevelCoroutine");
            StartCoroutine("NextLevelCoroutine");
        }
    }

    public void NextScreen()
    {
        if (_gameManager.UnlockSkin != null)
        {
            _gameManager.SkinUnlockedScreen();
        }
        else
        {
            _gameManager.NextLevel();
        }
    }

    private void OnRewarded(Advertisement.Placement placement)
    {
        Advertisement.OnRewarded -= OnRewarded;
        Advertisement.OnRewardedFailed -= OnRewardedFailed;

        Advertisement.SkipInterstitial(true);

        jackpotMoneyAnimation.Play("JackpotAmount");
        _gameManager.updateMoneyView = false;
        _gameManager.Money += jackpotMoney;
        _gameManager.Money += rewardValue * GetBonusValue();

        StopCoroutine("NextLevelCoroutine");
        StartCoroutine("NextLevelCoroutine");
    }

    private void OnRewardedFailed(Advertisement.Placement placement)
    {
        Advertisement.OnRewarded -= OnRewarded;
        Advertisement.OnRewardedFailed -= OnRewardedFailed;

        Advertisement.SkipInterstitial(true);

        _gameManager.Money += rewardValue;

        StopCoroutine("NextLevelCoroutine");
        StartCoroutine("NextLevelCoroutine");
    }

    WaitForSeconds nextLevelDelay = new WaitForSeconds(1.0f);

    IEnumerator NextLevelCoroutine()
    {
        claimRewardButton.interactable = false;

        yield return nextLevelDelay;

        if (Advertisement.skipInterstitial)
        {
            NextScreen();
        }
        else 
        {
            Advertisement.Show("Interstitial_lvl", success =>
            {
                NextScreen();
            });
        }
    }

    WaitForSeconds bonusBarDelay = new WaitForSeconds(0.01f);

    IEnumerator FloatingBonusBar()
    {
        invertBonusBar = false;
        bonusMultiplier.value = 0;

        while (true)
        {
            if (invertBonusBar)
                bonusMultiplier.value -= bonusBarSpeed;
            else
                bonusMultiplier.value += bonusBarSpeed;
            yield return bonusBarDelay;

            Reward = rewardValue * GetBonusValue();

            claimButtonText.text = $"CLAIM X{GetBonusValue()}";

            if (bonusMultiplier.value == 1 && !invertBonusBar) invertBonusBar = true;
            else if (bonusMultiplier.value == 0 && invertBonusBar) invertBonusBar = false;
        }
    }

    WaitForSeconds clueWordLettersDelay = new WaitForSeconds(0.1f);

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

        currentLetter = location.levels[level - 1].obstacles.Last().clue;

        string chars = "";


        for (int i = 0; i < level; i++)
            chars += location.levels[i].obstacles.Last().clue;

        var unlockedLetters = chars.ToCharArray();
        var letters = location.clue.ToCharArray();

        for (int i = 0; i < letters.Length; i++)
        {
            if (!letters[i].Equals(' ') && !unlockedLetters.Any(x => x.Equals(letters[i])))
                letters[i] = '?';
        }

        foreach (var l in letters)
        {
            var letter = Instantiate(clueWordLetterPrefab, clueWord.transform).GetComponent<ClueWordLetter>();

            clueWordLetters.Add(letter);

            bool isCurrent = l.Equals(Convert.ToChar(currentLetter));
            var background = Color.white;

            if (l.Equals('?')) background = clueLetterLocked;
            else background = clueLetterUnlocked;

            if (isCurrent)
                background = clueLetterCurrent;

            letter.Set(l.ToString(), background, isCurrent);
            letter.gameObject.SetActive(true);

            if (!l.Equals(' ') && !l.Equals('?'))
                letter.Shuffle();
        }

        if (true)
        //if (!letters.Any(x => x.Equals('?'))) 
        {
            Invoke("AnimateClueWord", 1.0f);
        }
    }

    private void AnimateClueWord()
    {
        if (!gameObject.activeInHierarchy)
            return;

        StopCoroutine("AnimateClueWordCoroutine");
        StartCoroutine("AnimateClueWordCoroutine");
    }

    WaitForSeconds animateClueWordDelay = new WaitForSeconds(0.5f);

    IEnumerator AnimateClueWordCoroutine()
    {
        foreach (var l in clueWordLetters)
        {
            l.PlayAnimation();
            yield return clueWordLettersDelay;
        }

        yield return animateClueWordDelay;

        var duplicates = clueWordLetters.Where(x => x.Value.Equals(currentLetter)).ToArray();

        foreach (var d in duplicates)
        {
            d.PlayAnimation();
            yield return animateClueWordDelay;
        }
    }

    void UpdateSkinProgress()
    {
        clueOutline.SetActive(true);

        if (_gameManager.IsGiftLevel)
        {
            var r = UnityEngine.Random.Range(0, 101);

            var rare = _gameManager.GetAvailableSkins(Configuration.Skin.Rarity.Rare, Configuration.Skin.Availability.Gift);

            // First gift skin
            if (PlayerPrefs.GetInt("firstGiftSkin") <= 0)
            {
                r = 0;

                PlayerPrefs.SetInt("firstGiftSkin", 1);
                PlayerPrefs.Save();
            }

            if (r <= _gameManager.Config.giftSkinChance && rare.Length > 0)
            {
                // Rare skin to unlock
                rare.Shuffle();

                _gameManager.UnlockSkin = rare[0];

                unknownSkin.SetActive(true);
            }
            else
            {
                jackpot.gameObject.SetActive(true);
                jackpotMoney = _gameManager.Config.giftFinalReward;
                jackpotAmount.text = $"+{jackpotMoney}";
                jackpotMoneyAnimation.gameObject.SetActive(true);
            }
        }
        else 
        {
            var usual = _gameManager.GetAvailableSkins(Configuration.Skin.Rarity.Usual, Configuration.Skin.Availability.Default);
            var nextSkin = usual.OrderBy(x => x.levelsToUnlock).FirstOrDefault();

            float progress = 0;

            if (nextSkin != null)
            {
                skinImage.gameObject.SetActive(true);

                var color = skinImage.color;
                color.a = 1.0f;
                skinImage.color = color;

                skinFill.gameObject.SetActive(true);
                skinProgress.gameObject.SetActive(true);

                var previousSkin = _gameManager.Config.skins
                    .Where(x => x.rarity == Configuration.Skin.Rarity.Usual && x.levelsToUnlock <= nextSkin.levelsToUnlock && !nextSkin.Equals(x))
                    .OrderBy(x => x.levelsToUnlock)
                    .LastOrDefault();

                int completed = _gameData.CompletedLevelsCount();
                int toUnlock = nextSkin.levelsToUnlock;

                string ps = "";

                if (previousSkin != null)
                {
                    ps = previousSkin.id;

                    completed -= previousSkin.levelsToUnlock;
                    toUnlock -= previousSkin.levelsToUnlock;
                }

                progress = (float)Math.Round((double)completed / toUnlock, 2);

                FirebaseManager.SetCustomKey("skin_progress", $"Previous: {ps} Next: {nextSkin.id} {completed}/{toUnlock}={progress}");

                if (progress >= 1f)
                    _gameManager.UnlockSkin = nextSkin;

                bool showSilhouette = !_gameManager.IsGiftLevel;

                if (skinFill.fillAmount == 1) skinFill.fillAmount = 0f;

                if (progress == 0) { progress = 1; clueOutline.SetActive(true); skinProgressValue = 1f; }
                else clueOutline.SetActive(false);

                skinProgress.text = $"{progress * 100}%";
                skinFill.fillAmount = 0;

                if (specialEffectCoroutine != null) StopCoroutine(specialEffectCoroutine);

                specialEffectCoroutine = StartCoroutine(SilhouetteSpecialEffect(progress));
            }
            else 
            {
                skinImage.gameObject.SetActive(true);
                skinsCommingSoon.SetActive(true);

                var color = skinImage.color;
                color.a = 0.5f;
                skinImage.color = color;
            }
        }
    }

    IEnumerator SilhouetteSpecialEffect(float _newFillPercentage)
    {
        float currentFillAmount = skinFill.fillAmount;
        float TargetFillAmount = _newFillPercentage;

        Vector3 EndScaleSize = silhouetteOriginalScale * 1.2f;

        float fillTime = 0.4f;
        float fillAmountStep = (TargetFillAmount - currentFillAmount) / (fillTime * 100f);
        float yieldTime = fillTime / fillAmountStep;

        Vector3 VectorStep = 2 * (EndScaleSize - silhouetteOriginalScale) / (fillTime * 100f);

        yield return new WaitForSeconds(0.25f);

        while (currentFillAmount < TargetFillAmount)
        {
            currentFillAmount += fillAmountStep;
            skinFill.fillAmount = currentFillAmount;

            skinImage.transform.localScale += VectorStep;

            if (skinImage.transform.localScale.x >= EndScaleSize.x) VectorStep *= -1;

            yield return null;
        }

        skinFill.fillAmount = TargetFillAmount;
        skinImage.transform.localScale = silhouetteOriginalScale;
    }
}
