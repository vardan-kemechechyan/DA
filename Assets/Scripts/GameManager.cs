using System;
using System.Linq;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Threading.Tasks;

public class GameManager : MonoBehaviour
{
    private static GameManager instance;

    [SerializeField] bool testConnection;
    [SerializeField] bool isTestConnected;

    public bool internetRequired;

    [HideInInspector]
    public bool checkInternetConnection;

    public static Action<GameState, GameState> OnChangeGameState;
    public static Action<Configuration.Location> OnChangeLocation;
    public static Action<Configuration.Skin> OnChangeSkin;
    public static Action<Trigger> OnHitTrigger;
    public static Action<Clue> OnHitClue;
    public static Action<Trigger> OnCamera;
    public static Action<Key> OnHitKey;
    public static Action<Gift> OnGift;
    public static Action<SwipeDirection> OnSwipe;

    [SerializeField] bool testBonusCases;
    [SerializeField] bool testSpaceButton;
    [SerializeField] bool clearPrefsInEditor;
    [SerializeField] bool clearGameDataInEditor;

    [SerializeField] Transform characterSkinRoot;

    [SerializeField] Configuration config;
    public Configuration Config { get => config; }

    [SerializeField] SoundManager soundManager;
    [SerializeField] UIManager ui;
    [SerializeField] AddMoneyView addMoneyView;
    [SerializeField] Image splash;
    [SerializeField] AppReview appReview;

    public float journey;
    public float path;

    public float speed;
    
    public bool isRunning;
    public bool isMakingTrick;

    bool skinUnlockedByProgress = false;

    bool internetConnection = true;
    public bool InternetConnection 
    {
        get => internetConnection;
        private set 
        {
            if (ui.GetScreen<BonusCaseScreen>().IsOpen)
                value = true;

            if (State == GameState.LevelComplete || State == GameState.LevelFailed)
                value = true;

            if (value != internetConnection) 
            {
                ShowConnectionScreen(!value);
            }

            internetConnection = value;
        }
    }

    UI.UIScreen[] screensBeforeConnectionScreen;

    [SerializeField] GameState state;
    public GameState State
    {
        get => state;
        set
        {
            if (state != value)
            {
                switch (value)
                {
                    case GameState.Start:
                        isSlowMotion = false;
                        isTutorial = false;

                        if (Advertisement.IsInitialized)
                            Advertisement.RequestAll();
                        break;
                    case GameState.Play:
                        var lvl = new Dictionary<string, object>();
                        lvl.Add("level", Level + 1);

                        AnalyticEvents.ReportEvent("level_start", lvl);
                        break;
                }

                OnChangeGameState?.Invoke(value, state);
                state = value;

                FirebaseManager.SetCustomKey("game_state", value.ToString());
            }
        }
    }

    public static GameState CurrentState() 
    {
        return instance.State;
    }

    public Trigger.Type deathType;

    public bool isSlowMotion;
    public bool isTutorial;
    public bool isPause;

    int moreKeysCount;

    public SwipeDirection tutorialMove;

    public bool IsInitialized { get; private set; }

    [SerializeField] int level = 0;
    public int Level
    {
        get 
        {
            if (level >= Location.levels.Length)
                level = Location.levels.Length - 1;

            if (level < 0)
                level = 0;

            return level;
        }
        private set
        {
            if (value < 0)
                value = 0;

            if (value != level)
            {
                _gameData.Progress.level = value;
            }

            level = value;

            FirebaseManager.SetCustomKey("level", value.ToString());
        }
    }

    [SerializeField] int totalLevelsCompleted;

    public int giftLevelCount;

    public bool IsTestButtonPressed { get; private set; }

    public bool IsGiftLevel { get; private set; }

    public bool IsBonusCaseGame { get; private set; }

    [SerializeField] int bonusLevelCount;
    public int BonusLevelCount 
    {
        get => bonusLevelCount;
        set 
        {
            bonusLevelCount = value;
        }
    }

