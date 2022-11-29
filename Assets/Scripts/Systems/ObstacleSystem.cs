using Leopotam.Ecs;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

sealed class ObstacleSystem : IEcsInitSystem, IEcsRunSystem
{
    //private readonly EcsWorld _world = null;

    private readonly EcsFilter<Player> playerFilter = null;
    private readonly EcsFilter<Obstacle> obstacleFilter = null;
    private readonly EcsFilter<TimingZone> timingZoneFilter = null;
    private readonly EcsFilter<LevelScroll> scrollFilter = null;

    private readonly GameData _gameData = null;
    private readonly GameManager _gameManager = null;
    private readonly SoundManager _soundManager = null;
    private readonly UIManager _ui = null;
    private readonly ObjectPool _pool = null;

    int obstaclesPoolCount = 2;
    float disposeObstacleDistance = -10.0f;

    float slowMotionRange = 7.0f;

    List<ObstacleController> obstacles = new List<ObstacleController>();

    int spawnObstacle;
    float distance;
    float pathLength;
    float speedByLevel;

    Obstacle currentObstacle;
    Configuration.Obstacle.Settings sharedObstacleSettings;

    public static bool HasTutorial { get; private set; }

    bool isMinDistance;
    bool isCriticalDistance;

    int passedObstacles;

    float failedDelay = 1.0f;
    float failedDelayTime;

    Clue tutorialClue;

    bool isStarted;

    int giftReward;

    bool timingZoneEnabled;

    int poisonPerLevel = 2;
    int poisonPerLevelCount;

    float rewardMultiplier;
    float obstaclesReward;
    float cluesReward;

    float spawnSceneryPosition;
    SceneryController spawnScenery;

    ObjectPool.Pool currentPropsPool;

    ElevatorController elevator;

    float distanceToElevator = 50.0f;

    bool skipSlowMotion;

    ObjectPool.Pool currentPool;

    Gift finalGift;

    private ObjectPool.Pool GetPool(string key)
    {
        return _pool.pools.ContainsKey(key) ? _pool.pools[key] : null;
    }

    public void Init()
    {
        GameManager.OnChangeGameState += OnChangeGameState;
        GameManager.OnHitTrigger += OnHitTrigger;
        GameManager.OnHitClue += OnCollectClue;
        GameManager.OnHitKey += OnCollectKey;
        GameManager.OnGift += OnCollectGift;
        GameManager.OnSwipe += OnSwipe;
    }

