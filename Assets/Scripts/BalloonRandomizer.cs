using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Perception.Randomization.Randomizers;

[AddComponentMenu("Perception/Randomizers/Balloon Randomizer")]
public class BalloonRandomizer : Randomizer
{
    [Tooltip(" The list of prefabs (e.g., planes/balloons) to spawn randomly.")]
    public GameObject[] balloonPrefabs;

    [Header("Spawn Settings")]
    [Tooltip("Minimum number of objects to spawn per iteration.")]
    public int minCount = 5;
    [Tooltip("Maximum number of objects to spawn per iteration.")]
    public int maxCount = 10;

    [Tooltip("Minimum distance from the camera (depth).")]
    public float spawnDistMin = 20.0f;
    [Tooltip("Maximum distance from the camera (depth).")]
    public float spawnDistMax = 80.0f;

    [Tooltip("Minimum uniform scale factor for the spawned objects.")]
    public float scaleMin = 120f;
    [Tooltip("Maximum uniform scale factor for the spawned objects.")]
    public float scaleMax = 120f;

    [Header("Color Settings")]
    [Tooltip("Gradient palette for the Parent object (e.g., the plane fuselage). A random color from this gradient will be picked.")]
    public Gradient parentColors;

    [Tooltip("Gradient palette for the Child object (e.g., the balloon). A random color from this gradient will be picked.")]
    public Gradient childColors;

    [Tooltip("Exact name of the child object in the hierarchy (e.g., 'WhiteBaloon') to apply the child color to.")]
    public string childObjectName = "WhiteBaloon";

    [Header("Math Fix / Collision Tuning")]
    [Tooltip("Correction factor for the radius calculation. Since the scale is very large (120+), use a small value (e.g., 0.05) to prevent the collision circles from being too large.")]
    [Range(0.001f, 1f)]
    public float radiusCorrection = 0.05f; 

    [Tooltip("Separation padding between objects. 1.0 means objects can touch; >1.0 requires more space; <1.0 allows slight overlap.")]
    public float separationPadding = 0.9f;

    [Header("Rotation Settings")]
    public float minRotationX = 0f;
    public float maxRotationX = 360f;
    public float minRotationY = 0f;
    public float maxRotationY = 360f;

    [Header("Material Randomization - Parent (Aircraft)")]
    [Tooltip("Enable material property randomization for parent objects (aircraft).")]
    public bool enableParentMaterialRandomization = false;
    
    [Tooltip("Use white-ish tones for parent objects (simulates real competition models).")]
    public bool parentUseWhiteTones = true;
    
    [Tooltip("Minimum roughness for parent material (0=shiny, 1=matte).")]
    [Range(0f, 1f)]
    public float parentRoughnessMin = 0.3f;
    
    [Tooltip("Maximum roughness for parent material.")]
    [Range(0f, 1f)]
    public float parentRoughnessMax = 0.7f;
    
    [Tooltip("Minimum metallic value for parent (0=plastic, 1=metal).")]
    [Range(0f, 1f)]
    public float parentMetallicMin = 0.0f;
    
    [Tooltip("Maximum metallic value for parent.")]
    [Range(0f, 1f)]
    public float parentMetallicMax = 0.2f;

    [Header("Material Randomization - Balloon")]
    [Tooltip("Enable material property randomization for balloons.")]
    public bool enableBalloonMaterialRandomization = false;
    
    [Tooltip("Minimum roughness for balloon material.")]
    [Range(0f, 1f)]
    public float balloonRoughnessMin = 0.4f;
    
    [Tooltip("Maximum roughness for balloon material.")]
    [Range(0f, 1f)]
    public float balloonRoughnessMax = 0.8f;
    
    [Tooltip("Minimum metallic value for balloon.")]
    [Range(0f, 1f)]
    public float balloonMetallicMin = 0.0f;
    
    [Tooltip("Maximum metallic value for balloon.")]
    [Range(0f, 1f)]
    public float balloonMetallicMax = 0.1f;

    private Camera cam;

    /// <summary>
    /// Helper class to store data about objects spawned in the current frame
    /// to perform screen-space overlap checks.
    /// </summary>
    class SpawnedBalloon
    {
        public Vector2 screenPos;
        public float screenRadius;
    }

    // List to keep track of screen-space positions for overlap detection
    List<SpawnedBalloon> used = new List<SpawnedBalloon>();
    
    // List to keep track of actual GameObjects to destroy them in the next iteration
    List<GameObject> spawned = new List<GameObject>();
    
