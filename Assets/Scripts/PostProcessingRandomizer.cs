using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Perception.Randomization.Randomizers;
using PerceptionFloat = UnityEngine.Perception.Randomization.Parameters.FloatParameter;


/// <summary>
/// Comprehensive Post-Processing Randomizer for Sim-to-Real Gap Reduction.
/// Randomizes 11 different HDRP post-processing effects to simulate real camera artifacts.
/// 
/// KURULUM:
/// 1. Bu script'i PerceptionScenario'ya Randomizer olarak ekleyin
/// 2. Volume Profile'a şu component'leri ekleyin (Unity Editor'da):
///    - Film Grain, Motion Blur, Lens Distortion, Vignette
///    - Color Adjustments, Bloom, White Balance
/// 3. Inspector'da parametre range'lerini ayarlayın
/// </summary>
[AddComponentMenu("Perception/Randomizers/Post Processing Randomizer")]
public class PostProcessingRandomizer : Randomizer
{
    [Header("Volume Reference")]
    [Tooltip("The Global Volume containing the post-processing effects. Auto-detected if not assigned.")]
    public Volume globalVolume;

    [Header("Film Grain (ISO Noise Simulation)")]
    [Tooltip("Grain intensity: 0 = no grain, 0.4 = heavy grain")]
    public PerceptionFloat grainIntensity;
    
    [Tooltip("Grain luminance response: higher = more visible in bright areas")]
    public PerceptionFloat grainResponse;

    [Header("Motion Blur")]
    [Tooltip("Motion blur intensity: 0 = disabled, 0.3 = strong blur")]
    public PerceptionFloat motionBlurIntensity;

    [Header("Chromatic Aberration (Lens Quality)")]
    [Tooltip("Color fringing at edges: 0 = none, 0.3 = strong")]
    public PerceptionFloat chromaticIntensity;

    [Header("Lens Distortion")]
    [Tooltip("Barrel/pincushion distortion: negative = barrel, positive = pincushion")]
    public PerceptionFloat lensDistortionIntensity;

    [Header("Vignette (Edge Darkening)")]
    [Tooltip("Corner darkening: 0 = none, 0.35 = noticeable")]
    public PerceptionFloat vignetteIntensity;

    [Header("Exposure")]
    [Tooltip("Uncheck to disable exposure randomization entirely and let the Volume's own Exposure value stay fixed.")]
    public bool randomizeExposure = true;

    [Tooltip("Fixed exposure value (EV): typical outdoor range 10-15")]
    public PerceptionFloat exposureValue;

    [Header("White Balance (Color Temperature)")]
    [Tooltip("Temperature shift: negative = cool/blue, positive = warm/yellow")]
    public PerceptionFloat whiteBalanceTemperature;

    [Header("Color Adjustments")]
    [Tooltip("Saturation adjustment: negative = desaturated, positive = vibrant")]
    public PerceptionFloat saturation;
    
    [Tooltip("Contrast adjustment: negative = flat, positive = punchy")]
    public PerceptionFloat contrast;

    [Header("Bloom (Glow Effect)")]
    [Tooltip("Bloom intensity: 0 = disabled, 0.3 = visible glow")]
    public PerceptionFloat bloomIntensity;

    // Cached HDRP effect references
    private FilmGrain m_FilmGrain;
    private MotionBlur m_MotionBlur;
    private ChromaticAberration m_ChromaticAberration;
    private LensDistortion m_LensDistortion;
    private Vignette m_Vignette;
    private Exposure m_Exposure;
    private WhiteBalance m_WhiteBalance;
    private ColorAdjustments m_ColorAdjustments;
    private Bloom m_Bloom;

    /// <summary>
    /// Sets default parameter ranges when component is first added.
    /// To apply: Remove and re-add the randomizer, or right-click → Reset in Inspector.
    /// </summary>
    private void Reset()
    {
        grainIntensity = new PerceptionFloat { value = new UnityEngine.Perception.Randomization.Samplers.UniformSampler(0.05f, 0.35f) };
        grainResponse = new PerceptionFloat { value = new UnityEngine.Perception.Randomization.Samplers.UniformSampler(0.6f, 0.9f) };
        motionBlurIntensity = new PerceptionFloat { value = new UnityEngine.Perception.Randomization.Samplers.UniformSampler(0f, 0.2f) };
        chromaticIntensity = new PerceptionFloat { value = new UnityEngine.Perception.Randomization.Samplers.UniformSampler(0f, 0.2f) };
        lensDistortionIntensity = new PerceptionFloat { value = new UnityEngine.Perception.Randomization.Samplers.UniformSampler(-0.12f, 0.12f) };
        vignetteIntensity = new PerceptionFloat { value = new UnityEngine.Perception.Randomization.Samplers.UniformSampler(0f, 0.3f) };
        exposureValue = new PerceptionFloat { value = new UnityEngine.Perception.Randomization.Samplers.UniformSampler(13f, 17f) };
        whiteBalanceTemperature = new PerceptionFloat { value = new UnityEngine.Perception.Randomization.Samplers.UniformSampler(-18f, 18f) };
        saturation = new PerceptionFloat { value = new UnityEngine.Perception.Randomization.Samplers.UniformSampler(-12f, 12f) };
        contrast = new PerceptionFloat { value = new UnityEngine.Perception.Randomization.Samplers.UniformSampler(-10f, 10f) };
        bloomIntensity = new PerceptionFloat { value = new UnityEngine.Perception.Randomization.Samplers.UniformSampler(0f, 0.2f) };
    }