    public void Run()
    {
        if (_gameManager.State == GameState.Start) 
        {
            ref var player = ref playerFilter.Get1(0);

            failedDelayTime = failedDelay;

            isStarted = false;

            player.passedObstacles = 0;
            player.collectedClues = 0;
            player.collectedKeys = 0;
        }
        else if (_gameManager.State == GameState.Play) 
        {          
            // Spawn obstacle
            ref var player = ref playerFilter.Get1(0);

            if (spawnObstacle < _gameManager.GetCurrentLevel().obstacles.Length && obstacles.Count < obstaclesPoolCount)
            {
                obstacles.Add(SpawnObstacle(_gameManager.GetCurrentLevel().obstacles[spawnObstacle], false));

                spawnObstacle++;
            }
            else
            {
                if (!isStarted) 
                {
                    isStarted = true;
                    obstacles[0]?.Show(true);

                    SetTimimgZone(obstacles[0].transform, 
                        _gameManager.GetCurrentLevel().obstacles[player.passedObstacles].timingZoneHint);

                    if (_gameManager.Config.endLevelWithElevator)
                    {
                        if (!elevator)
                            elevator = UnityEngine.Object.Instantiate(_gameManager.Config.elevatorPrefab).GetComponent<ElevatorController>();

                        foreach (Transform t in _gameManager.Location.lane.transform)
                        {
                            if (t.name.Contains("wall"))
                            {
                                elevator.SetMaterial(t.GetComponent<MeshRenderer>().sharedMaterial);
                                break;
                            }
                        }

                        elevator.transform.position = new Vector3(0, 0, pathLength + distanceToElevator);
                        elevator.gameObject.SetActive(true);
                        elevator.IsOpen = false;
                    }
                }
            }

            if (obstacles.Last().transform.position.z < -10.0f)
            {
                if(elevator)
                    elevator.IsOpen = true;

                if (!_gameManager.IsGiftLevel && !string.IsNullOrEmpty(_gameManager.Location.clue) && player.collectedClues < _gameManager.Location.levels[_gameManager.Level].
                    obstacles.Where(x => !string.IsNullOrEmpty(x.clue)).Count()) 
                {
                    // Clue not collected!
                    _ui.GetScreen<LevelFailedScreen>().FailedMessage("Clue not collected!");
                
                    _gameManager.State = GameState.LevelFailed;
                
                    failedDelayTime -= Time.deltaTime;
                
                    if (failedDelayTime <= 0) 
                    {
                        ref var entity = ref playerFilter.GetEntity(0);
                
                        entity.Get<AnimationEvent>() = new AnimationEvent
                        {
                            animation = "Idle",
                            speed = player.move.animationSpeed
                        };
                
                        return;
                    }
                }

                obstaclesReward = _gameManager.GetCurrentLevel().obstacleReward * player.passedObstacles;
                cluesReward = _gameManager.GetCurrentLevel().clueReward * player.collectedClues;

                if (giftReward <= 0)
                    giftReward = 10;

                _ui.GetScreen<LevelCompleteScreen>()
                    .SetLevelReward(_gameManager.IsGiftLevel ? 
                    giftReward : Mathf.FloorToInt(((float)obstaclesReward + (float)cluesReward) * rewardMultiplier));

                if (!_gameManager.Location.id.Equals("tutorial"))
                {
                    if (_gameManager.IsGiftLevel)
                        AnalyticEvents.ReportEvent("bonuslvl_complete");
                }

                if (!elevator || !elevator.gameObject.activeInHierarchy)
                {
                    if (!_gameManager.IsGiftLevel)
                        LevelCompleteSound();
                }

                _gameManager.CompleteLevel();
            }
        }

        foreach (var i in scrollFilter)
        {
            ref var scroll = ref scrollFilter.Get1(i);

            if (scroll.type == ComponentType.Scenery) 
            {
                if (!scroll.transform)
                {
                    // Dispose if entity removed from pool
                    ref var entity = ref scrollFilter.GetEntity(i);
                    entity.Destroy();
                }
                else
                {
                    if (scroll.transform.gameObject.activeInHierarchy)
                    {
                        if (scroll.transform.position.z <= -10.0f)
                        {
                            scroll.transform.gameObject.SetActive(false);
                            SpawnScenery();
                        }
                    }
                }
            }
        }

        UpdateChildItems();
        ScrollObstacles();

        if (_gameManager.isRunning && elevator && elevator.gameObject.activeInHierarchy) 
        {
            ref var player = ref playerFilter.Get1(0);

            if (player.transform.position.z >= elevator.transform.position.z + 1.0f) 
            {
                _gameManager.isRunning = false;
                _gameManager.ShowCompleteScreen();

                _soundManager.PlaySound(5);

                player.animator?.Play("Idle");
                player.sharedAnimator?.Play("Idle");

                elevator.IsOpen = false;
            }
        }
    }

    private void SpawnScenery() 
    {
        var pool = GetPool($"Props/{_gameManager.Location.id}");

        if (pool == null || pool.Count() <= 0)
            return;

        if (spawnScenery)
        {
            spawnSceneryPosition = spawnScenery.transform.position.z;
            spawnSceneryPosition += spawnScenery.width / 2;
            spawnSceneryPosition += _gameManager.Config.sceneryRange;
        }
        else
            spawnSceneryPosition = 0;

        spawnScenery = pool.Spawn().GetComponent<SceneryController>();

        float shifting = (spawnScenery.width / 2) + _gameManager.Config.sceneryGap;

        foreach (var obstacle in obstacles)
        {
            if (Math.Abs(spawnSceneryPosition - obstacle.transform.position.z) < shifting) 
            {
                spawnSceneryPosition += shifting;
                break;
            } 
        }

        spawnScenery.transform.position = new Vector3(0, 0, spawnSceneryPosition);
    }

