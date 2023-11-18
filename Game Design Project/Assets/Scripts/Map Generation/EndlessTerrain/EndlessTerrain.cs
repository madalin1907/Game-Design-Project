using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndlessTerrain : MonoBehaviour {

    [Header("Settings")]
    [SerializeField][Range(0, 10)] private int distanceViewChunks;

    [Header("Viewer")]
    [SerializeField] private Transform viewer;
    private Vector2 oldViewerPosition;

    void Start() {
        
    }

    void Update() {
        
    }
}
