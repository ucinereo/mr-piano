using Assets.Scripts;
using Meta.XR;
using Meta.XR.MRUtilityKit;
using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class PianoManager : MonoBehaviour
{
    #region Serialized members
    [Header("Settings")]
    [SerializeField] private float vertexPrefabWidth = 0.01f;
    [SerializeField] private GameObject pointVisualizerPrefab;

    // TODO: Move to PlaneController class for better abstraction
    [SerializeField] private GameObject planeCornerCorrectorNode;
    [SerializeField] private GameObject planeTranslateCorrectorNode;

    [Header("Visuals")]
    [SerializeField] private Color userPlaneColor = Color.green;
    [SerializeField] private Color mathPlaneColor = new Color(0.0f, 0.5f, 1.0f, 0.5f); // Blue with transparency
    [SerializeField] private float mathPlaneSize = 0.5f;
    #endregion

    #region Events
    public static event Action<DefinedPlane> OnPlaneDefined;
    #endregion

    #region Private members
    // Component references
    private LineRenderer lineRenderer;
    private Transform playerCameraTransform;

    // Materials - cached for performance
    private Material userPlaneMaterial;
    private Material mathPlaneMaterial;

    // State variables
    private static int REQUIRED_POINTS = 3;
    private int currentVertexIndex = -1;
    private bool planeDefined = false;
    private float sideLength = 0.0f;

    private Vector3[] initPlaneAnchors; // The initial set plane anchors
    private Vector3[] planeAnchors; // The corrected plane anchors
    private GameObject[] anchorPrefabs; // Prefabs stored in plane corners
    private GameObject leftCornerCorrecorNode;
    private GameObject rightCornerCorrectorNode;
    private GameObject translateCorrectorNode;
    private GameObject mathPlaneVisualizer;
    #endregion

    void Awake()
    {
        // --- Cache component references ---
        lineRenderer = GetComponent<LineRenderer>();
        playerCameraTransform = Camera.main.transform; // Get the main camera for player viewpoint

        // --- Initialize collections and arrays ---
        initPlaneAnchors = new Vector3[REQUIRED_POINTS];
        anchorPrefabs = new GameObject[REQUIRED_POINTS + 1];
        planeAnchors = new Vector3[REQUIRED_POINTS + 1]; // This is the only one that stores 4 positions

        // --- Configure LineRenderer ---
        lineRenderer.positionCount = 0;
        lineRenderer.enabled = false;
        lineRenderer.useWorldSpace = true;
        lineRenderer.startWidth = 0.005f;
        lineRenderer.endWidth = 0.005f;
        lineRenderer.loop = true;

        // --- Create and cache materials to avoid runtime generation ---
        userPlaneMaterial = new Material(Shader.Find("Sprites/Default"));
        userPlaneMaterial.color = userPlaneColor;
        lineRenderer.material = userPlaneMaterial;

        mathPlaneMaterial = new Material(Shader.Find("Standard"));
        mathPlaneMaterial.color = mathPlaneColor;
        // Set material for transparency
        SetupTransparentMaterial(mathPlaneMaterial);
    }

    private void Update()
    {
        if (planeDefined)
        {
            if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch))
            {
                DefinePlane();    
            }

            if (OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch))
            {
                // TODO
            }
        }
    }

    /// Captures a point in 3D space and visualizes it.
    public void RegisterPoint(Vector3 worldPosition)
    {
        if (currentVertexIndex >= REQUIRED_POINTS - 1) return;

        initPlaneAnchors[++currentVertexIndex] = worldPosition;
        VisualizePoint(worldPosition, currentVertexIndex);

        // Dynamically draw the user's line as they place points
        lineRenderer.positionCount = currentVertexIndex + 1;
        lineRenderer.SetPosition(currentVertexIndex, worldPosition);
        lineRenderer.enabled = true;
        Debug.Log("Set new points");

        // If fully defined, normalize and set correctors
        if (currentVertexIndex >= REQUIRED_POINTS - 1)
        {
            finishSetup();
        }
    }

    private void finishSetup()
    {
        // plane heuristic: all points lie on same y-value.
        // Corner 3 and 4 are perpendicular to corner 2 and 1 respectively.
        float yValue = (initPlaneAnchors[0] + initPlaneAnchors[1]).y / 2;

        planeAnchors[0] = initPlaneAnchors[0];
        planeAnchors[0].y = yValue;
        planeAnchors[1] = initPlaneAnchors[1];
        planeAnchors[1].y = yValue;

        lineRenderer.positionCount = REQUIRED_POINTS + 1;

        sideLength = (initPlaneAnchors[2] - initPlaneAnchors[1]).magnitude;
        SetBottomCorners();
        VisualizePoint(planeAnchors[3], 3);

        AddPlaneCorrectors();
        UpdateVisuals();
        planeDefined = true;
    }

    private void SetBottomCorners()
    {
        Vector3 baseLine = planeAnchors[1] - planeAnchors[0];
        Vector3 edgeVector = sideLength * Vector3.Cross(baseLine, Vector3.up).normalized;
        planeAnchors[2] = planeAnchors[1] - edgeVector;
        planeAnchors[3] = planeAnchors[0] - edgeVector;
    }

    private void UpdateVisuals()
    {
        // Assumes that planeAnchors contains the correct Positions
        for (int i = 0; i <= REQUIRED_POINTS; i++)
        {
            lineRenderer.SetPosition(i, planeAnchors[i]);
            anchorPrefabs[i].transform.position = planeAnchors[i];
        }
        Debug.Log("Updated all positions.");
    }

    public void MovePoint(int index, Vector3 delta)
    {
        planeAnchors[index] += delta;
        SetBottomCorners();
        UpdateVisuals();
    }

    /// <summary>
    /// Creates a visual marker for a captured point.
    /// </summary>
    private void VisualizePoint(Vector3 position, int index)
    {
        if (pointVisualizerPrefab != null)
        {
            GameObject tempVertexPrefab = Instantiate(pointVisualizerPrefab, position, Quaternion.identity);
            tempVertexPrefab.transform.localScale = Vector3.one * vertexPrefabWidth;
            anchorPrefabs[index] = tempVertexPrefab;
        }
    }

    private void AddPlaneCorrectors()
    {
        // Assumes that planeAnchors contains the correct Positions

        // Left Anchor
        Vector3 leftPos = planeAnchors[0] + 0.1f * (Vector3.up + Vector3.left);
        GameObject leftCorrectorObj = Instantiate(planeCornerCorrectorNode, leftPos, Quaternion.identity);
        PlaneCorrectionNode leftNode = leftCorrectorObj.GetComponent<PlaneCorrectionNode>();
        leftNode.manager = this;
        leftNode.vertexIndex = 0;
        leftCornerCorrecorNode = leftCorrectorObj;

        // Right Anchor
        Vector3 rightPos = planeAnchors[1] + 0.1f * (Vector3.up + Vector3.right);
        GameObject rightCorrectorObj = Instantiate(planeCornerCorrectorNode, rightPos, Quaternion.identity);
        PlaneCorrectionNode rightNode = rightCorrectorObj.GetComponent<PlaneCorrectionNode>();
        rightNode.manager = this;
        rightNode.vertexIndex = 1;
        rightCornerCorrectorNode = rightCorrectorObj;
    }
    

    /// <summary>
    /// Finalizes the plane definition after four points are captured.
    /// </summary>
    private void DefinePlane()
    {
        // --- Calculate the mathematical plane from the first three points ---
        Plane initialPlane = new Plane(planeAnchors[0], planeAnchors[1], planeAnchors[2]);

        // --- Ensure the plane's normal is oriented towards the player ---
        Vector3 planeCenter = (planeAnchors[0] + planeAnchors[1] + planeAnchors[2] + planeAnchors[3]) / 4f;
        Vector3 directionToPlayer = playerCameraTransform.position - planeCenter;

        // The dot product checks if the normal is pointing away from the player.
        // A negative result means the angle is > 90 degrees, so we need to flip the normal.
        if (Vector3.Dot(initialPlane.normal, directionToPlayer) < 0)
        {
            // Re-calculate the plane with a reversed vertex order (winding) to flip the normal.
            initialPlane = new Plane(planeAnchors[0], planeAnchors[2], planeAnchors[1]);
        }

        // Create our custom data structure with the correctly oriented plane.
        DefinedPlane finalPlane = new DefinedPlane(initialPlane, planeAnchors[0], planeAnchors[1], planeAnchors[2], planeAnchors[3]);

        // Draw the visual representations of the planes.
        DrawPlaneVisuals(finalPlane);

        // Notify listeners that a new plane has been defined.
        OnPlaneDefined?.Invoke(finalPlane);

        Debug.Log("Plane defined with normal oriented towards the player.");
    }

    /// <summary>
    /// Draws the user-defined quad and a representation of the mathematical plane.
    /// </summary>
    private void DrawPlaneVisuals(DefinedPlane definedPlane)
    {
        // 1. Finalize the user-defined quad visual
        lineRenderer.SetPositions(planeAnchors);

        // 2. Draw the mathematical plane visualizer
        if (mathPlaneVisualizer != null) Destroy(mathPlaneVisualizer);

        mathPlaneVisualizer = GameObject.CreatePrimitive(PrimitiveType.Quad);
        mathPlaneVisualizer.name = "MathPlaneVisualizer";
        mathPlaneVisualizer.transform.position = definedPlane.Center;
        // Orient the quad so its "up" (normal) matches the plane's normal.
        mathPlaneVisualizer.transform.rotation = Quaternion.LookRotation(definedPlane.Plane.normal);
        mathPlaneVisualizer.transform.localScale = Vector3.one * mathPlaneSize;

        // Apply the cached transparent material
        Renderer mathPlaneRenderer = mathPlaneVisualizer.GetComponent<Renderer>();
        if (mathPlaneRenderer != null)
        {
            mathPlaneRenderer.material = mathPlaneMaterial;
        }

        // setupGameObjects.Add(mathPlaneVisualizer); // Add for cleanup
    }

    /// <summary>
    /// Resets the plane definition process, clearing all visuals and state.
    /// </summary>
    public void ResetDefinition()
    {
        currentVertexIndex = -1;

        // Disable and clear the line renderer
        lineRenderer.enabled = false;
        lineRenderer.positionCount = 0;

        // Destroy all instantiated setup objects (point markers, plane visualizer)
        foreach (GameObject setupObject in anchorPrefabs)
        {
            // Check if object hasn't already been destroyed (e.g., scene change)
            if (setupObject != null)
            {
                Destroy(setupObject);
            }
        }
        // setupGameObjects.Clear();

        // Explicitly nullify the reference after destruction
        mathPlaneVisualizer = null;

        Debug.Log("Plane definition reset.");
    }

    /// <summary>
    /// Helper method to configure a material for standard transparency.
    /// </summary>
    private void SetupTransparentMaterial(Material material)
    {
        material.SetFloat("_Mode", 3); // Set rendering mode to Transparent
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }
}