    private ObstacleController SpawnObstacle(Configuration.Obstacle obstacle, bool preview)
    {
        FirebaseManager.SetCustomKey("spawn_obstacle", 
            $"location{_gameManager.Location.id} level:{_gameManager.Level} obstacle: {Array.IndexOf(_gameManager.GetCurrentLevel().obstacles, obstacle)}");

        ref var player = ref playerFilter.Get1(0);
        var level = _gameManager.GetCurrentLevel();

        if (obstacles.Count > 0) distance = obstacles.Last().transform.position.z + sharedObstacleSettings.distance;
        else distance = sharedObstacleSettings.distance;

        var obj = UnityEngine.Object.Instantiate(obstacle.prefab,
            new Vector3(0, 0, distance), Quaternion.identity, _gameManager.transform);

        obj.name = obj.name.Replace("(Clone)", "");

        var controller = obj.transform.GetComponent<ObstacleController>();

        // Spawn projector
        controller.projector = UnityEngine.Object.Instantiate(_gameManager.Config.projectorPrefab, obj.transform);
        controller.projector.transform.localPosition = new Vector3(0, 13.5f, -(sharedObstacleSettings.tutorialDistance + 7.0f));

        switch (_gameManager.Config.laserColorMode)
        {
            case LaserColorMode.Color:
                controller.startColor = obstacle.startColor;
                controller.endColor = obstacle.endColor;
                break;
            case LaserColorMode.Random:
                int a = UnityEngine.Random.Range(0, _gameManager.Config.randomLaserColors.Length);
                controller.startColor = _gameManager.Config.randomLaserColors[a];
                controller.endColor = _gameManager.Config.randomLaserColors[Extensions.RandomExcept(_gameManager.Config.randomLaserColors.Length, new int[] { a })];
                break;
        }

        controller.Init();

        CheckTutorial();

        // Spawn clue
        if (!string.IsNullOrEmpty(_gameManager.Location.clue) && !string.IsNullOrEmpty(level.obstacles[spawnObstacle].clue))
        {
            var clue = UnityEngine.Object.Instantiate(_gameManager.Config.cluePrefab, obj.transform);

            var letter = _gameManager.Config.alphabet.FirstOrDefault(x => x.name.Equals(level.obstacles[spawnObstacle].clue.ToLower()));

            if (letter)
            {
                var part = UnityEngine.Object.Instantiate(letter, clue.transform);

                clue.transform.eulerAngles = new Vector3(clue.transform.eulerAngles.x, 0, clue.transform.eulerAngles.z);
                part.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
                part.transform.localPosition = new Vector3(0, -0.5f, 0);
                part.transform.localEulerAngles = new Vector3(0, 180, 0);
                part.transform.SetParent(clue.GetComponent<Clue>().part.transform);

                controller.clue = clue.transform.GetChild(0).gameObject;
            }
            else
            {
                Debug.LogWarning($"Letter {level.obstacles[spawnObstacle].clue.ToUpper()} not found!");
            }
        }

        // Spawn key
        if (!_gameManager.IsGetLocationBonus && obstacle.key)
        {
            if (Array.IndexOf(_gameManager.Config.locations, _gameManager.Location) > 0) 
            {
                var key = UnityEngine.Object.Instantiate(_gameManager.Config.keyPrefab, obj.transform);
                controller.key = key;
            }
        }

        // Spawn gift
        if (_gameManager.IsGiftLevel) 
        {
            var gift = UnityEngine.Object.Instantiate(_gameManager.Config.giftPrefab, obj.transform).GetComponent<Gift>();

            gift.Setup(false, UnityEngine.Random.Range(1, 6) * 10);
            gift.transform.localPosition = player.anchors[UnityEngine.Random.Range(1, player.anchors.Length)].position * 2;

            if (obstacle.Equals(level.obstacles.Last())) 
            {
                finalGift = UnityEngine.Object.Instantiate(_gameManager.Config.giftPrefab, obj.transform).GetComponent<Gift>();

                finalGift.Setup(true, 0);
                finalGift.transform.localPosition = player.anchors[4].position * 2;

                finalGift.transform.localPosition = new Vector3(
                    finalGift.transform.localPosition.x,
                    finalGift.transform.localPosition.y,
                    finalGift.transform.localPosition.z + 80.0f);
            }
        }

        return controller;
    }

