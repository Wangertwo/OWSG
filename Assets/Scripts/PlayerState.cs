using System.Collections;
using UnityEngine;

public class PlayerState : MonoBehaviour
{
    public static PlayerState Instance { get; set; }

    public float currentHealth;
    public float maxHealth;

    public float currentHydration;
    public float maxHydration;

    public float currentCalories;
    public float maxCalories;

    public float hydrationTickSeconds = 2f;
    public float hydrationLossPerTick = 1f;

    public float metersPerCalorieLoss = 5f;
    public float calorieLossPerStep = 0.5f;

    public Transform trackedTransform;

    private Vector3 lastPosition;
    private float distanceAccumulator;

    private Coroutine hydrationRoutine;

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
    // Update is called once per frame

    private void Start()
    {
        InitializeVitals();
        InitializeTracking();
        StartHydrationDrain();
    }

    private void Update()
    {
        UpdateCaloriesByMovement();
    }

    private void InitializeVitals()
    {
        currentHealth = maxHealth;
        currentHydration = maxHydration;
        currentCalories = maxCalories;
    }

    private void OnDisable()
    {
        StopHydrationDrain();
    }

    private void InitializeTracking()
    {
        lastPosition = GetTrackedPosition();
        distanceAccumulator = 0f;
    }

    private void UpdateCaloriesByMovement()
    {
        if (metersPerCalorieLoss <= 0f || calorieLossPerStep <= 0f)
        {
            lastPosition = GetTrackedPosition();
            return;
        }

        Vector3 currentPosition = GetTrackedPosition();
        float frameDistance = Vector3.Distance(currentPosition, lastPosition);
        if (frameDistance <= 0f)
        {
            return;
        }

        distanceAccumulator += frameDistance;
        lastPosition = currentPosition;

        if (distanceAccumulator < metersPerCalorieLoss)
        {
            return;
        }

        int steps = Mathf.FloorToInt(distanceAccumulator / metersPerCalorieLoss);
        float totalLoss = steps * calorieLossPerStep;
        currentCalories = Mathf.Max(0f, currentCalories - totalLoss);
        distanceAccumulator -= steps * metersPerCalorieLoss;
    }

    private Vector3 GetTrackedPosition()
    {
        if (trackedTransform != null)
        {
            return trackedTransform.position;
        }

        return transform.position;
    }

    private void StartHydrationDrain()
    {
        if (hydrationRoutine != null)
        {
            StopCoroutine(hydrationRoutine);
        }

        hydrationRoutine = StartCoroutine(HydrationDrainLoop());
    }

    private void StopHydrationDrain()
    {
        if (hydrationRoutine != null)
        {
            StopCoroutine(hydrationRoutine);
            hydrationRoutine = null;
        }
    }

    private IEnumerator HydrationDrainLoop()
    {
        while (true)
        {
            if (hydrationTickSeconds > 0f && hydrationLossPerTick > 0f)
            {
                yield return new WaitForSeconds(hydrationTickSeconds);
                currentHydration = Mathf.Max(0f, currentHydration - hydrationLossPerTick);
            }
            else
            {
                yield return null;
            }
        }
    }

    public void setHealth(float value)
    {
        currentHealth = Mathf.Clamp(value, 0f, maxHealth);
    }

    public void setCalories(float value)
    {
        currentCalories = Mathf.Clamp(value, 0f, maxCalories);
    }

    public bool TrySpendCaloriesForAction(float amount)
    {
        if (amount <= 0f)
        {
            return true;
        }

        if (currentCalories <= 0f)
        {
            return false;
        }

        float remainingCalories = Mathf.Max(0f, currentCalories - amount);
        setCalories(remainingCalories);
        return true;
    }

    public void setHydration(float value)
    {
        currentHydration = Mathf.Clamp(value, 0f, maxHydration);
    }
}
