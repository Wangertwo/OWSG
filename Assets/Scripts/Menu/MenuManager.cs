using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuManager : MonoBehaviour
{
    public GameObject menuCanvas;
    public GameObject uiCanvas;

    public GameObject menu;
    public GameObject settingMenu;
    public GameObject saveMenu;
    public bool isMenuOpen;

    public static MenuManager Instance { get; private set; }


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.M) && !isMenuOpen)
        {
            OpenMenu();
        }
        else if (Input.GetKeyDown(KeyCode.M) && isMenuOpen)
        {
            CloseMenu();
        }
    }

    private void OpenMenu()
    {
        menuCanvas.SetActive(true);
        uiCanvas.SetActive(false);
        isMenuOpen = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SelectionManager.Instance.DisableSelection();
        SelectionManager.Instance.GetComponent<SelectionManager>().enabled = false;
    }

    private void CloseMenu()
    {
        menuCanvas.SetActive(false);
        uiCanvas.SetActive(true);
        isMenuOpen = false;
        
        if (!InventorySystem.Instance.isOpen && !CraftingSystem.Instance.isOpen)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        SelectionManager.Instance.EnableSelection();
        SelectionManager.Instance.GetComponent<SelectionManager>().enabled = true;
    }

}