    private ObstacleController GetObstacleController(Obstacle obstacle) 
    {
        return obstacles.FirstOrDefault(x => x.transform.Equals(obstacle.transform));
    }

    private Configuration.Obstacle GetObstacleData(string name) 
    {
        return _gameManager.GetCurrentLevel().
            obstacles.FirstOrDefault(x => x.prefab.name.Contains(name));
    }

    private string GetObstaclePrefix(ref Obstacle obstacle) 
    {
        switch (obstacle.type) 
        {
            case ObstacleType.Laser:
                return "S";
            case ObstacleType.Movable:
                return "MOV";
            case ObstacleType.Combined:
                return "COMB";
            case ObstacleType.Camera:
                return "CAM";
            case ObstacleType.Flamer:
                return "FL";
            default:
                return "S";
        }
    }

    float distanceToObstacle;
    float distanceToFirstObstacle;

    private void ScrollObstacles() 
    {
        if (_gameManager.State != GameState.LevelFailed && _gameManager.State != GameState.Start) 
        {
            ref var player = ref playerFilter.Get1(0);

            if(obstacles.Count > 0)
                distanceToFirstObstacle = obstacles[0].transform.position.z - player.transform.position.z;

            foreach (var i in obstacleFilter)
            {
                ref var obstacle = ref obstacleFilter.Get1(i);

                //obstacle.transform.Translate(-Vector3.forward * _gameManager.FrameDependentDelta() * speedByLevel);
                obstacle.transform.Translate(-Vector3.forward * Time.deltaTime * speedByLevel);

                distanceToObstacle = Mathf.Abs(player.transform.position.z - obstacle.transform.position.z);

                if (obstacles.Count > 0) 
                {
                    // Is to far to jump trough
                    if (distanceToFirstObstacle <= sharedObstacleSettings.minDistance)
                        isMinDistance = true;
                    else
                        isMinDistance = false;

                    // Is to close to avoid
                    if (distanceToFirstObstacle <= sharedObstacleSettings.criticalDistance)
                        isCriticalDistance = true;
                    else
                        isCriticalDistance = false;
                }

                // Select
                if (!obstacle.selected && distanceToObstacle <= sharedObstacleSettings.selectDistance)
                {
                    obstacle.selected = true;

                    _gameManager.importantMove = obstacle.importantMove;
                    _gameManager.correctMoves = obstacle.helperMoves.ToList();
                }

                // Slowmotion
                if (!obstacle.slowMotion && !_gameManager.isMakingTrick && distanceToObstacle <= sharedObstacleSettings.slowMotionDistance && obstacle.transform.position.z > player.transform.position.z)
                {
                    obstacle.slowMotion = true;
                    _gameManager.isSlowMotion = true;
                }

                if(distanceToObstacle <= sharedObstacleSettings.slowMotionDistance - slowMotionRange)
                {
                    _gameManager.isSlowMotion = false;
                }

                // Swipe range
                if (_gameManager.isSlowMotion && !player.isInSwipeRange)
                {
                    player.isInSwipeRange = true;
                }

                if (!obstacle.enableFlamers && distanceToObstacle <= sharedObstacleSettings.enableFlamersDistance + obstacle.enableFlamersOffset)
                {
                    obstacle.enableFlamers = true;
                    OnObstacleEnableFlamers(ref player, ref obstacle);
                }

                if (!obstacle.enter && distanceToObstacle <= sharedObstacleSettings.tutorialDistance)
                {
                    obstacle.enter = true;
                    OnObstacleTutorial(ref player, ref obstacle);
                }
                else if (!obstacle.enter && distanceToObstacle <= sharedObstacleSettings.enterDistance)
                {
                    obstacle.enter = true;
                    OnEnterObstacle(ref player, ref obstacle);
                }
                else if (!obstacle.passed && obstacle.transform.position.z <= player.transform.position.z)
                {
                    obstacle.passed = true;
                    OnPassedObstacle(ref player, ref obstacle);
                }
                else if (!obstacle.exit && obstacle.passed && distanceToObstacle >= sharedObstacleSettings.exitDistance)
                {
                    obstacle.exit = true;
                    OnExitObstacle(ref player, ref obstacle);
                }

                if (obstacle.transform.position.z <= (obstacleFilter.GetEntitiesCount() < 2 ? -50f : disposeObstacleDistance))
                {
                    SetTimimgZone(null, TimingZoneHint.None);

                    ref var entity = ref obstacleFilter.GetEntity(i);
                    obstacles.Remove(GetObstacleController(obstacle));

                    UnityEngine.Object.Destroy(obstacle.transform.gameObject);
                    entity.Destroy();

                    if (obstacles.Count > 0) 
                    {
                        obstacles[0].Show(true);
                        SetTimimgZone(obstacles[0].transform, 
                            _gameManager.GetCurrentLevel().obstacles[player.passedObstacles].timingZoneHint);
                    }
                }
            }
        }
    }

