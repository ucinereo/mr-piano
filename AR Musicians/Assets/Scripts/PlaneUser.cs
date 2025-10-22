using UnityEngine;

public class PlaneUser : MonoBehaviour
{
    [Header("Settings")]
    public GameObject objectToPlaceOnPlane;

    private bool _planeReceived = false;

    // Subscribe to the event when the script is enabled
    private void OnEnable()
    {
        PlaneDefiner.OnPlaneDefined += HandlePlaneDefined;
    }

    // ALWAYS unsubscribe when the script is disabled or destroyed to prevent memory leaks
    private void OnDisable()
    {
        PlaneDefiner.OnPlaneDefined -= HandlePlaneDefined;
    }

    // This is the event handler method. It will only be called when the event is fired.
    private void HandlePlaneDefined(DefinedPlane definedPlane)
    {
        Debug.Log("PlaneUser has received the plane!");
        _planeReceived = true;

        // Now we can use the plane data
        // Example: Place an object at the center of the defined plane
        if (objectToPlaceOnPlane != null)
        {
            GameObject placedObject = Instantiate(objectToPlaceOnPlane, definedPlane.Center, Quaternion.LookRotation(definedPlane.Plane.normal));
            placedObject.transform.Rotate(90, 0, 0); // Adjust rotation if needed
        }
    }

    // You can now use the plane data in Update if needed, but only after it has been received.
    void Update()
    {
        if (!_planeReceived) return;

        // Do something every frame that requires the plane...
        // For example, check if the user's controller is pointing at the plane.
    }
}