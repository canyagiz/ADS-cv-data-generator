using UnityEngine;
using UnityEngine.Perception.Randomization.Randomizers;
using UnityEngine.Perception.Randomization.Parameters;

[AddComponentMenu("Perception/Randomizers/Camera Angle Randomizer")]
public class CameraAngleRandomizer : Randomizer
{
    // FloatParameter allows you to define a range (e.g., -10 to 10) or a distribution (Uniform, Normal) in the Inspector.

    [Header("Rotation Settings")]
    [Tooltip("Rotation around the X-axis (Pitch). Controls looking Up and Down.")]
    public FloatParameter rotX; 

    [Tooltip("Rotation around the Y-axis (Yaw). Controls turning Left and Right.")]
    public FloatParameter rotY; 

    [Tooltip("Rotation around the Z-axis (Roll). Controls tilting the camera sideways (Dutch angle).")]
    public FloatParameter rotZ; 

    private Camera cam;

    /// <summary>
    /// Called once when the randomization scenario begins.
    /// Finds and caches the Main Camera reference to improve performance.
    /// </summary>
    protected override void OnScenarioStart()
    {
        cam = Camera.main;
    }

    /// <summary>
    /// Called at the start of every simulation iteration (frame).
    /// Applies a new random rotation to the camera.
    /// </summary>
    protected override void OnIterationStart()
    {
        if (cam == null) return;

        // NOTE: We do NOT modify the camera's Position. 
        // The camera remains fixed at the location where you placed it in the scene.
        
        // We only randomize the Rotation (Orientation).
        // The .Sample() method picks a random value based on the distribution settings in the Inspector.
        cam.transform.rotation = Quaternion.Euler(
            rotX.Sample(), // Pitch
            rotY.Sample(), // Yaw
            rotZ.Sample()  // Roll
        );
    }
}