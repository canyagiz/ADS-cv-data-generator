using UnityEngine;
using UnityEngine.Perception.Randomization.Randomizers;
using UnityEngine.Perception.Randomization.Parameters;

[AddComponentMenu("Perception/Randomizers/Camera Angle Randomizer")]
public class CameraAngleRandomizer : Randomizer
{
    public FloatParameter rotX; // yukarı-aşağı bakış
    public FloatParameter rotY; // sağ-sol dönüş
    public FloatParameter rotZ; // hafif tilt (çapraz)

    Camera cam;

    protected override void OnScenarioStart()
    {
        cam = Camera.main;
    }

    protected override void OnIterationStart()
    {
        // Kameranın pozisyonunu ELLEME → sabit kalacak
        // Sadece açı randomize ediliyor
        cam.transform.rotation = Quaternion.Euler(
            rotX.Sample(),
            rotY.Sample(),
            rotZ.Sample()
        );
    }
}
