using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MapGenerator))]
public class MapGeneratorEditor : Editor {

    public override void OnInspectorGUI() {
        MapGenerator mapGen = (MapGenerator)target;

        if (DrawDefaultInspector()) {
            if (mapGen.GetAutoUpdate()) {
                mapGen.DrawMapInEditor();
            }
        }

        if (GUILayout.Button("Generate")) {
            mapGen.DrawMapInEditor();
        }

        if (GUILayout.Button("Clear")) {
            mapGen.ClearData();
        }
    }

}