    private void SetTimimgZone(Transform parent, TimingZoneHint hint) 
    {
        ref var timingZone = ref timingZoneFilter.Get1(0);

        timingZone.green.SetActive(false);
        timingZone.red.SetActive(false);

        timingZone.transform.SetParent(parent);

        if (parent) 
        {
            timingZone.transform.localPosition = 
                new Vector3(0, 1.3f, -(sharedObstacleSettings.tutorialDistance));

            if (hint != TimingZoneHint.None)
            {
                switch (hint)
                {
                    case TimingZoneHint.Both:
                        timingZone.green.SetActive(true);
                        timingZone.red.SetActive(true);
                        break;
                    case TimingZoneHint.Green:
                        timingZone.green.SetActive(true);
                        break;
                    case TimingZoneHint.Red:
                        timingZone.red.SetActive(true);
                        break;
                }

                timingZoneEnabled = true;
                timingZone.animator.Play("TimingZoneEnabled");
            }
        }
    }

    private void DisableTimingZone() 
    {
        if (timingZoneEnabled)
        {
            timingZoneEnabled = false;

            ref var timingZone = ref timingZoneFilter.Get1(0);

            timingZone.animator.Play("TimingZoneDisabled");
        }
    }

    private void CheckTutorial()
    {
        ref var player = ref playerFilter.Get1(0);

        if (player.passedObstacles < _gameManager.GetCurrentLevel().obstacles.Length 
            && _gameManager.GetCurrentLevel().obstacles[player.passedObstacles].tutorial)
            _gameManager.ObstacleHasTutorial = true;
        else
            _gameManager.ObstacleHasTutorial = false;
    }

    private void LevelCompleteSound() 
    {
        _soundManager.StopMusic();
        _soundManager.PlaySound(5);
    }

    private void OnObstacleEnableFlamers(ref Player player, ref Obstacle obstacle) 
    {
        obstacle.laserColor.EnableFlamers();
    }

    private void OnObstacleTutorial(ref Player player, ref Obstacle obstacle)
    {
        currentObstacle = obstacle;

        var level = _gameManager.GetCurrentLevel();

        if (player.moveState != MoveState.Move && player.moveState != MoveState.End &&
            player.passedObstacles < level.obstacles.Length && level.obstacles[player.passedObstacles].tutorial)
        {
            if (!string.IsNullOrEmpty(level.obstacles[player.passedObstacles].clue))
            {
                foreach (Transform t in obstacle.transform)
                {
                    if (t.CompareTag("Clue")) 
                    {
                        tutorialClue = t.GetComponent<Clue>();
                        tutorialClue.ShowTutorial(true);
                    }
                }
            }

            ref var timingZone = ref timingZoneFilter.Get1(0);
            timingZone.animator.Play("TimingZoneWarning");

            if (level.obstacles[player.passedObstacles].tutorialMove != SwipeDirection.None) 
                _gameManager.tutorialMove = level.obstacles[player.passedObstacles].tutorialMove;
            else
                _gameManager.tutorialMove = obstacle.moves[UnityEngine.Random.Range(0, obstacle.moves.Length - 1)];

            _gameManager.isTutorial = true;
            _ui.GetScreen<GameScreen>().ShowTutorial(_gameManager.tutorialMove, 
                level.obstacles[player.passedObstacles].tutorialText);
        }
    }

