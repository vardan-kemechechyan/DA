using Leopotam.Ecs;
using System.Linq;
using UnityEngine;

sealed class LevelScrollSystem : IEcsInitSystem, IEcsRunSystem
{
    //private readonly EcsWorld _world = null;

    private readonly EcsFilter<Player> playerFilter = null;
    private readonly EcsFilter<LevelScroll> scrollFilter = null;

    private readonly GameManager _gameManager = null;

    int lanesCount = 6;
    float step = 63.86f;
    float journey;

    bool spawnLanes;

    float speedByLevel;

    GameObject[] lanePrefabs;

    public void Init()
    {
        GameManager.OnChangeGameState += OnChangeGameState;
        GameManager.OnChangeLocation += OnChangeLocation;
    }

    public void Run()
    {
        if (!spawnLanes) 
        {
            if (_gameManager.State == GameState.Play
            || _gameManager.State == GameState.LevelComplete)
            {
                if (!_gameManager.isRunning)
                    return;

                ref var player = ref playerFilter.Get1(0);

                //player.journeyLength += _gameManager.FrameDependentDelta() * speedByLevel;
                player.journeyLength += Time.deltaTime * speedByLevel;

                //journey = _gameManager.FrameDependentDelta() * speedByLevel;
                journey = Time.deltaTime * speedByLevel;

                foreach (var i in scrollFilter)
                {
                    ref var scroll = ref scrollFilter.Get1(i);

                    if (scroll.transform.gameObject.activeInHierarchy)
                        scroll.transform.Translate(-Vector3.forward * journey);

                    if (scroll.type == ComponentType.Lane && scroll.transform.position.z <= -step)
                        scroll.transform.position = new Vector3(scroll.transform.position.x, scroll.transform.position.y, scroll.transform.position.z + (lanesCount * step));
                }
            }
        }
    }

    private void OnChangeGameState(GameState state, GameState previousState)
    {
        switch (state)
        {
            case GameState.Start:
                speedByLevel = _gameManager.Config.gameSpeed * _gameManager.GetLevelSpeed();

                ref var player = ref playerFilter.Get1(0);
                player.journeyLength = 0;
                break;
        }
    }

    private void OnChangeLocation(Configuration.Location location)
    {
        foreach (var i in scrollFilter)
        {
            ref var scroll = ref scrollFilter.Get1(i);

            if (scroll.type == ComponentType.Lane) 
            {
                ref var entity = ref scrollFilter.GetEntity(i);

                UnityEngine.Object.Destroy(scroll.transform.gameObject);
                entity.Destroy();
            }
        }

        for (int i = 0; i < lanesCount; i++)
        {
            var lane = UnityEngine.Object.Instantiate(location.lane, _gameManager.transform).transform;
            lane.position = new Vector3(lane.position.x, lane.position.y, lane.position.z + step * i);
        }
    }
}
