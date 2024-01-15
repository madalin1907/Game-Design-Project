using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEditor.Progress;

public class InventoryMechanism : MonoBehaviour {

    private const string pathToSelectedIcon = "Selected Mask";
    private const string pathToSlotIcon = "Background/Item Icon";
    private const float cooldDownSwitchInventory = 0.2f;
    private const float cooldDownDropItem = 0.2f;
    private const float cooldDownEatItem = 0.2f;

    private bool isInventoryOpen = false;
    private int currentSelectedItemHotbar = 0;
    private float lastTimeInventorySwitched = -1f;
    private float lastTimeItemDropped = -1f;
    private float lastTimeItemEaten = -1f;

    private List<GameObject> inventoryItems = new List<GameObject>();
    private List<GameObject> hotbarItems = new List<GameObject>();

    [SerializeField] private ItemData itemData;
    [SerializeField] private GameObject inventory;
    [SerializeField] private GameObject hotbar;
    [SerializeField] private GameObject itemDroppedPrefab;

    private void Start() {
        inventory.SetActive(false);
        isInventoryOpen = false;
        currentSelectedItemHotbar = -1;

        DefaultInit();
    }

    public void SwitchVisibilityInventory(InputAction.CallbackContext context) {
        int keyPressed = (int)context.ReadValue<float>();
        if (keyPressed != -1) 
            return;

        if (Time.time - lastTimeInventorySwitched < cooldDownSwitchInventory) 
            return;
        lastTimeInventorySwitched = Time.time;

        isInventoryOpen = !isInventoryOpen;
        inventory.SetActive(isInventoryOpen);

        if (isInventoryOpen)
            Cursor.lockState = CursorLockMode.None;
        else {
            Cursor.lockState = CursorLockMode.Locked;
            ItemBehaviour.OnCloseInventory();
        }
    }
    public void ChangeSelectedItemHotbar(InputAction.CallbackContext context) {
        int keyPressed = (int) context.ReadValue<float>();
        if (keyPressed <= 0)
            return;

        ChangeSelectedItemHotbarTo(keyPressed - 1);
    }

    public void DropItem(InputAction.CallbackContext context) {
        int keyPressed = (int)context.ReadValue<float>();
        if (keyPressed !=-2)
            return;

        if (Time.time - lastTimeItemDropped < cooldDownDropItem)
            return;
        lastTimeItemDropped = Time.time;

        ItemBehaviour.DropItem();
    }

    public void EatItem(InputAction.CallbackContext context) {
        if (Time.time - lastTimeItemEaten < cooldDownEatItem)
            return;
        lastTimeItemEaten = Time.time;

        ItemBehaviour itemBehaviour = hotbarItems[currentSelectedItemHotbar].GetComponent<ItemBehaviour>();
        if (itemBehaviour.GetItem().id == 0)
            return;

        if (itemData.items[itemBehaviour.GetItem().id]._type == ItemType.CONSUMABLE) {
            itemBehaviour.IncrementCount(-1);
            StatsMechanism statsMechanism = GetComponent<StatsMechanism>();
            statsMechanism.IncrementHunger(30f);
        }
    }

    public void AddItemInInventory(GameObject item) {
        int remain;
        ItemBehaviour itemBehaviour = item.GetComponent<ItemBehaviour>();
        ItemBehaviour slotBehaviour;

        if (itemBehaviour == null || itemBehaviour.GetItem().id == 0)
            return;

        for (int index = 0; index < hotbarItems.Count; index++) {
            slotBehaviour = hotbarItems[index].GetComponent<ItemBehaviour>();
            remain = TryAddItemAt(itemBehaviour, slotBehaviour);
            if (remain == -1) {
                continue;
            } else if (remain == 0) {
                Destroy(item);
                return;
            } else {
                itemBehaviour.SetItem(new ItemSlot(itemBehaviour.GetItem().id, remain));
            }
        }

        for (int index = 0; index < inventoryItems.Count; index++) {
            slotBehaviour = inventoryItems[index].GetComponent<ItemBehaviour>();
            remain = TryAddItemAt(itemBehaviour, slotBehaviour);
            if (remain == -1) {
                continue;
            } else if (remain == 0) {
                Destroy(item);
                return;
            } else {
                itemBehaviour.SetItem(new ItemSlot(itemBehaviour.GetItem().id, remain));
            }
        }
    }

    private int TryAddItemAt(ItemBehaviour itemBehaviour, ItemBehaviour slotBehaviour) {
        int remain = -1;

        if (slotBehaviour.GetItem().id == 0) {
            slotBehaviour.UpdateItem(itemBehaviour.GetItem().id, itemBehaviour.GetItem().count);
            remain = 0;
        } else if (slotBehaviour.GetItem().id == itemBehaviour.GetItem().id) {
            remain = slotBehaviour.IncrementCount(itemBehaviour.GetItem().count);
            if (remain != 0) {
                itemBehaviour.SetItem(new ItemSlot(itemBehaviour.GetItem().id, remain));
            }
        }

        return remain;
    }

    private void ChangeSelectedItemHotbarTo(int value) {
        if (currentSelectedItemHotbar != -1)
            GetSelectedIconGameObjectFromItem(hotbarItems[currentSelectedItemHotbar]).SetActive(false);

        currentSelectedItemHotbar = value;
        GetSelectedIconGameObjectFromItem(hotbarItems[currentSelectedItemHotbar]).SetActive(true);
    }

    // ------------------ GETTERS ------------------

    public bool IsInventoryOpen() {
        return isInventoryOpen;
    }

    public Sprite GetItemSprite(int id) {
        if (id == -1 || id >= itemData.items.Length)
            return null;
        return itemData.items[id]._icon;
    }

    public int GetItemStack(int id) {
        if (id == -1 || id >= itemData.items.Length)
            return 0;
        return (int)itemData.items[id]._maxStack;
    }

    public GameObject GetSelectedIconGameObjectFromItem(GameObject item) {
        return item.transform.Find(pathToSelectedIcon).gameObject;
    }

    public GameObject GetSlotIconGameObjectFromItem(GameObject item) {
        return item.transform.Find(pathToSlotIcon).gameObject;
    }

    public GameObject GetInventory() {
        return inventory;
    }

    public GameObject GetItemDroppedPrefab() {
        return itemDroppedPrefab;
    }


    // ------------------ DEFAULT ------------------

    private void DefaultInit()
    {
        GetGameobjectItemsFromHotbar();
        GetGameobjectItemsFromInventory();
        ChangeSelectedItemHotbarTo(0);
        ItemBehaviour.SetInventoryMechanism(this);
        CraftingIngredientsMechanism.SetCraftingMechanism(gameObject.GetComponent<CraftingMechanism>());
        ResourceBehavior.SetItemData(itemData);
    }

    private void GetGameobjectItemsFromHotbar() {
        GameObject itemsParent = hotbar.transform.Find("Items").gameObject;

        for (int i = 0; i < itemsParent.transform.childCount; i++) {
            hotbarItems.Add(itemsParent.transform.GetChild(i).gameObject);
        }
    }

    private void GetGameobjectItemsFromInventory() {
        GameObject itemsParent = inventory.transform.Find("Items").gameObject;
        Transform rowTransform;

        for (int row = 0; row < itemsParent.transform.childCount; row++) {
            rowTransform = itemsParent.transform.GetChild(row);

            for (int column = 0; column < rowTransform.childCount; column++) 
                inventoryItems.Add(rowTransform.GetChild(column).gameObject);
        }
    }


}