    private void OnEnterObstacle(ref Player player, ref Obstacle obstacle) 
    {
        DisableTimingZone();
    }

    private void OnPassedObstacle(ref Player player, ref Obstacle obstacle)
    {
        player.passedObstacles++;

        player.isInSwipeRange = false;

        _ui.GetScreen<GameScreen>().UpdateProgressBar(player.passedObstacles);

        CheckTutorial();

        obstacle.laserColor.color = obstacle.laserColor.endColor;
    }

    private void ReportObstaclePlayability(ref Obstacle obstacle, bool success) 
    {
        if (_gameManager.State != GameState.Play)
            return;

        var parameters = new Dictionary<string, object>();
        var obstacleName = $"{GetObstaclePrefix(ref obstacle)}{obstacle.transform.name}";

        parameters.Add("location", _gameManager.Location.id);
        parameters.Add("level", _gameManager.Level + 1);
        parameters.Add("obstacle", obstacleName);

        AnalyticEvents.ReportEvent(success ? "obstacle_success" : "obstacle_fail", parameters);

        parameters.Clear();

        parameters.Add("location", _gameManager.Location.id);
        parameters.Add("level", _gameManager.Level + 1);
        parameters.Add("success", success);

        AnalyticEvents.ReportEvent(obstacleName, parameters);
    }

    private void OnExitObstacle(ref Player player, ref Obstacle obstacle)
    {
        ReportObstaclePlayability(ref obstacle, true);
    }

    private void OnHitTrigger(Trigger trigger)
    {
        if (_gameManager.State == GameState.Play) 
        {
            ref var player = ref playerFilter.Get1(0);
            ref var entity = ref playerFilter.GetEntity(0);

            _ui.GetScreen<LevelFailedScreen>().FailedMessage("");

            switch (trigger.type) 
            {
                case Trigger.Type.Laser:
                    entity.Get<AnimationEvent>() = new AnimationEvent { animation = "Death" };

                    _soundManager.PlaySound(2);
                    break;
                case Trigger.Type.Flamer:
                    if (trigger != null && trigger.type == Trigger.Type.Flamer)
                        _gameManager.Skin = _gameManager.Config.burnedSkin;

                    entity.Get<AnimationEvent>() = new AnimationEvent { animation = "Death" };

                    _soundManager.PlaySound(3);
                    break;
                case Trigger.Type.Camera:
                    entity.Get<AnimationEvent>() = new AnimationEvent { animation = "DeathRedCamera" };
                    player.cameraDeathRenderer.Play();
                    break;
                case Trigger.Type.Poison:
                    entity.Get<AnimationEvent>() = new AnimationEvent { animation = "DeathGreenCloud" };
                    player.poisonDeathEffect.Play();
                    _soundManager.PlaySound(4);
                    break;
            }

            ReportObstaclePlayability(ref currentObstacle, false);

            _gameManager.GameOver(trigger.type);

            DisableTimingZone();
        }
    }

    private void OnCollectClue(Clue clue)
    {
        ref var player = ref playerFilter.Get1(0);

        player.collectedClues++;
    }

    private void OnCollectKey(Key key)
    {
        _gameManager.CollectedKeys++;

        int count = _gameData.Progress.bonusKeys;

        _ui.GetScreen<GameScreen>().UpdateKeysCount(count + _gameManager.CollectedKeys);
    }

    private void OnCollectGift(Gift gift)
    {
        giftReward += gift.Reward;

        if(gift.IsFinal && !elevator)
            LevelCompleteSound();
    }

