using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainChunk {

    private static int distanceViewChunks;
    private static Vector2Int viewerChunkPosition;
    private static MapGenerator mapGenerator;

    private bool mobWasSpawned = false;
    private Vector2Int positionChunk;
    private Terrain terrain;
    private MapData mapData;

    // ----------------- Getters and setters -----------------

    public TerrainChunk(Terrain parentTerrain, Vector2Int pos, int dir) {
        positionChunk = pos;
        terrain = mapGenerator.GenerateNewTerrainChunk();
        mapGenerator.SetDefaultSettingsTerrainChunk(terrain, parentTerrain, positionChunk, dir);
        terrain.gameObject.SetActive(false);
        mapGenerator.RequestChunk(terrain, parentTerrain, positionChunk, dir, OnReceivedTerrainData);
    }

    public void OnReceivedTerrainData (MapData _mapData) {
        mapData = _mapData;
        mapGenerator.DrawTerrain(terrain, mapData);
        terrain.gameObject.SetActive(true);
    }

    public int GetDistanceFromViewer() {
        return GetDistance(viewerChunkPosition, positionChunk);
    }

    public Terrain GetTerrainChunk() {
        return terrain;
    }

    public bool GetMobWasSpawned() {
        return mobWasSpawned;
    }

    public ref MapData GetMapData() {
        return ref mapData;
    }

    public void SetVisibility(bool value) {
        terrain.gameObject.SetActive(value);
    }

    public void SetMobWasSpawned(bool value) {
        mobWasSpawned = value;
    }

    // ----------------- Static methods -----------------

    public static void SetDistanceViewChunks(int distanceViewChunks) {
        TerrainChunk.distanceViewChunks = distanceViewChunks;
    }

    public static void SetViewerChunkPosition(Vector2Int viewerChunkPosition) {
        TerrainChunk.viewerChunkPosition = viewerChunkPosition;
    }

    public static void SetMapGenerator(MapGenerator mapGenerator) {
        TerrainChunk.mapGenerator = mapGenerator;
    }

    public static int GetDistance(Vector2Int a, Vector2Int b) {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
}
