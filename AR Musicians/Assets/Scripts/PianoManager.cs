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
    [SerializeField] private int requiredPoints = 4;
    [SerializeField] private GameObject pointVisualizerPrefab;
    [SerializeField] private GameObject planeCorrectorPrefab;

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
    private Vector3[] vertices;
    private int currentVertexIndex = -1;
    private readonly Vector3 OVR_CONTROLLER_RADIUS = new(0.0f, 0.0f, 0.03f);

    private GameObject[] setupGameObjects;
    private GameObject[] planeCorrectionHandlers;
    private GameObject mathPlaneVisualizer;
    #endregion

    void Awake()
    {
        // --- Cache component references ---
        lineRenderer = GetComponent<LineRenderer>();
        playerCameraTransform = Camera.main.transform; // Get the main camera for player viewpoint

        // --- Initialize collections and arrays ---
        setupGameObjects = new GameObject[requiredPoints];
        vertices = new Vector3[requiredPoints];
        planeCorrectionHandlers = new GameObject[requiredPoints];

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
        if (currentVertexIndex == requiredPoints - 1)
        {
            if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch))
            {
                DefinePlane();    
            }

            if (OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch))
            {
                NormalizePoints();
            }
        }
    }

    private void NormalizePoints()
    {
        Vector3 pos1 = planeCorrectionHandlers[0].transform.position;
        Vector3 pos2 = planeCorrectionHandlers[1].transform.position;
        Vector3 pos3 = planeCorrectionHandlers[2].transform.position;
        Vector3 pos4 = planeCorrectionHandlers[3].transform.position;

        Vector3 edgeDirection = (pos2 - pos1).normalized;
        float rightLengt = (pos3 - pos2).magnitude;
        Vector3 front = Vector3.Cross(edgeDirection, Vector3.up).normalized;
        pos3 = pos2 - rightLengt * front;
        pos4 = pos1 - rightLengt * front;

        planeCorrectionHandlers[2].transform.position = pos3;
        planeCorrectionHandlers[3].transform.position = pos4;
    }

    /// Captures a point in 3D space and visualizes it.
    public void RegisterPoint(Vector3 worldPosition)
    {
        if (currentVertexIndex >= requiredPoints - 1) return;

        vertices[++currentVertexIndex] = worldPosition;
        VisualizePoint(worldPosition, currentVertexIndex);
        AddPlaneCorrector(worldPosition, currentVertexIndex);

        // Dynamically draw the user's line as they place points
        lineRenderer.positionCount = currentVertexIndex + 1;
        lineRenderer.SetPosition(currentVertexIndex, worldPosition);
        lineRenderer.enabled = true;
    }

    public void MovePoint(int index, Vector3 delta)
    {
        Vector3 oldLinePosition = lineRenderer.GetPosition(index);
        lineRenderer.SetPosition(index, oldLinePosition + delta);
        setupGameObjects[index].transform.position += delta;
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
            setupGameObjects[index] = tempVertexPrefab;
        }
    }

    private void AddPlaneCorrector(Vector3 position, int index)
    {
        position += (0.1f * Vector3.up);
        bool left = index == 0 || index == 3;
        position += 0.1f * (left ? Vector3.left : Vector3.right);

        GameObject planeCorrector = Instantiate(planeCorrectorPrefab, position, Quaternion.identity);
        PlaneCorrectionNode correctionNode = planeCorrector.GetComponent<PlaneCorrectionNode>();
        correctionNode.manager = this;
        correctionNode.vertexIndex = index;
        planeCorrectionHandlers[index] = planeCorrector;
    }
    

    /// <summary>
    /// Finalizes the plane definition after four points are captured.
    /// </summary>
    private void DefinePlane()
    {
        // --- Calculate the mathematical plane from the first three points ---
        Plane initialPlane = new Plane(vertices[0], vertices[1], vertices[2]);

        // --- Ensure the plane's normal is oriented towards the player ---
        Vector3 planeCenter = (vertices[0] + vertices[1] + vertices[2] + vertices[3]) / 4f;
        Vector3 directionToPlayer = playerCameraTransform.position - planeCenter;

        // The dot product checks if the normal is pointing away from the player.
        // A negative result means the angle is > 90 degrees, so we need to flip the normal.
        if (Vector3.Dot(initialPlane.normal, directionToPlayer) < 0)
        {
            // Re-calculate the plane with a reversed vertex order (winding) to flip the normal.
            initialPlane = new Plane(vertices[0], vertices[2], vertices[1]);
        }

        // Create our custom data structure with the correctly oriented plane.
        DefinedPlane finalPlane = new DefinedPlane(initialPlane, vertices[0], vertices[1], vertices[2], vertices[3]);

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
        lineRenderer.SetPositions(vertices);

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
        foreach (GameObject setupObject in setupGameObjects)
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