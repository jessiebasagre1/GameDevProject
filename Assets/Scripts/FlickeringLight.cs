using System.Collections;
using UnityEngine;

// Put this on a light (or its parent). Every "interval" seconds the light blinks a few times, then stays on again.
// The Light must be set to Realtime (or Mixed). Baked lights can't change while the game runs.
public class FlickeringLight : MonoBehaviour
{
    [Header("What blinks")]
    [Tooltip("Leave empty to use every Light on this object and its children")]
    public Light[] lights;
    [Tooltip("Optional: the bulb mesh that glows (its material needs Emission switched on)")]
    public Renderer emissiveRenderer;
    public int emissiveMaterialIndex = 0;
    [Tooltip("URP / Built-in: _EmissionColor    HDRP: _EmissiveColor")]
    public string emissionProperty = "_EmissionColor";

    [Header("Timing")]
    [Tooltip("Seconds the light stays on between blinks")]
    public float interval = 5f;
    [Tooltip("Random +/- seconds added to the interval (0 = exactly every interval)")]
    public float randomness = 0f;
    [Tooltip("Tick this when you have many lights, so they don't all blink at the same moment")]
    public bool randomStartOffset = false;

    [Header("Blink")]
    [Tooltip("How many quick off/on flickers happen each time")]
    public int blinks = 3;
    [Tooltip("Seconds for each off or on")]
    public float blinkSpeed = 0.08f;
    [Range(0f, 1f), Tooltip("0 = completely dark during a blink, 0.3 = dims to 30%")]
    public float offBrightness = 0f;

    float[] baseIntensity;
    Color baseEmission;
    bool hasEmission;
    MaterialPropertyBlock block;

    void Awake()
    {
        if (lights == null || lights.Length == 0) lights = GetComponentsInChildren<Light>();

        baseIntensity = new float[lights.Length];
        for (int i = 0; i < lights.Length; i++) baseIntensity[i] = lights[i].intensity;

        if (emissiveRenderer != null)
        {
            Material[] mats = emissiveRenderer.sharedMaterials;
            if (emissiveMaterialIndex < mats.Length && mats[emissiveMaterialIndex].HasProperty(emissionProperty))
            {
                baseEmission = mats[emissiveMaterialIndex].GetColor(emissionProperty);
                block = new MaterialPropertyBlock();
                hasEmission = true;
            }
            else
            {
                Debug.LogWarning("FlickeringLight: the material has no '" + emissionProperty + "' property, so the bulb glow won't blink.", this);
            }
        }
    }

    void OnEnable()
    {
        StartCoroutine(BlinkLoop());
    }

    void OnDisable()
    {
        StopAllCoroutines();
        if (baseIntensity != null) SetBrightness(1f);   // never leave the light dimmed
    }

    IEnumerator BlinkLoop()
    {
        SetBrightness(1f);

        if (randomStartOffset)
            yield return new WaitForSeconds(Random.Range(0f, interval));

        while (true)
        {
            SetBrightness(1f);
            yield return new WaitForSeconds(Mathf.Max(0.1f, interval + Random.Range(-randomness, randomness)));

            for (int i = 0; i < blinks; i++)
            {
                SetBrightness(offBrightness);
                yield return new WaitForSeconds(blinkSpeed);
                SetBrightness(1f);
                yield return new WaitForSeconds(blinkSpeed);
            }
        }
    }

    void SetBrightness(float b)
    {
        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i] != null) lights[i].intensity = baseIntensity[i] * b;
        }

        if (hasEmission && emissiveRenderer != null)
        {
            block.SetColor(emissionProperty, baseEmission * b);
            emissiveRenderer.SetPropertyBlock(block, emissiveMaterialIndex);
        }
    }
}