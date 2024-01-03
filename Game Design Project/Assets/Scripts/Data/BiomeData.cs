using UnityEngine;
using System.Collections;

public enum BiomeTag {
    COLD_OCEAN,
    TEMPERATE_OCEAN,
    TROPICAL_OCEAN,
    BEACH,
    ICE,
    TUNDRA,
    GRASSLAND,
    FOREST,
    DESERT,
    SAVANNA,
    JUNGLE,
    ROCK,
    MOUNTAIN_ICE,
    NOTHING,
    EVERYTHING
}

[CreateAssetMenu()]
public class BiomeData : UpdatableData {

    public Biome[] biomes;

    [System.Serializable]
    public struct Biome {
        public string name;
        public BiomeTag tag;
        public TerrainLayer layer;
        public float heat;
        public float moisture;
        public float height;
    }
}