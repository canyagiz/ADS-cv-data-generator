using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Perception.Randomization.Randomizers;

#if UNITY_EDITOR
using UnityEditor;
#endif

[Serializable]
[AddRandomizerMenu("Perception/HDRI Sky Randomizer (Smart Load)")]
public class HDRISkyRandomizer : Randomizer
{
    // DÜZELTME: Karmaþýk Parameter yapýsý yerine düz string kullanýyoruz.
    // Artýk hata vermeyecek.
    [Tooltip("HDRI dosyalarýnýn olduðu klasörün yolu.")]
    public string hdriFolderPath = "Assets/Resources/Environments";

    public Volume globalVolume;

    private HDRISky m_HdriSky;
    private List<string> m_HdriPaths;
    private Cubemap currentCubemap;

    protected override void OnAwake()
    {
        base.OnAwake();

        // Volume ve HDRI Sky Bileþenini Bul
        if (globalVolume == null)
        {
            globalVolume = GameObject.FindObjectOfType<Volume>();
        }

        if (globalVolume != null && globalVolume.profile.TryGet(out HDRISky sky))
        {
            m_HdriSky = sky;
        }
        else
        {
            Debug.LogError("HATA: Sahnedeki Volume içinde 'HDRI Sky' bulunamadý!");
        }

        LoadFilePaths();
    }

    private void LoadFilePaths()
    {
        m_HdriPaths = new List<string>();

#if UNITY_EDITOR
        // Yolun sonundaki eðik çizgiyi temizle
        string folder = hdriFolderPath; // Artýk .value dememize gerek yok
        if (folder.EndsWith("/")) folder = folder.Substring(0, folder.Length - 1);

        // Klasördeki tüm Cubemap dosyalarýný bul
        string[] guids = AssetDatabase.FindAssets("t:Cubemap", new[] { folder });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            m_HdriPaths.Add(path);
        }
        
        Debug.Log($"[HDRI Randomizer] {m_HdriPaths.Count} adet HDRI bulundu.");
#endif
    }

    protected override void OnIterationStart()
    {
        if (m_HdriSky == null || m_HdriPaths == null || m_HdriPaths.Count == 0) return;

#if UNITY_EDITOR
        string randomPath = m_HdriPaths[UnityEngine.Random.Range(0, m_HdriPaths.Count)];

        if (currentCubemap != null)
        {
            Resources.UnloadAsset(currentCubemap);
            currentCubemap = null;
        }

        currentCubemap = AssetDatabase.LoadAssetAtPath<Cubemap>(randomPath);

        if (currentCubemap != null)
        {
            m_HdriSky.hdriSky.value = currentCubemap;
            m_HdriSky.hdriSky.overrideState = true;
        }
#endif
    }
}