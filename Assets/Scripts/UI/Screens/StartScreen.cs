using UnityEngine;
using UnityEngine.UI;
using UI;
using System;
using System.Collections.Generic;
using System.Linq;

public class StartScreen : UIScreen
{
    [SerializeField] GameObject menu;
    [SerializeField] Image currentLocation;
    [SerializeField] Image nextLocation;
    [SerializeField] Button soundButton;
    [SerializeField] Button restorePurchasesButton;
    [SerializeField] Dropdown selectlocation;
    [SerializeField] Dropdown selectlevel;
    [SerializeField] Color[] settingsButtonColors;
    [SerializeField] Color[] progressBarItemColors;
    [SerializeField] Transform roadmapContent;
    [SerializeField] RoadmapItem roadmapItem;
    [SerializeField] Sprite[] levelEventIcons;
    [SerializeField] GameObject roadMap;
    [SerializeField] GameObject giftLevelTitle;
    [SerializeField] GameObject tapToPlay;
    [SerializeField] Image skinsNotificationIcon;
    [SerializeField] Button skinsButton;
    [SerializeField] Button noAdsButton;

    List<RoadmapItem> roadmapItems = new List<RoadmapItem>();

    [SerializeField] Color passedLevel;
    [SerializeField] Color currentLevel;
    [SerializeField] Color nextLevel;

    [SerializeField] Text level;
    [SerializeField] CoinsCounter money;

    GameManager _gameManager;
    GameData _gameData;
    IAP _iap;

    public void Init(GameManager gameManager, GameData gameData, IAP iap)
    {
        _gameManager = gameManager;
        _gameData = gameData;
        _iap = iap;
    }

    public override void Open()
    {
        base.Open();

        skinsButton.onClick.RemoveAllListeners();
        skinsButton.onClick.AddListener(() => { _gameManager.OpenSkins(); });

        noAdsButton.onClick.RemoveAllListeners();
        noAdsButton.onClick.AddListener(() => { _iap.Purchase(_iap.GetItem("no_ads")); });

        noAdsButton.gameObject.SetActive(false);

        CancelInvoke("UpdateNoAdsButton");
        InvokeRepeating("UpdateNoAdsButton", 0, 1);

        SkinsNotificationIcon(_gameManager.CurrentUnlockedSkin != null);

        if (_gameManager.IsLastLevel() && _gameManager.IsLevelCompleted())
            tapToPlay.SetActive(false);
        else
            tapToPlay.SetActive(true);

        roadMap.SetActive(false);
        giftLevelTitle.SetActive(false);

        if (_gameManager.IsGiftLevel)
        {
            level.text = "";
            giftLevelTitle.SetActive(true);
        }
        else 
        {
            //level.text = $"MISSION {_gameManager.TotalLevelsCompleted + 1}";
            level.text = $"MISSION {_gameManager.GetLevelMissionNumber()}";
            roadMap.SetActive(true);
        }

        //money.UpdateCounter(_gameManager.Money);
        money.SetCounter(_gameManager.Money);

        ShowSettings(false);

        menu.SetActive(true);

        UpdateRoadMap();

        _gameManager.PlayMenuMusic(true);

        //if(!_gameManager.SkipAd())
        //    AdMob.Instance.RequestBanner();

        //var locationOptions = new List<Dropdown.OptionData>();
        //
        //foreach (var location in _gameManager.Config.locations)
        //    locationOptions.Add(new Dropdown.OptionData(location.title));
        //
        //selectlocation.options = locationOptions;
        //selectlocation.value = Array.IndexOf(config.locations, _gameManager.Location);
        //
        //var levelOptions = new List<Dropdown.OptionData>();
        //
        //foreach (var level in _gameManager.Location.levels)
        //    levelOptions.Add(new Dropdown.OptionData((Array.IndexOf(_gameManager.Location.levels, level) + 1).ToString()));
    }

    private void OnDisable()
    {
        CancelInvoke("UpdateNoAdsButton");
    }

