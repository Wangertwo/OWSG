using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
 
public class EquipSystem : MonoBehaviour
{
    public static EquipSystem Instance { get; set; }
 
    // -- UI -- //
    public GameObject quickSlotsPanel;
 
    public List<GameObject> quickSlotsList = new List<GameObject>();
    public GameObject numbersHolder;
 
    public int selectedNumber = -1;
    public GameObject selectedItem;
    public GameObject toolHolder;

    private GameObject currentEquippedModel;

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

    void Update()
    {
        for (int i = 1; i <= 7; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + (i - 1)))
            {
                HandleKeyInput(i);
                break;
            }
        }
    }
    void HandleKeyInput(int keyNumber)
    {
        switch (keyNumber)
        {
            case 1:
                SelectQuickSlot(1);
                break;
            case 2:
                SelectQuickSlot(2);
                break;
            case 3:
                SelectQuickSlot(3);
                break;
            case 4:
                SelectQuickSlot(4);
                break;
            case 5:
                SelectQuickSlot(5);
                break;
            case 6:
                SelectQuickSlot(6);
                break;
            case 7:
                SelectQuickSlot(7);
                break;
            default:
                Debug.Log("Unknown key");
                break;
        }
    }
    void SelectQuickSlot(int number)
    {
        if (checkIfSlotIsFull(number))
        {
            if (selectedNumber != number)
            {
                selectedNumber = number;

                if (selectedItem != null)
                {
                    selectedItem.GetComponent<InventoryItem>().isSelected = false;
                }

                GameObject slot = quickSlotsList[number - 1];
                if (slot.transform.childCount > 0)
                {
                    selectedItem = slot.transform.GetChild(0).gameObject;

                    InventoryItem itemComponent = selectedItem.GetComponent<InventoryItem>();
                    itemComponent.isSelected = true;

                    if (itemComponent.isEquipable)
                    {
                        SetEquippedModel(selectedItem);
                    }

                    foreach (Transform child in numbersHolder.transform)
                    {
                        Transform textTransform = child.transform.Find("Text");
                        if (textTransform == null)
                        {
                            continue;
                        }

                        TMP_Text textComponent = textTransform.GetComponent<TMP_Text>();
                        if (textComponent != null)
                        {
                            textComponent.color = Color.gray;
                        }
                    }

                    Transform numberTransform = numbersHolder.transform.Find("Number" + number);
                    if (numberTransform != null)
                    {
                        TMP_Text toBeChanged = numberTransform.transform.Find("Text").GetComponent<TMP_Text>();
                        if (toBeChanged != null)
                        {
                            toBeChanged.color = Color.white;
                        }
                    }
                }
            }
            else
            {
                selectedNumber = -1;
                selectedItem.GetComponent<InventoryItem>().isSelected = false;
                selectedItem = null;

                if (currentEquippedModel != null)
                {
                    DestroyImmediate(currentEquippedModel.gameObject);
                    currentEquippedModel = null;
                }

                foreach (Transform child in numbersHolder.transform)
                {
                    Transform textTransform = child.transform.Find("Text");
                    if (textTransform == null)
                    {
                        continue;
                    }

                    TMP_Text textComponent = textTransform.GetComponent<TMP_Text>();
                    if (textComponent != null)
                    {
                        textComponent.color = Color.gray;
                    }
                }
            }
        }
    }
    
    public void ClearSelection()
    {
        if (selectedItem != null)
        {
            selectedItem.GetComponent<InventoryItem>().isSelected = false;
            selectedItem = null;
        }
        
        selectedNumber = -1;
        
        if (currentEquippedModel != null)
        {
            DestroyImmediate(currentEquippedModel.gameObject);
            currentEquippedModel = null;
        }
        
        foreach (Transform child in numbersHolder.transform)
        {
            Transform textTransform = child.transform.Find("Text");
            if (textTransform == null) continue;
            
            TMP_Text textComponent = textTransform.GetComponent<TMP_Text>();
            if (textComponent != null)
            {
                textComponent.color = Color.gray;
            }
        }
    }

    private void SetEquippedModel(GameObject selectedItem)
    {
        if (toolHolder == null)
        {
            Debug.LogWarning("Tool holder is not assigned", this);
            return;
        }

        string selectedItemName = selectedItem.name.Replace("(Clone)", "").Trim();
        GameObject selectedObject = Resources.Load<GameObject>(selectedItemName + "_Model");
        if (selectedObject == null)
        {
            Debug.LogWarning("Equip model not found in Resources: " + selectedItemName + "_Model", this);
            return;
        }

        if (currentEquippedModel != null)
        {
            Destroy(currentEquippedModel);
        }

        currentEquippedModel = Instantiate(selectedObject, toolHolder.transform, false);
        currentEquippedModel.transform.SetParent(toolHolder.transform, false);
    }

    bool checkIfSlotIsFull(int number)
    {
        GameObject slot = quickSlotsList[number - 1];
        if (slot.transform.childCount > 0)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    private void Start()
    {
        PopulateSlotList();
    }
 
    private void PopulateSlotList()
    {
        foreach (Transform child in quickSlotsPanel.transform)
        {
            if (child.CompareTag("QuickSlot"))
            {
                quickSlotsList.Add(child.gameObject);
            }
        }
    }
 
    public void AddToQuickSlots(GameObject itemToEquip)
    {
        // Find next free slot
        GameObject availableSlot = FindNextEmptySlot();
        // Set transform of our object
        itemToEquip.transform.SetParent(availableSlot.transform, false);
        InventorySystem.Instance.ReCalculateList();
    }
 
 
    private GameObject FindNextEmptySlot()
    {
        foreach (GameObject slot in quickSlotsList)
        {
            if (slot.transform.childCount == 0)
            {
                return slot;
            }
        }
        return new GameObject();
    }
 
    public bool CheckIfFull()
    {
 
        int counter = 0;
 
        foreach (GameObject slot in quickSlotsList)
        {
            if (slot.transform.childCount > 0)
            {
                counter += 1;
            }
        }
 
        if (counter == 7)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}
