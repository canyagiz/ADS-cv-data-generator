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
    [Header("Settings")]
    [Tooltip("The project path to the folder containing your HDRI Cubemap assets (e.g., 'Assets/Resources/Environments').")]
    public string hdriFolderPath = "Assets/Resources/Environments";

    [Tooltip("The Global Volume in the scene that controls the sky. If left empty, the script attempts to find one automatically.")]
    public Volume globalVolume;

    // Internal references
    private HDRISky m_HdriSky;
    private List<string> m_HdriPaths;
    private Cubemap currentCubemap;

    /// <summary>
    /// Called when the Randomizer is initialized. 
    /// Finds the Volume, the HDRI Sky component, and indexes all available files.
    /// </summary>
    protected override void OnAwake()
    {
        base.OnAwake();

        // 1. Locate the Volume and the HDRI Sky Component
        if (globalVolume == null)
        {
            globalVolume = GameObject.FindObjectOfType<Volume>();
        }

        // Try to get the HDRISky override from the Volume Profile
        if (globalVolume != null && globalVolume.profile.TryGet(out HDRISky sky))
        {
            m_HdriSky = sky;
        }
        else
        {
            Debug.LogError("[HDRISkyRandomizer] ERROR: Could not find an 'HDRI Sky' override in the scene Volume!");
        }

        // 2. Index the files in the specified folder
        LoadFilePaths();
    }

    /// <summary>
    /// Scans the target folder and stores the paths of all found Cubemaps.
    /// This runs only in the Editor.
    /// </summary>
    private void LoadFilePaths()
    {
        m_HdriPaths = new List<string>();

#if UNITY_EDITOR
        // Ensure the path string doesn't end with a slash to avoid path errors
        string folder = hdriFolderPath;
        if (folder.EndsWith("/")) folder = folder.Substring(0, folder.Length - 1);

        // Find all assets of type 'Cubemap' inside the target folder
        string[] guids = AssetDatabase.FindAssets("t:Cubemap", new[] { folder });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            m_HdriPaths.Add(path);
        }
        
        Debug.Log($"[HDRISkyRandomizer] Found {m_HdriPaths.Count} HDRI Cubemaps in folder: {folder}");
#endif
    }

    /// <summary>
    /// Called at the start of every simulation iteration (frame).
    /// Picks a random HDRI and assigns it to the sky.
    /// </summary>
    protected override void OnIterationStart()
    {
        // Safety checks
        if (m_HdriSky == null || m_HdriPaths == null || m_HdriPaths.Count == 0) return;

#if UNITY_EDITOR
        // 1. Pick a random file path
        string randomPath = m_HdriPaths[UnityEngine.Random.Range(0, m_HdriPaths.Count)];

        // 2. Memory Cleanup: Unload the previous texture to prevent memory leaks during long simulations
        if (currentCubemap != null)
        {
            Resources.UnloadAsset(currentCubemap);
            currentCubemap = null;
        }

        // 3. Load the new Cubemap
        currentCubemap = AssetDatabase.LoadAssetAtPath<Cubemap>(randomPath);

        // 4. Apply to HDRP Volume
        if (currentCubemap != null)
        {
            m_HdriSky.hdriSky.value = currentCubemap;
            m_HdriSky.hdriSky.overrideState = true; // Ensure the override is active
        }
#endif
    }
}