using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CraftingIngredientsMechanism : MonoBehaviour {

    private static CraftingMechanism craftingMechanism = null;

    [SerializeField] private bool isResultIngredient;

    public void CheckCraftingAvailability() {
        craftingMechanism.CheckCraftingAvailability();
    }

    public void Craft() {
        craftingMechanism.Craft();
    }

    public void ResultPicked() {
        craftingMechanism.ResultPicked();
    }

    public void SetIsResultIngredient(bool isResultIngredient) {
        this.isResultIngredient = isResultIngredient;
    }

    public bool GetIsResultIngredient() {
        return isResultIngredient;
    }

    public static void SetCraftingMechanism(CraftingMechanism craftingMechanism) {
        CraftingIngredientsMechanism.craftingMechanism = craftingMechanism;
    }

}
