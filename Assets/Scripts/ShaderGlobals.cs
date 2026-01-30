using UnityEngine;

public class ShaderGlobals : MonoBehaviour
{
    [SerializeField] Light sun;

    void Update() 
    {
        if (!sun) return;


        Shader.SetGlobalVector("_MainLightDirection", -sun.transform.forward);

        Shader.SetGlobalColor("_MainLightColor", sun.color);

        Shader.SetGlobalFloat("_MainLightIntensity", sun.intensity);
    }
}