    private void OnSwipe(SwipeDirection direction) 
    {
        _gameManager.isSlowMotion = false;

        if (IsCorrectMove(direction))
        {
            if (_gameManager.tutorialMove != SwipeDirection.None && direction == _gameManager.tutorialMove)
            {
                Time.timeScale = 1.0f;
            }
            else if(_gameManager.tutorialMove == SwipeDirection.None)
            {
                Time.timeScale = 1.0f;
            }
        }

        ref var player = ref playerFilter.Get1(0);

        if (tutorialClue)
            tutorialClue.ShowTutorial(false);

        if (direction == _gameManager.tutorialMove) 
        {
            _gameManager.isTutorial = false;
            _ui.GetScreen<GameScreen>().CloseTutorial();
        }

        if (!_gameManager.isTutorial && isMinDistance && !isCriticalDistance && IsCorrectMove(direction)) 
        {
            _ui.GetScreen<GameScreen>().ShowFloatingMessage(
                (_gameManager.Config.floatingMessages[UnityEngine.Random.Range(0, _gameManager.Config.floatingMessages.Length)]).ToUpper());

            DisableTimingZone();
        }
    }

    private bool IsCorrectMove(SwipeDirection move) 
    {
        if (currentObstacle.moves == null || currentObstacle.moves.Length <= 0)
            return false;

        foreach (var m in currentObstacle.moves)
        {
            if (m.Equals(move))
                return true;
        }

        return false;
    }

    private void UpdateChildItems() 
    {
        if (obstacles.Count <= 0)
            return;

        // Setup clues, keys, poison
        foreach (var i in obstacleFilter)
        {
            ref var player = ref playerFilter.Get1(0);
            ref var obstacle = ref obstacleFilter.Get1(i);

            if (!obstacle.updateChildItems)
            {
                obstacle.updateChildItems = true;

                obstacle.helperMoves = new List<SwipeDirection>();
                obstacle.helperMoves.AddRange(obstacle.moves);

                foreach (Transform t in obstacle.transform)
                {
                    if (t.CompareTag("Clue"))
                    {
                        obstacle.clue = t.GetComponent<Clue>();

                        var o = GetObstacleData(obstacle.transform.name);

                        var anchor = SwipeDirection.None;

                        if (o != null && o.clueAnchor != SwipeDirection.None)
                        {
                            anchor = o.clueAnchor;
                            t.localPosition = player.anchors[(int)anchor].position * 2;
                        }
                        else
                        {
                            anchor = obstacle.moves[UnityEngine.Random.Range(0, obstacle.moves.Length)];
                            t.localPosition = player.anchors[(int)anchor].position * 2;
                        }

                        if (anchor == SwipeDirection.DownLeft || anchor == SwipeDirection.Left || anchor == SwipeDirection.UpLeft)
                            obstacle.clue.isLeftSide = true;

                        o.tutorialMove = anchor;

                        obstacle.importantMove = anchor;

                        break;
                    }

                    if (t.CompareTag("Key"))
                    {
                        obstacle.key = t.GetComponent<Key>();
                    
                        var o = GetObstacleData(obstacle.transform.name);

                        var anchor = obstacle.moves[UnityEngine.Random.Range(0, obstacle.moves.Length)];
                        t.localPosition = player.anchors[(int)anchor].position * 2;

                        obstacle.importantMove = anchor;
                    }
                }

                if (_gameManager.Config.poisonEnabled && _gameManager.Location.hasPoison && obstacle.allowPosion && !_gameManager.IsInitialLevel() && 
                    _gameManager.Location.levels[_gameManager.Level].hasPoison && 
                    obstacle.moves.Length > 1 && obstacle.clue == null && obstacle.key == null)
                {
                    if(poisonPerLevelCount < poisonPerLevel)
                    {
                        var random = UnityEngine.Random.Range(0, 2);

                        if(random > 0)
                        {
                            var anchor = obstacle.moves[UnityEngine.Random.Range(0, obstacle.moves.Length)];

                            var poison = UnityEngine.Object.Instantiate(_gameManager.Config.poisonPrefab, obstacle.transform).GetComponent<Poison>();
                            poison.transform.localPosition = player.anchors[(int)anchor].position * 2;
                            poison.gameObject.SetActive(false); //¯\_(ツ)_/¯
                            GetObstacleController(obstacle).poison = poison.gameObject;
                            poisonPerLevelCount++;

                            obstacle.helperMoves.RemoveAll(x => x == anchor);
                        }
                    }
                }
            }
        }
    }

