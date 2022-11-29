using UnityEngine;
using UnityEngine.UI;
using UI;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Collections;

public class SkinsScreen : UIScreen
{
    [SerializeField] Transform content;
    [SerializeField] Color[] paginationColors;
    [SerializeField] SkinsListItem skinItem;
    [SerializeField] SkinRenderer skinRenderer;
    [SerializeField] Button paginationButton;
    [SerializeField] CoinsCounter money;
    [SerializeField] Text rewardOptionText;
    [SerializeField] Text buyOptionText;
    [SerializeField] Button rewardOptionButton;
    [SerializeField] Button buyOptionButton;
    [SerializeField] Button unlockRandomButton;
    [SerializeField] Button buySkinButton;
    [SerializeField] Text buySkinButtonText;
    [SerializeField] ParticleSystem[] unlockParticles;
    [SerializeField] Button nextPage;
    [SerializeField] Button previousPage;
    [SerializeField] Text skinListRarityTitle;

    [SerializeField] Color[] itemBackgroundColors;

    Dictionary<Configuration.Skin.Rarity, Configuration.Skin[]> skins;

    [SerializeField] SkinsTab tabItem;

    SkinsTab currentSkinTab;
    List<SkinsTab> skinsTabs = new List<SkinsTab>();

    [SerializeField] List<Button> paginationButtons = new List<Button>();
    [SerializeField] List<SkinsListItem> skinsItems = new List<SkinsListItem>();
    [SerializeField] List<SkinRenderer> renderers = new List<SkinRenderer>();

    int page;
    int skinsPerPage = 9;

    Configuration.Skin selectedSkin;
    Configuration.Skin appliedSkin;

    GameManager _gameManager;
    GameData _gameData;
    IAP _iap;

    public bool notifyNewSkin;

    public bool IsUnlocking { get; private set; }

    public void Init(GameManager gameManager, GameData gameData, IAP iap)
    {
        _gameManager = gameManager;
        _gameData = gameData;
        _iap = iap;
    }

    public override void Open()
    {
        base.Open();

        nextPage.onClick.AddListener(() => { ChangePage(1); });
        previousPage.onClick.AddListener(() => { ChangePage(-1); });

        appliedSkin = _gameManager.Skin;

        //money.UpdateCounter(_gameManager.Money);
        money.SetCounter(_gameManager.Money);

        LoadTabs();

        rewardOptionButton.interactable = false;
        unlockRandomButton.interactable = false;

        CancelInvoke("UpdateRewardAvailability");
        InvokeRepeating("UpdateRewardAvailability", 0, 1);

        if (_gameManager.CurrentUnlockedSkin != null)
        {
            SelectTab(skinsTabs.First(x => x.Rarity == _gameManager.CurrentUnlockedSkin.rarity));

            var skin = skinsItems.FirstOrDefault(x => x.Skin.Equals(_gameManager.CurrentUnlockedSkin));

            Select(skin.Skin);
            SelectSkinPage(_gameManager.CurrentUnlockedSkin);

            _gameManager.CurrentUnlockedSkin = null;
        }
        else 
        {
            SelectTab(skinsTabs.First(x => x.Rarity == _gameManager.Skin.rarity));
            Select(_gameManager.Skin);
        }

        Advertisement.HideAll();

        AnalyticEvents.ReportEvent("Skins_open");
    }

    private void OnDisable()
    {
        Advertisement.OnRewarded -= OnRewarded;
        Advertisement.OnRewarded -= OnRewardedRandomSkin;
        Advertisement.OnRewardedFailed -= OnRewardedFailed;
    }

    private void UpdateRewardAvailability() 
    {
        rewardOptionButton.interactable = Advertisement.GetPlacement("Reward_Store").IsReady();

        //unlockRandomButton.interactable = !IsUnlocking && Advertisement.GetPlacement("Reward_Epic_Skin").IsReady();
        unlockRandomButton.interactable = !IsUnlocking && Advertisement.GetPlacement("Reward_Store").IsReady();
    }

    public override void Close()
    {
        base.Close();

        nextPage.onClick.RemoveAllListeners();
        previousPage.onClick.RemoveAllListeners();

        foreach (var p in unlockParticles)
            p.Clear();

        ClearAll();

        if(appliedSkin != null)
            _gameManager.Skin = appliedSkin;
    }

