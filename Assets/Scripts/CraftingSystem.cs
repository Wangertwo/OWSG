using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CraftingSystem : MonoBehaviour
{
    public GameObject craftingScreenUI;
    public GameObject toolsScreenUI, surviveScreenUI, refineScreenUI, constructionScreenUI;

    public List<string> inventoryItemList = new List<string> ();

    //Category Buttons
    Button toolsBTN, surviveBTN, refineBTN, constructionBTN;

    //Craft Buttons
    Button craftAxeBTN;
    Button craftPlankBTN;
    Button craftWallBTN;
    Button craftFoundationBTN;

    //Requirement Text
    TextMeshProUGUI AxeReq1, AxeReq2;
    TextMeshProUGUI PlankReq1;
    TextMeshProUGUI WallReq1;
    TextMeshProUGUI FoundationReq1;

    public bool isOpen;

    //All Blueprints
    Blueprint axeBlueprint;
    Blueprint plankBlueprint;
    Blueprint wallBlueprint;
    Blueprint foundationBlueprint;

    public static CraftingSystem Instance { get; set; }


    private void Awake()
    {
        if (Instance !=null && Instance !=this)
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

        if (craftingScreenUI == null)
        {
            Debug.LogError("craftingScreenUI is NULL! Please assign in Inspector");
            return;
        }

        toolsBTN = craftingScreenUI.transform.Find("ToolsButton").GetComponent<Button> ();
        if (toolsBTN != null) 
        {
            toolsBTN.onClick.AddListener(delegate { OpenToolsCategory(); });
        }

        surviveBTN = craftingScreenUI.transform.Find("SurviveButton").GetComponent<Button>();
        if (surviveBTN != null)
        {
            surviveBTN.onClick.AddListener(delegate { OpenSurviveCategory(); });
        }

        refineBTN = craftingScreenUI.transform.Find("RefineButton").GetComponent<Button>();
        if (refineBTN != null)
        {
            refineBTN.onClick.AddListener(delegate { OpenRefineCategory(); });
        }

        constructionBTN = craftingScreenUI.transform.Find("ConstructionButton").GetComponent<Button>();
        if (constructionBTN != null)
        {
            constructionBTN.onClick.AddListener(delegate { OpenConstructionCategory(); });
        }

        // AXE
        AxeReq1 = toolsScreenUI.transform.Find("Axe").Find("req1").GetComponent<TextMeshProUGUI>();
        AxeReq2 = toolsScreenUI.transform.Find("Axe").Find("req2").GetComponent<TextMeshProUGUI>();

        craftAxeBTN = toolsScreenUI.transform.Find("Axe").Find("Button").GetComponent<Button>();
        if (craftAxeBTN != null)
        {
            craftAxeBTN.onClick.AddListener(delegate { CraftAxe(); });
        }

        // PLANK
        PlankReq1 = refineScreenUI.transform.Find("Plank").Find("req1").GetComponent<TextMeshProUGUI>();

        craftPlankBTN = refineScreenUI.transform.Find("Plank").Find("Button").GetComponent<Button>();
        if (craftPlankBTN != null)
        {
            craftPlankBTN.onClick.AddListener(delegate { CraftPlank(); });
        }

        // WALL
        WallReq1 = constructionScreenUI.transform.Find("Wall").Find("req1").GetComponent<TextMeshProUGUI>();

        craftWallBTN = constructionScreenUI.transform.Find("Wall").Find("Button").GetComponent<Button>();
        if (craftWallBTN != null)
        {
            craftWallBTN.onClick.AddListener(delegate { CraftWall(); });
        }

        // FOUNDATION
        FoundationReq1 = constructionScreenUI.transform.Find("Foundation").Find("req1").GetComponent<TextMeshProUGUI>();

        craftFoundationBTN = constructionScreenUI.transform.Find("Foundation").Find("Button").GetComponent<Button>();
        if (craftFoundationBTN != null)
        {
            craftFoundationBTN.onClick.AddListener(delegate { CraftFoundation(); });
        }

        // Blueprints
        axeBlueprint = new Blueprint("Axe", new List<Blueprint.Ingredient>
        {
            new Blueprint.Ingredient("Stone", 3),
            new Blueprint.Ingredient("Stick", 3)
        });

        plankBlueprint = new Blueprint("Plank", new List<Blueprint.Ingredient>
        {
            new Blueprint.Ingredient("Log", 1)
        });

        wallBlueprint = new Blueprint("Wall", new List<Blueprint.Ingredient>
        {
            new Blueprint.Ingredient("Plank", 4)
        });

        foundationBlueprint = new Blueprint("Foundation", new List<Blueprint.Ingredient>
        {
            new Blueprint.Ingredient("Plank", 2)
        });

        RefreshNeededItems();
    }

    void OpenToolsCategory() => ShowCategory(toolsScreenUI);
    void OpenSurviveCategory() => ShowCategory(surviveScreenUI);
    void OpenRefineCategory() => ShowCategory(refineScreenUI);
    void OpenConstructionCategory() => ShowCategory(constructionScreenUI);

    void ShowCategory(GameObject categoryScreen)
    {
        craftingScreenUI.SetActive(false);
        toolsScreenUI.SetActive(false);
        surviveScreenUI.SetActive(false);
        refineScreenUI.SetActive(false);
        constructionScreenUI.SetActive(false);

        if (categoryScreen != null)
        {
            categoryScreen.SetActive(true);
        }
    }

    void CraftAxe()
    {
        if (!CanCraftBlueprint(axeBlueprint, 1, out string error))
        {
            Debug.LogWarning(error);
            RefreshNeededItems();
            return;
        }

        CraftItem(axeBlueprint, 1);
    }

    void CraftPlank()
    {
        if (!CanCraftBlueprint(plankBlueprint, 2, out string error))
        {
            Debug.LogWarning(error);
            RefreshNeededItems();
            return;
        }

        CraftItem(plankBlueprint, 2);
    }

    void CraftWall()
    {
        if (!CanCraftBlueprint(wallBlueprint, 1, out string error))
        {
            Debug.LogWarning(error);
            RefreshNeededItems();
            return;
        }

        CraftItem(wallBlueprint, 1);
    }

    void CraftFoundation()
    {
        if (!CanCraftBlueprint(foundationBlueprint, 1, out string error))
        {
            Debug.LogWarning(error);
            RefreshNeededItems();
            return;
        }

        CraftItem(foundationBlueprint, 1);
    }

    void CraftItem(Blueprint blueprint, int outputCount)
    {
        foreach (Blueprint.Ingredient ingredient in blueprint.Ingredients)
        {
            InventorySystem.Instance.RemoveItem(ingredient.ItemName, ingredient.Amount);
        }

        InventorySystem.Instance.AddToInventory(blueprint.ResultItemName, outputCount);

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayCraftingSound();
        }

        StartCoroutine(Calculate());
    }

    bool CanCraftBlueprint(Blueprint blueprint, int outputCount, out string errorMessage)
    {
        errorMessage = "";

        if (blueprint == null)
        {
            errorMessage = "Blueprint is missing";
            return false;
        }

        int requiredSlots = GetRequiredFreeSlots(blueprint, outputCount);

        if (!blueprint.CanCraft(InventorySystem.Instance))
        {
            errorMessage = $"Not enough resources to craft {blueprint.ResultItemName}";
            return false;
        }

        if (!InventorySystem.Instance.HasFreeSlots(requiredSlots))
        {
            errorMessage = $"Not enough inventory slots to craft {blueprint.ResultItemName}";
            return false;
        }

        return true;
    }

    public IEnumerator Calculate()
    {
        yield return new WaitForSeconds(0.1f);
        InventorySystem.Instance.ReCalculateList();
        RefreshNeededItems();
    }

    public void RefreshNeededItems()
    {
        int stoneCount = GetItemCount("Stone");
        int stickCount = GetItemCount("Stick");
        int logCount = GetItemCount("Log");
        int plankCount = GetItemCount("Plank");

        if (AxeReq1 != null) AxeReq1.text = $"3 Stone [{stoneCount}]";
        if (AxeReq2 != null) AxeReq2.text = $"3 Stick [{stickCount}]";
        if (PlankReq1 != null) PlankReq1.text = $"1 Log [{logCount}]";
        if (WallReq1 != null) WallReq1.text = $"4 Plank [{plankCount}]";
        if (FoundationReq1 != null) FoundationReq1.text = $"2 Plank [{plankCount}]";

        UpdateCraftButtonState();
    }

    int GetItemCount(string itemName)
    {
        int count = 0;
        foreach (string item in InventorySystem.Instance.itemList)
        {
            if (item == itemName)
            {
                count++;
            }
        }
        return count;
    }

    void UpdateCraftButtonState()
    {
        UpdateButtonState(craftAxeBTN, CanCraftBlueprint(axeBlueprint, 1, out _));
        UpdateButtonState(craftPlankBTN, CanCraftBlueprint(plankBlueprint, 2, out _));
        UpdateButtonState(craftWallBTN, CanCraftBlueprint(wallBlueprint, 1, out _));
        UpdateButtonState(craftFoundationBTN, CanCraftBlueprint(foundationBlueprint, 1, out _));
    }

    void UpdateButtonState(Button button, bool canCraft)
    {
        if (button == null) return;

        button.interactable = canCraft;
        button.gameObject.SetActive(canCraft);

        if (canCraft && button.transform.parent != null)
        {
            button.transform.parent.gameObject.SetActive(true);
        }
    }

    int GetRequiredFreeSlots(Blueprint blueprint, int outputCount)
    {
        if (blueprint == null)
        {
            return outputCount;
        }

        int ingredientCount = 0;
        foreach (Blueprint.Ingredient ingredient in blueprint.Ingredients)
        {
            ingredientCount += ingredient.Amount;
        }

        return Mathf.Max(0, outputCount - ingredientCount);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C) && !isOpen)
        {
            craftingScreenUI.SetActive(true);
            toolsScreenUI.SetActive(false);
            surviveScreenUI.SetActive(false);
            refineScreenUI.SetActive(false);
            constructionScreenUI.SetActive(false);
            
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SelectionManager.Instance.DisableSelection();
            SelectionManager.Instance.GetComponent<SelectionManager>().enabled = false;
            isOpen = true;
            RefreshNeededItems();
        }
        else if (Input.GetKeyDown(KeyCode.C) && isOpen)
        {
            craftingScreenUI.SetActive(false);
            toolsScreenUI.SetActive(false);
            surviveScreenUI.SetActive(false);
            refineScreenUI.SetActive(false);
            constructionScreenUI.SetActive(false);
            if (!InventorySystem.Instance.isOpen) {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

                SelectionManager.Instance.EnableSelection();
                SelectionManager.Instance.GetComponent<SelectionManager>().enabled = true;
            }
            isOpen = false;
        }
        else if (isOpen)
        {
            RefreshNeededItems();
        }
    }
}
