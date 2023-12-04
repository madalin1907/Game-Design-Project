using UnityEngine;
using System.Collections;

[CreateAssetMenu()]
public class NoiseData : UpdatableData {

    public string _name;
    public Noise.NormalizeMode normalizeMode;

    public float noiseScale;

    public int octaves;
    public float persistance;
    public float lacunarity;
    public float amplitude;
    public float frequency;
    public float minLevel;


    public int seed;
    public Vector2 offset;

#if UNITY_EDITOR

    protected override void OnValidate() {
        if (lacunarity < 1) {
            lacunarity = 1;
        }
        if (octaves < 0) {
            octaves = 0;
        }

        base.OnValidate();
    }
#endif

}