    public void LoadTabs() 
    {
        foreach (var skinsTab in skinsTabs)
            Destroy(skinsTab.gameObject);

        skinsTabs.Clear();

        skins = _gameManager.Config.skins.GroupBy(x => x.rarity).ToDictionary(x => x.Key, x => x.ToArray());

        foreach (var rarity in skins)
        {
            var tab = Instantiate(tabItem, tabItem.transform.parent).GetComponent<SkinsTab>();

            tab.Init(this, rarity.Key, itemBackgroundColors[(int)rarity.Key]);
            tab.gameObject.SetActive(true);

            tab.GetComponent<Button>().onClick.AddListener(() => 
            { 
                SelectTab(tab);
                Select(_gameManager.Skin);
            });

            skinsTabs.Add(tab);
        }
    }

    private void ClearAll() 
    {
        foreach (var s in skinsItems)
            Destroy(s.gameObject);

        foreach (var r in renderers)
            Destroy(r.gameObject);

        skinsItems.Clear();
        renderers.Clear();

        foreach (var b in paginationButtons)
            Destroy(b.gameObject);

        paginationButtons.Clear();
    }

    private void LoadSkins() 
    {
        ClearAll();

        float rendererOffset = 10;

        foreach (var s in skins[currentSkinTab.Rarity])
        {
            var skin = Instantiate(skinItem.gameObject, content).GetComponent<SkinsListItem>();
            var renderer = Instantiate(skinRenderer).GetComponent<SkinRenderer>();
            var rendererPosition = renderer.transform.position;

            skin.gameObject.SetActive(true);

            skinsItems.Add(skin);
            renderers.Add(renderer);

            skin.GetComponent<Button>().onClick.AddListener(() => 
            {
                if (IsUnlocking)
                    return;

                skin.Animations.Play();

                if (selectedSkin != skin.Skin && !skin.IsUnknown())
                    Select(s);
            });

            rendererPosition.x += rendererOffset;
            renderer.transform.position = rendererPosition;
            rendererOffset += 10;

            var renderTexture = new RenderTexture(256, 256, 16, RenderTextureFormat.ARGB32);
            skin.Init(_gameData, s, renderer, renderTexture, itemBackgroundColors[(int)currentSkinTab.Rarity]);

            renderer.Setup(renderTexture, s.prefab, true);
        }

        int pagesCount = Mathf.CeilToInt((float)skinsItems.Count / (float)skinsPerPage);

        for (int i = 0; i < pagesCount; i++) 
        {
            var b = Instantiate(paginationButton.gameObject, paginationButton.transform.parent).GetComponent<Button>();
            b.onClick.AddListener(() => 
            {
                SelectPage(b);
            });

            paginationButtons.Add(b);
            b.gameObject.SetActive(true);
        }

        SelectPage(paginationButtons[0]);
    }

    private void ChangePage(int value) 
    {
        SelectPage(paginationButtons[page + value]);
    }

    public void SelectTab(SkinsTab tab) 
    {
        if (currentSkinTab == tab)
            return;

        currentSkinTab = tab;

        skinListRarityTitle.text = currentSkinTab.Rarity.ToString().ToUpper();

        foreach (var t in skinsTabs)
            t.Expand(false);

        tab.Expand(true);

        LoadSkins();
        ShowOptions();
    }

    private void SelectPage(Button button)
    {
        page = Array.IndexOf(paginationButtons.ToArray(), button);

        if (page < paginationButtons.Count - 1) nextPage.gameObject.SetActive(true);
        else nextPage.gameObject.SetActive(false);

        if (page > 0) previousPage.gameObject.SetActive(true);
        else previousPage.gameObject.SetActive(false);

        foreach (var s in skinsItems)
            s.Show(false);

        int skin = 0;

        for (int i = 0; i < skinsPerPage; i++)
        {
            skin = page * skinsPerPage + i;

            if (skin < skinsItems.Count)
                skinsItems[skin].Show(true);
            else
                break;
        }

        foreach (var x in paginationButtons)
            x.GetComponent<Image>().color = paginationColors[0];

        paginationButtons[page].GetComponent<Image>().color = paginationColors[1];
    }

