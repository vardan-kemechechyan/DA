using System;
using UnityEngine;

[Serializable]
public struct Player
{
    public SwipeDirection direction;
    public Transform transform;
    public Transform[] anchors;
    public MoveState moveState;
    public int passedObstacles;
    public int collectedClues;
    public int collectedKeys;
    public float journeyLength;
    public float moveTime;
    public float moveDelayTime;
    public bool isInSwipeRange;
    public Vector3 anchor;
    public ParticleSystem poisonDeathEffect;
    public CameraDeathRenderer cameraDeathRenderer;
    public Configuration.PlayerMove move;
    public Animator animator;
    public MeshRenderer meshRenderer;
    public Animator sharedAnimator;
    public Collider[] bodyColliders;
}
