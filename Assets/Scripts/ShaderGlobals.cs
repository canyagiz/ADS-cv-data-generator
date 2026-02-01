using UnityEngine;

[AddComponentMenu("Perception/Helpers/Shader Globals")]
public class ShaderGlobals : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("The Directional Light acting as the sun. If left empty, the script will try to find it automatically.")]
    public Light sun;

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Tries to locate the Sun if not manually assigned.
    /// </summary>
    void Awake()
    {
        if (sun == null)
        {
            // 1. Try to find a light tagged as "MainLight" or the first Directional light
            Light[] lights = FindObjectsOfType<Light>();
            foreach (var l in lights)
            {
                if (l.type == LightType.Directional)
                {
                    sun = l;
                    Debug.Log($"[ShaderGlobals] Auto-assigned Directional Light: {l.name}");
                    break;
                }
            }
        }

        if (sun == null)
        {
            Debug.LogWarning("[ShaderGlobals] Warning: No Directional Light (Sun) found! Shader global values will not update.");
        }
    }

    /// <summary>
    /// Called every frame.
    /// Updates the global shader variables so ALL materials can access the light data.
    /// </summary>
    void Update() 
    {
        // Safety check to prevent errors
        if (sun == null) return;

        // 1. Set Light Direction
        // We use (-sun.transform.forward) because shaders usually expect the vector 
        // pointing FROM the surface TO the light source (L), which is opposite to the light's forward direction.
        Shader.SetGlobalVector("_MainLightDirection", -sun.transform.forward);

        // 2. Set Light Color
        // Passes the RGB color of the light to the shader.
        Shader.SetGlobalColor("_MainLightColor", sun.color);

        // 3. Set Light Intensity
        // Passes the brightness value to the shader.
        Shader.SetGlobalFloat("_MainLightIntensity", sun.intensity);
    }
}