    public void SelectSkinPage(Configuration.Skin skin) 
    {
        var s = skinsItems.FirstOrDefault(x => x.Skin.Equals(skin));
        var page = Mathf.CeilToInt((float)Array.IndexOf(skinsItems.ToArray(), s) / (float)skinsPerPage) - 1;

        if (page < 0 || page >= paginationButtons.Count)
            page = 0;

        SelectPage(paginationButtons[page]);
    }

    private void ShowOptions()
    {
        rewardOptionButton.gameObject.SetActive(false);
        buyOptionButton.gameObject.SetActive(false);
        unlockRandomButton.gameObject.SetActive(false);
        buySkinButton.gameObject.SetActive(false);

        // Restore purchase
        if (selectedSkin != null && selectedSkin.rarity == Configuration.Skin.Rarity.Legend && !string.IsNullOrEmpty(selectedSkin.storeId))
        {
            if (_iap.IsPurchased(selectedSkin.storeId))
                _gameData.UnlockSkin(selectedSkin.id);
        }

        var isUnlocked = selectedSkin != null && _gameData.GetSkin(selectedSkin.id).unlocked;

        if (selectedSkin == null || selectedSkin.rarity != currentSkinTab.Rarity)
            isUnlocked = true;

        if (currentSkinTab.Rarity == Configuration.Skin.Rarity.Usual || currentSkinTab.Rarity == Configuration.Skin.Rarity.Rare)
        {
            rewardOptionButton.gameObject.SetActive(!isUnlocked);
            buyOptionButton.gameObject.SetActive(!isUnlocked);

            rewardOptionText.text = $"Earn {_gameManager.Config.shopAdReward}";

            if (selectedSkin != null)
                buyOptionText.text = $"for {selectedSkin.cost}";
        }
        else if (currentSkinTab.Rarity == Configuration.Skin.Rarity.Epic)
        {
            unlockRandomButton.gameObject.SetActive(skins[Configuration.Skin.Rarity.Epic].Any(x => !_gameData.GetSkin(x.id).unlocked));
        }
        else if(currentSkinTab.Rarity == Configuration.Skin.Rarity.Legend) 
        {
            buySkinButton.gameObject.SetActive(!isUnlocked);

            var price = selectedSkin != null && !string.IsNullOrEmpty(selectedSkin.storeId) ? $"{_iap.GetItem(selectedSkin.storeId).GetPrice()}" : "";
            price = price.Length > 0 ? $"\n{price}" : price;

            buySkinButtonText.text = $"Purchase{price}";
        }
    }

    private void EnableRewardButton(bool enable) 
    {
        rewardOptionButton.interactable = Advertisement.GetPlacement("Reward_Store").IsReady();
    }

    private void Select(Configuration.Skin skin)
    {
        foreach (var s in skinsItems)
            s.Select(s.Skin.Equals(skin));

        selectedSkin = skin;

        _gameManager.Skin = skin;

        ShowOptions();

        if (_gameManager.IsSkinUnlocked(skin.id))
        {
            appliedSkin = skin;
        }
    }

    public void GetCoins()
    {
        Advertisement.OnRewarded -= OnRewarded;
        Advertisement.OnRewardedFailed -= OnRewardedFailed;
        Advertisement.OnRewarded += OnRewarded;
        Advertisement.OnRewardedFailed += OnRewardedFailed;

        Advertisement.Show("Reward_Store");

        AnalyticEvents.ReportEvent("Reward_store");
    }

    public void UnlockRandomSkin()
    {
        IsUnlocking = true;

        Advertisement.OnRewarded -= OnRewardedRandomSkin;
        Advertisement.OnRewardedFailed -= OnRewardedFailed;
        Advertisement.OnRewarded += OnRewardedRandomSkin;
        Advertisement.OnRewardedFailed += OnRewardedFailed;

        //Advertisement.Show("Reward_Epic_Skin");
        Advertisement.Show("Reward_Store");

        //AnalyticEvents.ReportEvent("Reward_Epic_Skin");
        AnalyticEvents.ReportEvent("Reward_Store");
    }

