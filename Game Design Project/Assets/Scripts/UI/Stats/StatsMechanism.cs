using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatsMechanism : MonoBehaviour {

    [SerializeField] private bool isPlayer;

    [Header("Health")]
    private float innerHealth;
    [SerializeField] private float health;
    [SerializeField] private float maxHealth;

    [Header("Hunger")]
    private float innerHunger;
    [SerializeField] private float hunger;
    [SerializeField] private float maxHunger;
    [SerializeField] private float decreaseHungerRate;

    [Header("Energy")]
    private bool isSprinting;
    private float innerEnergy;
    [SerializeField] private float energy;
    [SerializeField] private float maxEnergy;
    [SerializeField] private float decreaseEnergyRate;
    [SerializeField] private float increaseEnergyRate;

    [Header("Oxygen")]
    private bool isUnderWater;
    private float innerOxygen;
    [SerializeField] private float oxygen;
    [SerializeField] private float maxOxygen;
    [SerializeField] private float decreaseOxygenRate;
    [SerializeField] private float increaseOxygenRate;

    [Header("Sliders")]
    [SerializeField] private SliderBehaviour healthSlider = null;
    [SerializeField] private SliderBehaviour hungerSlider = null;
    [SerializeField] private SliderBehaviour energySlider = null; 
    [SerializeField] private SliderBehaviour oxygenSlider = null;

    private void Awake() {
        health = maxHealth;
        hunger = maxHunger;
        energy = maxEnergy;
        oxygen = maxOxygen;
        UpdateMaxValuesSliders();
    }

    private void FixedUpdate() {
        if (isSprinting)
            Sprint();
        else if (energy != maxEnergy)
            RegenerateEnergy();

        if (isUnderWater)
            SwimUnderWater();
        else if (oxygen != maxOxygen)
            RegenerateOxygen();

        if (hunger <= 0f) {
            TakeDamage(0.04f);
        }

        if (hunger >= 80f && health < maxHealth) {
            Heal(0.02f);
            IncrementHunger(-0.02f);
        }

        Lives();

        CheckIfNeedToBeDisplayed();
        UpdateValuesExtern();
    }

    private void CheckIfNeedToBeDisplayed() {
        if (energySlider != null)
            energySlider.gameObject.SetActive(energy < maxEnergy);
        if (oxygenSlider != null)
            oxygenSlider.gameObject.SetActive(oxygen < maxOxygen);
    }

    private void UpdateValuesExtern() {
        if (innerHealth != health)
            TakeDamage(0);
        if (innerHunger != hunger)
            IncrementHunger(0);
        if (innerEnergy != energy)
            IncrementEnergy(0);
        if (innerOxygen != oxygen)
            IncrementOxygen(0);
    }

    private void UpdateMaxValuesSliders() {
        if (healthSlider != null)
            healthSlider.SetMaxValue(maxHealth);
        if (hungerSlider != null)
            hungerSlider.SetMaxValue(maxHunger);
        if (energySlider != null)
            energySlider.SetMaxValue(maxEnergy);
        if (oxygenSlider != null)
            oxygenSlider.SetMaxValue(maxOxygen);
    }

    private void RegenerateEnergy() {
        IncrementEnergy(increaseEnergyRate * Time.deltaTime);
    }

    private void RegenerateOxygen() {
        IncrementOxygen(increaseOxygenRate * Time.deltaTime);
    }

    private void Sprint() {
        IncrementEnergy(-decreaseEnergyRate * Time.deltaTime);
    }

    private void SwimUnderWater() {
        IncrementOxygen(-decreaseOxygenRate * Time.deltaTime);
    }

    private void Lives() {
        IncrementHunger(-decreaseHungerRate * Time.deltaTime);
    }

    public void TakeDamage(float damage) {
        health -= damage;
        health = Mathf.Clamp(health, 0, maxHealth);
        innerHealth = health;

        if (healthSlider != null)
            healthSlider.SetValue(health / maxHealth);

        if (health <= 0) {
            // pass
        }
    }

    public void Heal(float heal) {
        health += heal;
        health = Mathf.Clamp(health, 0, maxHealth);
        innerHealth = health;

        if (healthSlider != null)
            healthSlider.SetValue(health / maxHealth);
    }

    public void IncrementHunger(float value) {
        hunger += value;
        hunger = Mathf.Clamp(hunger, 0, maxHunger);
        innerHunger = hunger;

        if (hungerSlider != null)
            hungerSlider.SetValue(hunger / maxHunger);
    }

    public void IncrementEnergy(float value) {
        energy += value;
        energy = Mathf.Clamp(energy, 0, maxEnergy);
        innerEnergy = energy;

        if (energySlider != null)
            energySlider.SetValue(energy / maxEnergy);
    }

    public void IncrementOxygen(float value) {
        oxygen += value;
        oxygen = Mathf.Clamp(oxygen, 0, maxOxygen);
        innerOxygen = oxygen;

        if (oxygenSlider != null)
            oxygenSlider.SetValue(oxygen / maxOxygen);
    }

    public bool GetIsSprinting() {
        return isSprinting;
    }

    public void SetIsSprinting(bool value) {
        isSprinting = value;
    }

    public void SetIsUnderWater(bool value) {
        isUnderWater = value;
    }

    public float GetEnergy() {
        return energy;
    }

    public float GetHealth() {
        return health;
    }

}
