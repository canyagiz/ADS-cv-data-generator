using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Perception.Randomization.Randomizers;

[AddComponentMenu("Perception/Randomizers/Model Randomizer")]
public class ModelRandomizer : Randomizer
{
    [Tooltip("The list of prefabs (e.g., planes/Models) to spawn randomly.")]
    public GameObject[] ModelPrefabs;

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

    [Tooltip("Gradient palette for the Child object (e.g., the Model). A random color from this gradient will be picked.")]
    public Gradient childColors;

    [Tooltip("Exact name of the child object in the hierarchy (e.g., 'WhiteBaloon') to apply the child color to.")]
    public string childObjectName = "WhiteBaloon";

    [Header("Overlap Tuning")]
    [Tooltip("Separation padding between objects based on actual screen-space bounding boxes. 1.0 = rects must not overlap; >1.0 = requires gap (recommended 1.1-1.3); <1.0 = allows slight overlap.")]
    public float separationPadding = 1.1f;

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

    [Header("Material Randomization - Model")]
    [Tooltip("Enable material property randomization for Models.")]
    public bool enableModelMaterialRandomization = false;

    [Tooltip("Minimum roughness for Model material.")]
    [Range(0f, 1f)]
    public float ModelRoughnessMin = 0.4f;

    [Tooltip("Maximum roughness for Model material.")]
    [Range(0f, 1f)]
    public float ModelRoughnessMax = 0.8f;

    [Tooltip("Minimum metallic value for Model.")]
    [Range(0f, 1f)]
    public float ModelMetallicMin = 0.0f;

    [Tooltip("Maximum metallic value for Model.")]
    [Range(0f, 1f)]
    public float ModelMetallicMax = 0.1f;

    private Camera cam;

    class SpawnedModel
    {
        public Rect screenRect;
    }

    List<SpawnedModel> used = new List<SpawnedModel>();
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

        foreach (var prefab in ModelPrefabs)
        {
            int count = Random.Range(minCount, maxCount + 1);
            int fails = 0;

            for (int i = 0; i < count; i++)
            {
                if (!SpawnModel(prefab))
                {
                    fails++;
                    if (fails > 20) break;
                }
            }
        }
    }

    bool SpawnModel(GameObject prefab)
    {
        float scale = Random.Range(scaleMin, scaleMax);
        int maxAttempts = 100;

        for (int attempts = 0; attempts < maxAttempts; attempts++)
        {
            Vector2 vp = new Vector2(
                Random.Range(0.1f, 0.9f),
                Random.Range(0.1f, 0.9f)
            );
            float dist = Random.Range(spawnDistMin, spawnDistMax);
            Vector3 worldPos = cam.ViewportToWorldPoint(new Vector3(vp.x, vp.y, dist));

            // Pre-calculate final rotation so bounds check uses the real orientation
            float rotX = Random.Range(minRotationX, maxRotationX);
            float rotY = Random.Range(minRotationY, maxRotationY);
            float rotZ = Random.Range(minRotationZ, maxRotationZ);
            Vector3 baseEuler = prefab.transform.eulerAngles;
            Quaternion finalRotation = Quaternion.Euler(baseEuler.x + rotX, baseEuler.y + rotY, baseEuler.z + rotZ);

            // Spawn with final transform so Renderer.bounds reflects real screen footprint
            GameObject Model = GameObject.Instantiate(prefab, worldPos, finalRotation);
            Model.transform.localScale = Vector3.one * scale;

            Rect screenRect = GetScreenRect(Model);

            if (!IsVisuallyOverlapping(screenRect))
            {
                used.Add(new SpawnedModel { screenRect = screenRect });

                Renderer parentRenderer = Model.GetComponent<Renderer>();
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

                Transform childTransform = Model.transform.Find(childObjectName);
                if (childTransform != null)
                {
                    Renderer childRenderer = childTransform.GetComponent<Renderer>();
                    if (childRenderer != null)
                    {
                        Material childMat = childRenderer.material;
                        childMat.color = childColors.Evaluate(Random.value);

                        if (enableModelMaterialRandomization)
                        {
                            float roughness = Random.Range(ModelRoughnessMin, ModelRoughnessMax);
                            childMat.SetFloat("_Smoothness", 1f - roughness);

                            float metallic = Random.Range(ModelMetallicMin, ModelMetallicMax);
                            childMat.SetFloat("_Metallic", metallic);
                        }

                        createdMaterials.Add(childMat);
                    }
                }

                spawned.Add(Model);
                return true;
            }

            // Position causes overlap — discard and retry
            GameObject.Destroy(Model);
        }

        return false;
    }

    Rect GetScreenRect(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return Rect.zero;

        Bounds worldBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            worldBounds.Encapsulate(renderers[i].bounds);

        // Project all 8 corners of the world bounding box to screen space
        Vector3 mn = worldBounds.min;
        Vector3 mx = worldBounds.max;
        Vector3[] corners = new Vector3[]
        {
            new Vector3(mn.x, mn.y, mn.z), new Vector3(mn.x, mn.y, mx.z),
            new Vector3(mn.x, mx.y, mn.z), new Vector3(mn.x, mx.y, mx.z),
            new Vector3(mx.x, mn.y, mn.z), new Vector3(mx.x, mn.y, mx.z),
            new Vector3(mx.x, mx.y, mn.z), new Vector3(mx.x, mx.y, mx.z),
        };

        float minX = float.MaxValue, minY = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue;

        foreach (var corner in corners)
        {
            Vector3 sp = cam.WorldToScreenPoint(corner);
            if (sp.z < 0) continue; // behind camera
            if (sp.x < minX) minX = sp.x;
            if (sp.y < minY) minY = sp.y;
            if (sp.x > maxX) maxX = sp.x;
            if (sp.y > maxY) maxY = sp.y;
        }

        return new Rect(minX, minY, maxX - minX, maxY - minY);
    }

    bool IsVisuallyOverlapping(Rect candidateRect)
    {
        // Expand candidate rect by separationPadding to enforce minimum gap
        float ex = candidateRect.width  * (separationPadding - 1f) * 0.5f;
        float ey = candidateRect.height * (separationPadding - 1f) * 0.5f;
        Rect expanded = new Rect(
            candidateRect.x - ex, candidateRect.y - ey,
            candidateRect.width + 2f * ex, candidateRect.height + 2f * ey
        );

        foreach (var b in used)
        {
            if (expanded.Overlaps(b.screenRect))
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