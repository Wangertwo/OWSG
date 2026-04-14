using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ChatPanelController : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button sendButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private ChatMessageView messageView;
    [SerializeField] private ChatStatusView statusView;
    [SerializeField] private bool submitOnEnter = true;
    [SerializeField] private bool clearInputOnSend = true;

    public event Action<string> SendRequested;
    public event Action CloseRequested;

    public ChatStatusView StatusView => statusView;
    public bool IsOpen => GetPanelRoot().activeSelf;

    private void Awake()
    {
        if (sendButton != null)
        {
            sendButton.onClick.AddListener(HandleSendClick);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(HandleCloseClick);
        }
    }

    private void OnDestroy()
    {
        if (sendButton != null)
        {
            sendButton.onClick.RemoveListener(HandleSendClick);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(HandleCloseClick);
        }
    }

    private void Update()
    {
        if (!submitOnEnter || !IsOpen)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            HandleSendClick();
        }
    }

    public void ShowPanel()
    {
        GetPanelRoot().SetActive(true);
        StartCoroutine(FocusInputNextFrame());
    }

    public void HidePanel()
    {
        GetPanelRoot().SetActive(false);
    }

    public void FocusInput()
    {
        if (inputField == null)
        {
            return;
        }

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(inputField.gameObject);
        }

        inputField.ActivateInputField();
        inputField.Select();
    }

    public void SetInputLocked(bool isLocked)
    {
        if (inputField != null)
        {
            inputField.interactable = !isLocked;
        }

        if (sendButton != null)
        {
            sendButton.interactable = !isLocked;
        }
    }

    public void SetSendInteractable(bool isInteractable)
    {
        if (sendButton != null)
        {
            sendButton.interactable = isInteractable;
        }

        if (inputField != null)
        {
            inputField.interactable = isInteractable;
        }
    }

    public void AppendPlayerMessage(string message)
    {
        if (messageView == null)
        {
            return;
        }

        messageView.AppendPlayerMessage(message);
    }

    public void AppendAssistantMessage(string message)
    {
        if (messageView == null)
        {
            return;
        }

        messageView.AppendAssistantMessage(message);
    }

    public void AppendSystemMessage(string message)
    {
        if (messageView == null)
        {
            return;
        }

        messageView.AppendSystemMessage(message);
    }

    public void ClearMessages()
    {
        if (messageView == null)
        {
            return;
        }

        messageView.Clear();
    }

    private void HandleSendClick()
    {
        string text = inputField == null ? string.Empty : inputField.text;
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        SendRequested?.Invoke(text.Trim());

        if (!clearInputOnSend || inputField == null)
        {
            return;
        }

        inputField.text = string.Empty;
        FocusInput();
    }

    private void HandleCloseClick()
    {
        CloseRequested?.Invoke();
    }

    private GameObject GetPanelRoot()
    {
        if (panelRoot != null)
        {
            return panelRoot;
        }

        return gameObject;
    }

    private IEnumerator FocusInputNextFrame()
    {
        yield return null;
        FocusInput();
    }
}
