using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrawManager: MonoBehaviour
{
    private LineRenderer lineRenderer;
    public GameObject drawingPrefab;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            GameObject drawing = Instantiate(drawingPrefab);
            lineRenderer = drawing.GetComponent<LineRenderer>();
        }

        if (Input.GetMouseButton(0))
        {
            FreeDraw();
        }
    }

    void FreeDraw()
    {
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        Vector3 mousePos = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 10f);
        lineRenderer.positionCount++;
        lineRenderer.SetPosition(lineRenderer.positionCount - 1, Camera.main.ScreenToWorldPoint(mousePos));

    }
}