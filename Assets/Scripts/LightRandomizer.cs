using UnityEngine;
using UnityEngine.Perception.Randomization.Randomizers;
using UnityEngine.Perception.Randomization.Parameters;

[AddComponentMenu("Perception/Randomizers/Light Randomizer")]
public class LightRandomizer : Randomizer
{
    public GameObject lightObj;

    public FloatParameter intensity;
    public FloatParameter rotX;
    public FloatParameter rotY;
    public FloatParameter rotZ;

    Light sceneLight;

    protected override void OnScenarioStart()
    {
        sceneLight = lightObj.GetComponent<Light>();
    }

    protected override void OnIterationStart()
    {
        sceneLight.intensity = intensity.Sample();

        lightObj.transform.rotation = Quaternion.Euler(
            rotX.Sample(),
            rotY.Sample(),
            rotZ.Sample()
        );
    }
}
