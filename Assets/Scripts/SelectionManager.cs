
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
 
public class SelectionManager : MonoBehaviour
{

    public static SelectionManager Instance { get; set; }
 
    public GameObject interaction_Info_UI;

    public bool onTarget;
    TextMeshProUGUI interaction_text;

    public GameObject selectedObject;

    public GameObject handIcon;
    public GameObject centerDotImage;
    public bool handIsVisible;

    public GameObject selectedTree;
    public GameObject chopHolder;
 
    private void Start()
    {
        onTarget = false;
        interaction_text = interaction_Info_UI.GetComponent<TextMeshProUGUI>();
    }

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

    void Update()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            var selectionTransform = hit.transform;
            InteractableObject interactableObject = selectionTransform.GetComponent<InteractableObject>();
            
            ChoppableTree choppableTree = selectionTransform.GetComponent<ChoppableTree>();

            if (choppableTree && choppableTree.playerInRange)
            {
                choppableTree.canBeChopped = true;
                selectedTree = choppableTree.gameObject;
                chopHolder.gameObject.SetActive(true);
            }
            else
            {
                if (selectedTree != null)
                {
                    selectedTree.GetComponent<ChoppableTree>().canBeChopped = false;
                    selectedTree = null;
                    chopHolder.gameObject.SetActive(false);
                }
            }
            
            if (interactableObject && interactableObject.PlayerInRange)
            {
                onTarget = true;
                selectedObject = interactableObject.gameObject;
                interaction_text.text = interactableObject.GetItemName();
                interaction_Info_UI.SetActive(true);

                if (interactableObject.CompareTag("pickable"))
                {
                    handIcon.SetActive(true);
                    centerDotImage.SetActive(false);
                    handIsVisible = true;
                }
                else
                {
                    handIcon.SetActive(false);
                    centerDotImage.SetActive(true);
                    handIsVisible = false;
                }
            }
            else 
            { 
                onTarget = false;
                interaction_Info_UI.SetActive(false);
                handIcon.SetActive(false);
                centerDotImage.SetActive(true);
                handIsVisible = false;
            }
 
        }
        else 
        { 
            onTarget = false;
            interaction_Info_UI.SetActive(false);
            handIcon.SetActive(false);
            centerDotImage.SetActive(true);
            handIsVisible = false;
        }
    }

    public void EnableSelection()
    {
        handIcon.SetActive(true);
        centerDotImage.SetActive(true);
        interaction_Info_UI.SetActive(true);
    }

    public void DisableSelection()
    {
        handIcon.SetActive(false);
        centerDotImage.SetActive(false);
        interaction_Info_UI.SetActive(false);
        selectedObject = null;
    }
}
