using UnityEngine;
using System.Collections;
using System;

[CreateAssetMenu()]
public class CraftingData : ScriptableObject {

    const int ROWS = 3;
    const int COLUMNS = 3;

    public bool update;
    public ItemData itemData;
    public Recipe[] recipes;

    [Serializable]
    public class Recipe {
        public Ingredient result;

        [Header("Ingredients")]

        public RowRecipe row0;
        public RowRecipe row1;
        public RowRecipe row2;

        public Recipe() {
            row0 = new RowRecipe();
            row1 = new RowRecipe();
            row2 = new RowRecipe();
            result = new Ingredient("", 0, 0);
        }

        public RowRecipe GetRowRecipe(int row) {
            switch (row) {
                case 0:
                    return row0;
                case 1:
                    return row1;
                case 2:
                    return row2;
                default:
                    return null;
            }
        }

        public Ingredient GetIngredient(int row, int column) {
            switch (row) {
                case 0:
                    return row0.GetIngredient(column);
                case 1:
                    return row1.GetIngredient(column);
                case 2:
                    return row2.GetIngredient(column);
                default:
                    return null;
            }
        }
    }

    [Serializable]
    public class RowRecipe {
        public Ingredient ingredient0;
        public Ingredient ingredient1;
        public Ingredient ingredient2;

        public RowRecipe() {
            ingredient0 = new Ingredient("", 0, 0);
            ingredient1 = new Ingredient("", 0, 0);
            ingredient2 = new Ingredient("", 0, 0);
        }

        public Ingredient GetIngredient(int column) {
            switch (column) {
                case 0:
                    return ingredient0;
                case 1:
                    return ingredient1;
                case 2:
                    return ingredient2;
                default:
                    return null;
            }
        }
    }

    [Serializable]
    public class Ingredient {
        private string last_name;
        private int last_id;

        public string _name;
        public int _id;
        public int _count;

        public Ingredient(string name, int id, int count) {
            _name = name;
            _id = id;
            last_name = name;
            last_id = id;
            _count = count;
        }

        public string GetLastName() {
            return last_name;
        }

        public int GetLastId() {
            return last_id;
        }

        public void SetLastName(string name) {
            last_name = name;
        }

        public void SetLastId(int id) {
            last_id = id;
        }
    }

    public void OnValidate() {
        for (int i = 0; i < recipes.Length; i++) {
            for (int j = 0; j < ROWS; j++) {
                for (int k = 0; k < COLUMNS; k++) {
                    UpdateIngredientValues(recipes[i].GetIngredient(j, k));
                }
            }
            UpdateIngredientValues(recipes[i].result);
        }

        update = false;
    }

    public void UpdateIngredientValues(Ingredient ingredient) {
        if (update || ingredient.GetLastName() != ingredient._name) {
            ingredient._id = itemData.GetIdFromName(ingredient._name);
        } else if (ingredient._id == 0 || ingredient.GetLastId() != ingredient._id) {
            ingredient._name = itemData.GetNameFromId(ingredient._id);
        }

        ingredient.SetLastName(ingredient._name);
        ingredient.SetLastId(ingredient._id);
    }

}