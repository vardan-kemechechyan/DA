public enum ComponentType 
{
    None,
    Lane,
    Scenery
}

public enum ObstacleType
{
    Laser,
    Movable,
    Combined,
    Camera,
    Flamer
}

public enum GameState
{
    None,
    Start,
    Play,
    Pause,
    LevelComplete,
    LevelFailed
}

public enum CameraState
{
    Main,
    LaserDeath,
    CameraDeath,
    PoisonDeath
}

public enum SwipeDirection
{
    None,
    Right,
    Left,
    Up,
    Down,
    UpRight,
    UpLeft,
    DownRight,
    DownLeft
}

public enum TimingZoneHint
{
    None,
    Green,
    Red,
    Both
}

public enum SwipePhase
{
    Start,
    Hold,
    Release
}

public enum PlayerState
{
    Idle,
    Run,
    Move,
    Death
}

public enum MoveState
{
    None,
    Start,
    Move,
    End
}

public enum LaserColorMode
{
    Random,
    Color
}

public enum CharactersAnimationMode
{
    Root,
    Anchored
}

public enum LevelEvent
{
    None,
    Camera,
    Flamer,
    Poison,
    Skin,
    Gift
}
