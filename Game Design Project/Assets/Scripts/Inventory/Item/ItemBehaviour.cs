using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ItemBehaviour : 
    MonoBehaviour, 
    IBeginDragHandler, 
    IDragHandler, 
    IEndDragHandler,
    IPointerDownHandler,
    IPointerUpHandler
{
    private const float decreaseCooldownPlaceOneItemDown = 0.025f;
    private const float cooldDownPlaceOneItemDown = 0.25f;

    private static bool holdPlaceOneItemDown = false;
    private static int currentIterationPlaceOneItemDown = 0;
    private static float timeSpentSinceLastPlaceOneItemDown = 0f;
    private static GameObject originalItemBeingDragged = null;
    private static GameObject itemBeingDragged = null;
    private static GameObject lastSlotWhereWasPlaced = null;
    private static InventoryMechanism inventoryMechanism = null;

    private bool temporaryItem = false;
    private ItemSlot item = new(0, 0);

    private void Update() {
        ItemBehaviour itemHovered;
        CraftingIngredientsMechanism craftingIngredientsMechanism;

        if (itemBeingDragged == gameObject) {
            itemHovered = GetItemBehaviourIfHovered();
            if (holdPlaceOneItemDown && itemHovered != null) {
                timeSpentSinceLastPlaceOneItemDown += Time.deltaTime;

                if (lastSlotWhereWasPlaced != itemHovered.gameObject) {
                    currentIterationPlaceOneItemDown = 0;
                    timeSpentSinceLastPlaceOneItemDown = 0;
                    lastSlotWhereWasPlaced = itemHovered.gameObject;
                    PlaceOneItemDownInInventory(itemHovered);

                    craftingIngredientsMechanism = itemHovered.GetComponent<CraftingIngredientsMechanism>();
                    if (craftingIngredientsMechanism != null)
                        craftingIngredientsMechanism.CheckCraftingAvailability();
                } else {
                    float timeNeededToWait = Mathf.Max(0, cooldDownPlaceOneItemDown - currentIterationPlaceOneItemDown * decreaseCooldownPlaceOneItemDown);
                    if (timeSpentSinceLastPlaceOneItemDown >= timeNeededToWait) {
                        currentIterationPlaceOneItemDown++;
                        timeSpentSinceLastPlaceOneItemDown = 0;
                        PlaceOneItemDownInInventory(itemHovered);

                        craftingIngredientsMechanism = itemHovered.GetComponent<CraftingIngredientsMechanism>();
                        if (craftingIngredientsMechanism != null)
                            craftingIngredientsMechanism.CheckCraftingAvailability();
                    }
                }
            }
        }
    }

    public void OnBeginDrag(PointerEventData eventData) {
        if (item.id == 0 || !inventoryMechanism.IsInventoryOpen())
            return;

        if (eventData.button == PointerEventData.InputButton.Left)
            BeginMoveItemSlot();
    }

    public void OnDrag(PointerEventData eventData) {
        if (item.id == 0 || !inventoryMechanism.IsInventoryOpen())
            return;

        if (eventData.button == PointerEventData.InputButton.Left) {
            itemBeingDragged.GetComponent<RectTransform>().position = Input.mousePosition;
        }
    }

    public void OnEndDrag(PointerEventData eventData) {
        if (item.id == 0 || !inventoryMechanism.IsInventoryOpen())
            return;

        if (eventData.button == PointerEventData.InputButton.Left) {
            EndMoveItemSlot();
            if (holdPlaceOneItemDown) 
                holdPlaceOneItemDown = false;
        }
    }

    public void OnPointerDown(PointerEventData eventData) {
        if (item.id == 0 || !inventoryMechanism.IsInventoryOpen())
            return;

        if (eventData.button == PointerEventData.InputButton.Right) {
            holdPlaceOneItemDown = true;
            lastSlotWhereWasPlaced = null;
        }
    }

    public void OnPointerUp(PointerEventData eventData) {
        if (!inventoryMechanism.IsInventoryOpen())
            return;

        if (eventData.button == PointerEventData.InputButton.Right) {
            holdPlaceOneItemDown = false;
        }
    }

    private void BeginMoveItemSlot() {
        ItemBehaviour itemBehaviour;
        CraftingIngredientsMechanism craftingIngredientsMechanism;

        if (itemBeingDragged != null) {
            Destroy(itemBeingDragged);
            itemBeingDragged = null;
        }

        originalItemBeingDragged = gameObject;
        itemBeingDragged = Instantiate(
            gameObject,
            transform.position,
            Quaternion.identity,
            GameObject.Find("Canvas").transform
        );

        itemBehaviour = itemBeingDragged.GetComponent<ItemBehaviour>();
        craftingIngredientsMechanism = itemBeingDragged.GetComponent<CraftingIngredientsMechanism>();

        itemBeingDragged.GetComponent<RectTransform>().sizeDelta = GetComponent<RectTransform>().sizeDelta;
        itemBeingDragged.SetActive(true);
        itemBehaviour.SetTemporaryItem(true);
        itemBehaviour.SetItem(item);
        inventoryMechanism.GetSelectedIconGameObjectFromItem(itemBeingDragged).SetActive(false);

        if (craftingIngredientsMechanism != null && craftingIngredientsMechanism.GetIsResultIngredient())
            craftingIngredientsMechanism.Craft();
    }

    private void EndMoveItemSlot() {
        int remain;
        ItemSlot tempItemSlot;
        ItemBehaviour hoveredItemBehaviour = GetItemBehaviourIfHovered();

        if (hoveredItemBehaviour != null) {
            if (hoveredItemBehaviour.GetItem().id != item.id) {
                if (hoveredItemBehaviour.GetItem().id == 0 || !CheckIfItemMovedWasCraftingResult()) {
                    tempItemSlot = hoveredItemBehaviour.GetItem();
                    hoveredItemBehaviour.UpdateItem(item.id, item.count);
                    UpdateItem(tempItemSlot.id, tempItemSlot.count);
                }
            } else if (hoveredItemBehaviour.gameObject != gameObject) {
                remain = hoveredItemBehaviour.IncrementCount(item.count);
                UpdateItem(item.id, remain);

                if (remain <= 0)
                    MarkCraftingResultAsPicked();
            }
        }

        Destroy(itemBeingDragged);
        itemBeingDragged = null;
        originalItemBeingDragged = null;
    }

    private bool CheckIfItemMovedWasCraftingResult() {
        CraftingIngredientsMechanism craftingIngredientsMechanism;

        craftingIngredientsMechanism = itemBeingDragged.GetComponent<CraftingIngredientsMechanism>();
        
        if (craftingIngredientsMechanism != null && craftingIngredientsMechanism.GetIsResultIngredient())
            return true;
        return false;
    }

    private void MarkCraftingResultAsPicked() {
        CraftingIngredientsMechanism craftingIngredientsMechanism;

        craftingIngredientsMechanism = itemBeingDragged.GetComponent<CraftingIngredientsMechanism>();
        if (craftingIngredientsMechanism != null && craftingIngredientsMechanism.GetIsResultIngredient()) {
            craftingIngredientsMechanism.ResultPicked();
        }
    }

    public void UpdateItem(int id, int count) {
        GameObject slotIcon;

        if (count <= 0)
            id = 0;

        if (item.id != id) {
            item.id = id;
            slotIcon = inventoryMechanism.GetSlotIconGameObjectFromItem(gameObject);
            slotIcon.GetComponent<Image>().sprite = inventoryMechanism.GetItemSprite(id);
        }

        item.count = count;
        if (count > 1)
            transform.Find("Count").GetComponent<TextMeshProUGUI>().text = count.ToString();
        else
             transform.Find("Count").GetComponent<TextMeshProUGUI>().text = "";
    }

    public int IncrementCount(int count) {
        int incrementValue = count;
        int maxStack = inventoryMechanism.GetItemStack(item.id);

        if (item.count + count > maxStack) {
            incrementValue = maxStack - item.count;
            UpdateItem(item.id, maxStack);
        } else {
            UpdateItem(item.id, item.count + incrementValue);
        }

        return count - incrementValue;
    }

    public void SetTemporaryItem(bool value) {
        temporaryItem = value;
    }

    public ItemSlot GetItem() {
        return item;
    }

    public void SetItem(ItemSlot slot) {
        item = slot;
    }

    public bool IsTemporaryItem() {
        return temporaryItem;
    }

    private static ItemBehaviour GetItemBehaviourIfHovered() {
        ItemBehaviour hoveredItemBehaviour;
        PointerEventData eventDataCurrentPosition;
        List<RaycastResult> results;

        eventDataCurrentPosition = new PointerEventData(EventSystem.current);
        eventDataCurrentPosition.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);

        if (results.Count > 0) {
            foreach (RaycastResult result in results) {
                hoveredItemBehaviour = result.gameObject.GetComponent<ItemBehaviour>();
                if (hoveredItemBehaviour != null && !hoveredItemBehaviour.IsTemporaryItem())
                    return hoveredItemBehaviour;
            }
        }

        return null;
    }

    public static void PlaceOneItemDownInInventory(ItemBehaviour itemHovered) {
        ItemBehaviour originalItem;
        ItemBehaviour itemDragged;

        if (itemHovered == null || itemBeingDragged == null || itemHovered.gameObject == originalItemBeingDragged)
            return;

        originalItem = originalItemBeingDragged.GetComponent<ItemBehaviour>();
        itemDragged = itemBeingDragged.GetComponent<ItemBehaviour>();

        if (itemHovered.GetItem().id == 0) {
            itemHovered.UpdateItem(originalItem.GetItem().id, 1);
            originalItem.IncrementCount(-1);
            itemDragged.IncrementCount(-1);
        } else if (itemHovered.GetItem().id == originalItem.GetItem().id) {
            itemHovered.IncrementCount(1);
            originalItem.IncrementCount(-1);
            itemDragged.IncrementCount(-1);

            if (itemDragged.GetItem().count == 0)
                Destroy(itemBeingDragged);
        }
    }

    public static void OnCloseInventory() {
        if (itemBeingDragged != null) {
            Destroy(itemBeingDragged);
            itemBeingDragged = null;
        }
        originalItemBeingDragged = null;
    }

    public static void DropItem() {
        ItemBehaviour itemHovered;
        Rigidbody itemDroppedRigidbody;
        ItemBehaviour itemDroppedBehaviour;
        GameObject itemDropped;
        
        itemHovered = GetItemBehaviourIfHovered();
        if (itemHovered == null || itemHovered.GetItem().id == 0)
            return;

        itemDropped = Instantiate(
            inventoryMechanism.GetItemDroppedPrefab(),
            inventoryMechanism.gameObject.transform.position + inventoryMechanism.gameObject.transform.forward,
            Quaternion.identity
        );

        itemDroppedBehaviour = itemDropped.GetComponent<ItemBehaviour>();
        itemDroppedRigidbody = itemDropped.GetComponent<Rigidbody>();

        itemDroppedRigidbody.velocity = inventoryMechanism.gameObject.GetComponent<Rigidbody>().velocity;
        itemDroppedRigidbody.AddForce(inventoryMechanism.gameObject.transform.forward * 3, ForceMode.Impulse);
        itemDroppedRigidbody.AddForce(inventoryMechanism.gameObject.transform.up * 4.5f, ForceMode.Impulse);

        itemDroppedBehaviour.SetItem(itemHovered.GetItem());

        itemHovered.UpdateItem(0, 0);
    }

    public static void SetInventoryMechanism(InventoryMechanism inventoryMechanism) {
        ItemBehaviour.inventoryMechanism = inventoryMechanism;
    }

}
