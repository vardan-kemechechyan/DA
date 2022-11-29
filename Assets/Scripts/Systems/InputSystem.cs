using Leopotam.Ecs;
using UnityEngine;

sealed class InputSystem : IEcsRunSystem
{
    //private readonly EcsWorld _world = null;

    private readonly EcsFilter<Player> _filter = null;
    private readonly GameManager _gameManager = null;

    private Vector2 _startPosition;
    private Vector2 _endPosition;
    private float _minSwipeDistance = 30.0f;
    private float _startTime;
    private float _maxSwipeTime = 0.5f;

    float axisRange = 0.2f;

    SwipeDirection swipeDirection;

    float gestureTime;
    float gestureDist;
    Vector2 direction;

    public void Run()
    {
        if (_gameManager.InternetConnection && _gameManager.State == GameState.Play) 
        {
            if (Input.GetMouseButtonDown(0))
            {
                _startTime = Time.time;
                _startPosition = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
            }
            else if (Input.GetMouseButtonUp(0))
            {             
                _endPosition = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
                
                gestureTime = Time.time - _startTime;
                gestureDist = (_endPosition - _startPosition).magnitude;
                direction = _endPosition - _startPosition;

                if (gestureTime < _maxSwipeTime && gestureDist > _minSwipeDistance)
                {
                    if (Mathf.Abs(direction.normalized.y) <= axisRange)
                    {
                        if (Mathf.Abs(direction.normalized.y) >= axisRange)
                        {
                            if (direction.normalized.x > 0) swipeDirection =
                                     direction.normalized.y > 0 ? SwipeDirection.UpRight : SwipeDirection.DownRight;
                            else swipeDirection =
                                     direction.normalized.y > 0 ? SwipeDirection.UpLeft : SwipeDirection.DownLeft;
                        }
                        else 
                        {
                            if (direction.normalized.x > 0) swipeDirection = SwipeDirection.Right;
                            else swipeDirection = SwipeDirection.Left;
                        }
                    }
                    else
                    {
                        if (Mathf.Abs(direction.normalized.x) >= axisRange)
                        {
                            if (direction.normalized.y > 0) swipeDirection =
                                     direction.normalized.x > 0 ? SwipeDirection.UpRight : SwipeDirection.UpLeft;
                            else swipeDirection =
                                     direction.normalized.x > 0 ? SwipeDirection.DownRight : SwipeDirection.DownLeft;
                        }
                        else
                        {
                            if (direction.normalized.y > 0) swipeDirection = SwipeDirection.Up;
                            else swipeDirection = SwipeDirection.Down;
                        }
                    }
                }
                else
                {
                    swipeDirection = SwipeDirection.None;
                }
            }

            foreach (var i in _filter)
            {
                ref var player = ref _filter.Get1(i);

                player.direction = swipeDirection;
            }
        }
    }
}
