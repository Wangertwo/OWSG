using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class FishingRod : MonoBehaviour
{
    public event Action<FishingArea> CastStarted;
    public event Action PullTriggered;

    [Header("State")]
    public bool isEquipped = true;
    public bool isFishingAvailable;
    public bool isCasted;
    public bool isPulling;

    [Header("Casting")]
    [SerializeField] private string fishingAreaTag = "FishingArea";
    [SerializeField] private float maxCastDistance = 300f;
    [SerializeField] private LayerMask castMask = Physics.DefaultRaycastLayers;
    [SerializeField] private float baitSpawnDelay = 1f;
    [SerializeField] private float pullRecoverDuration = 0.8f;
    [SerializeField] private float baitSurfaceOffset = 0.05f;
    [SerializeField] private float targetCheckInterval = 0.1f;
    [SerializeField] private Camera playerCamera;

    [Header("References")]
    public GameObject baitPrefab;
    public GameObject endof_of_rope;
    public GameObject start_of_rope;
    public GameObject start_of_rod;
    [SerializeField] private LineRenderer fishingLine;

    private Animator animator;
    private Transform baitPosition;
    private Vector3 pendingCastPoint;
    private Coroutine baitSpawnRoutine;
    private Coroutine pullRoutine;
    private bool hasSpawnedBaitThisCast;
    private bool warnedMissingRopeReferences;
    private bool warnedMissingBaitPrefab;
    private readonly RaycastHit[] raycastHitsBuffer = new RaycastHit[64];
    private GameObject baitInstance;
    private WaitForSeconds castDelayYield;
    private WaitForSeconds pullDelayYield;
    private float nextTargetCheckTime;
    private Vector3 cachedTargetPoint;
    private bool hasCachedFishingTarget;
    private Collider cachedFishingAreaCollider;

    public FishingArea ActiveFishingArea { get; private set; }

    private static readonly int CastTrigger = Animator.StringToHash("Cast");
    private static readonly int PullTrigger = Animator.StringToHash("Pull");

    private void Awake()
    {
        animator = GetComponent<Animator>();
        RefreshDelayYields();
        RemoveInvalidAnimationEvents();

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        if (fishingLine == null && start_of_rope != null)
        {
            fishingLine = start_of_rope.GetComponent<LineRenderer>();
        }

        if (fishingLine != null)
        {
            fishingLine.positionCount = 2;
            fishingLine.enabled = false;
        }
    }

    private void OnValidate()
    {
        if (baitSpawnDelay < 0f)
        {
            baitSpawnDelay = 0f;
        }

        if (pullRecoverDuration < 0f)
        {
            pullRecoverDuration = 0f;
        }

        if (maxCastDistance < 1f)
        {
            maxCastDistance = 1f;
        }

        if (targetCheckInterval < 0.02f)
        {
            targetCheckInterval = 0.02f;
        }

        if (baitSurfaceOffset < 0f)
        {
            baitSurfaceOffset = 0f;
        }

        RefreshDelayYields();
    }

    private void OnEnable()
    {
        isEquipped = true;
        hasCachedFishingTarget = false;
        cachedFishingAreaCollider = null;
        nextTargetCheckTime = 0f;
    }

    private void Update()
    {
        if (!isEquipped)
        {
            return;
        }

        if (!IsFishingRodSelected())
        {
            if (isCasted || isPulling)
            {
                CompletePull();
                isCasted = false;
            }

            isFishingAvailable = false;
            hasCachedFishingTarget = false;
            return;
        }

        bool canProcessInput = CanProcessInput();
        if (!canProcessInput)
        {
            isFishingAvailable = false;
            hasCachedFishingTarget = false;
            return;
        }

        if (Time.unscaledTime >= nextTargetCheckTime)
        {
            hasCachedFishingTarget = TryGetFishingTarget(out cachedTargetPoint, out cachedFishingAreaCollider);
            isFishingAvailable = hasCachedFishingTarget;
            nextTargetCheckTime = Time.unscaledTime + targetCheckInterval;
        }

        if (isFishingAvailable && Input.GetMouseButtonDown(0) && !isCasted && !isPulling)
        {
            if (!hasCachedFishingTarget)
            {
                hasCachedFishingTarget = TryGetFishingTarget(out cachedTargetPoint, out cachedFishingAreaCollider);
                isFishingAvailable = hasCachedFishingTarget;
            }

            if (hasCachedFishingTarget)
            {
                StartCast(cachedTargetPoint, cachedFishingAreaCollider);
            }
        }

        if (isCasted && !isPulling && canProcessInput && Input.GetMouseButtonDown(1))
        {
            PullRod();
        }

        UpdateRopeEndpoints();
    }

    private bool CanProcessInput()
    {
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            return false;
        }

        if (InventorySystem.Instance != null && InventorySystem.Instance.isOpen)
        {
            return false;
        }

        if (CraftingSystem.Instance != null && CraftingSystem.Instance.isOpen)
        {
            return false;
        }

        if (MenuManager.Instance != null && MenuManager.Instance.isMenuOpen)
        {
            return false;
        }

        if (ConstructionManager.Instance != null && ConstructionManager.Instance.inConstructionMode)
        {
            return false;
        }

        return true;
    }

    private bool TryGetFishingTarget(out Vector3 targetPoint, out Collider fishingAreaCollider)
    {
        Camera activeCamera = playerCamera != null ? playerCamera : Camera.main;
        if (activeCamera == null)
        {
            targetPoint = default;
            fishingAreaCollider = null;
            return false;
        }

        Vector3 screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
        Ray ray = activeCamera.ScreenPointToRay(screenCenter);

        int hitCount = Physics.RaycastNonAlloc(
            ray,
            raycastHitsBuffer,
            maxCastDistance,
            castMask,
            QueryTriggerInteraction.Collide);

        if (hitCount <= 0)
        {
            targetPoint = default;
            fishingAreaCollider = null;
            return false;
        }

        if (TryFindClosestFishingArea(raycastHitsBuffer, hitCount, out Vector3 bestPoint, out Collider bestCollider))
        {
            targetPoint = bestPoint;
            fishingAreaCollider = bestCollider;
            return true;
        }

        if (hitCount >= raycastHitsBuffer.Length)
        {
            RaycastHit[] fallbackHits = Physics.RaycastAll(ray, maxCastDistance, castMask, QueryTriggerInteraction.Collide);
            if (fallbackHits != null && fallbackHits.Length > 0 && TryFindClosestFishingArea(fallbackHits, fallbackHits.Length, out bestPoint, out bestCollider))
            {
                targetPoint = bestPoint;
                fishingAreaCollider = bestCollider;
                return true;
            }
        }

        targetPoint = default;
        fishingAreaCollider = null;
        return false;
    }

    private bool TryFindClosestFishingArea(RaycastHit[] hits, int count, out Vector3 point, out Collider collider)
    {
        float closestDistance = float.MaxValue;
        bool found = false;
        Vector3 bestPoint = default;
        Collider bestCollider = null;

        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.collider != null && hit.collider.CompareTag(fishingAreaTag) && hit.distance < closestDistance)
            {
                closestDistance = hit.distance;
                bestPoint = hit.point;
                bestCollider = hit.collider;
                found = true;
            }
        }

        point = bestPoint;
        collider = bestCollider;
        return found;
    }

    private void StartCast(Vector3 targetPoint, Collider targetCollider)
    {
        isCasted = true;
        isPulling = false;
        pendingCastPoint = ResolveBaitSpawnPoint(targetPoint, targetCollider);
        ActiveFishingArea = ResolveFishingArea(targetCollider);
        CastStarted?.Invoke(ActiveFishingArea);
        hasSpawnedBaitThisCast = false;

        if (pullRoutine != null)
        {
            StopCoroutine(pullRoutine);
            pullRoutine = null;
        }

        animator.ResetTrigger(PullTrigger);
        animator.SetTrigger(CastTrigger);

        if (baitSpawnRoutine != null)
        {
            StopCoroutine(baitSpawnRoutine);
        }

        baitSpawnRoutine = StartCoroutine(SpawnBaitAfterDelay());
    }

    private IEnumerator SpawnBaitAfterDelay()
    {
        yield return castDelayYield;

        if (!isCasted || hasSpawnedBaitThisCast)
        {
            yield break;
        }

        SpawnBait(pendingCastPoint);
    }

    private void SpawnBait(Vector3 position)
    {
        if (baitPrefab == null)
        {
            if (!warnedMissingBaitPrefab)
            {
                Debug.LogWarning("FishingRod: baitPrefab is not assigned.", this);
                warnedMissingBaitPrefab = true;
            }
            return;
        }

        warnedMissingBaitPrefab = false;

        if (baitInstance == null)
        {
            baitInstance = Instantiate(baitPrefab);
        }

        baitInstance.transform.position = position;
        baitInstance.transform.rotation = Quaternion.identity;
        if (!baitInstance.activeSelf)
        {
            baitInstance.SetActive(true);
        }

        baitPosition = baitInstance.transform;
        hasSpawnedBaitThisCast = true;
    }

    private void PullRod()
    {
        animator.ResetTrigger(CastTrigger);
        animator.SetTrigger(PullTrigger);

        PullTriggered?.Invoke();

        isCasted = false;
        isPulling = true;

        if (pullRoutine != null)
        {
            StopCoroutine(pullRoutine);
        }

        pullRoutine = StartCoroutine(FinishPullAfterDelay());
    }

    private IEnumerator FinishPullAfterDelay()
    {
        yield return pullDelayYield;
        CompletePull();
    }

    private void RefreshDelayYields()
    {
        castDelayYield = new WaitForSeconds(baitSpawnDelay);
        pullDelayYield = new WaitForSeconds(pullRecoverDuration);
    }

    private Vector3 ResolveBaitSpawnPoint(Vector3 rawPoint, Collider targetCollider)
    {
        Vector3 spawnPoint = rawPoint;

        if (targetCollider != null)
        {
            float surfaceY = targetCollider.bounds.max.y + baitSurfaceOffset;
            if (spawnPoint.y < surfaceY)
            {
                spawnPoint.y = surfaceY;
            }
        }
        else
        {
            spawnPoint.y += baitSurfaceOffset;
        }

        return spawnPoint;
    }

    private bool IsFishingRodSelected()
    {
        if (EquipSystem.Instance == null)
        {
            return true;
        }

        GameObject selectedItem = EquipSystem.Instance.selectedItem;
        if (selectedItem == null)
        {
            return true;
        }

        string selectedName = selectedItem.name.Replace("(Clone)", "").Trim();
        return selectedName == "FishingRod" || selectedName == "FishingRod_Model";
    }

    private void RemoveInvalidAnimationEvents()
    {
        RuntimeAnimatorController runtimeController = animator != null ? animator.runtimeAnimatorController : null;
        if (runtimeController == null)
        {
            return;
        }

        AnimationClip[] clips = runtimeController.animationClips;
        if (clips == null || clips.Length == 0)
        {
            return;
        }

        foreach (AnimationClip clip in clips)
        {
            if (clip == null)
            {
                continue;
            }

            AnimationEvent[] events = clip.events;
            if (events == null || events.Length == 0)
            {
                continue;
            }

            List<AnimationEvent> validEvents = null;
            for (int i = 0; i < events.Length; i++)
            {
                AnimationEvent animationEvent = events[i];
                if (animationEvent == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(animationEvent.functionName))
                {
                    continue;
                }

                if (validEvents == null)
                {
                    validEvents = new List<AnimationEvent>(events.Length);
                }

                validEvents.Add(animationEvent);
            }

            int validCount = validEvents != null ? validEvents.Count : 0;
            if (validCount == events.Length)
            {
                continue;
            }

            try
            {
                clip.events = validCount == 0 ? new AnimationEvent[0] : validEvents.ToArray();
            }
            catch (System.Exception)
            {
                // Ignore clips that are read-only at runtime.
            }
        }
    }

    private void CompletePull()
    {
        isPulling = false;
        ActiveFishingArea = null;

        if (baitInstance != null)
        {
            baitInstance.SetActive(false);
        }

        baitPosition = null;
    }

    private void UpdateRopeEndpoints()
    {
        if (!isCasted && !isPulling)
        {
            if (fishingLine != null)
            {
                fishingLine.enabled = false;
            }

            return;
        }

        if (start_of_rope == null || start_of_rod == null || endof_of_rope == null)
        {
            if (!warnedMissingRopeReferences)
            {
                Debug.LogWarning("FishingRod: missing rope references.", this);
                warnedMissingRopeReferences = true;
            }

            if (fishingLine != null)
            {
                fishingLine.enabled = false;
            }

            return;
        }

        warnedMissingRopeReferences = false;

        start_of_rope.transform.position = start_of_rod.transform.position;

        bool hasActiveBait = baitPosition != null && hasSpawnedBaitThisCast;
        if (!hasActiveBait)
        {
            if (fishingLine != null)
            {
                fishingLine.enabled = false;
            }

            return;
        }

        endof_of_rope.transform.position = baitPosition.position;

        if (fishingLine != null)
        {
            fishingLine.enabled = true;
            fishingLine.positionCount = 2;
            fishingLine.SetPosition(0, start_of_rope.transform.position);
            fishingLine.SetPosition(1, endof_of_rope.transform.position);
        }
    }

    public void OnCastRelease()
    {
        if (isCasted && !hasSpawnedBaitThisCast)
        {
            SpawnBait(pendingCastPoint);
        }
    }

    public void OnReelStart()
    {
        if (isCasted && !isPulling)
        {
            PullRod();
        }
    }

    public void OnReelFinish()
    {
        CompletePull();
    }

    private void OnDisable()
    {
        if (baitSpawnRoutine != null)
        {
            StopCoroutine(baitSpawnRoutine);
            baitSpawnRoutine = null;
        }

        if (pullRoutine != null)
        {
            StopCoroutine(pullRoutine);
            pullRoutine = null;
        }

        isFishingAvailable = false;
        isCasted = false;
        isPulling = false;
        hasSpawnedBaitThisCast = false;
        cachedFishingAreaCollider = null;
        ActiveFishingArea = null;

        if (baitPosition != null)
        {
            baitPosition = null;
        }

        if (baitInstance != null)
        {
            baitInstance.SetActive(false);
        }

        if (fishingLine != null)
        {
            fishingLine.enabled = false;
        }
    }

    private void OnDestroy()
    {
        if (baitInstance != null)
        {
            Destroy(baitInstance);
            baitInstance = null;
        }
    }

    private FishingArea ResolveFishingArea(Collider targetCollider)
    {
        if (targetCollider == null)
        {
            return null;
        }

        FishingArea local = targetCollider.GetComponent<FishingArea>();
        if (local != null)
        {
            return local;
        }

        return targetCollider.GetComponentInParent<FishingArea>();
    }
}