    [SerializeField] int bonusKeys;
    public int BonusKeys 
    {
        get => bonusKeys;
        set 
        {
            if (value < 0)
                value = 0;

            _gameData.Progress.bonusKeys = value;

            bonusKeys = value;

            FirebaseManager.SetCustomKey("bonus_keys", value.ToString());
        }
    }

    public bool IsBonusCaseInteractable { get; private set; }

    public int CollectedKeys { get; set; }

    public bool ObstacleHasTutorial { get; set; }

    [SerializeField] int remainingKeys;
    public int RemainingKeys 
    {
        get => remainingKeys;
        private set 
        {
            if (ui.GetScreen<BonusCaseScreen>().IsOpen)
                ui.GetScreen<BonusCaseScreen>().UpdateKeysCount(value);

            remainingKeys = value;

            FirebaseManager.SetCustomKey("remaining_keys", value.ToString());
        }
    }

    public bool IsLastLevel() 
    {
        return Location.Equals(config.locations.Last()) && IsLastLocationLevel();
    }

    public bool IsLastLocationLevel() 
    {
        return Location.levels[Level].Equals(Location.levels.Last());
    }

    public bool IsLevelCompleted() 
    {
        return _gameData.GetLevel(Location.id, Level).completed;
    }

    public bool IsInitialLevel()
    {
        if (Location.Equals(config.locations[0]) && Level == 0)
            return true;
        else
            return false;
    }

    public int CompletedLevelsCount() 
    {
        return _gameData.CompletedLevelsCount();
    }

    public SwipeDirection importantMove;
    public List<SwipeDirection> correctMoves;

    [SerializeField] int money;
    public int Money
    {
        get => money;
        set
        {
            if (value <= 0)
                value = 0;

            if (value != money)
            {
                _gameData.Progress.money = value;
            }

            if (updateMoneyView && money < value)
                addMoneyView.AddMoney(value - money);

            updateMoneyView = true;

            money = value;

            ui.GetScreen<LevelCompleteScreen>().UpdateMoneyCounter();

            FirebaseManager.SetCustomKey("money", value.ToString());
        }
    }

    public bool updateMoneyView;

    //[SerializeField] List<string> skins;
    //public List<string> Skins
    //{
    //    get => skins;
    //    set
    //    {
    //        _gameData.Progress.skins = JsonConvert.SerializeObject(value);
    //
    //        skins = value;
    //
    //        FirebaseManager.SetCustomKey("skins", JsonConvert.SerializeObject(value));
    //    }
    //}

    [SerializeField]
    public Configuration.Skin skin;
    public Configuration.Skin Skin 
    {
        get => skin;
        set 
        {
            PreviousSkin = skin;

            if (config.skins.Contains(value) && IsSkinUnlocked(value.id)) 
            {
                _gameData.SetSkin(value.id);
            }

            OnChangeSkin?.Invoke(value);

            skin = value;

            FirebaseManager.SetCustomKey("skin", value.id);
        }
    }

    public Configuration.Skin UnlockSkin { get; set; }

    public Configuration.Skin PreviousSkin { get; private set; }

    public Configuration.Skin CurrentUnlockedSkin { get; set; }

    [SerializeField]
    Configuration.Location location;    
    public Configuration.Location Location 
    {
        get => location;
        private set
        {
            if (value != location) 
            {
                _gameData.Progress.location = value.id;

                location = value;

                OnChangeLocation?.Invoke(value);

                FirebaseManager.SetCustomKey("location", location.id);
            }
        }
    }

    public Configuration.Location GetLocation(string id)
    {
        var value = config.locations.FirstOrDefault(x => x.id == id);

        if (value != null) return value;
        else
        {
            Debug.LogWarning($"Location {id} not found!");
            return config.locations[0];
        }
    }