    private void OnChangeGameState(GameState state, GameState previousState)
    {
        // Clone obstacle settings
        if (sharedObstacleSettings == null)
            sharedObstacleSettings = new Configuration.Obstacle.Settings();

        _gameManager.Config.sharedObstacleSettings.Clone(sharedObstacleSettings);

        // Adjust with current game speed
        sharedObstacleSettings.distance *= _gameManager.GetLevelSpeed();
        sharedObstacleSettings.enableFlamersDistance *= _gameManager.GetLevelSpeed();
        sharedObstacleSettings.tutorialDistance *= _gameManager.GetLevelSpeed();
        sharedObstacleSettings.minDistance *= _gameManager.GetLevelSpeed();
        sharedObstacleSettings.criticalDistance *= _gameManager.GetLevelSpeed();
        sharedObstacleSettings.enterDistance *= _gameManager.GetLevelSpeed();
        sharedObstacleSettings.exitDistance *= _gameManager.GetLevelSpeed();
        sharedObstacleSettings.slowMotionDistance *= _gameManager.GetLevelSpeed();

        var pool = GetPool($"Props/{_gameManager.Location.id}");

        if (pool != null && pool != currentPropsPool) 
        {
            // Fill location props pool
            currentPropsPool = pool;

            pool.Fill();

            // Dispose if entity removed from pool
            foreach (var i in scrollFilter)
            {
                ref var scroll = ref scrollFilter.Get1(i);

                if (scroll.type == ComponentType.Scenery) 
                {
                    ref var entity = ref scrollFilter.GetEntity(i);

                    if (!scroll.transform)
                        entity.Destroy();
                }
            }
        }

        switch (state)
        {
            case GameState.Start:
                if (elevator)
                    elevator.gameObject.SetActive(false);

                speedByLevel = _gameManager.Config.gameSpeed * _gameManager.GetLevelSpeed();
                pathLength = 0;
                giftReward = 0;
                _gameManager.CollectedKeys = 0;
                poisonPerLevelCount = 0;

                rewardMultiplier = _gameManager.Config.obstacleRewardByLevel.Evaluate(_gameManager.Level);

                foreach (var o in _gameManager.GetCurrentLevel().obstacles)
                    pathLength += sharedObstacleSettings.distance;

                SetTimimgZone(null, TimingZoneHint.None);

                foreach (var i in obstacleFilter)
                {
                    ref var o = ref obstacleFilter.Get1(i);
                    ref var entity = ref obstacleFilter.GetEntity(i);

                    obstacles.Remove(GetObstacleController(o));

                    UnityEngine.Object.Destroy(o.transform.gameObject);
                    entity.Destroy();
                }

                obstacles.Clear();

                spawnObstacle = 0;
                distance = 0;

                // Show first obstalce at start
                if (!_gameManager.IsGiftLevel) 
                {
                    var previewObstacle = SpawnObstacle(_gameManager.Location.levels[_gameManager.Level].obstacles[0], true);
                    previewObstacle.Show(true);

                    obstacles.Add(previewObstacle);
                    spawnObstacle++;
                }

                // Scenery
                spawnScenery = null;

                foreach (var i in scrollFilter)
                {
                    ref var scroll = ref scrollFilter.Get1(i);

                    if (scroll.type == ComponentType.Scenery)
                        scroll.transform.gameObject.SetActive(false);
                }

                for (int i = 0; i < _gameManager.Config.sceneryPerLevel; i++)
                {
                    SpawnScenery();
                }
                break;
            case GameState.Play:
                foreach (var i in playerFilter)
                {
                    ref var player = ref playerFilter.Get1(i);

                    player.isInSwipeRange = false;
                }

                _gameManager.isRunning = true;

                if (_gameManager.IsGiftLevel)
                    AnalyticEvents.ReportEvent("bonuslvl_open");
                break;
            case GameState.LevelFailed:
                var parameters = new Dictionary<string, object>();
                parameters.Add("level", _gameManager.Level + 1);

                AnalyticEvents.ReportEvent("level_fail", parameters);
                break;
        }
    }
}
