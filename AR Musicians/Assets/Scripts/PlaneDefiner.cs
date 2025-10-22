using Assets.Scripts;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class PlaneDefiner : MonoBehaviour
{
    #region Serialized members
    [Header("Settings")]
    [SerializeField] private float vertexPrefabWidth = 0.01f;
    [SerializeField] private int requiredPoints = 4;
    [SerializeField] private GameObject pointVisualizerPrefab;
    #endregion

    #region
    public static event Action<DefinedPlane> OnPlaneDefined;
    #endregion

    #region Private members
    private LineRenderer lineRenderer;
    private Vector3[] vertices;
    private int currentVertexIndex = -1;
    private readonly Vector3 OVR_CONTROLLER_RADIUS = new(0.0f, 0.0f, 0.03f);
    private List<GameObject> setupGameObjects;
    #endregion
    void Awake()
    {
        setupGameObjects = new List<GameObject>();
        lineRenderer = GetComponent<LineRenderer>();
        vertices = new Vector3[requiredPoints];
        lineRenderer.positionCount = requiredPoints;
        lineRenderer.enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch))
        {
            Vector3 currentPos = OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch) + OVR_CONTROLLER_RADIUS;
            CapturePoint(currentPos);
        }
        if (OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.LTouch))
        {
            ResetDefinition();
        }
    }

    private void CapturePoint(Vector3 worldPosition)
    {
        Debug.Log($"Detecting vertex at controller position. Spawning vertex.");
        if (currentVertexIndex < vertices.Length - 1)
        {
            vertices[++currentVertexIndex] = worldPosition;

            if (pointVisualizerPrefab != null)
            {
                GameObject tempVertexPrefab = Instantiate(pointVisualizerPrefab, vertices[currentVertexIndex], Quaternion.identity);
                tempVertexPrefab.transform.localScale = new Vector3(vertexPrefabWidth, vertexPrefabWidth, vertexPrefabWidth);
                Debug.Log($"Add volume vertex at coords: {vertices[currentVertexIndex]}");
                setupGameObjects.Add(tempVertexPrefab);
            }

            if (currentVertexIndex == requiredPoints - 1)
            {
                DefinePlane();
            }
        }
    }

    private void DefinePlane()
    {
        // A plane is mathematically defined by 3 points.
        // We use the first three to get the plane's orientation.
        Plane mathPlane = new Plane(vertices[0], vertices[1], vertices[2]);

        // Create our custom data structure
        DefinedPlane newPlane = new DefinedPlane(mathPlane, vertices[0], vertices[1], vertices[2], vertices[3]);

        // Create an anchor at the center of the plane to persist it in the real world.
        //StartCoroutine(UtilityMethods.CreateSpatialAnchorWithCallback(HandleAnchorCreated));

        // Update the LineRenderer to show the defined quad
        lineRenderer.enabled = true;
        lineRenderer.SetPositions(vertices);
        lineRenderer.loop = true;

        // Fire the event, notifying all listeners that the plane is ready.
        // Check if anyone is listening before invoking to avoid errors.
        OnPlaneDefined?.Invoke(newPlane);

        Debug.Log("Plane defined and event fired!");
    }

    //private OVRSpatialAnchor savedAnchor;

    ///// <summary>
    ///// This is our callback method. It will be called by the coroutine when the anchor is ready.
    ///// Its signature must match the Action<OVRSpatialAnchor>.
    ///// </summary>
    ///// <param name="createdAnchor">The anchor that was created (or null if it failed).</param>
    //private void HandleAnchorCreated(OVRSpatialAnchor createdAnchor)
    //{
    //    if (createdAnchor == null)
    //    {
    //        Debug.LogError("Anchor creation failed. Cannot place content.");
    //        return;
    //    }

    //    // --- SAVING AND USING THE RESULT ---
    //    Debug.Log($"Callback received! Anchor {createdAnchor.Uuid} is ready to use.");

    //    // 1. Save the anchor to a class variable for later use.
    //    savedAnchor = createdAnchor;
    //}

    public void ResetDefinition()
    {
        currentVertexIndex = 0;
        lineRenderer.enabled = false;
        Debug.Log($"Destroying {setupGameObjects.Count} setup objects");
        foreach (GameObject setupObject in setupGameObjects)
        {
            Destroy(setupObject);
        }
    }
}