    public Configuration.Location GetNextLocation()
    {
        var i = Array.IndexOf(config.locations, location) + 1;
        i = i < config.locations.Length ? i : config.locations.Length - 1;
        return config.locations[i < config.locations.Length ? i : config.locations.Length - 1];
    }

    public Configuration.Skin GetSkin(string id)
    {
        var value = config.skins.FirstOrDefault(x => x.id == id);

        if (value != null) return value;
        else 
        {
            if (!string.IsNullOrEmpty(id))
                Debug.LogWarning($"Skin {id} not found!");

            return config.skins[0];
        }
    }

    public bool IsGetLocationBonus { get; private set; }

    public Configuration.Level GetCurrentLevel() 
    {
        return IsGiftLevel ? config.giftLevel : Location.levels[Level];
    }

    public Configuration.Skin[] GetAvailableSkins(Configuration.Skin.Rarity rarity, Configuration.Skin.Availability availability)
    {
        return config.skins.Where(x => x.rarity == rarity && !IsSkinUnlocked(x.id) && !IsSkinExplored(x.id) && x.IsAvailable(new Configuration.Skin.Availability[] { availability })).ToArray();
    }

    public Configuration.Skin[] GetAvailableSkins(Configuration.Skin.Rarity rarity, Configuration.Skin.Availability[] availability)
    {
        return config.skins.Where(x => x.rarity == rarity && !IsSkinUnlocked(x.id) && !IsSkinExplored(x.id) && x.IsAvailable(availability)).ToArray();
    }

    public bool IsSkinExplored(string id)
    {
        var skin = _gameData.GetSkin(id);
        return skin != null && skin.explored;
    }

    public bool IsSkinUnlocked(string id) 
    {
        var skin = _gameData.GetSkin(id);
        return skin != null && skin.unlocked;
    }

    public int GetLevelMissionNumber() 
    {
        int value = 0;
        bool skip = false;

        foreach (var location in config.locations)
        {
            foreach (var level in location.levels)
            {
                if (level == GetCurrentLevel())
                {
                    skip = true;
                    break;
                }
                else
                    value++;
            }

            if (skip)
                break;
        }

        return value;
    }

    public int GetLevelObstaclesCount() 
    {
        var level = IsGiftLevel ? config.giftLevel : Location.levels[Level];
        return level.obstacles.Length;
    }

    public float GetLevelSpeed() 
    {
        return 1 + (Location.gameSpeedMultiplier / 100);
    }

    public float FrameDependentDelta() 
    {
        return (0.02f * Time.timeScale);
    }

    public bool SkipAd() 
    {
        return _gameData.CompletedLevelsCount() < 1;
    }

    GameData _gameData;
    IAP _iap;

    private void Awake()
    {
        instance = this;
    }

    public void Initialize(GameData gameData, IAP iap)
    {
        _gameData = gameData;
        _iap = iap;

        _iap.OnRestore += OnRestorePurchases;
        _iap.OnPurchase += OnPurchase;

        CheckConnection();

#if UNITY_EDITOR
        if (clearPrefsInEditor)
            PlayerPrefs.DeleteAll();
#endif

        IsGetLocationBonus = _gameData.Progress.locationBonus;

        Level = _gameData.Progress.level;
        Money = _gameData.Progress.money;
        BonusKeys = _gameData.Progress.bonusKeys;

        if (_gameData.Progress.skins.Count <= 0) 
        {
            _gameData.Progress.skins.Add(new GameData.Skin { id = Config.skins[0].id,});
        }

        var skin = GetSkin(_gameData.Progress.skin);

        if (skin != null) Skin = skin;
        else Skin = Config.skins[0];

        if (!string.IsNullOrEmpty(_gameData.Progress.location))
            Location = GetLocation(_gameData.Progress.location);
        else Location = Config.locations[0];

        if (string.IsNullOrEmpty(_gameData.Progress.bestCase)) 
        {
            _gameData.Progress.bestCase = "Skin";
            _gameData.Progress.bestCaseType = 1;
            _gameData.Progress.bestCaseValue = 0;
            _gameData.Progress.bestCaseItem = Config.skins[0].id;
        }

        IsInitialized = true;
    }

