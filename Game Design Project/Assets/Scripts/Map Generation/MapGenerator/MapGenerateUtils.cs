using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public static class MapGenerateUtils {

    public static float[,,] GenerateSplatsMapForHeight(
        TerrainData terrainData, 
        float[,] heightMap, 
        int terrainBaseTextureResolution, 
        int terrainSmoothingEdge                        
    ) {

        int layerCount = terrainData.terrainLayers.Length;
        float[,,] splatmapData = new float[terrainBaseTextureResolution, terrainBaseTextureResolution, layerCount];

        /*
        for (int y = 0; y < terrainBaseTextureResolution; y++) {
            for (int k = 0; k < 2; k++) {
                splatmapData[y, k, 0] = 1 - heightMap[y, k];
                splatmapData[y, k, 1] = heightMap[y, k];
                splatmapData[y, terrainBaseTextureResolution - k - 1, 0] = 1 - heightMap[y, terrainBaseTextureResolution - k];
                splatmapData[y, terrainBaseTextureResolution - k - 1, 1] = heightMap[y, terrainBaseTextureResolution - k];
            }
        }

        // now for x
        for (int x = 0; x < terrainBaseTextureResolution; x++) {
            for (int k = 0; k < 2; k++) {
                splatmapData[k, x, 0] = 1 - heightMap[k, x];
                splatmapData[k, x, 1] = heightMap[k, x];
                splatmapData[terrainBaseTextureResolution - k - 1, x, 0] = 1 - heightMap[terrainBaseTextureResolution - k, x];
                splatmapData[terrainBaseTextureResolution - k - 1, x, 1] = heightMap[terrainBaseTextureResolution - k, x];
            }
        }
        splatmapData[terrainBaseTextureResolution - 1, terrainBaseTextureResolution - 1, 0] = 1 - heightMap[terrainBaseTextureResolution, terrainBaseTextureResolution];
        splatmapData[terrainBaseTextureResolution - 1, terrainBaseTextureResolution - 1, 1] = heightMap[terrainBaseTextureResolution, terrainBaseTextureResolution];

        return splatmapData;
        */

        /*
        for (int y = 0; y < terrainBaseTextureResolution; y++) {
            for (int x = 0; x < terrainBaseTextureResolution; x++) {
                bool validateX = (x >= terrainBaseTextureResolution - terrainSmoothingEdge);
                bool validateY = (y >= terrainBaseTextureResolution - terrainSmoothingEdge);
                int numDivisions = 0;
                float height = 0;

                if (validateX) {
                    int distance = terrainBaseTextureResolution - x;
                    height = heightMap[y, x + 1] * (1 - 1f * distance / terrainSmoothingEdge) + heightMap[y, x] * (1f * distance / terrainSmoothingEdge);
                    ++numDivisions;
                } 

                if (validateY) {
                    int distance = terrainBaseTextureResolution - y;
                    height += heightMap[y + 1, x] * (1 - 1f * distance / terrainSmoothingEdge) + heightMap[y, x] * (1f * distance / terrainSmoothingEdge);
                    ++numDivisions;
                }

                if (numDivisions > 0)
                    height /= numDivisions;
                else height = heightMap[y, x];

                splatmapData[y, x, 0] = 1 - height;
                splatmapData[y, x, 1] = height;
            }
        }
        */

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

        /*
        int missingY = 33;
        for (int y = 0; y < terrainSmoothingEdge; y++) {
            for (int x = 0; x < terrainBaseTextureResolution; x++) {
                int sourceY = Mathf.RoundToInt((y + missingY) * stepY);
                
                float height = 0;
                height += (1f * y / terrainSmoothingEdge) * heightMap[sourceY, x];
                height += (1f - 1f * y / terrainSmoothingEdge) * heightMap[sourceY - 1, x];

                splatmapData[y + missingY, x, 0] = 1 - height;
                splatmapData[y + missingY, x, 1] = height;
            }
        }

        int missingX = heightMap.GetLength(0) - 1;
        for (int y = 0; y < terrainBaseTextureResolution; y++) {
            for (int x = 0; x < terrainSmoothingEdge; x++) {
                int sourceX = missingX - x;

                float height = 0;
                height += (1f - 1f * x / terrainSmoothingEdge) * (heightMap[y, sourceX]) / 1f;
                height += (1f * x / terrainSmoothingEdge) * heightMap[y, sourceX - 1];

                splatmapData[y, sourceX - 1, 0] = 1 - height;
                splatmapData[y, sourceX - 1, 1] = height;
            }
        }
        */

        return splatmapData;
    }

    /*
    public static float[,,] GenerateSplatsMapForLevel(
        TerrainData terrainData,
        float[,] heightMap,
        int terrainBaseTextureResolution,
        int terrainSmoothingEdge
    ) {



    }
    */

}
