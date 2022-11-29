using Leopotam.Ecs;
using UnityEngine;

sealed class TimeScaleSystem : IEcsRunSystem
{
    //private readonly EcsWorld _world = null;

    private readonly GameManager _gameManager = null;

    public void Run()
    {
        if (_gameManager.State == GameState.Pause) Time.timeScale = 0;
        else 
        {
            if (!_gameManager.InternetConnection || _gameManager.isTutorial)
            {
                if(_gameManager.State == GameState.Play)
                    Time.timeScale = 0;

                //Time.timeScale =
                //Mathf.Lerp(Time.timeScale, 0, 0.1f);
            }
            else 
            {
                if (_gameManager.State != GameState.Play)
                    _gameManager.isSlowMotion = false;

                if (_gameManager.Config.slowmotion && _gameManager.isSlowMotion)
                    Time.timeScale = Mathf.Lerp(Time.timeScale, 0.3f, 0.5f);
                else
                    Time.timeScale = 1.0f;
            }
        }
    }
}
