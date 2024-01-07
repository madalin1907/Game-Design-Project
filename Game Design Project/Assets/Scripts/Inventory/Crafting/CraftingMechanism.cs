using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CraftingMechanism : MonoBehaviour {

    [SerializeField] private CraftingData craftingData;
    [SerializeField] private GameObject resultItem;

    [SerializeField] private GameObject craftingRow1;
    [SerializeField] private GameObject craftingRow2;
    [SerializeField] private GameObject craftingRow3;

    private bool itemWasCrafted = false;
    private CraftingData.Recipe currentRecipe;
    private List<ItemBehaviour> ingredientsItems;

    private void Start() {
        DefaultInit();
    }

    public void CheckCraftingAvailability() {
        if (itemWasCrafted)
            return;

        foreach (CraftingData.Recipe recipe in craftingData.recipes) {
            if (CheckRecipe(recipe)) {
                resultItem.GetComponent<ItemBehaviour>().UpdateItem(recipe.result._id, recipe.result._count);
                currentRecipe = recipe;
                return;
            }
        }
        currentRecipe = null;
    }

    public void Craft() {
        if (itemWasCrafted)
            return;

        for (int row = 0; row < 3; row++) {
            for (int column = 0; column < 3; column++) {
                ItemBehaviour ingredient = GetIngredient(row, column);
                if (ingredient == null)
                    continue;

                CraftingData.Ingredient recipeIngredient = currentRecipe.GetIngredient(row, column);
                if (recipeIngredient == null)
                    continue;

                IncrementCountItem(ingredient, -recipeIngredient._count);
            }
        }
        itemWasCrafted = true;
    }

    public void ResultPicked() {
        itemWasCrafted = false;
        CheckCraftingAvailability();
    }

    private bool CheckRecipe(CraftingData.Recipe recipe) {
        for (int row = 0; row < 3; row++) {
            for (int column = 0; column < 3; column++) {
                if (!CheckIngredient(recipe, row, column))
                    return false;
            }
        }
        return true;
    }

    private bool CheckIngredient(CraftingData.Recipe recipe, int row, int column) {
        ItemBehaviour ingredient = GetIngredient(row, column);
        if (ingredient == null)
            return false;

        CraftingData.Ingredient recipeIngredient = recipe.GetIngredient(row, column);
        if (recipeIngredient == null)
            return false;

        return ingredient.GetItem().id == recipeIngredient._id && ingredient.GetItem().count >= recipeIngredient._count;
    }

    private void IncrementCountItem(ItemBehaviour item, int count) {
        item.IncrementCount(count);
    }

    // ----------------- Getters -----------------

    public ItemBehaviour GetIngredient(int row, int column) {
        return ingredientsItems[row * 3 + column];
    }

    // ----------------- Default -----------------

    private void DefaultInit() {
        DefaultIngredientsItems();
    }

    private void DefaultIngredientsItems() {
        ingredientsItems = new List<ItemBehaviour>();
        for (int i = 0; i < craftingRow1.transform.childCount; i++) {
            ingredientsItems.Add(craftingRow1.transform.GetChild(i).GetComponent<ItemBehaviour>());
        }
        for (int i = 0; i < craftingRow2.transform.childCount; i++) {
            ingredientsItems.Add(craftingRow2.transform.GetChild(i).GetComponent<ItemBehaviour>());
        }
        for (int i = 0; i < craftingRow3.transform.childCount; i++) {
            ingredientsItems.Add(craftingRow3.transform.GetChild(i).GetComponent<ItemBehaviour>());
        }
    }

}