    /// <summary>
    /// Called when the Randomizer is initialized.
    /// Finds and caches all post-processing effect references.
    /// </summary>
    protected override void OnAwake()
    {
        base.OnAwake();

        // Auto-detect Volume if not assigned
        if (globalVolume == null)
        {
            globalVolume = GameObject.FindObjectOfType<Volume>();
        }

        if (globalVolume == null || globalVolume.profile == null)
        {
            Debug.LogError("[PostProcessingRandomizer] ERROR: No Volume found! Please assign a Global Volume.");
            return;
        }

        VolumeProfile profile = globalVolume.profile;

        // Try to get each effect from the Volume Profile
        // If an effect doesn't exist, the variable will be null and we'll skip it during randomization
        profile.TryGet(out m_FilmGrain);
        profile.TryGet(out m_MotionBlur);
        profile.TryGet(out m_ChromaticAberration);
        profile.TryGet(out m_LensDistortion);
        profile.TryGet(out m_Vignette);
        profile.TryGet(out m_Exposure);
        profile.TryGet(out m_WhiteBalance);
        profile.TryGet(out m_ColorAdjustments);
        profile.TryGet(out m_Bloom);

        // Log which effects are available
        LogEffectStatus();
    }

    /// <summary>
    /// Logs which effects are available in the Volume Profile for debugging.
    /// </summary>
    private void LogEffectStatus()
    {
        string status = "[PostProcessingRandomizer] Effect Status:\n";
        status += $"  Film Grain: {(m_FilmGrain != null ? "✓" : "✗ (Add to Volume Profile)")}\n";
        status += $"  Motion Blur: {(m_MotionBlur != null ? "✓" : "✗ (Add to Volume Profile)")}\n";
        status += $"  Chromatic Aberration: {(m_ChromaticAberration != null ? "✓" : "✗ (Add to Volume Profile)")}\n";
        status += $"  Lens Distortion: {(m_LensDistortion != null ? "✓" : "✗ (Add to Volume Profile)")}\n";
        status += $"  Vignette: {(m_Vignette != null ? "✓" : "✗ (Add to Volume Profile)")}\n";
        status += $"  Exposure: {(m_Exposure != null ? "✓" : "✗ (Add to Volume Profile)")}\n";
        status += $"  White Balance: {(m_WhiteBalance != null ? "✓" : "✗ (Add to Volume Profile)")}\n";
        status += $"  Color Adjustments: {(m_ColorAdjustments != null ? "✓" : "✗ (Add to Volume Profile)")}\n";
        status += $"  Bloom: {(m_Bloom != null ? "✓" : "✗ (Add to Volume Profile)")}";
        
        Debug.Log(status);
    }

    /// <summary>
    /// Called at the start of every simulation iteration.
    /// Applies random values to all available post-processing effects.
    /// </summary>
    protected override void OnIterationStart()
    {
        // Film Grain
        if (m_FilmGrain != null)
        {
            m_FilmGrain.active = true;
            m_FilmGrain.intensity.overrideState = true;
            m_FilmGrain.intensity.value = grainIntensity.Sample();
            
            m_FilmGrain.response.overrideState = true;
            m_FilmGrain.response.value = grainResponse.Sample();
        }

        // Motion Blur
        if (m_MotionBlur != null)
        {
            float blurValue = motionBlurIntensity.Sample();
            m_MotionBlur.active = blurValue > 0.01f;
            m_MotionBlur.intensity.overrideState = true;
            m_MotionBlur.intensity.value = blurValue;
        }

        // Chromatic Aberration
        if (m_ChromaticAberration != null)
        {
            float chromaValue = chromaticIntensity.Sample();
            m_ChromaticAberration.active = chromaValue > 0.01f;
            m_ChromaticAberration.intensity.overrideState = true;
            m_ChromaticAberration.intensity.value = chromaValue;
        }

        // Lens Distortion
        if (m_LensDistortion != null)
        {
            float distortValue = lensDistortionIntensity.Sample();
            m_LensDistortion.active = Mathf.Abs(distortValue) > 0.01f;
            m_LensDistortion.intensity.overrideState = true;
            m_LensDistortion.intensity.value = distortValue;
        }

        // Vignette
        if (m_Vignette != null)
        {
            float vigValue = vignetteIntensity.Sample();
            m_Vignette.active = vigValue > 0.01f;
            m_Vignette.intensity.overrideState = true;
            m_Vignette.intensity.value = vigValue;
        }

        // Exposure — only runs if randomizeExposure is enabled in Inspector.
        // Uncheck 'Randomize Exposure' to keep the Volume's fixed value untouched.
        if (randomizeExposure && m_Exposure != null)
        {
            m_Exposure.active = true;
            m_Exposure.mode.overrideState = true;
            m_Exposure.mode.value = ExposureMode.Fixed;
            m_Exposure.fixedExposure.overrideState = true;
            m_Exposure.fixedExposure.value = exposureValue.Sample();
        }

        // White Balance
        if (m_WhiteBalance != null)
        {
            m_WhiteBalance.active = true;
            m_WhiteBalance.temperature.overrideState = true;
            m_WhiteBalance.temperature.value = whiteBalanceTemperature.Sample();
        }

        // Color Adjustments
        if (m_ColorAdjustments != null)
        {
            m_ColorAdjustments.active = true;
            m_ColorAdjustments.saturation.overrideState = true;
            m_ColorAdjustments.saturation.value = saturation.Sample();
            
            m_ColorAdjustments.contrast.overrideState = true;
            m_ColorAdjustments.contrast.value = contrast.Sample();
        }

        // Bloom
        if (m_Bloom != null)
        {
            float bloomValue = bloomIntensity.Sample();
            m_Bloom.active = bloomValue > 0.01f;
            m_Bloom.intensity.overrideState = true;
            m_Bloom.intensity.value = bloomValue;
        }
    }
}
