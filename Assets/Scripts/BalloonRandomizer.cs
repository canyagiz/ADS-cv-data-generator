using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Perception.Randomization.Randomizers;

[AddComponentMenu("Perception/Randomizers/Balloon Randomizer")]
public class BalloonRandomizer : Randomizer
{
    public GameObject[] balloonPrefabs;

    [Header("Spawn Settings")]
    public int minCount = 5;
    public int maxCount = 10;

    // Senin istediğin değerler (Dokunmana gerek yok)
    public float spawnDistMin = 20.0f;
    public float spawnDistMax = 80.0f;

    public float scaleMin = 120f;
    public float scaleMax = 120f;

    [Header("Math Fix")]
    [Tooltip("Scale değeri yüksek ama balon görsel olarak küçükse bu değeri düşür (Örn: 0.01 veya 0.05).")]
    [Range(0.001f, 1f)]
    public float radiusCorrection = 0.05f; // <--- SİHİRLİ AYAR BURASI

    [Tooltip("Balonların birbirine ne kadar yaklaşabileceği (1.0 tam sınır)")]
    public float separationPadding = 0.9f;

    [Header("Rotation")]
    public float minRotationX = 0f;
    public float maxRotationX = 360f;
    public float minRotationY = 0f;
    public float maxRotationY = 360f;

    private Camera cam;

    class SpawnedBalloon
    {
        public Vector2 screenPos;
        public float screenRadius;
    }

    List<SpawnedBalloon> used = new List<SpawnedBalloon>();
    List<GameObject> spawned = new List<GameObject>();

    protected override void OnScenarioStart()
    {
        cam = Camera.main;
    }

    protected override void OnIterationStart()
    {
        foreach (var b in spawned)
        {
            if (b != null) GameObject.Destroy(b);
        }
        spawned.Clear();
        used.Clear();

        if (cam == null) cam = Camera.main;

        foreach (var prefab in balloonPrefabs)
        {
            int count = Random.Range(minCount, maxCount + 1);
            // Sonsuz döngüden kaçınmak için genel bir deneme limiti
            int fails = 0;

            for (int i = 0; i < count; i++)
            {
                if (!SpawnBalloon(prefab))
                {
                    fails++;
                    if (fails > 20) break; // Çok fazla yer bulamazsa bu kareyi pas geç
                }
            }
        }
    }

    bool SpawnBalloon(GameObject prefab)
    {
        float scale = Random.Range(scaleMin, scaleMax);

        // MATEMATİKSEL DÜZELTME:
        // Scale 120 olsa bile, radiusCorrection ile (örn: 0.05) çarparak
        // kodun algıladığı "fiziksel yarıçapı" küçültüyoruz.
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

            // Ekran hesaplamaları
            screenPos = cam.WorldToScreenPoint(worldPos);

            // Balonun kenar noktasını bulup ekran yarıçapını hesapla
            // worldRadius artık düzeltilmiş (küçültülmüş) değer olduğu için devasa çıkmayacak
            Vector3 edgePointWorld = worldPos + cam.transform.right * worldRadius;
            Vector2 edgePointScreen = cam.WorldToScreenPoint(edgePointWorld);

            screenRadius = Vector2.Distance(screenPos, edgePointScreen);

            // Çakışma Kontrolü
            if (!IsVisuallyOverlapping(screenPos, screenRadius))
            {
                positionFound = true;
            }

        } while (!positionFound && attempts < maxAttempts);

        if (positionFound)
        {
            used.Add(new SpawnedBalloon { screenPos = screenPos, screenRadius = screenRadius });

            GameObject balloon = GameObject.Instantiate(prefab, worldPos, Quaternion.identity);
            // GÖRSEL SCALE DEĞİŞMEDİ: Balon hala 120 scale ile oluşuyor
            balloon.transform.localScale = Vector3.one * scale;

            float rotX = Random.Range(minRotationX, maxRotationX);
            float rotY = Random.Range(minRotationY, maxRotationY);
            balloon.transform.rotation = Quaternion.Euler(rotX, rotY, 0f);

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
}