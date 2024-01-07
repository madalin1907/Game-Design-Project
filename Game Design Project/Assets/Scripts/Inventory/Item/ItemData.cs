using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct ItemSlot {
    public int id;
    public int count;

    public ItemSlot(int id, int count) {
        this.id = id;
        this.count = count;
    }
}

public enum ItemType {
    TOOL,
    WEAPON,
    ARMOR,
    CONSUMABLE,
    MATERIAL,
    MISC
}

public enum ItemStack {
    SINGLE = 1,
    STACK_64 = 64
}

[CreateAssetMenu()]
public class ItemData : ScriptableObject {

    private Dictionary<int, string> mapIdToName = new Dictionary<int, string>();
    private Dictionary<string, int> mapNameToId = new Dictionary<string, int>();

    public ItemInfo[] items;

    [Serializable]
    public struct ItemInfo {
        private int _id;

        public string _name;
        public ItemStack _maxStack;
        public ItemType _type;
        public Sprite _icon;

        public void SetId(int id) {
            _id = id;
        }

        public int GetId() {
            return _id;
        }
    }

    public void OnValidate() {
        for (int i = 0; i < items.Length; i++) {
            items[i].SetId(i);
        }

        mapIdToName.Clear();
        mapNameToId.Clear();

        for (int i = 0; i < items.Length; i++) {
            mapIdToName.Add(items[i].GetId(), items[i]._name);
            mapNameToId.Add(items[i]._name, items[i].GetId());
        }
    }

    public string GetNameFromId(int id) {
        if (mapIdToName.ContainsKey(id))
            return mapIdToName[id];
        return "";
    }

    public int GetIdFromName(string name) {
        if (mapNameToId.ContainsKey(name))
            return mapNameToId[name];
        return -1;
    }
}
