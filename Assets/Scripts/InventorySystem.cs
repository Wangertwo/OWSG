using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

 
public class InventorySystem : MonoBehaviour
{
 
   public static InventorySystem Instance { get; set; }
 
    public GameObject inventoryScreenUI;

    public List<GameObject> slotList = new List<GameObject>();

    public List<string> itemList = new List<string>();

    private GameObject itemToAdd;

    private GameObject whatSlotToEquip;

    public bool isOpen;

    public GameObject PickupAlert;
    public TextMeshProUGUI PickupName;
    public Image PickupImage;
    public float PickupAlertDuration = 1.5f;
    public GameObject ItemInfoUI;

    private void PopulateSlotList()
    {
        foreach (Transform child in inventoryScreenUI.transform)
        {
            if (child.CompareTag("Slot"))
            {
                slotList.Add(child.gameObject);
            }
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }
 
 
    void Start()
    {
        isOpen = false;
        PopulateSlotList();

        Cursor.visible = false;
    }
 
 
    void Update()
    {
 
        if (Input.GetKeyDown(KeyCode.I) && !isOpen && !ConstructionManager.Instance.inConstructionMode)
        {
 
		    Debug.Log("i is pressed");
            inventoryScreenUI.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SelectionManager.Instance.DisableSelection();
            SelectionManager.Instance.GetComponent<SelectionManager>().enabled = false;
            isOpen = true;
 
        }
        else if (Input.GetKeyDown(KeyCode.I) && isOpen)
        {
            inventoryScreenUI.SetActive(false);
            if (!CraftingSystem.Instance.isOpen)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                SelectionManager.Instance.EnableSelection();
                SelectionManager.Instance.GetComponent<SelectionManager>().enabled = true;
            }
            isOpen = false;
        }
    }
 
    public void AddToInventory(string itemName, int amount = 1)
    {
        if (amount <= 0)
        {
            return;
        }

        for (int i = 0; i < amount; i++)
        {
            whatSlotToEquip = FindNextEmptySlot();
            if (whatSlotToEquip == null)
            {
                Debug.LogWarning("No empty inventory slots available for " + itemName);
                return;
            }

            itemToAdd = Instantiate(Resources.Load<GameObject>(itemName), whatSlotToEquip.transform.position, whatSlotToEquip.transform.rotation);
            itemToAdd.transform.SetParent(whatSlotToEquip.transform);

            itemList.Add(itemName);

            if (PickupAlert != null && PickupName != null && PickupImage != null)
            {
                Image itemImage = itemToAdd.GetComponent<Image>();
                if (itemImage != null)
                {
                    TriggerPickupAlert(itemName, itemImage.sprite);
                }
            }
        }
    }

    public void RemoveItem(string itemName, int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        int removedCount = 0;

        foreach (GameObject slot in slotList)
        {
            if (removedCount >= amount)
            {
                break;
            }

            if (slot.transform.childCount == 0)
            {
                continue;
            }

            Transform child = slot.transform.GetChild(0);
            string childName = child.gameObject.name.Replace("(Clone)", "").Trim();
            if (childName != itemName)
            {
                continue;
            }

            child.SetParent(null);
            Destroy(child.gameObject);
            itemList.Remove(itemName);
            removedCount++;
        }

    }

    public void ReCalculateList()
    {
        itemList.Clear();

        foreach (GameObject slot in slotList)
        {
            if (slot.transform.childCount == 0)
            {
                continue;
            }

            Transform child = slot.transform.GetChild(0);
            string childName = child.gameObject.name.Replace("(Clone)", "").Trim();
            itemList.Add(childName);
        }

    }

    private GameObject FindNextEmptySlot()
    {
        foreach (GameObject slot in slotList)
        {
            if (slot.transform.childCount == 0)
            {
                return slot;
            }
        }
        return null;
    }

    public bool CheckIfFull()
    {
        if (itemList.Count >= slotList.Count)
        {
            return true;
        }
        else
        {
            return false;
        }
    }   

    public int GetFreeSlotCount()
    {
        int freeSlots = 0;
        foreach (GameObject slot in slotList)
        {
            if (slot.transform.childCount == 0)
            {
                freeSlots++;
            }
        }

        return freeSlots;
    }

    public bool HasFreeSlots(int requiredSlots)
    {
        if (requiredSlots <= 0)
        {
            return true;
        }

        return GetFreeSlotCount() >= requiredSlots;
    }

    public void TriggerPickupAlert(string itemName, Sprite itemSprite)
    {
        PickupName.text = itemName;
        PickupImage.sprite = itemSprite;
        PickupAlert.SetActive(true);
        StartCoroutine(HidePickupAlert());
    }

    private IEnumerator HidePickupAlert()
    {
        yield return new WaitForSeconds(PickupAlertDuration);
        if (PickupAlert != null)
        {
            PickupAlert.SetActive(false);
        }
    }
}
