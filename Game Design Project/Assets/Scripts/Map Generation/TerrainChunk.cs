using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;

public class TerrainChunk {

    private static Vector2Int viewerChunkPosition;
    private static int distanceViewChunks;

    private GameObject waterGameObject;
    private Vector2Int positionChunk;
    private Terrain terrain;
    private MapData mapData;
    private static MapGenerator mapGenerator;

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
        mapGenerator.DrawTerrain(terrain, mapData, positionChunk);
        terrain.gameObject.SetActive(true);
    }

    public int GetDistanceFromViewer() {
        return GetDistance(viewerChunkPosition, positionChunk);
    }

    public Terrain GetTerrainChunk() {
        return terrain;
    }

    public void SetVisibility(bool value) {
        terrain.gameObject.SetActive(value);
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
