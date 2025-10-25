using Assets.Scripts;
using System;
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
    private List<GameObject> setupGameObjects;
    private GameObject mathPlaneVisualizer;
    #endregion

    void Awake()
    {
        // --- Cache component references ---
        lineRenderer = GetComponent<LineRenderer>();
        playerCameraTransform = Camera.main.transform; // Get the main camera for player viewpoint

        // --- Initialize collections and arrays ---
        setupGameObjects = new List<GameObject>();
        vertices = new Vector3[requiredPoints];

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

    void Update()
    {
        // Use the primary button on the right controller to place points
        if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch))
        {
            Vector3 currentPos = OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch) + OVR_CONTROLLER_RADIUS;
            CapturePoint(currentPos);
        }

        // Use the secondary button on the left controller to reset
        if (OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.LTouch))
        {
            //ResetDefinition();
        }
    }

    /// <summary>
    /// Captures a point in 3D space and visualizes it.
    /// </summary>
    private void CapturePoint(Vector3 worldPosition)
    {
        if (currentVertexIndex >= requiredPoints - 1) return;

        vertices[++currentVertexIndex] = worldPosition;
        VisualizePoint(worldPosition);

        // Dynamically draw the user's line as they place points
        lineRenderer.positionCount = currentVertexIndex + 1;
        lineRenderer.SetPosition(currentVertexIndex, worldPosition);
        lineRenderer.enabled = true;

        if (currentVertexIndex == requiredPoints - 1)
        {
            DefinePlane();
        }
    }

    /// <summary>
    /// Creates a visual marker for a captured point.
    /// </summary>
    private void VisualizePoint(Vector3 position)
    {
        if (pointVisualizerPrefab != null)
        {
            GameObject tempVertexPrefab = Instantiate(pointVisualizerPrefab, position, Quaternion.identity);
            tempVertexPrefab.transform.localScale = Vector3.one * vertexPrefabWidth;
            setupGameObjects.Add(tempVertexPrefab);
        }
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

        setupGameObjects.Add(mathPlaneVisualizer); // Add for cleanup
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
        setupGameObjects.Clear();

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