    private void OnRewarded(Advertisement.Placement placement)
    {
        Advertisement.OnRewarded -= OnRewarded;
        Advertisement.OnRewardedFailed -= OnRewardedFailed;

        money.UpdateCounter(_gameManager.Money,
            _gameManager.Money + _gameManager.Config.shopAdReward);

        _gameManager.Money += _gameManager.Config.shopAdReward;
    }

    private void OnRewardedRandomSkin(Advertisement.Placement placement)
    {
        Advertisement.OnRewarded -= OnRewardedRandomSkin;
        Advertisement.OnRewardedFailed -= OnRewardedFailed;

        StartCoroutine(UnlockRandomSkinCoroutine());
    }

    private void OnRewardedFailed(Advertisement.Placement placement)
    {
        Advertisement.OnRewarded -= OnRewarded;
        Advertisement.OnRewardedFailed -= OnRewardedFailed;

        IsUnlocking = false;
    }

    public void Buy()
    {
        if (_gameManager.Money >= selectedSkin.cost)
        {
            var skin = new Dictionary<string, object>();
            skin.Add("skin", selectedSkin.id);

            AnalyticEvents.ReportEvent("Skin_purchase", skin);

            money.UpdateCounter(_gameManager.Money, 
                _gameManager.Money - selectedSkin.cost);

            _gameManager.Money -= selectedSkin.cost;

            _gameData.UnlockSkin(selectedSkin.id);

            foreach (var p in unlockParticles) 
            {
                if (!p.gameObject.activeInHierarchy)
                    p.gameObject.SetActive(true);

                p.Play();
            }

            skinsItems.First(x => x.Skin.Equals(selectedSkin)).UpdateItem();

            Select(selectedSkin);
        }
        else 
        {

        }
    }

    public void Purchase() 
    {
        if (selectedSkin != null && !string.IsNullOrEmpty(selectedSkin.storeId)) 
        {
            _iap.OnPurchase -= OnPurchase;
            _iap.OnPurchase += OnPurchase;

            _iap.Purchase(_iap.GetItem(selectedSkin.storeId));
        }
    }

    private void OnPurchase(IAP.Item item) 
    {
        //var skin = new Dictionary<string, object>();
        //skin.Add("skin", selectedSkin.id);
        //
        //AnalyticEvents.ReportEvent("Skin_iap_purchase", skin);

        var purchasedSkin = _gameManager.Config.skins.FirstOrDefault(x => x.storeId.Equals(item.id));

        _gameData.UnlockSkin(purchasedSkin.id);

        foreach (var p in unlockParticles)
        {
            if (!p.gameObject.activeInHierarchy)
                p.gameObject.SetActive(true);

            p.Play();
        }

        skinsItems.First(x => x.Skin.Equals(purchasedSkin)).UpdateItem();

        Select(purchasedSkin);
    }

    WaitForSeconds pickUnclockItem = new WaitForSeconds(0.25f);

    IEnumerator UnlockRandomSkinCoroutine() 
    {
        IsUnlocking = true;

        var itemsToUnlock = skinsItems.Where(x => !_gameData.GetSkin(x.Skin.id).unlocked).ToArray();
        itemsToUnlock.Shuffle();

        foreach (var item in itemsToUnlock) 
        {
            item.Animations.Play("PickUnlockItem");
            yield return pickUnclockItem;
        }

        yield return pickUnclockItem;

        var skin = _gameData.GetSkin(itemsToUnlock.Last().Skin.id);

        skin.watchedAds++;

        if (skin.watchedAds >= itemsToUnlock.Last().Skin.adsToUnlock) 
        {
            //Unlock item
            selectedSkin = itemsToUnlock.Last().Skin;
            _gameData.UnlockSkin(selectedSkin.id);

            foreach (var p in unlockParticles)
            {
                if (!p.gameObject.activeInHierarchy)
                    p.gameObject.SetActive(true);

                p.Play();
            }
        }

        itemsToUnlock.Last().Animations.Play("PickUnlockItem");
        itemsToUnlock.Last().UpdateItem();

        Select(selectedSkin);

        IsUnlocking = false;
    }
}

