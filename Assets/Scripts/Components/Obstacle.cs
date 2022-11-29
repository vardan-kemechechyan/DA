using System;
using System.Collections.Generic;
using UnityEngine;
using Voody.UniLeo;

[Serializable]
public struct Obstacle
{
    public ObstacleType type;
    public Transform transform;
    public bool allowPosion;
    public bool hasKey;
    public bool enableFlamers;
    public float enableFlamersOffset;
    public bool selected;
    public bool enter;
    public bool passed;
    public bool exit;
    public bool slowMotion;
    public bool isInSwipeRange;

    [HideInInspector]
    public bool updateChildItems;

    public Clue clue;
    public Key key;
    public ObstacleController laserColor;
    public SwipeDirection[] moves;
    public List<SwipeDirection> helperMoves;
    public SwipeDirection importantMove;
    public List<SwipeDirection> occupiedMoves;
    public SwipeDirection tutorialMove;
}
