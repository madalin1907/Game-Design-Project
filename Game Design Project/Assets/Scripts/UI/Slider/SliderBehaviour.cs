using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SliderBehaviour : MonoBehaviour {

    const float rightSliderOffset = 3f;

    [SerializeField] private bool autoUpdate;
    [SerializeField] private int maxValue;
    [SerializeField] private float value;
    private float gradualValue;
    [SerializeField] private Color color;
    [SerializeField] private string valueBeforeString;

    [SerializeField] private TextMeshProUGUI valueText;
    [SerializeField] private RectTransform sliderRectTransform;
    [SerializeField] private Image iconImage;
    [SerializeField] private Sprite iconSprite;
    [SerializeField] private Image textBgImage;
    [SerializeField] private List<Image> itemsImages;

    private void Awake() {
        autoUpdate = false;
        DefaultSettings();
    }

    private void Update() {
        if (value != gradualValue) {
            if (gradualValue > value) {
                gradualValue -= 0.5f * Time.deltaTime;
                if (gradualValue < value)
                    gradualValue = value;
            } else if (gradualValue < value) {
                gradualValue += 0.5f * Time.deltaTime;
                if (gradualValue > value)
                    gradualValue = value;
            }

            ChangeToValue(gradualValue);
        }
    }

    public void ChangeToValue(float newValue) {
        float sliderParentWidth = sliderRectTransform.parent.GetComponent<RectTransform>().rect.width * 0.85f;
        float rightOffset = (1 - newValue) * sliderParentWidth + rightSliderOffset;
        int valueInt = (int)(newValue * maxValue);

        sliderRectTransform.offsetMax = new Vector2(-rightOffset, sliderRectTransform.offsetMax.y);
        valueText.text = valueBeforeString + " " + valueInt.ToString() + "/" + maxValue;
    }

    public void DefaultSettings() {
        value = Mathf.Clamp01(value);

        foreach(Image image in itemsImages) {
            image.color = color;
        }
        textBgImage.color = new Color(color.r, color.g, color.b, textBgImage.color.a);
        iconImage.sprite = iconSprite;
        gradualValue = value;
        ChangeToValue(value);
    }

    public bool GetAutoUpdate() {
        return autoUpdate;
    }

    public void SetValue(float value) {
        this.value = Mathf.Clamp01(value);
    }

    public void SetMaxValue(float maxValue) {
        this.maxValue = (int)maxValue;
    }
}
