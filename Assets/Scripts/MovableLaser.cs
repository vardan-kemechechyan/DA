using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovableLaser : MonoBehaviour
{
    [SerializeField] Vector3 endPosition;
    [SerializeField] Quaternion endRotation;

    [SerializeField] bool move;
    [SerializeField] bool rotate;

    [HideInInspector] public bool loop;

    [SerializeField] float moveSpeed = 1.0f;
    [SerializeField] float rotationSpeed = 1.0f;

    Vector3 startPosition;
    Quaternion startRotation;

    bool reachedTargetPosition;
    bool reachedTargetRotation;

    private float startMoveTime;
    private float journeyLength;

    float distCovered;
    float fractionOfJourney;

    // fixedScaleMultiplier = 1.0 if fixed timestep 0.02
    // fixedScaleMultiplier = 0.5 if fixed timestep 0.01
    float fixedScaleMultiplier = 1.0f;

    private void Start()
    {
        moveSpeed *= fixedScaleMultiplier;
        rotationSpeed *= fixedScaleMultiplier;

        startPosition = transform.localPosition;
        startRotation = transform.localRotation;
    }

    // Must be fixed update to ensure position is correct even with low framerate
    void FixedUpdate()
    {
        if (move && GameManager.CurrentState() == GameState.Play) 
        {
            if (!reachedTargetPosition)
            {
                if (startMoveTime <= 0) 
                {
                    startMoveTime = Time.time;
                    journeyLength = Vector3.Distance(transform.localPosition, endPosition);
                }

                distCovered = (Time.time - startMoveTime) * fractionOfJourney >= 0.9f ? moveSpeed * 0.8f : moveSpeed;
                fractionOfJourney = distCovered / journeyLength;

                transform.localPosition = Vector3.Lerp(transform.localPosition, endPosition, fractionOfJourney);

                if (Vector3.Distance(transform.localPosition, endPosition) <= 0.02f) 
                {
                    startMoveTime = 0;
                    reachedTargetPosition = true;
                }
            }
            else
            {
                if (loop) 
                {
                    if (startMoveTime <= 0)
                    {
                        startMoveTime = Time.time;
                        journeyLength = Vector3.Distance(transform.localPosition, startPosition);
                    }

                    float distCovered = (Time.time - startMoveTime) * moveSpeed;
                    float fractionOfJourney = distCovered / journeyLength;

                    transform.localPosition = Vector3.Lerp(transform.localPosition, startPosition, fractionOfJourney);

                    if (Vector3.Distance(transform.localPosition, startPosition) <= 0.02f)
                    {
                        startMoveTime = 0;
                        reachedTargetPosition = false;
                    }
                }
            }
        }

        if (rotate) 
        {
            if (!reachedTargetRotation)
            {
                transform.localRotation = Quaternion.Lerp(transform.localRotation, endRotation, Time.time * rotationSpeed);

                if (Quaternion.Angle(transform.localRotation, endRotation) <= 0.25f)
                    reachedTargetRotation = true;
            }
            else
            {
                if (loop) 
                {
                    transform.localRotation = Quaternion.Lerp(transform.localRotation, endRotation, Time.time * rotationSpeed);

                    if (Quaternion.Angle(transform.localRotation, startRotation) <= 0.25f)
                        reachedTargetRotation = false;
                }
            }
        }
    }
}