    // List to track dynamically created materials for proper cleanup (prevents VRAM leaks)
    List<Material> createdMaterials = new List<Material>();
    
    // Counter for periodic garbage collection
    private int iterationCount = 0;
    private const int GC_INTERVAL = 50; // Run garbage collection every N iterations

    /// <summary>
    /// Called when the Randomization Scenario begins.
    /// </summary>
    protected override void OnScenarioStart()
    {
        cam = Camera.main;
        iterationCount = 0;
    }

    /// <summary>
    /// Called at the start of every simulation iteration (frame).
    /// Handles cleaning up old objects and spawning new ones.
    /// </summary>
    protected override void OnIterationStart()
    {
        // 1. Cleanup: Destroy dynamically created materials first (prevents VRAM leaks)
        foreach (var mat in createdMaterials)
        {
            if (mat != null) Object.Destroy(mat);
        }
        createdMaterials.Clear();
        
        // 2. Cleanup: Destroy GameObjects from the previous iteration
        foreach (var b in spawned)
        {
            if (b != null) GameObject.Destroy(b);
        }
        spawned.Clear();
        used.Clear();

        // 3. Periodic garbage collection to free up memory during long simulations
        iterationCount++;
        if (iterationCount % GC_INTERVAL == 0)
        {
            Resources.UnloadUnusedAssets();
            System.GC.Collect();
        }

        if (cam == null) cam = Camera.main;

        // 4. Spawning Logic
        foreach (var prefab in balloonPrefabs)
        {
            int count = Random.Range(minCount, maxCount + 1);
            int fails = 0; // Safety counter to prevent infinite loops

            for (int i = 0; i < count; i++)
            {
                // Attempt to spawn a balloon. If it fails (due to lack of space), we try again.
                if (!SpawnBalloon(prefab))
                {
                    fails++;
                    // If we fail to find a spot 20 times, we skip this specific balloon for this frame.
                    if (fails > 20) break; 
                }
            }
        }
    }

