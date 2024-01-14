using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class ResourceBehavior : MonoBehaviour
{

    private static ItemData itemData;

    private int hitPoints = 100;

    [SerializeField] private string _itemName;
    [SerializeField] private int _itemAmount;
    [SerializeField] private GameObject itemDroppedPrefab;
    [SerializeField] LayerMask layermask;

    private void DropItem() {
        GameObject temp_obj = Instantiate(itemDroppedPrefab, transform.position + new Vector3(0, 2, 0), Quaternion.identity);
        temp_obj.AddComponent<ItemBehaviour>();
        temp_obj.AddComponent<BoxCollider>();
        temp_obj.AddComponent<Rigidbody>();
        temp_obj.tag = "Item";
        ItemBehaviour itemBehaviour = temp_obj.GetComponent<ItemBehaviour>();
        int idItem = itemData.GetIdFromName(_itemName);
        itemBehaviour.SetItem(new ItemSlot(idItem, _itemAmount));
        Destroy(gameObject);
    }

    public static void SetItemData(ItemData itemData)
    {
        ResourceBehavior.itemData = itemData;
    }

    public void TakeDamage(int value)
    {
        hitPoints -= value;
        if (hitPoints <= 0)
        {
            DropItem();
        }
    }
}