using UnityEngine;
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

    /// <summary>
    /// Called once when the simulation scenario begins.
    /// Loads all textures from the Resources folder into memory to avoid lag during simulation.
    /// </summary>
    protected override void OnScenarioStart()
    {
        // 1. Load all Texture2D assets from the specified Resources folder
        backgroundImages = Resources.LoadAll<Texture2D>(resourceFolderPath);

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

        // Loop through every assigned plane
        foreach (var plane in backgroundPlanes)
        {
            if (plane == null) continue;

            Renderer planeRenderer = plane.GetComponent<Renderer>();

            if (planeRenderer != null)
            {
                // Pick a random texture from the loaded list
                int randIndex = Random.Range(0, backgroundImages.Length);
                
                // Assign it to the material
                // Note: Accessing .material creates a standard instance suitable for randomization
                planeRenderer.material.mainTexture = backgroundImages[randIndex];
            }
            else
            {
                Debug.LogWarning($"[PlaneBackgroundRandomizer] The object '{plane.name}' does not have a Renderer component!");
            }
        }
    }
}