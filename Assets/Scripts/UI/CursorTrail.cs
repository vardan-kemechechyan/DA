using UnityEngine;
using System.Collections.Generic;

public class CursorTrail : MonoBehaviour
{
    [SerializeField] LineRenderer trailPrefab = null;
    [SerializeField] Camera Cam;
    [SerializeField] float clearSpeed = 1;
    [SerializeField] float distanceFromCamera = 1;

    private LineRenderer currentTrail;
    private List<Vector3> points = new List<Vector3>();

    [SerializeField] RectTransform cursor;

    [SerializeField] bool simulate;

    [SerializeField] Vector2 shifting;

    bool clicked;

    private void Update()
    {
        if (simulate)
        {
            if (clicked)
                AddPoint();

            UpdateTrailPoints();
            ClearTrailPoints();
        }
        else 
        {
            if (Input.GetMouseButtonDown(0))
            {
                DestroyCurrentTrail();
                CreateCurrentTrail();
                AddPoint();
            }

            if (Input.GetMouseButton(0))
            {
                AddPoint();
            }

            UpdateTrailPoints();
            ClearTrailPoints();
        }
    }

    public void AnimationEvent(string animationEvent)
    {
        if (!clicked)
        {
            clicked = true;

            DestroyCurrentTrail();
            CreateCurrentTrail();
            AddPoint();
        }
        else 
        {
            clicked = false;
        }
    }

    private void DestroyCurrentTrail()
    {
        if (currentTrail != null)
        {
            Destroy(currentTrail.gameObject);
            currentTrail = null;
            points.Clear();
        }
    }

    private void CreateCurrentTrail()
    {
        currentTrail = Instantiate(trailPrefab);
        currentTrail.transform.SetParent(transform, true);
    }

    private void AddPoint()
    {
        Vector3 mousePosition = simulate ? (Vector3)(RectTransformToScreenSpace(cursor).position + shifting): Input.mousePosition;
        points.Add(Cam.ViewportToWorldPoint(new Vector3(mousePosition.x / Screen.width, mousePosition.y / Screen.height, distanceFromCamera)));
    }

    private void UpdateTrailPoints()
    {
        if (currentTrail != null && points.Count > 1)
        {
            currentTrail.positionCount = points.Count;
            currentTrail.SetPositions(points.ToArray());
        }
        else
        {
            DestroyCurrentTrail();
        }
    }

    private void ClearTrailPoints()
    {
        float clearDistance = Time.unscaledDeltaTime * clearSpeed;
        while (points.Count > 1 && clearDistance > 0)
        {
            float distance = (points[1] - points[0]).magnitude;
            if (clearDistance > distance)
            {
                points.RemoveAt(0);
            }
            else
            {
                points[0] = Vector3.Lerp(points[0], points[1], clearDistance / distance);
            }
            clearDistance -= distance;
        }
    }

    void OnDisable()
    {
        clicked = false;

        DestroyCurrentTrail();
    }

    public Rect RectTransformToScreenSpace(RectTransform transform)
    {
        Vector2 size = Vector2.Scale(transform.rect.size, transform.lossyScale);
        return new Rect((Vector2)transform.position - (size * 0.5f), size);
    }
}