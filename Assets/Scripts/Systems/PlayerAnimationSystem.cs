using Leopotam.Ecs;
using UnityEngine;
using System.Linq;
using System;
using System.Collections.Generic;

sealed class PlayerAnimationSystem : IEcsInitSystem, IEcsRunSystem
{
    //private readonly EcsWorld _world = null;

    private readonly EcsFilter<Player> playerFilter = null;
    private readonly EcsFilter<Player, AnimationEvent> playerAnimationFilter = null;

    private readonly GameManager _gameManager = null;
    private readonly SoundManager _soundManager = null;

    public void Init()
    {
        GameManager.OnChangeGameState += OnChangeGameState;
        GameManager.OnChangeSkin += OnChangeSkin;
    }

    Animator currentAnimator;
    string currentAnimation;

    public void Run()
    {
        foreach (var i in playerAnimationFilter)
        {
            ref var player = ref playerAnimationFilter.Get1(i);
            ref var animationEvent = ref playerAnimationFilter.Get2(i);

            player.animator?.SetFloat("speed", animationEvent.speed * _gameManager.GetLevelSpeed());
            player.animator?.Play(animationEvent.animation);

            player.sharedAnimator?.SetFloat("speed", animationEvent.speed * _gameManager.GetLevelSpeed());
            player.sharedAnimator?.Play(animationEvent.animation);

            FirebaseManager.SetCustomKey("player_animation", animationEvent.animation);
        }

        if (_gameManager.State == GameState.Play)
        {
            foreach (var i in playerFilter)
            {
                ref var player = ref playerAnimationFilter.Get1(i);

                if (player.animator != null)
                {
                    if (player.animator.GetCurrentAnimatorClipInfo(0).Length > 0 &&
                        !player.animator.GetCurrentAnimatorClipInfo(0).First().clip.name.Equals("Run"))
                    {
                        if (_soundManager.IsSoundPlaying(0))
                            _soundManager.StopSound(0);
                    }
                    else
                    {
                        if (!_soundManager.IsSoundPlaying(0))
                            _soundManager.PlaySound(0);
                    }
                }

                if (player.animator.GetCurrentAnimatorClipInfo(0).Length > 0) 
                {
                    if (player.animator.GetCurrentAnimatorClipInfo(0).First().clip.name.Equals("Run") ||
                        player.animator.GetCurrentAnimatorClipInfo(0).First().clip.name.Equals("Idle"))
                        _gameManager.isMakingTrick = false;
                    else
                        _gameManager.isMakingTrick = true;
                }
            }
        }
        else 
        {
            if (_soundManager.IsSoundPlaying(0))
                _soundManager.StopSound(0);
        }
    }

    private void OnChangeGameState(GameState state, GameState previousState)
    {
        if(previousState == GameState.Play)
            _gameManager.isMakingTrick = false;

        foreach (var i in playerFilter)
        {
            ref var player = ref playerFilter.Get1(0);

            player.transform.GetChild(1).GetComponent<SkinGeometry>().Divide(state == GameState.LevelFailed);
        }
    }

    private void OnChangeSkin(Configuration.Skin skin)
    {
        foreach (var i in playerFilter)
        {
            ref var player = ref playerFilter.Get1(0);
            ref var entity = ref playerFilter.GetEntity(0);

            foreach (Transform t in player.transform)
                if (t.GetSiblingIndex() > 0)
                    UnityEngine.Object.Destroy(t.gameObject);

            var s = UnityEngine.Object.Instantiate(skin.prefab,
                player.transform.transform);

            player.animator = s.GetComponent<Animator>();

            entity.Get<AnimationEvent>() = new AnimationEvent
            {
                animation = "Idle",
                speed = 1
            };
        }
    }
}
