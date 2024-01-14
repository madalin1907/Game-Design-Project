using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class ResourceBehavior : MonoBehaviour
{

    private static ItemData itemData;

    private int hitPoints = 100;
    public Camera camera;
    private Rigidbody rigidbody;
    public float rayLength;

    [SerializeField] private string _itemName;
    [SerializeField] private int _itemAmount;
    [SerializeField] private GameObject itemDroppedPrefab;
    [SerializeField] LayerMask layermask;


    void Update()
    {
        /*Ray ray = new Ray(transform.position, transform.TransformDirection(Vector3.forward));

        if(Physics.Raycast (ray, out RaycastHit hitinfo, 20f, layermask, QueryTriggerInteraction.Ignore))
        {
            Debug.Log("Hit something");
        }
        else
        {
            Debug.Log("Hit Nothing");
        }*/

        if (Input.GetMouseButtonDown(0))// && !EventSystem.current.IsPointerOverGameObject()) 
        {
            RaycastHit hitInfo;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out hitInfo, rayLength, layermask))
            {
                if (hitInfo.collider.gameObject.GetComponent<ResourceBehavior>() != null)
                {
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
            }
            
        }
    }

    public static void SetItemData(ItemData itemData)
    {
        ResourceBehavior.itemData = itemData;
    }

}