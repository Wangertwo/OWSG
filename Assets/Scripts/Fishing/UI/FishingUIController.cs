using System.Collections;
using TMPro;
using UnityEngine;

public class FishingUIController : MonoBehaviour
{
    public static FishingUIController Instance { get; private set; }

    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI lakeNameText;
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private TextMeshProUGUI questText;
    [Min(0.1f)]
    [SerializeField] private float autoHideSeconds = 3f;

    public bool IsVisible => panel != null && panel.activeSelf;
    public static bool IsPanelVisible => Instance != null && Instance.IsVisible;
    private Coroutine autoHideRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void SetVisible(bool visible)
    {
        if (autoHideRoutine != null)
        {
            StopCoroutine(autoHideRoutine);
            autoHideRoutine = null;
        }

        if (panel != null)
        {
            panel.SetActive(visible);
        }

        if (visible)
        {
            autoHideRoutine = StartCoroutine(HideAfterDelay());
        }
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(autoHideSeconds);

        if (panel != null)
        {
            panel.SetActive(false);
        }

        autoHideRoutine = null;
    }

    public void ShowLake(string lakeName)
    {
        if (lakeNameText != null)
        {
            lakeNameText.text = string.IsNullOrWhiteSpace(lakeName) ? "湖区: -" : "湖区: " + lakeName;
        }
    }

    public void ShowResult(string text)
    {
        if (resultText != null)
        {
            resultText.text = string.IsNullOrWhiteSpace(text) ? "结果: -" : "结果: " + text;
        }
    }

    public void ShowPriceHint(string text)
    {
        if (priceText != null)
        {
            priceText.text = string.IsNullOrWhiteSpace(text) ? "鱼价: -" : "鱼价: " + text;
        }
    }

    public void ShowQuestHint(string text)
    {
        if (questText != null)
        {
            questText.text = string.IsNullOrWhiteSpace(text) ? "委托: -" : "委托: " + text;
        }
    }
}
