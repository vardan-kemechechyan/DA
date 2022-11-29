using Leopotam.Ecs;
using System.Collections;
using UnityEngine;
using Voody.UniLeo;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using System.Linq;
using System;
using System.Collections.Generic;

sealed class Startup : MonoBehaviour
{
    EcsWorld _world;
    EcsSystems _updateSystems;
    EcsSystems _fixedUpdateSystems;

    [SerializeField] Animation splash;

    [SerializeField] UIManager ui;
    [SerializeField] GameData gameData;
    [SerializeField] GameManager gameManager;
    [SerializeField] SoundManager soundManager;
    [SerializeField] AnalyticEvents analyticEvents;
    [SerializeField] ObjectPool pool;
    [SerializeField] FirebaseManager firebase;
    [SerializeField] IAP iap;

#if FACEBOOK
    [SerializeField] FacebookManager facebook;
#endif

    [SerializeField] TenjinManager tenjin;

    //[SerializeField] Volume volume;

    private IEnumerator Start()
    {
        yield return new WaitForSeconds(0.1f);

        Application.targetFrameRate = 60;

        //if (Application.isMobilePlatform)
        //    QualitySettings.vSyncCount = 0;

        //UnityThread.initUnityThread();

        //int qualityLevel = QualitySettings.GetQualityLevel();
        //
        //if (volume != null && qualityLevel <= 0)
        //{
        //    Debug.LogWarning("Force disable postprocessing volume on low end device!");
        //
        //    volume.enabled = false;
        //}
        //else 
        //{
        //    volume.enabled = true;
        //}

        pool.Initialize(gameManager.Config);

        _world = new EcsWorld();
        _updateSystems = new EcsSystems(_world);

        _updateSystems.ConvertScene();

        _updateSystems.Inject(gameData);
        _updateSystems.Inject(pool);
        _updateSystems.Inject(ui);
        _updateSystems.Inject(gameManager);
        _updateSystems.Inject(soundManager);

        _updateSystems
            .Add(new InputSystem())
            .Add(new PlayerMovementSystem())
            .Add(new PlayerAnimationSystem())
            .Add(new ObstacleSystem())
            .Add(new LevelScrollSystem())
            .Add(new TimeScaleSystem())

            //.OneFrame<SwipeEvent>()
            .OneFrame<AnimationEvent>()

            .Init();

        firebase.Initialize();
        analyticEvents.Initialize();

#if FACEBOOK
        facebook.Initialize();
#endif
        //tenjin.Initialize();

        iap.Initialize();

        Advertisement.Initialize(gameManager);

        Advertisement.OnOpen += (placement) =>
        {
            if (placement.type == Advertisement.Placement.Type.Interstitial) 
            {
                if (placement.placement.Equals("Interstitial_restart"))
                {
                    var parameters = new Dictionary<string, object>();
                    parameters.Add("location", gameManager.Location.id);
                    parameters.Add("level", gameManager.Level + 1);

                    AnalyticEvents.ReportEvent("restart_interstitial", parameters);
                }
                else
                {
                    var lvl = new Dictionary<string, object>();
                    lvl.Add("level", gameManager.Level + 1);

                    AnalyticEvents.ReportEvent("Interstitial", lvl);
                }
            }
        };

        //yield return new WaitUntil(() => Advertisement.IsInitialized);

        FirebaseManager.OnFetch += ApplyRemoteConfig;

        gameData.Initialize(gameManager.Config);
        ui.Initialize(gameData, gameManager, soundManager, iap);
        gameManager.Initialize(gameData, iap);
        soundManager.Initialize();

        gameManager.State = GameState.Start;

        splash.Play();

        yield return null;
    }

    private void ApplyRemoteConfig() 
    {
        // AdMob
        bool timeout10sec = FirebaseManager.GetRemoteConfigBoolean("Timeout_10sec");
        bool timeout30sec = FirebaseManager.GetRemoteConfigBoolean("Timeout_30sec");
        bool timeout45sec = FirebaseManager.GetRemoteConfigBoolean("Timeout_45sec");
        bool timeout60sec = FirebaseManager.GetRemoteConfigBoolean("Timeout_60sec");

        if (timeout60sec)
            Advertisement.interstitialDelay = 60;
        else if (timeout45sec)
            Advertisement.interstitialDelay = 45;
        else if (timeout30sec)
            Advertisement.interstitialDelay = 30;
        else if (timeout10sec)
            Advertisement.interstitialDelay = 10;

        // Disabled on current release
        // Switch tutorial
        //bool tutorialLasers5 = FirebaseManager.GetRemoteConfigBoolean("tutorial_5lasers");
        //bool tutorialLasers8 = FirebaseManager.GetRemoteConfigBoolean("tutorial_8lasers");
        //
        //Debug.Log($"Firebase remote config: tutorial_5lasers:{tutorialLasers5} tutorial_8lasers:{tutorialLasers8}");
        //
        //
        //if (tutorialLasers5)
        //    gameManager.SetTutorial(0);
        //else if (tutorialLasers8)
        //    gameManager.SetTutorial(1);

        // Tutorial on clue levels 1,2
        bool extendedTutorial = FirebaseManager.GetRemoteConfigBoolean("Tutorial_v1_update");
        gameManager.Config.locations[1].levels[0].obstacles.Last().tutorial = extendedTutorial;
        gameManager.Config.locations[1].levels[1].obstacles.Last().tutorial = extendedTutorial;

        // Internet reachability
        var internetRequired = FirebaseManager.GetRemoteConfigBoolean("no_internet");        
        gameManager.internetRequired = internetRequired;

        // Slowmotion
        bool slowmo = FirebaseManager.GetRemoteConfigBoolean("slowmo");
        gameManager.Config.slowmotion = slowmo;

        // Allow jump out of range
        bool noPriorJumps = FirebaseManager.GetRemoteConfigBoolean("no_prior_jumps");      
        gameManager.Config.allowJumpOutOfRange = !noPriorJumps;

        // Swipe helper
        int swipeHelper = FirebaseManager.GetRemoteConfigInteger("swipe_helper");
        gameManager.Config.correctSwipeChance = swipeHelper;

        var remoteConfigValues = "";

        remoteConfigValues += $" interstitial_delay: {Advertisement.interstitialDelay} |";
        remoteConfigValues += $" extended_tutorial: {extendedTutorial} |";
        remoteConfigValues += $" internet_required: {internetRequired} |";
        remoteConfigValues += $" slowmo: {slowmo} |";
        remoteConfigValues += $" no_prior_jumps: {noPriorJumps} |";
        remoteConfigValues += $" swipe_helper: {swipeHelper} |";

        FirebaseManager.SetCustomKey("remote_config", remoteConfigValues);
        Debug.Log($"Remote config: {remoteConfigValues}");
    }

    void OnApplicationFocus(bool focus)
    {
        if (analyticEvents.IsInitialized()) 
        {
            if (focus)
                AnalyticEvents.ReportEvent("foregroup_app");
            else
                AnalyticEvents.ReportEvent("background_app");
        }
    }

    void Update()
    {
        _updateSystems?.Run();
    }

    void OnDestroy()
    {
        if (_updateSystems != null)
        {
            _updateSystems.Destroy();
            _updateSystems = null;
        }

        if (_fixedUpdateSystems != null)
        {
            _fixedUpdateSystems.Destroy();
            _fixedUpdateSystems = null;
        }

        _world?.Destroy();
        _world = null;
    }
}
