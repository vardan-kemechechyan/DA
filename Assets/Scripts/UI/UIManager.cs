using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UI;
using System.Linq;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    public static Action<CameraState, CameraState> OnChangeCameraState;

    [SerializeField] CameraState cameraState;
    [SerializeField] GameObject addMoneyVew;

    public CameraState CameraState
    {
        get => cameraState;
        set
        {
            if (cameraState != value)
            {
                foreach (var c in cameras)
                    c.enabled = false;

                cameras[value == CameraState.Main ? 0 : 1].enabled = true;

                if (value != CameraState.Main)
                    deathCameraAnimator.Play(value.ToString());

                OnChangeCameraState?.Invoke(value, cameraState);
                cameraState = value;
            }
        }
    }

    [SerializeField] CursorTrail swipeRenderer;

    [SerializeField] Camera[] cameras;
    [SerializeField] Animator deathCameraAnimator;

    [SerializeField] float gameOverDelayTime = 2.0f;

    public List<UIScreen> screens;

    WaitForSeconds gameOverDelay;

    GameData _gameData;
    GameManager _gameManager;
    SoundManager _soundManager;
    IAP _iap;

    List<UIScreen> activeScreens;
    public List<UIScreen> ActiveScreens => activeScreens;

    public bool IsInitialized { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    public void Initialize(GameData gameData, GameManager gameManager, SoundManager soundManager, IAP iap)
    {
        _gameData = gameData;
        _gameManager = gameManager;
        _soundManager = soundManager;
        _iap = iap;

        addMoneyVew.SetActive(true);

        activeScreens = new List<UIScreen>();

        foreach (Transform t in transform)
        {
            if (t.TryGetComponent(out UIScreen screen))
                screens.Add(screen);
        }

        GetScreen<TermsScreen>().Init(gameManager);
        GetScreen<ConsentScreen>().Init(gameManager);
        GetScreen<BonusCaseScreen>().Init(gameManager);
        GetScreen<GameScreen>().Init(gameManager);
        GetScreen<InitialScreen>().Init(gameManager);
        GetScreen<LevelCompleteScreen>().Init(gameManager, gameData);
        GetScreen<LevelFailedScreen>().Init(gameManager);
        GetScreen<NextLocationScreen>().Init(gameManager);
        GetScreen<SkinsScreen>().Init(gameManager, gameData, iap);
        GetScreen<SkinUnlockedScreen>().Init(gameManager, gameData);
        GetScreen<StartScreen>().Init(gameManager, gameData, iap);
        GetScreen<CompleteStoryScreen>().Init(gameManager);

        foreach (var s in screens)
            s?.Close();

        gameOverDelay = new WaitForSeconds(gameOverDelayTime);

        GameManager.OnChangeGameState += OnChangeGameState;

        IsInitialized = true;
    }

    private void OnChangeGameState(GameState state, GameState previousState) 
    {
        if (state == GameState.Play)
            swipeRenderer.enabled = true;
        else
        {
            swipeRenderer.enabled = false;
        }

        switch (state) 
        {
            case GameState.Start:
                if (!_gameManager.IsRestarting) 
                {
                    if (!PlayerPrefs.HasKey("terms"))
                    {
                        ShowScreen<TermsScreen>();
                    }
                    else
                    {
                        if (!PlayerPrefs.HasKey("consent"))
                        {
                            ShowScreen<ConsentScreen>();
                        }
                        else
                        {
                            Advertisement.SetConsent(Convert.ToBoolean(PlayerPrefs.GetInt("consent")));

                            if (_gameData.Progress.completeTutorial)
                                _gameManager.ShowStartScreen();
                            else ShowScreen<InitialScreen>();

                            _gameManager.checkInternetConnection = true;
                        }
                    }
                }
                break;
            case GameState.Play:
                ShowScreen<GameScreen>();
                break;
            case GameState.Pause:
                //ShowScreen<PauseScreen>();
                break;
            case GameState.LevelComplete:
                if(_gameManager.IsTestButtonPressed || !_gameManager.Config.endLevelWithElevator)
                    ShowScreen<LevelCompleteScreen>();
                break;
            case GameState.LevelFailed:
                StopCoroutine("LevelFailedCoroutine");
                StartCoroutine("LevelFailedCoroutine");
                break;
        }
    }

    IEnumerator LevelFailedCoroutine()
    {
        switch (_gameManager.deathType) 
        {
            case Trigger.Type.Laser:
                CameraState = CameraState.LaserDeath;
                break;
            case Trigger.Type.Camera:
                CameraState = CameraState.CameraDeath;
                break;
            case Trigger.Type.Poison:
                CameraState = CameraState.PoisonDeath;
                break;
            case Trigger.Type.Flamer:
                CameraState = CameraState.LaserDeath;
                break;
        }

        ShowScreen<DeathScreen>();
        yield return gameOverDelay;
        ShowScreen<LevelFailedScreen>();
        CameraState = CameraState.Main;
    }

    public T GetScreen<T>()
    {
        UIScreen value = null;

        foreach (var screen in screens)
        {
            if (screen.GetType() == typeof(T))
            {
                value = screen;
            }
        }

        return (T)(object)value;
    }

    public void Show(UIScreen screen)
    {
        ShowScreen(screen);
    }

    public void ShowScreen<T>()
    {
        FirebaseManager.SetCustomKey("previous_screens", GetActiveScreens());

        foreach (var s in screens)
        {
            if (s.GetType() == typeof(T))
            {
                activeScreens.Add(s);

                s.Open();
            }
            else
            {
                activeScreens.Remove(s);

                if (s.IsOpen)
                    s.Close();
            }
        }

        FirebaseManager.SetCustomKey("active_screens", GetActiveScreens());
    }

    public void ShowScreen(UIScreen screen)
    {
        FirebaseManager.SetCustomKey("previous_screens", GetActiveScreens());

        foreach (var s in screens)
        {
            if (s.Equals(screen))
            {
                activeScreens.Add(s);

                s.Open();
            }
            else
            {
                activeScreens.Remove(s);

                s.Close();
            }
        }

        FirebaseManager.SetCustomKey("active_screens", GetActiveScreens());
    }

    public void ShowScreen(UIScreen[] screens)
    {
        FirebaseManager.SetCustomKey("previous_screens", GetActiveScreens());

        var scr = "";

        foreach (var s in screens)
            scr += s.GetType().ToString();

        Debug.Log($"{GetActiveScreens()} => {scr}");

        var screensList = screens.ToList();

        foreach (var s in this.screens)
        {
            if (screensList.Contains(s))
            {
                activeScreens.Add(s);

                s.Open();
            }
            else
            {
                activeScreens.Remove(s);

                s.Close();
            }
        }

        FirebaseManager.SetCustomKey("active_screens", GetActiveScreens());
    }

    private string GetActiveScreens() 
    {
        string value = "";

        foreach (var s in activeScreens)
            value += $"{s.GetType()}, ";

        if (value.Contains(", "))
            value = value.Remove(value.Length - 2);

        return value;
    }
}
