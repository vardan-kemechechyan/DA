using UnityEngine;
using System.Collections.Generic;

public class CUITrail : MonoBehaviour
{
    [SerializeField] RectTransform cursor;
    [SerializeField] LineRenderer trailPrefab = null;
    [SerializeField] Camera Cam;
    [SerializeField] float clearSpeed = 1;
    [SerializeField] float distanceFromCamera = 1;

    private LineRenderer currentTrail;
    private List<Vector3> points = new List<Vector3>();

    private void Update()
    {
        if (Time.timeScale > 0) 
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
        }

        UpdateTrailPoints();

        ClearTrailPoints();
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
        Vector3 mousePosition = Input.mousePosition;
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
        float clearDistance = Time.deltaTime * clearSpeed;
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
        DestroyCurrentTrail();
    }

}
