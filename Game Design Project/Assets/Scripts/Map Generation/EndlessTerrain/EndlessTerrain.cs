using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class EndlessTerrain : MonoBehaviour {

    [Header("Settings")]
    [SerializeField][Range(0, 10)] private int distanceViewChunks;
    [SerializeField] private int mapChunkSize;

    [Header("Viewer")]
    [SerializeField] private Transform viewer;
    private Vector2Int viewerChunkPosition;
    private Vector2Int oldViewerChunkPosition;

    private static MapGenerator mapGenerator;

    private Queue< Vector2Int > queue = new Queue< Vector2Int >();

    private Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
    private Dictionary<Vector2, bool> terrainChunkWasSeenDictionary = new Dictionary<Vector2, bool>();

    private List<TerrainChunk> visibleTerrainChunks = new List<TerrainChunk>();

    void Start() {
        mapGenerator = FindObjectOfType<MapGenerator>();
        mapGenerator.Initialize();
        mapChunkSize = mapGenerator.GetMapChunkSize();

        TerrainChunk.SetDistanceViewChunks(distanceViewChunks);
        TerrainChunk.SetMapGenerator(mapGenerator);

        oldViewerChunkPosition = new Vector2Int(-(int)2e9, -(int)2e9);
        viewerChunkPosition = new Vector2Int(
            Mathf.RoundToInt(viewer.position.x / (2 * (mapChunkSize - 1))),
            Mathf.RoundToInt(viewer.position.z / (2 * (mapChunkSize - 1)))
        );

        TerrainChunk firstTerrainChunk = new TerrainChunk(null, viewerChunkPosition, -1);
        terrainChunkDictionary.Add(viewerChunkPosition, firstTerrainChunk);
        visibleTerrainChunks.Add(firstTerrainChunk);
    }

    void Update() {
        viewerChunkPosition = new Vector2Int(
            Mathf.RoundToInt(viewer.position.x / (2 * (mapChunkSize - 1))),
            Mathf.RoundToInt(viewer.position.z / (2 * (mapChunkSize - 1)))
        );

        if (viewerChunkPosition != oldViewerChunkPosition) {
            UpdateVisibleChunks();
        }

        oldViewerChunkPosition = viewerChunkPosition;
    }

    void UpdateVisibleChunks() {
        TerrainChunk.SetViewerChunkPosition(viewerChunkPosition);   

        bool isChunkFar;
        int numVisibleChunks = visibleTerrainChunks.Count;
        for (int i = numVisibleChunks - 1; i >= 0; i--) {
            isChunkFar = (visibleTerrainChunks[i].GetDistanceFromViewer() > distanceViewChunks);

            if (isChunkFar) {
                visibleTerrainChunks[i].SetVisibility(false);
                visibleTerrainChunks.RemoveAt(i);
            }
        }

        Vector2Int chunkPosition = viewerChunkPosition;
        Terrain terrainChunk;

        queue.Enqueue(chunkPosition);
        while (queue.Count > 0) {
            chunkPosition = new Vector2Int(queue.Peek().x, queue.Peek().y);
            queue.Dequeue();

            if (!terrainChunkDictionary.ContainsKey(chunkPosition))
                continue;
            terrainChunk = terrainChunkDictionary[chunkPosition].GetTerrainChunk();

            for (int dir = 0; dir < 4; dir++) {
                Vector2Int offset = chunkPosition + Direction.GetOffset(dir);
                int distance = TerrainChunk.GetDistance(viewerChunkPosition, offset);
                if (distance > distanceViewChunks || terrainChunkWasSeenDictionary.ContainsKey(offset)) {
                    continue;
                }
                terrainChunkWasSeenDictionary.Add(offset, true);

                if (!terrainChunkDictionary.ContainsKey(offset)) {
                    TerrainChunk newTerrainChunk = new TerrainChunk(terrainChunk, offset, dir);
                    terrainChunkDictionary.Add(offset, newTerrainChunk);
                    visibleTerrainChunks.Add(newTerrainChunk);
                } else {
                    TerrainChunk existingTerrainChunk = terrainChunkDictionary[offset];
                    if (!existingTerrainChunk.GetTerrainChunk().gameObject.activeSelf) {
                        existingTerrainChunk.SetVisibility(true);
                        visibleTerrainChunks.Add(existingTerrainChunk);
                    }
                }

                queue.Enqueue(offset);
            }
        }
        queue.Clear();
        terrainChunkWasSeenDictionary.Clear();

    }
}
