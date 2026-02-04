using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Perception.Randomization.Randomizers;

[AddComponentMenu("Perception/Randomizers/Plane Background Randomizer")]
public class PlaneBackgroundRandomizer : Randomizer
{
    [Header("Configuration")]
    [Tooltip("The list of background Plane objects in the scene to apply textures to.")]
    public GameObject[] backgroundPlanes;

    [Tooltip("The folder path inside 'Assets/Resources/' where your background images are stored. (e.g., 'Backgrounds')")]
    public string resourceFolderPath = "Backgrounds";

    // Internal cache for loaded textures
    private Texture2D[] backgroundImages;
    
    // Cache for material instances to reuse instead of creating new ones each frame
    // Key: Renderer instance ID, Value: Cached material
    private Dictionary<int, Material> cachedMaterials = new Dictionary<int, Material>();
    
    // Counter for periodic garbage collection
    private int iterationCount = 0;
    private const int GC_INTERVAL = 50;

    /// <summary>
    /// Called once when the simulation scenario begins.
    /// Loads all textures from the Resources folder into memory to avoid lag during simulation.
    /// </summary>
    protected override void OnScenarioStart()
    {
        // 1. Load all Texture2D assets from the specified Resources folder
        backgroundImages = Resources.LoadAll<Texture2D>(resourceFolderPath);
        iterationCount = 0;
        cachedMaterials.Clear();

        // 2. Validation Check
        if (backgroundImages.Length == 0)
        {
            Debug.LogError($"[PlaneBackgroundRandomizer] ERROR: No textures found in 'Resources/{resourceFolderPath}'! Please check the folder path and ensure images are Texture2D.");
        }
        else
        {
            Debug.Log($"[PlaneBackgroundRandomizer] Successfully loaded {backgroundImages.Length} background images.");
        }
    }

    /// <summary>
    /// Called at the start of every simulation iteration (frame).
    /// Assigns a random texture to each defined plane.
    /// </summary>
    protected override void OnIterationStart()
    {
        // Safety check: If no images loaded or no planes assigned, skip execution
        if (backgroundImages == null || backgroundImages.Length == 0) return;
        if (backgroundPlanes == null || backgroundPlanes.Length == 0) return;

        // Periodic garbage collection to free up memory during long simulations
        iterationCount++;
        if (iterationCount % GC_INTERVAL == 0)
        {
            Resources.UnloadUnusedAssets();
            System.GC.Collect();
        }

        // Loop through every assigned plane
        foreach (var plane in backgroundPlanes)
        {
            if (plane == null) continue;

            Renderer planeRenderer = plane.GetComponent<Renderer>();

            if (planeRenderer != null)
            {
                // Get or create cached material for this renderer
                // This prevents creating a new material instance every frame (memory leak fix)
                int rendererId = planeRenderer.GetInstanceID();
                Material mat;
                
                if (!cachedMaterials.TryGetValue(rendererId, out mat))
                {
                    // First time: Create the material instance and cache it
                    mat = planeRenderer.material;
                    cachedMaterials[rendererId] = mat;
                }
                
                // Pick a random texture from the loaded list
                int randIndex = Random.Range(0, backgroundImages.Length);
                
                // Assign texture to the cached material
                mat.mainTexture = backgroundImages[randIndex];
            }
            else
            {
                Debug.LogWarning($"[PlaneBackgroundRandomizer] The object '{plane.name}' does not have a Renderer component!");
            }
        }
    }

    /// <summary>
    /// Cleanup when the scenario ends or the component is destroyed.
    /// Properly destroys cached materials to prevent memory leaks.
    /// </summary>
    protected override void OnScenarioComplete()
    {
        CleanupMaterials();
    }

    protected override void OnDestroy()
    {
        CleanupMaterials();
    }

    private void CleanupMaterials()
    {
        foreach (var kvp in cachedMaterials)
        {
            if (kvp.Value != null)
            {
                Object.Destroy(kvp.Value);
            }
        }
        cachedMaterials.Clear();
    }
}