    void Update()
    {
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Space) && testSpaceButton)
        {
            Test();
        }
#endif
    }

    bool goToNextLocation;

    async void CheckConnection() 
    {
        while (true)
        {
            if (!Application.isPlaying)
                break;

            if (!internetRequired)
            {
                InternetConnection = !testConnection ? true : isTestConnected;
                await Task.Delay(1000);
            }
            else
            {
                if (!checkInternetConnection)
                {
                    await Task.Delay(1000);
                    InternetConnection = !testConnection ? true : isTestConnected;
                }
                else 
                {
                    using (UnityWebRequest www = UnityWebRequest.Get($"https://google.com"))
                    {
                        www.timeout = 5;
                        await www.SendWebRequest();

                        InternetConnection = !testConnection ? www.result == UnityWebRequest.Result.Success : isTestConnected;

                        await Task.Delay(1000);
                    }
                }
            }
        }
    }

    public void ShowConnectionScreen(bool show) 
    {
        if (show)
        {
            screensBeforeConnectionScreen = ui.ActiveScreens.ToArray();
            ui.ShowScreen<ConnectionScreen>();
        }
        else 
        {
            if (screensBeforeConnectionScreen.Length > 0)
                ui.ShowScreen(screensBeforeConnectionScreen);
            else
                ShowStartScreen();
        }
    }

    public void Test() 
    {
        IsTestButtonPressed = true;

        CompleteLevel();

        //if(!IsInitialLevel() && testBonusCases)
        //    OpenBonusGame();

        IsTestButtonPressed = false;
    }

    public void Restart() 
    {
        StopCoroutine("RestartCoroutine");
        StartCoroutine("RestartCoroutine");

        ReportHomeOrRestart();
    }

    public void LeaveGame() 
    {
        var skin = GetSkin(_gameData.Progress.skin);

        if (Skin != skin)
            Skin = skin;

        State = GameState.Start;

        ReportHomeOrRestart();
    }

    private void ReportHomeOrRestart() 
    {
        var parameters = new Dictionary<string, object>();

        parameters.Add("location", Location.id);
        parameters.Add("level", Level + 1);

        AnalyticEvents.ReportEvent("level_restart", parameters);
    }

    public bool IsRestarting { get; private set; }

    IEnumerator RestartCoroutine() 
    {
        IsRestarting = true;

        var skin = GetSkin(_gameData.Progress.skin);

        if (Skin != skin)
            Skin = skin;

        State = GameState.Start;

        IsRestarting = false;

        yield return new WaitForSeconds(0.1f);

        State = GameState.Play;

        PlayLocationMusic(true);
    }

    public void SetTutorial(int tutorial) 
    {
        Extensions.Clone(Config.tutorials[tutorial], Config.locations[0].levels[0]);
    }

    public void SetGameState(string state) 
    {
        if (Enum.TryParse(state, out GameState s)) State = s;
        else Debug.LogWarning($"State {state} not exist!");
    }

    public void GameOver(Trigger.Type type)
    {
        deathType = type;

        if (State == GameState.Play)
            State = GameState.LevelFailed;
    }

    public void NextLevel()
    {
        BonusKeys += CollectedKeys;

        var level = Level;

        if (!IsGiftLevel)
        {
            giftLevelCount++;

            if (giftLevelCount >= config.giftLevelRate)
            {
                giftLevelCount = 0;

                IsGiftLevel = true;
            }

            level++;
        }
        else
        {
            IsGiftLevel = false;
        }

        if (level >= Location.levels.Length)
        {
            if (Location != config.locations.Last())
            {
                ui.ShowScreen<NextLocationScreen>();
                goToNextLocation = true;
            }
            else
            {
                Debug.LogWarning("Reached last location and last level!");
                State = GameState.Start;
            }
        }
        else
        {
            Level = level;
            State = GameState.Start;
        }

        if (BonusKeys >= config.locationKeysCount && !IsGetLocationBonus)
        {
            IsGetLocationBonus = true;

            _gameData.Progress.locationBonus = IsGetLocationBonus;

            OpenBonusGame();
        }

        appReview.RepeatCounter++;
    }

    public void NextLocation() 
    {
        State = GameState.None;

        IsGetLocationBonus = false;

        Location = config.locations[Array.IndexOf(config.locations, Location) + 1];
        Level = 0;

        State = GameState.Start;
    }

    public void CompleteLevel() 
    {
        if (IsInitialLevel())
            _gameData.Progress.completeTutorial = true;

        _gameData.GetLevel(Location.id, Level).completed = true;
        _gameData.Save();

        State = GameState.LevelComplete;
    }

    public void ShowCompleteScreen() 
    {
        ui.ShowScreen<LevelCompleteScreen>();
    }

    public void PlayMenuMusic(bool play) 
    {
        if (play) soundManager.PlayMusic(config.menuMusic);
        else soundManager.StopMusic();
    }

    public void PlayLocationMusic(bool play)
    {
        if (play) soundManager.PlayMusic(Location.music);
        else soundManager.StopMusic();
    }

    public void ToggleSound() 
    {
        soundManager.ToggleSound();
    }

    public void OpenSkins() 
    {
        ui.ShowScreen<SkinsScreen>();
    }

    public void GetBonusKeys() 
    {
        RemainingKeys = config.locationKeysCount;
    }

    public void OpenBonusGame() 
    {
        if(!skinUnlockedByProgress)
            CurrentUnlockedSkin = null;

        BonusKeys = 0;
        GetBonusKeys();

        ui.ShowScreen<BonusGameScreen>();
    }

    public void OpenBonusCases()
    {
        IsBonusCaseGame = true;
        IsBonusCaseInteractable = true;

        ui.ShowScreen<BonusCaseScreen>();
    }

    public void CloseBonusGame()
    {
        IsBonusCaseGame = false;
        BonusKeys = 0;
        moreKeysCount = 0;
        State = GameState.Start;

        skinUnlockedByProgress = false;

        if (goToNextLocation)
            ui.ShowScreen<NextLocationScreen>();
        else
            ui.ShowScreen<StartScreen>();
    }

    public void SkinUnlockedScreen()
    {
        ui.ShowScreen<SkinUnlockedScreen>();
	}

    public void CloseSkinsScreen() 
    {
        if (ui.GetScreen<SkinsScreen>().IsUnlocking)
            return;

        ShowStartScreen();
    }

    public void SkipBonusGame()
    {
        moreKeysCount = 0;
        State = GameState.Start;

        ShowStartScreen();
    }

    public void ShowStartScreen() 
    {
        if (ui.ActiveScreens.Contains(ui.GetScreen<CompleteStoryScreen>()))
        {
            ui.ShowScreen<StartScreen>();
        }
        else 
        {
            if (IsLastLevel() && IsLevelCompleted())
                ui.ShowScreen<CompleteStoryScreen>();
            else
            {
                if (IsLastLocationLevel() && IsLevelCompleted())
                {
                    ui.ShowScreen<NextLocationScreen>();
                }
                else
                    ui.ShowScreen<StartScreen>();
            }
        }
    }

    public Configuration.BonusCase[] GetBonusCases(int capacity) 
    {
        Configuration.BonusCase[] value = new Configuration.BonusCase[capacity];
        
        var moneyCases = config.bonusCases.Where(x => x.type == Configuration.BonusCase.Type.Money).ToArray();
        
        for (int i = 0; i < value.Length; i++) 
        {
            value[i] = moneyCases[UnityEngine.Random.Range(0, moneyCases.Length)];
        }
        
        var rare = GetAvailableSkins(Configuration.Skin.Rarity.Rare, Configuration.Skin.Availability.Case);

        rare.Shuffle();
        
        if (rare.Length > 0)
        {
            rare.Shuffle();

            var bonusSkin = new Configuration.BonusCase();
            bonusSkin.type = Configuration.BonusCase.Type.Skin;
            bonusSkin.skin = rare[0];

            value[UnityEngine.Random.Range(0, value.Length)] = bonusSkin;
        }

        return value;
    }

    public void GetBonusCase(Configuration.BonusCase bonus) 
    {
        IsBonusCaseInteractable = false;

        RemainingKeys--;

        switch (bonus.type)
        {
            case Configuration.BonusCase.Type.Money:
                StopCoroutine("UpdateBonusCaseMoneyCoroutine");
                StartCoroutine("UpdateBonusCaseMoneyCoroutine", bonus.money);
                break;
            case Configuration.BonusCase.Type.Skin:
                var item = bonus.skin;

                _gameData.UnlockSkin(item.id);
                CurrentUnlockedSkin = GetSkin(item.id);

                bonus.money = item.cost;

                IsBonusCaseInteractable = true;
                break;
        }

        if (RemainingKeys <= 0)
            Invoke("OpenMoreKeys", 2.0f);
    }

    public void UnlockAndSelectTheSkin(Configuration.Skin unclockedSkin)
    {
        skinUnlockedByProgress = true;

        CurrentUnlockedSkin = null;

        _gameData.UnlockSkin(unclockedSkin.id);

        CurrentUnlockedSkin = GetSkin(unclockedSkin.id);
    }

    WaitForSeconds updateBonusCaseMoneyDelay = new WaitForSeconds(1.0f);

    IEnumerator UpdateBonusCaseMoneyCoroutine(int money) 
    {
        yield return updateBonusCaseMoneyDelay;
        Money += money;
        yield return updateBonusCaseMoneyDelay;
        ui.GetScreen<BonusCaseScreen>().UpdateMoneyCounter();

        IsBonusCaseInteractable = true;
    }

    private void OpenMoreKeys() 
    {
        bool isAdReady = Advertisement.GetPlacement("Reward_Keys").IsReady();

        if (moreKeysCount < config.moreKeysCount && isAdReady)
        {
            moreKeysCount++;

            ui.GetScreen<BonusCaseScreen>().ShowGetMoreKeys();
        }
        else 
        {
            ui.GetScreen<BonusCaseScreen>().Continue();
        }
    }

    public void ShowTerms() 
    {
        AnalyticEvents.ReportEvent("terms_go");
        Application.OpenURL(config.termsUrl);
    }

    public void ShowPrivacyPolicy()
    {
        AnalyticEvents.ReportEvent("policy_go");
        Application.OpenURL(config.privacyPolicyUrl);
    }

    public void AcceptTerms() 
    {
        AnalyticEvents.ReportEvent("terms_policy_accept");

        PlayerPrefs.SetInt("terms", 1);
        PlayerPrefs.Save();

        State = GameState.None;
        State = GameState.Start;
    }

    public void AcceptConsent(bool accept) 
    {
        PlayerPrefs.SetInt("consent", Convert.ToInt16(accept));
        PlayerPrefs.Save();

        Advertisement.SetConsent(accept);

        State = GameState.None;
        State = GameState.Start;

        var parameters = new Dictionary<string, object>();
        parameters.Add("accept", accept);
        AnalyticEvents.ReportEvent("persadspopup_request", parameters);
    }

    public void RestorePurchases() 
    {
        ui.ShowScreen<RestorePurchasesScreen>();

        _iap.Restore();
    }

    private void OnRestorePurchases() 
    {
        ui.ShowScreen<StartScreen>();
    }

    private void OnPurchase(IAP.Item item) 
    {
        // Unlock purchased skin
        var skin = config.skins.FirstOrDefault(x => x.storeId.Equals(item.id));

        if (skin != null) 
        {
            _gameData.UnlockSkin(skin.id);
        }
    }
}