    /// <summary>
    /// Attempts to spawn a single balloon at a random valid position.
    /// </summary>
    /// <returns>True if the balloon was successfully spawned; False if no valid position was found.</returns>
    bool SpawnBalloon(GameObject prefab)
    {
        float scale = Random.Range(scaleMin, scaleMax);

        // MATHEMATICAL CORRECTION:
        // Because the object scale is large, we multiply by 'radiusCorrection' 
        // to get a reasonable physical radius for collision calculations.
        float worldRadius = 0.5f * scale * radiusCorrection;

        Vector3 worldPos = Vector3.zero;
        Vector2 screenPos = Vector2.zero;
        float screenRadius = 0;

        int attempts = 0;
        bool positionFound = false;
        int maxAttempts = 100; // Maximum attempts to find a non-overlapping position

        // Loop to find a valid position in the camera view
        do
        {
            attempts++;

            // 1. Pick a random position in Viewport space (0,0 to 1,1)
            // We use 0.1 to 0.9 to keep objects somewhat centered and not cut off by screen edges.
            Vector2 vp = new Vector2(
                Random.Range(0.1f, 0.9f),
                Random.Range(0.1f, 0.9f)
            );

            // 2. Convert Viewport + Random Distance to World Position
            float dist = Random.Range(spawnDistMin, spawnDistMax);
            worldPos = cam.ViewportToWorldPoint(new Vector3(vp.x, vp.y, dist));

            // 3. Convert World Position to Screen Position (Pixels)
            screenPos = cam.WorldToScreenPoint(worldPos);

            // 4. Calculate Screen Radius
            // We project a point on the edge of the sphere to screen space to determine
            // how many pixels wide the object is on screen at this distance.
            Vector3 edgePointWorld = worldPos + cam.transform.right * worldRadius;
            Vector2 edgePointScreen = cam.WorldToScreenPoint(edgePointWorld);

            screenRadius = Vector2.Distance(screenPos, edgePointScreen);

            // 5. Check for Overlaps
            if (!IsVisuallyOverlapping(screenPos, screenRadius))
            {
                positionFound = true;
            }

        } while (!positionFound && attempts < maxAttempts);

        // If a valid position was found, instantiate and randomize the object
        if (positionFound)
        {
            // Record the position so subsequent balloons don't spawn on top of this one
            used.Add(new SpawnedBalloon { screenPos = screenPos, screenRadius = screenRadius });

            GameObject balloon = GameObject.Instantiate(prefab, worldPos, Quaternion.identity);
            
            // Apply scale
            balloon.transform.localScale = Vector3.one * scale;

            // Apply random rotation
            float rotX = Random.Range(minRotationX, maxRotationX);
            float rotY = Random.Range(minRotationY, maxRotationY);
            balloon.transform.rotation = Quaternion.Euler(rotX, rotY, 0f);


            // ---------------------------------------------------------
            // COLOR & MATERIAL RANDOMIZATION LOGIC
            // Note: Accessing .material creates a new instance. We track these
            // in createdMaterials list for proper cleanup to prevent VRAM leaks.
            // ---------------------------------------------------------

            // 1. Apply Color and Material to Parent (e.g., Ally-plane body)
            Renderer parentRenderer = balloon.GetComponent<Renderer>();
            if (parentRenderer != null)
            {
                Material parentMat = parentRenderer.material;
                
                // Color: Use white-ish tones if enabled, otherwise use gradient
                if (parentUseWhiteTones)
                {
                    // Generate white-ish color: high RGB values with slight variation
                    float baseWhite = Random.Range(0.85f, 1.0f);
                    float rVariation = Random.Range(-0.05f, 0.05f);
                    float gVariation = Random.Range(-0.05f, 0.05f);
                    float bVariation = Random.Range(-0.05f, 0.05f);
                    parentMat.color = new Color(
                        Mathf.Clamp01(baseWhite + rVariation),
                        Mathf.Clamp01(baseWhite + gVariation),
                        Mathf.Clamp01(baseWhite + bVariation)
                    );
                }
                else
                {
                    parentMat.color = parentColors.Evaluate(Random.value);
                }
                
                // Material properties (if enabled)
                if (enableParentMaterialRandomization)
                {
                    // Smoothness (inverse of roughness) - HDRP uses _Smoothness
                    float roughness = Random.Range(parentRoughnessMin, parentRoughnessMax);
                    parentMat.SetFloat("_Smoothness", 1f - roughness);
                    
                    // Metallic
                    float metallic = Random.Range(parentMetallicMin, parentMetallicMax);
                    parentMat.SetFloat("_Metallic", metallic);
                }
                
                createdMaterials.Add(parentMat);
            }

            // 2. Apply Color and Material to Child (e.g., WhiteBaloon)
            Transform childTransform = balloon.transform.Find(childObjectName);
            if (childTransform != null)
            {
                Renderer childRenderer = childTransform.GetComponent<Renderer>();
                if (childRenderer != null)
                {
                    Material childMat = childRenderer.material;
                    
                    // Balloon gets full color variety
                    childMat.color = childColors.Evaluate(Random.value);
                    
                    // Material properties (if enabled)
                    if (enableBalloonMaterialRandomization)
                    {
                        float roughness = Random.Range(balloonRoughnessMin, balloonRoughnessMax);
                        childMat.SetFloat("_Smoothness", 1f - roughness);
                        
                        float metallic = Random.Range(balloonMetallicMin, balloonMetallicMax);
                        childMat.SetFloat("_Metallic", metallic);
                    }
                    
                    createdMaterials.Add(childMat);
                }
            }
            // ---------------------------------------------------------

            spawned.Add(balloon);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if a candidate circle overlaps with any previously spawned circles in screen space.
    /// </summary>
    /// <param name="candidatePos">Screen position (pixels) of the new object.</param>
    /// <param name="candidateRadius">Radius (pixels) of the new object.</param>
    /// <returns>True if an overlap is detected.</returns>
    bool IsVisuallyOverlapping(Vector2 candidatePos, float candidateRadius)
    {
        foreach (var b in used)
        {
            float dist = Vector2.Distance(b.screenPos, candidatePos);
            // Calculate minimum allowed distance based on radii and padding
            float minSeparation = (b.screenRadius + candidateRadius) * separationPadding;

            if (dist < minSeparation)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Called when the simulation scenario completes.
    /// Performs final cleanup of all spawned objects and materials.
    /// </summary>
    protected override void OnScenarioComplete()
    {
        CleanupAll();
    }

    protected override void OnDestroy()
    {
        CleanupAll();
    }

    /// <summary>
    /// Cleans up all spawned objects and materials to prevent memory leaks.
    /// </summary>
    private void CleanupAll()
    {
        // Destroy all tracked materials
        foreach (var mat in createdMaterials)
        {
            if (mat != null) Object.Destroy(mat);
        }
        createdMaterials.Clear();

        // Destroy all spawned GameObjects
        foreach (var b in spawned)
        {
            if (b != null) GameObject.Destroy(b);
        }
        spawned.Clear();
        used.Clear();

        // Final garbage collection
        Resources.UnloadUnusedAssets();
        System.GC.Collect();
    }
}