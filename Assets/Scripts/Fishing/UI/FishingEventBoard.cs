using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FishingEventBoard : MonoBehaviour
{
    public static FishingEventBoard Instance { get; private set; }

    [Header("Display")]
    [SerializeField] private GameObject boardPanel;
    [SerializeField] private bool hideOnStart = true;
    [SerializeField] private KeyCode toggleKey = KeyCode.B;
    [SerializeField] private bool autoShowOnNewEvent = true;
    [SerializeField] private float autoHideSeconds = 8f;

    [Header("Content")]
    [SerializeField] private TextMeshProUGUI boardText;
    [SerializeField] private int maxEntries = 5;

    private readonly Queue<string> entries = new Queue<string>();
    private float autoHideAt;
    private bool isVisible;
    private CanvasGroup cachedCanvasGroup;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (hideOnStart)
        {
            SetVisible(false);
        }
        else
        {
            SetVisible(true);
        }

        RefreshView();
    }

    private void Update()
    {
        if (isVisible && autoHideAt > 0f && Time.unscaledTime >= autoHideAt)
        {
            SetVisible(false);
            autoHideAt = 0f;
        }
    }

    public void AddEvent(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        entries.Enqueue(text.Trim());
        while (entries.Count > Mathf.Max(1, maxEntries))
        {
            entries.Dequeue();
        }

        RefreshView();

        if (autoShowOnNewEvent)
        {
            ShowTemporarily(autoHideSeconds);
        }
    }

    public static void ProcessGlobalToggleInput()
    {
        FishingEventBoard[] boards = Resources.FindObjectsOfTypeAll<FishingEventBoard>();
        for (int i = 0; i < boards.Length; i++)
        {
            FishingEventBoard board = boards[i];
            if (board == null || !board.IsSceneObject())
            {
                continue;
            }

            if (board.toggleKey == KeyCode.None)
            {
                continue;
            }

            if (Input.GetKeyDown(board.toggleKey))
            {
                board.Toggle();
            }
        }
    }

    public static void PostEvent(string text)
    {
        FishingEventBoard board = Instance != null ? Instance : FindAnyBoard();
        if (board != null)
        {
            board.AddEvent(text);
        }
    }

    public static void PostRumor(string text)
    {
        FishingEventBoard board = Instance != null ? Instance : FindAnyBoard();
        if (board != null)
        {
            board.ReplaceWithEvent(text);
        }
    }

    public void ReplaceWithEvent(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        entries.Clear();
        AddEvent(text);
    }

    public void RefreshView()
    {
        if (boardText == null)
        {
            return;
        }

        if (entries.Count == 0)
        {
            boardText.text = "今日暂无渔闻。";
            return;
        }

        boardText.text = string.Join("\n", entries.ToArray());
    }

    public void Toggle()
    {
        SetVisible(!isVisible);
        autoHideAt = 0f;
    }

    public void ShowTemporarily(float seconds)
    {
        SetVisible(true);

        if (seconds > 0f)
        {
            autoHideAt = Time.unscaledTime + seconds;
        }
        else
        {
            autoHideAt = 0f;
        }
    }

    public void SetVisible(bool visible)
    {
        isVisible = visible;
        if (boardPanel != null)
        {
            if (boardPanel == gameObject)
            {
                if (visible)
                {
                    if (!gameObject.activeSelf)
                    {
                        gameObject.SetActive(true);
                    }

                    ApplySelfPanelVisibility(true);
                }
                else
                {
                    ApplySelfPanelVisibility(false);
                    if (gameObject.activeSelf)
                    {
                        gameObject.SetActive(false);
                    }
                }
            }
            else
            {
                boardPanel.SetActive(visible);
            }

            return;
        }

        if (boardText != null)
        {
            boardText.gameObject.SetActive(visible);
        }
    }

    private void ApplySelfPanelVisibility(bool visible)
    {
        if (cachedCanvasGroup == null)
        {
            cachedCanvasGroup = GetComponent<CanvasGroup>();
        }

        if (cachedCanvasGroup != null)
        {
            cachedCanvasGroup.alpha = visible ? 1f : 0f;
            cachedCanvasGroup.interactable = visible;
            cachedCanvasGroup.blocksRaycasts = visible;
            if (boardText != null)
            {
                boardText.gameObject.SetActive(visible);
            }

            return;
        }

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child != null)
            {
                child.gameObject.SetActive(visible);
            }
        }

        if (boardText != null)
        {
            boardText.gameObject.SetActive(visible);
        }
    }

    private bool IsSceneObject()
    {
        return gameObject.scene.IsValid() && (hideFlags & HideFlags.DontSave) == 0;
    }

    private static FishingEventBoard FindAnyBoard()
    {
        FishingEventBoard[] boards = Resources.FindObjectsOfTypeAll<FishingEventBoard>();
        for (int i = 0; i < boards.Length; i++)
        {
            if (boards[i] != null && boards[i].IsSceneObject())
            {
                return boards[i];
            }
        }

        return null;
    }
}
