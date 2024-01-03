using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public static class MapGenerateUtils {

    const float epsilon = 0.001f;

    private static Dictionary<BiomeTag, int> biomeTagIndex = new Dictionary<BiomeTag, int>();
    private static Dictionary<int, BiomeTag> biomeIndexTag = new Dictionary<int, BiomeTag>();

    public static float[,,] GenerateSplatsMapForHeight(
        TerrainData terrainData, 
        float[,] heightMap, 
        int terrainBaseTextureResolution, 
        int terrainSmoothingEdge                        
    ) {

        int layerCount = terrainData.terrainLayers.Length;
        float[,,] splatmapData = new float[terrainBaseTextureResolution, terrainBaseTextureResolution, layerCount];

        float stepX = heightMap.GetLength(0) / (float)terrainBaseTextureResolution;
        float stepY = heightMap.GetLength(1) / (float)terrainBaseTextureResolution;

        for (int y = 0; y < terrainBaseTextureResolution; y++) {
            for (int x = 0; x < terrainBaseTextureResolution; x++) {
                int sourceY = Mathf.RoundToInt(y * stepY);
                int sourceX = Mathf.RoundToInt(x * stepX);

                float height = heightMap[sourceY, sourceX];

                splatmapData[y, x, 0] = 1 - height;
                splatmapData[y, x, 1] = height;
            }
        }

        int missing = 33;
        for (int y = 1; y < terrainBaseTextureResolution - 1; y++) {
            for (int x = 1; x < terrainBaseTextureResolution - 1; x++) {
                if (Mathf.Abs(x - missing) >= terrainSmoothingEdge && Mathf.Abs(y - missing) >= terrainSmoothingEdge) 
                    continue;

                // smooth
                float smoothHeight = 0;
                int numDivisions = 0;
                for (int i = -1; i <= 1; i++) {
                    if (y + i < 0 || y + i >= terrainBaseTextureResolution)
                        continue;

                    for (int j = -1; j <= 1; j++) {
                        if (x + j < 0 || x + j >= terrainBaseTextureResolution)
                            continue;

                        smoothHeight += splatmapData[y + i, x + j, 1];
                        ++numDivisions;
                    }
                }
                smoothHeight /= numDivisions;

                splatmapData[y, x, 0] = 1 - smoothHeight;
                splatmapData[y, x, 1] = smoothHeight;
            }
        }

        return splatmapData;
    }

    public static float[,,] GenerateSplatsMapForControl(
        TerrainData terrainData,
        int[,] controlMap,
        int terrainBaseTextureResolution
    ) {

        int layerCount = terrainData.terrainLayers.Length;
        float[,,] splatmapData = new float[terrainBaseTextureResolution, terrainBaseTextureResolution, layerCount];

        float stepX = controlMap.GetLength(0) / (float)terrainBaseTextureResolution;
        float stepY = controlMap.GetLength(1) / (float)terrainBaseTextureResolution;

        for (int y = 0; y < terrainBaseTextureResolution; y++) {
            for (int x = 0; x < terrainBaseTextureResolution; x++) {
                int sourceY = Mathf.RoundToInt(y * stepY);
                int sourceX = Mathf.RoundToInt(x * stepX);

                int control = controlMap[sourceY, sourceX];

                splatmapData[y, x, control] = 1;
            }
        }

        return splatmapData;
        /*
        for (int y = 0; y < terrainBaseTextureResolution; y++) {
            for (int x = 0; x < terrainBaseTextureResolution; x++) {
                int control = 0;
                for (int k = 0; k < layerCount; k++) {
                    if (splatmapData[y, x, k] == 1) {
                        control = k;
                        break;
                    }
                }

                for (int i = -1; i <= 1; i++) {
                    if (y + i < 0 || y + i >= terrainBaseTextureResolution)
                        continue;

                    for (int j = -1; j <= 1; j++) {
                        if (x + j < 0 || x + j >= terrainBaseTextureResolution)
                            continue;

                        for (int k = 0; k < layerCount; k++) {
                            if (k == control)
                                continue;
                            if (splatmapData[y + i, x + j, k] > 0) {
                                splatmapData[y, x, k] = splatmapData[y + i, x + j, k] / 2f;
                                if (splatmapData[y, x, k] < 0.1f)
                                    splatmapData[y, x, k] = 0;
                            }
                        }
                        
                    }
                }
                
            }
        }

        return splatmapData;
        */

    }

    public static float[,] CreateHeatNoise(
        int mapChunkSize, 
        Vector2 centre, 
        NoiseData noiseData, 
        float[,] heightMap
    ) {

        float[,] heatMap = Noise.GenerateNoiseMap(mapChunkSize, mapChunkSize, centre, noiseData);

        for (int y = 0; y < mapChunkSize; y++) {
            for (int x = 0; x < mapChunkSize; x++) {
                heatMap[x, y] = Mathf.Clamp01(heatMap[x, y]);
                heatMap[x, y] = Mathf.Clamp(heatMap[x, y] - 1f / 4 * (heightMap[x, y] * heightMap[x, y]) * heatMap[x, y], 0, 0.999f);
            }
        }

        return heatMap;
    }

    public static float[,] CreateMoistureNoise(
        int mapChunkSize, 
        Vector2 centre,
        NoiseData noiseData, 
        float[,] heightMap, 
        float[,] heatMap, 
        AnimationCurve _moistureHeightCurve
    ) {

        AnimationCurve moistureHeightCurve = new AnimationCurve(_moistureHeightCurve.keys);
        float[,] moistureNoise = Noise.GenerateNoiseMap(mapChunkSize, mapChunkSize, centre, noiseData);

        for (int y = 0; y < mapChunkSize; y++) {
            for (int x = 0; x < mapChunkSize; x++) {
                moistureNoise[x, y] += (2f / 3 * (heatMap[x, y] * heatMap[x, y]) - 0.3f) * moistureNoise[x, y];
                moistureNoise[x, y] = Mathf.Clamp01(moistureNoise[x, y] + moistureHeightCurve.Evaluate(heightMap[x, y]));
            }
        }

        for (int y = 0; y < mapChunkSize; y++) {
            for (int x = 0; x < mapChunkSize; x++) {
                float roundTo = ((int)(moistureNoise[x, y] * 10)) / 10f;
                moistureNoise[x, y] = roundTo;
            }
        }

        return moistureNoise;
    }

    static bool CheckInRange(int k, BiomeData biomeData, float heatValue, float moistureValue, float heightValue) {
        bool returnValue = true;

        returnValue &= heatValue <= biomeData.biomes[k].heat + epsilon;
        returnValue &= moistureValue <= biomeData.biomes[k].moisture + epsilon;
        returnValue &= heightValue <= biomeData.biomes[k].height + epsilon;

        return returnValue;
    }

    public static int[,] CreateBiomesNoise(
        int mapChunkSize, float[,] heightMap, float[,] heatMap, float[,] moistureMap, BiomeData biomeData
    ) {

        int[,] biomesNoise = new int[mapChunkSize, mapChunkSize];

        for (int y = 0; y < mapChunkSize; y++) {
            for (int x = 0; x < mapChunkSize; x++) {
                int lengthBiomes = biomeData.biomes.Length - 1;
                int typeBiome = lengthBiomes;

                for (int k = 0; k <= lengthBiomes; k++) {
                    if (CheckInRange(k, biomeData, heatMap[x, y], moistureMap[x, y], heightMap[x, y])) {
                        typeBiome = k;
                        break;
                    }
                }

                biomesNoise[x, y] = typeBiome;
            }
        }

        return biomesNoise;
    }

    public static int intPseudoRandom2(int x, int y) {
        long v = (long)x;
        long u = (long)y;
        long res;
        long m = 2147483647;

        if (v < 0) v += m;
        if (u < 0) u += m;

        v = 36969 * (v & 65535) + (v >> 16);
        u = 18000 * (u & 65535) + (u >> 16);
        res = ((v << 16) + (u & 65535)) % m;

        return (int)res;
    }

    public static void InitializeBiomesTagIndex(BiomeData biomeData) {
        biomeTagIndex.Clear();
        for (int i = 0; i < biomeData.biomes.Length; i++) {
            biomeTagIndex.Add(biomeData.biomes[i].tag, i);
        }

        biomeIndexTag.Clear();
        for (int i = 0; i < biomeData.biomes.Length; i++) {
            biomeIndexTag.Add(i, biomeData.biomes[i].tag);
        }
    }

    public static int GetBiomeIndex(BiomeTag tag) {
        return biomeTagIndex[tag];
    }

    public static BiomeTag GetBiomeTag(int index) {
        return biomeIndexTag[index];
    }
}
