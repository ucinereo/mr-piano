using Meta.XR;
using UnityEngine;

public class CVPlaneFinder: AbstractPlaneFinder
{
    [Header("Configuration")]
    public EnvironmentRaycastManager raycastManager;

    public void parseRays(Ray[] rays)
    {
        foreach (Ray ray in rays)
        {
            if (raycastManager.Raycast(ray, out var hit))
            {
                CapturePoint(hit.point);
                Debug.Log("Point registered!");
            }
        }
    }
}