    private void UpdateNoAdsButton() 
    {
        //noAdsButton.gameObject.SetActive(_iap.IsInitialized && !_iap.IsNoAdsPurchased());
        noAdsButton.gameObject.SetActive(false);
    }

    private void OnPurchase(IAP.Item item) 
    {
        if (_iap.IsInitialized && _iap.IsPurchased(_iap.GetItem("no_ads"))) 
        {
            CancelInvoke("UpdateNoAdsButton");
            noAdsButton.gameObject.SetActive(false);
        }
    }

    public void SkinsNotificationIcon(bool show) 
    {
        skinsNotificationIcon.gameObject.SetActive(show);
    }

    public void ShowSettings(bool show) 
    {
        if (show)
            UpdateSettingsButtons();

        soundButton.transform.parent.gameObject.SetActive(show);
    }

    public void ToggleSound() 
    {
        _gameManager.ToggleSound();
        UpdateSettingsButtons();
    }

    public void RestorePurchases() 
    {
        _gameManager.RestorePurchases();
    }

    private void UpdateSettingsButtons() 
    {
        soundButton.GetComponent<Image>().color = settingsButtonColors[Convert.ToInt16(SoundManager.sound)];
        restorePurchasesButton.interactable = Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.OSXPlayer;
    }

    public void UpdateMoneyCount(int from, int to) 
    {
        money.UpdateCounter(from, to);
    }

    private void UpdateRoadMap() 
    {
        var locations = (Configuration.Location[])_gameManager.Config.locations.Clone();

        var current = locations.First(x => x.id.Equals(_gameManager.Location.id));
        var next = _gameManager.GetNextLocation();

        currentLocation.sprite = current.icons[1];

        if (currentLocation.Equals(next)) nextLocation.sprite = next.icons[0];
        else nextLocation.sprite = next.icons[0];

        foreach (var i in roadmapItems)
            Destroy(i.gameObject);

        roadmapItems.Clear();

        var color = Color.white;
        Sprite eventIcon = null;

        var levels = new List<Configuration.Level>();

        foreach (var l in locations)
            if(!l.id.Equals("tutorial"))
                levels.AddRange(l.levels);

        var usual = _gameManager.GetAvailableSkins(Configuration.Skin.Rarity.Usual, Configuration.Skin.Availability.Default);

        foreach (var l in levels)
        {
            if (l.levelEvent == LevelEvent.Skin)
                l.levelEvent = LevelEvent.None;
        }

        foreach (var u in usual)
        {
            if (u.levelsToUnlock > 0 && u.levelsToUnlock < levels.Count)
                levels[u.levelsToUnlock - 1].levelEvent = LevelEvent.Skin;
        }

        for (int i = 0; i < current.levels.Length; i++)
        {
            var item = Instantiate(roadmapItem, roadmapContent);

            roadmapItems.Add(item);

            // Set event icon
            eventIcon = levelEventIcons[(int)current.levels[i].levelEvent];

            bool isCurrentLevel = i == _gameManager.Level;

            if (i < _gameManager.Level)
            {
                color = passedLevel;

                // Skip event icon for passed level
                eventIcon = null;
            }
            else if (isCurrentLevel) 
            {
                color = currentLevel;
            }
            else color = nextLevel;

            item.Set(false, color, eventIcon ? 30 : 20, eventIcon, isCurrentLevel);

            item.gameObject.SetActive(true);
        }

        var scale = 1.2f;

        if (roadmapItems.Count > 13 && roadmapItems.Count < 18)
            scale = 1.0f;
        else if (roadmapItems.Count >= 18)
            scale = 0.9f;

        roadmapContent.GetComponent<RectTransform>().localScale = new Vector3(scale, scale);
    }

    public void Play()
    {
        _gameManager.SetGameState("Play");
        _gameManager.PlayLocationMusic(true);
    }

    public void SelectLevel() 
    {
        //selectlevel.options
    }
}
