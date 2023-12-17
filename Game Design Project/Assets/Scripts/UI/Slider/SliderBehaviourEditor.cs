using UnityEditor;

[CustomEditor(typeof(SliderBehaviour))]
public class SliderBehaviourEditor : Editor {

    public override void OnInspectorGUI() {
        SliderBehaviour sliderBehaviour = (SliderBehaviour)target;

        if (DrawDefaultInspector()) {
            if (sliderBehaviour.GetAutoUpdate()) {
                 sliderBehaviour.DefaultSettings();
            }
        }
    }
}