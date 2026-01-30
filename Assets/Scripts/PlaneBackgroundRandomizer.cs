using UnityEngine;
using UnityEngine.Perception.Randomization.Randomizers;

[AddComponentMenu("Perception/Randomizers/Plane Background Randomizer")]
public class PlaneBackgroundRandomizer : Randomizer
{
    public GameObject[] backgroundPlanes;   // 6 tane plane
    private Texture2D[] backgroundImages;   // Resources klasöründen otomatik yüklenecek

    protected override void OnScenarioStart()
    {
        // Resources/Backgrounds içindeki tüm görselleri otomatik yükle
        backgroundImages = Resources.LoadAll<Texture2D>("Backgrounds");

        if (backgroundImages.Length == 0)
            Debug.LogError("Resources/Backgrounds klasöründe hiç görsel bulunamadý!");
    }

    protected override void OnIterationStart()
    {
        if (backgroundImages == null || backgroundImages.Length == 0)
            return;

        foreach (var plane in backgroundPlanes)
        {
            var renderer = plane.GetComponent<Renderer>();
            var mat = renderer.material;

            int rand = Random.Range(0, backgroundImages.Length);
            mat.mainTexture = backgroundImages[rand];
        }
    }
}