using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TowerGlowEffect : MonoBehaviour
{
    [Header("Glow Settings")]
    [SerializeField] private Color glowColor = new Color(1f, 0.8f, 0f, 1f);
    [SerializeField] private float minIntensity = 0.5f;
    [SerializeField] private float maxIntensity = 2f;
    [SerializeField] private float pulseSpeed = 2f;
    
    [Header("Material Settings")]
    [SerializeField] private bool useSharedMaterial = false;
    
    [Header("Particle Effect")]
    [SerializeField] private GameObject glowParticlesPrefab;
    [SerializeField] private Vector3 particleOffset = new Vector3(0, 0.5f, 0);
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = false;
    
    private Renderer[] towerRenderers;
    private Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();
    private Dictionary<Renderer, Material[]> glowMaterials = new Dictionary<Renderer, Material[]>();
    private Coroutine glowCoroutine;
    private GameObject particleInstance;
    private bool isGlowing = false;
    
    private static readonly string EMISSION_KEYWORD = "_EMISSION";
    private static readonly int EMISSION_COLOR_ID = Shader.PropertyToID("_EmissionColor");
    
    void Awake()
    {
        towerRenderers = GetComponentsInChildren<Renderer>();
        StoreOriginalMaterials();
    }
    
    private void StoreOriginalMaterials()
    {
        foreach (Renderer renderer in towerRenderers)
        {
            if (renderer != null)
            {
                originalMaterials[renderer] = renderer.materials;
                
                Material[] glowMats = new Material[renderer.materials.Length];
                for (int i = 0; i < renderer.materials.Length; i++)
                {
                    if (useSharedMaterial)
                    {
                        glowMats[i] = renderer.materials[i];
                    }
                    else
                    {
                        glowMats[i] = new Material(renderer.materials[i]);
                    }
                }
                glowMaterials[renderer] = glowMats;
            }
        }
    }
    
    public void StartGlowing()
    {
        if (isGlowing) return;
        
        isGlowing = true;
        
        if (debugMode)
        {
            Debug.Log($"TowerGlowEffect: Starting glow on {gameObject.name}");
        }
        
        ApplyGlowMaterials();
        
        if (glowCoroutine != null)
        {
            StopCoroutine(glowCoroutine);
        }
        glowCoroutine = StartCoroutine(PulseGlow());
        
        CreateParticleEffect();
    }
    
    public void StopGlowing()
    {
        if (!isGlowing) return;
        
        isGlowing = false;
        
        if (debugMode)
        {
            Debug.Log($"TowerGlowEffect: Stopping glow on {gameObject.name}");
        }
        
        if (glowCoroutine != null)
        {
            StopCoroutine(glowCoroutine);
            glowCoroutine = null;
        }
        
        RestoreOriginalMaterials();
        DestroyParticleEffect();
    }
    
    private void ApplyGlowMaterials()
    {
        foreach (Renderer renderer in towerRenderers)
        {
            if (renderer != null && glowMaterials.ContainsKey(renderer))
            {
                renderer.materials = glowMaterials[renderer];
                
                foreach (Material mat in renderer.materials)
                {
                    mat.EnableKeyword(EMISSION_KEYWORD);
                    mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                }
            }
        }
    }
    
    private void RestoreOriginalMaterials()
    {
        foreach (Renderer renderer in towerRenderers)
        {
            if (renderer != null && originalMaterials.ContainsKey(renderer))
            {
                if (!useSharedMaterial)
                {
                    foreach (Material mat in renderer.materials)
                    {
                        mat.DisableKeyword(EMISSION_KEYWORD);
                        mat.SetColor(EMISSION_COLOR_ID, Color.black);
                    }
                }
                
                renderer.materials = originalMaterials[renderer];
            }
        }
    }
    
    private IEnumerator PulseGlow()
    {
        float time = 0f;
        
        while (isGlowing)
        {
            time += Time.deltaTime * pulseSpeed;
            float intensity = Mathf.Lerp(minIntensity, maxIntensity, (Mathf.Sin(time) + 1f) * 0.5f);
            
            Color emissionColor = glowColor * intensity;
            
            foreach (Renderer renderer in towerRenderers)
            {
                if (renderer != null && glowMaterials.ContainsKey(renderer))
                {
                    foreach (Material mat in renderer.materials)
                    {
                        mat.SetColor(EMISSION_COLOR_ID, emissionColor);
                    }
                }
            }
            
            yield return null;
        }
    }
    
    private void CreateParticleEffect()
    {
        if (glowParticlesPrefab != null && particleInstance == null)
        {
            particleInstance = Instantiate(glowParticlesPrefab, transform.position + particleOffset, Quaternion.identity, transform);
            
            ParticleSystem particles = particleInstance.GetComponent<ParticleSystem>();
            if (particles != null)
            {
                var main = particles.main;
                main.startColor = glowColor;
            }
        }
    }
    
    private void DestroyParticleEffect()
    {
        if (particleInstance != null)
        {
            ParticleSystem particles = particleInstance.GetComponent<ParticleSystem>();
            if (particles != null)
            {
                particles.Stop();
                Destroy(particleInstance, particles.main.startLifetime.constantMax);
            }
            else
            {
                Destroy(particleInstance);
            }
            particleInstance = null;
        }
    }
    
    public void SetGlowColor(Color color)
    {
        glowColor = color;
        
        if (isGlowing && particleInstance != null)
        {
            ParticleSystem particles = particleInstance.GetComponent<ParticleSystem>();
            if (particles != null)
            {
                var main = particles.main;
                main.startColor = glowColor;
            }
        }
    }
    
    public void SetGlowIntensity(float min, float max)
    {
        minIntensity = Mathf.Max(0, min);
        maxIntensity = Mathf.Max(minIntensity, max);
    }
    
    public void SetPulseSpeed(float speed)
    {
        pulseSpeed = Mathf.Max(0.1f, speed);
    }
    
    public bool IsGlowing => isGlowing;
    
    void OnDestroy()
    {
        StopGlowing();
        
        if (!useSharedMaterial)
        {
            foreach (var kvp in glowMaterials)
            {
                foreach (Material mat in kvp.Value)
                {
                    if (mat != null)
                    {
                        Destroy(mat);
                    }
                }
            }
        }
    }
    
    [ContextMenu("Test Start Glow")]
    private void TestStartGlow()
    {
        StartGlowing();
    }
    
    [ContextMenu("Test Stop Glow")]
    private void TestStopGlow()
    {
        StopGlowing();
    }
}