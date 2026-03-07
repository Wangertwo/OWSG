using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Blueprint
{
    [System.Serializable]
    public class Ingredient
    {
        public string ItemName;
        public int Amount;

        public Ingredient(string itemName, int amount)
        {
            ItemName = itemName;
            Amount = amount;
        }
    }

    public string ResultItemName;
    public List<Ingredient> Ingredients;

    public Blueprint(string resultItemName, List<Ingredient> ingredients)
    {
        ResultItemName = resultItemName;
        Ingredients = ingredients;
    }

    public bool CanCraft(InventorySystem inventorySystem)
    {
        if (inventorySystem == null)
        {
            return false;
        }

        foreach (Ingredient ingredient in Ingredients)
        {
            int count = 0;
            foreach (string item in inventorySystem.itemList)
            {
                if (item == ingredient.ItemName)
                {
                    count++;
                }
            }

            if (count < ingredient.Amount)
            {
                return false;
            }
        }

        return true;
    }
}
