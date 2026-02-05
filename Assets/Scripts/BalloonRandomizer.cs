using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Perception.Randomization.Randomizers;

[AddComponentMenu("Perception/Randomizers/Balloon Randomizer")]
public class BalloonRandomizer : Randomizer
{
    [Tooltip("The list of prefabs (e.g., planes/balloons) to spawn randomly.")]
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

    // --- YENİ EKLENEN Z EKSENİ AYARLARI ---
    [Tooltip("Minimum rotation on the Z axis (Roll).")]
    public float minRotationZ = 0f;
    [Tooltip("Maximum rotation on the Z axis (Roll).")]
    public float maxRotationZ = 360f;
    // ---------------------------------------

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

    class SpawnedBalloon
    {
        public Vector2 screenPos;
        public float screenRadius;
    }

    List<SpawnedBalloon> used = new List<SpawnedBalloon>();
    List<GameObject> spawned = new List<GameObject>();
    List<Material> createdMaterials = new List<Material>();

    private int iterationCount = 0;
    private const int GC_INTERVAL = 50;

    protected override void OnScenarioStart()
    {
        cam = Camera.main;
        iterationCount = 0;
    }

    protected override void OnIterationStart()
    {
        foreach (var mat in createdMaterials)
        {
            if (mat != null) Object.Destroy(mat);
        }
        createdMaterials.Clear();

        foreach (var b in spawned)
        {
            if (b != null) GameObject.Destroy(b);
        }
        spawned.Clear();
        used.Clear();

        iterationCount++;
        if (iterationCount % GC_INTERVAL == 0)
        {
            Resources.UnloadUnusedAssets();
            System.GC.Collect();
        }

        if (cam == null) cam = Camera.main;

        foreach (var prefab in balloonPrefabs)
        {
            int count = Random.Range(minCount, maxCount + 1);
            int fails = 0;

            for (int i = 0; i < count; i++)
            {
                if (!SpawnBalloon(prefab))
                {
                    fails++;
                    if (fails > 20) break;
                }
            }
        }
    }

    bool SpawnBalloon(GameObject prefab)
    {
        float scale = Random.Range(scaleMin, scaleMax);
        float worldRadius = 0.5f * scale * radiusCorrection;

        Vector3 worldPos = Vector3.zero;
        Vector2 screenPos = Vector2.zero;
        float screenRadius = 0;

        int attempts = 0;
        bool positionFound = false;
        int maxAttempts = 100;

        do
        {
            attempts++;
            Vector2 vp = new Vector2(
                Random.Range(0.1f, 0.9f),
                Random.Range(0.1f, 0.9f)
            );

            float dist = Random.Range(spawnDistMin, spawnDistMax);
            worldPos = cam.ViewportToWorldPoint(new Vector3(vp.x, vp.y, dist));
            screenPos = cam.WorldToScreenPoint(worldPos);

            Vector3 edgePointWorld = worldPos + cam.transform.right * worldRadius;
            Vector2 edgePointScreen = cam.WorldToScreenPoint(edgePointWorld);

            screenRadius = Vector2.Distance(screenPos, edgePointScreen);

            if (!IsVisuallyOverlapping(screenPos, screenRadius))
            {
                positionFound = true;
            }

        } while (!positionFound && attempts < maxAttempts);

        if (positionFound)
        {
            used.Add(new SpawnedBalloon { screenPos = screenPos, screenRadius = screenRadius });

            GameObject balloon = GameObject.Instantiate(prefab, worldPos, Quaternion.identity);

            balloon.transform.localScale = Vector3.one * scale;

            // ---------------------------------------------------------
            // GÜNCELLENMİŞ ROTASYON MANTIĞI (X, Y ve Z)
            // ---------------------------------------------------------
            float rotX = Random.Range(minRotationX, maxRotationX);
            float rotY = Random.Range(minRotationY, maxRotationY);
            float rotZ = Random.Range(minRotationZ, maxRotationZ); // Z Eklendi

            balloon.transform.rotation = Quaternion.Euler(rotX, rotY, rotZ);
            // ---------------------------------------------------------

            Renderer parentRenderer = balloon.GetComponent<Renderer>();
            if (parentRenderer != null)
            {
                Material parentMat = parentRenderer.material;

                if (parentUseWhiteTones)
                {
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

                if (enableParentMaterialRandomization)
                {
                    float roughness = Random.Range(parentRoughnessMin, parentRoughnessMax);
                    parentMat.SetFloat("_Smoothness", 1f - roughness);

                    float metallic = Random.Range(parentMetallicMin, parentMetallicMax);
                    parentMat.SetFloat("_Metallic", metallic);
                }

                createdMaterials.Add(parentMat);
            }

            Transform childTransform = balloon.transform.Find(childObjectName);
            if (childTransform != null)
            {
                Renderer childRenderer = childTransform.GetComponent<Renderer>();
                if (childRenderer != null)
                {
                    Material childMat = childRenderer.material;
                    childMat.color = childColors.Evaluate(Random.value);

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

            spawned.Add(balloon);
            return true;
        }

        return false;
    }

    bool IsVisuallyOverlapping(Vector2 candidatePos, float candidateRadius)
    {
        foreach (var b in used)
        {
            float dist = Vector2.Distance(b.screenPos, candidatePos);
            float minSeparation = (b.screenRadius + candidateRadius) * separationPadding;

            if (dist < minSeparation)
                return true;
        }
        return false;
    }

    protected override void OnScenarioComplete()
    {
        CleanupAll();
    }

    protected override void OnDestroy()
    {
        CleanupAll();
    }

    private void CleanupAll()
    {
        foreach (var mat in createdMaterials)
        {
            if (mat != null) Object.Destroy(mat);
        }
        createdMaterials.Clear();

        foreach (var b in spawned)
        {
            if (b != null) GameObject.Destroy(b);
        }
        spawned.Clear();
        used.Clear();

        Resources.UnloadUnusedAssets();
        System.GC.Collect();
    }
}