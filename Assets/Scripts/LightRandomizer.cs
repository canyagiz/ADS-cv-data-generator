using UnityEngine;
using UnityEngine.Perception.Randomization.Randomizers;
using UnityEngine.Perception.Randomization.Parameters;

[AddComponentMenu("Perception/Randomizers/Light Randomizer")]
public class LightRandomizer : Randomizer
{
    [Header("Target Settings")]
    [Tooltip("The GameObject containing the Light component (usually the Directional Light / Sun). If left empty, the script will try to find one automatically.")]
    public GameObject lightObj;

    [Header("Randomization Parameters")]
    [Tooltip("The brightness of the light. Higher values mean a brighter scene.")]
    public FloatParameter intensity;

    [Tooltip("Rotation around the X-axis. For a Directional Light, this controls the height of the sun (Day/Night cycle).")]
    public FloatParameter rotX;

    [Tooltip("Rotation around the Y-axis. This controls the compass direction of the shadows (North/South/East/West).")]
    public FloatParameter rotY;

    [Tooltip("Rotation around the Z-axis. Usually has little effect on Directional Lights but useful for Spotlights.")]
    public FloatParameter rotZ;

    // Internal reference to the actual Light component
    private Light sceneLight;

    /// <summary>
    /// Called once when the simulation scenario begins.
    /// caches the light component to ensure high performance.
    /// </summary>
    protected override void OnScenarioStart()
    {
        // 1. Automatic Detection: If the user forgot to assign a light, try to find the Main Light.
        if (lightObj == null)
        {
            // Try to find the first Directional Light in the scene
            Light[] lights = GameObject.FindObjectsOfType<Light>();
            foreach (var l in lights)
            {
                if (l.type == LightType.Directional)
                {
                    lightObj = l.gameObject;
                    sceneLight = l;
                    break;
                }
            }
            
            if (lightObj == null && lights.Length > 0)
            {
                // Fallback: Just take the first light found if no directional light exists
                lightObj = lights[0].gameObject;
                sceneLight = lights[0];
            }
        }
        else
        {
            // If the user assigned an object, get the Light component from it
            sceneLight = lightObj.GetComponent<Light>();
        }

        // 2. Error Check
        if (sceneLight == null)
        {
            Debug.LogError("[LightRandomizer] No Light component found! Please assign a valid GameObject.");
        }
    }

    /// <summary>
    /// Called at the start of every simulation iteration (frame).
    /// Applies random intensity and rotation to the light.
    /// </summary>
    protected override void OnIterationStart()
    {
        if (sceneLight == null) return;

        // 1. Randomize Intensity (Brightness)
        // .Sample() picks a random value based on the range defined in the Inspector
        sceneLight.intensity = intensity.Sample();

        // 2. Randomize Rotation (Shadow Direction)
        // Note: We use the lightObj transform because the Light component itself doesn't have a transform property in this context.
        lightObj.transform.rotation = Quaternion.Euler(
            rotX.Sample(),
            rotY.Sample(),
            rotZ.Sample()
        );
    }
}