using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DayNightCycle : MonoBehaviour {

    const int dayLength = 1440;

    public float currentTimeEditorMode;
    public float speedTimeEditorMode;

    public static float currentTime;
    public static float speedTime;

    public GameObject dayLightObject, nightLightObject;

    public AnimationCurve lightningCurve;
    public AnimationCurve smoothnessCurve;
    public AnimationCurve skyboxCurve;
    public Material skyboxMaterial;

    private static int timeTransition;

    private float lastValueForUpdate;

    private Transform dayLightTransform;
    private Light dayLight, nightLight;

    private static AnimationCurve _smoothnessCurve;

    private void Start() {
        _smoothnessCurve = smoothnessCurve;

        dayLight = dayLightObject.GetComponent<Light>();
        nightLight = nightLightObject.GetComponent<Light>();

        dayLightTransform = dayLightObject.transform;
        dayLightTransform.localRotation = Quaternion.Euler(15, 0, dayLightTransform.localRotation.eulerAngles.z);

        timeTransition = 50;
    }

    private void Update() {
        currentTime += speedTime;
        currentTimeEditorMode += speedTime;
        if (currentTime >= dayLength) {
            currentTime -= dayLength;
            currentTimeEditorMode = currentTime;
        }

        if(speedTime != speedTimeEditorMode)
            speedTime = speedTimeEditorMode;
        if(smoothnessCurve != _smoothnessCurve)
            _smoothnessCurve = smoothnessCurve;
        if(Mathf.Abs(currentTime - currentTimeEditorMode) >= 0.001f)
            currentTime = currentTimeEditorMode;

        float xRotation = (float)currentTime / dayLength * 360;
        float zRotation = dayLightTransform.localRotation.eulerAngles.z;
        dayLightTransform.localRotation = Quaternion.Euler(xRotation, 0, zRotation);

        float currentMomentInTransition = GetCurrentMomentInTransition();
        UpdateValues(currentMomentInTransition);

        if(currentTimeEditorMode < 780) {
            dayLightObject.SetActive(true);
        } else {
            dayLightObject.SetActive(false);
        }
    }

    void UpdateValues(float currentMomentInTransition) {
        if (Mathf.Abs(currentMomentInTransition - lastValueForUpdate) < 0.005f)
            return;
        lastValueForUpdate = currentMomentInTransition;

        RenderSettings.ambientIntensity = lightningCurve.Evaluate(currentMomentInTransition);
        nightLight.intensity = lightningCurve.Evaluate(1 - currentMomentInTransition) * 0.35f;
        skyboxMaterial.SetFloat("_AtmosphereThickness", skyboxCurve.Evaluate(currentMomentInTransition));
    }

    public static float GetCurrentMomentInTransition() {
        float currentMomentInTransition = 0;

        // 0 -> night
        // 1 -> day

        if (Mathf.Abs(dayLength - currentTime) <= timeTransition || currentTime <= timeTransition) {

            if (dayLength - timeTransition < currentTime)
                currentMomentInTransition = (currentTime - dayLength + timeTransition) / (2f * timeTransition);
            else currentMomentInTransition = (currentTime + timeTransition) / (2f * timeTransition);

            if (1 - currentMomentInTransition < 0.01f)
                currentMomentInTransition = 1;
        } else if (Mathf.Abs(dayLength / 2 - currentTime) <= timeTransition) {

            if (dayLength / 2 - timeTransition < currentTime)
                currentMomentInTransition = (currentTime - dayLength / 2 + timeTransition) / (2f * timeTransition);
            else currentMomentInTransition = (currentTime + timeTransition) / (2f * timeTransition);

            currentMomentInTransition = 1 - currentMomentInTransition;
            if (currentMomentInTransition < 0.01f)
                currentMomentInTransition = 0;
        } else if (currentTime > timeTransition && currentTime < dayLength / 2 - timeTransition)
            currentMomentInTransition = 1;

        return currentMomentInTransition;
    }

    public static float GetSmoothnessCurveAtCurrentTime() {
        float currentMomentInTransition = GetCurrentMomentInTransition();

        return _smoothnessCurve.Evaluate(currentMomentInTransition);
    }

}
