using Leopotam.Ecs;
using UnityEngine;
using System;
using System.Linq;

sealed class PlayerMovementSystem : IEcsInitSystem, IEcsRunSystem
{
    //private readonly EcsWorld _world = null;

    private readonly EcsFilter<Player> _filter = null;

    private readonly GameManager _gameManager = null;
    private readonly SoundManager _soundManager = null;
    private readonly UIManager _ui = null;

    SwipeDirection dir;
    Configuration.PlayerMove move;

    public void Init()
    {
        GameManager.OnChangeGameState += OnChangeGameState;
    }

    public void Run()
    {
        if (_gameManager.State == GameState.Play) 
        {
            if (Input.GetMouseButtonUp(0))
            {
                foreach (var i in _filter)
                {
                    ref var player = ref _filter.Get1(i);
                    ref var entity = ref _filter.GetEntity(i);

                    if (player.direction != SwipeDirection.None)
                    {
                        // Blocked before landing
                        if (!player.animator.GetCurrentAnimatorClipInfo(0).First().clip.name.Equals("Run"))
                            return;

                        // Blocked by tutorial
                        if (_gameManager.ObstacleHasTutorial && !_ui.GetScreen<GameScreen>().IsTutorialOpen())
                            return;

                        // Out of swipe area
                        if (!_gameManager.Config.allowJumpOutOfRange && !player.isInSwipeRange)
                            return;

                        if (_gameManager.isTutorial && _gameManager.tutorialMove != player.direction) 
                        {
                            return;
                        }

                        dir = player.direction;

                        // Swipe helper
                        var relatedMoves = _gameManager.Config.relatedDirections.First(x => x.direction == dir).related;
                        var important = relatedMoves.FirstOrDefault(x => x == _gameManager.importantMove);

                        if (UnityEngine.Random.Range(0, 101) <= _gameManager.Config.correctSwipeChance) 
                        {
                            if (important != SwipeDirection.None)
                            {
                                dir = important;
                            }
                            else
                            {
                                if (!_gameManager.correctMoves.Any(x => x == dir))
                                {
                                    relatedMoves = _gameManager.Config.relatedDirections.First(x => x.direction == dir).related;

                                    _gameManager.correctMoves.Shuffle();

                                    foreach (var r in relatedMoves)
                                    {
                                        foreach (var c in _gameManager.correctMoves)
                                        {
                                            if (r == c)
                                            {
                                                Debug.Log($"Swipe helper: {dir} => {r}");
                                                dir = r;
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        move = _gameManager.Config.playerMoves.FirstOrDefault(x => x.direction == dir);

                        if (move == null)
                        {
                            Debug.LogWarning($"Move {player.direction} not found!");
                            break;
                        }

                        GameManager.OnSwipe.Invoke(player.direction);

                        if (move != null)
                        {
                            player.move = move;
                            player.anchor = player.anchors[(int)move.direction].position;
                            player.moveTime = move.duration;
                            player.moveDelayTime = move.delay;
                            player.moveState = MoveState.Start;

                            entity.Get<AnimationEvent>() = new AnimationEvent 
                            { 
                                animation = player.move.animation, 
                                speed = player.move.animationSpeed 
                            };

                            _soundManager.StopSound(0);
                            _soundManager.PlaySound(1);
                        }
                    }
                }
            }

            Move();
        }
    }

    private void OnChangeGameState(GameState state, GameState previousState)
    {
        switch (state)
        {
            case GameState.Start:
                SetAnimation("Idle");

                foreach (var i in _filter)
                {
                    ref var player = ref _filter.Get1(i);

                    player.transform.localPosition = _gameManager.Config.mainAnchor;
                    player.moveState = MoveState.None;
                }
                break;
            case GameState.Play:
                if(previousState != GameState.Pause)
                    SetAnimation("Run");
                break;
            case GameState.LevelComplete:
                //SetAnimation("Idle");
                break;
            case GameState.LevelFailed:
                //SetAnimation("Idle");
                break;
        }
    }

    private void SetAnimation(string animation) 
    {
        foreach (var i in _filter)
        {
            ref var player = ref _filter.Get1(i);
            ref var entity = ref _filter.GetEntity(i);

            entity.Get<AnimationEvent>() = new AnimationEvent
            {
                animation = animation,
                speed = 1.0f
            };
        }
    }

    private void Move()
    {
        foreach (var i in _filter)
        {
            ref var player = ref _filter.Get1(i);

            switch (player.moveState)
            {
                case MoveState.Start:

                    player.moveDelayTime -= Time.deltaTime;

                    if (_gameManager.Config.characterMotionMode == CharactersAnimationMode.Anchored && player.moveDelayTime <= 0)
                        player.transform.localPosition =
                        Vector3.Lerp(player.transform.localPosition, player.anchor, player.move.moveSpeed * Time.deltaTime);

                    if (Vector3.Distance(player.transform.localPosition, player.anchor) <= 0.1f)
                        player.moveState = MoveState.Move;
                    break;
                case MoveState.Move:
                    player.moveTime -= Time.deltaTime;

                    if (player.moveTime <= 0)
                        player.moveState = MoveState.End;
                    break;
                case MoveState.End:

                    if(_gameManager.Config.characterMotionMode == CharactersAnimationMode.Anchored)
                        player.transform.localPosition =
                        Vector3.Lerp(player.transform.localPosition, player.anchors[4].position, player.move.returnSpeed * Time.deltaTime);

                    if (Vector3.Distance(player.transform.localPosition, player.anchors[4].position) <= 0.3f)
                        player.moveState = MoveState.None;
                    break;
                case MoveState.None:
                    if (_gameManager.Config.characterMotionMode == CharactersAnimationMode.Anchored)
                        player.transform.localPosition =
                        Vector3.Lerp(player.transform.localPosition, _gameManager.Config.mainAnchor, 1.0f * Time.deltaTime);
                    break;
            }
        }
    }
}
