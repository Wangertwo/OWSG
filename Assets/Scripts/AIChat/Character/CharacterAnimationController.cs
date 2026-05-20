using UnityEngine;

public class CharacterAnimationController : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private string animationParameter = "ChatAnimationState";
    [SerializeField] private bool preferDirectStatePlay = true;
    [SerializeField] private bool logFallback = true;
    [SerializeField] private bool logStateChanges;

    public void ApplyAnimation(string animationState)
    {
        int mappedAnimation = MapAnimationToInt(animationState);
        bool isIdleRequest = IsIdleCommand(animationState);

        if (animator == null)
        {
            if (logFallback)
            {
                Debug.Log("Animation fallback: " + animationState);
            }

            return;
        }

        bool hasIntParameter = HasIntParameter(animator, animationParameter);
        if (!hasIntParameter)
        {
            if (logFallback)
            {
                Debug.LogWarning("Animator missing int parameter: " + animationParameter);
            }
        }

        if (hasIntParameter)
        {
            animator.SetInteger(animationParameter, mappedAnimation);
        }

        bool playedState = false;
        bool shouldTryDirectPlay = preferDirectStatePlay || !hasIntParameter;
        if (shouldTryDirectPlay)
        {
            string stateName;
            int layerIndex;
            if (TryResolveAnimatorState(animationState, out stateName, out layerIndex))
            {
                playedState = TryPlayState(stateName, layerIndex);
            }
        }

        if (logStateChanges)
        {
            Debug.Log("[CharacterAnimationController] " + gameObject.name +
                      " set " + animationParameter + " = " + mappedAnimation +
                      " (input: " + animationState + ", directPlay: " + playedState + ")");
        }

        if (isIdleRequest && !playedState)
        {
            bool forcedIdle = TryForceResetToDefaultIdle();
            if (logStateChanges)
            {
                Debug.Log("[CharacterAnimationController] " + gameObject.name +
                          " idle fallback reset applied: " + forcedIdle);
            }
        }

        if (!hasIntParameter && !playedState && logFallback)
        {
            Debug.LogWarning("Animation fallback failed to resolve state for input: " + animationState);
        }
    }

    private int MapAnimationToInt(string animationState)
    {
        if (string.IsNullOrWhiteSpace(animationState))
        {
            return 0;
        }

        string normalizedState = animationState.Trim().ToLowerInvariant();

        switch (normalizedState)
        {
            case "greet":
                return 1;
            case "talk":
                return 2;
            default:
                return 0;
        }
    }

    private bool HasIntParameter(Animator targetAnimator, string parameterName)
    {
        foreach (AnimatorControllerParameter parameter in targetAnimator.parameters)
        {
            if (parameter.name == parameterName && parameter.type == AnimatorControllerParameterType.Int)
            {
                return true;
            }
        }

        return false;
    }

    private bool TryResolveAnimatorState(string animationState, out string stateName, out int layerIndex)
    {
        stateName = string.Empty;
        layerIndex = 0;

        if (string.IsNullOrWhiteSpace(animationState))
        {
            return TryResolveFirstExistingState(
                new[] { "NPC_Idle", "Mayor_Idle", "Hunter_Idle", "Hunter_Idle 0", "Fisherman_Idle", "Idle" },
                out stateName,
                out layerIndex);
        }

        string normalizedState = animationState.Trim().ToLowerInvariant();

        switch (normalizedState)
        {
            case "talk":
                return TryResolveFirstExistingState(
                    new[] { "NPC_Talking", "Mayor_Talking", "Hunter_Talking", "Fisherman_Talking", "Talking", "Talk" },
                    out stateName,
                    out layerIndex);

            case "greet":
                return TryResolveFirstExistingState(
                    new[] { "Mayor_Talking", "Hunter_Talking", "Fisherman_Talking", "NPC_Talking", "Greet", "Greeting" },
                    out stateName,
                    out layerIndex);

            default:
                return TryResolveFirstExistingState(
                    new[] { "NPC_Idle", "Mayor_Idle", "Hunter_Idle", "Hunter_Idle 0", "Fisherman_Idle", "Idle" },
                    out stateName,
                    out layerIndex);
        }
    }

    private bool TryResolveFirstExistingState(string[] candidates, out string stateName, out int layerIndex)
    {
        stateName = string.Empty;
        layerIndex = 0;

        if (animator == null || candidates == null)
        {
            return false;
        }

        for (int i = 0; i < candidates.Length; i++)
        {
            string candidate = candidates[i];
            if (string.IsNullOrWhiteSpace(candidate))
            {
                continue;
            }

            if (TryResolvePlayableState(candidate, out stateName, out layerIndex))
            {
                return true;
            }
        }

        return false;
    }

    private bool TryResolvePlayableState(string candidate, out string stateName, out int layerIndex)
    {
        stateName = string.Empty;
        layerIndex = 0;

        if (animator == null || string.IsNullOrWhiteSpace(candidate))
        {
            return false;
        }

        int layerCount = Mathf.Max(1, animator.layerCount);
        for (int i = 0; i < layerCount; i++)
        {
            int rawHash = Animator.StringToHash(candidate);
            if (animator.HasState(i, rawHash))
            {
                stateName = candidate;
                layerIndex = i;
                return true;
            }

            string fullPath = animator.GetLayerName(i) + "." + candidate;
            int fullPathHash = Animator.StringToHash(fullPath);
            if (animator.HasState(i, fullPathHash))
            {
                stateName = fullPath;
                layerIndex = i;
                return true;
            }
        }

        return false;
    }

    private bool TryPlayState(string stateName, int layerIndex)
    {
        if (animator == null || string.IsNullOrWhiteSpace(stateName))
        {
            return false;
        }

        int stateHash = Animator.StringToHash(stateName);
        if (!animator.HasState(layerIndex, stateHash))
        {
            return false;
        }

        animator.CrossFade(stateHash, 0.08f, layerIndex, 0f);
        return true;
    }

    private bool IsIdleCommand(string animationState)
    {
        if (string.IsNullOrWhiteSpace(animationState))
        {
            return true;
        }

        string normalizedState = animationState.Trim().ToLowerInvariant();
        return normalizedState == "idle" || normalizedState == "default" || normalizedState == "none";
    }

    private bool TryForceResetToDefaultIdle()
    {
        if (animator == null)
        {
            return false;
        }

        animator.Rebind();
        animator.Update(0f);
        return true;
    }
}
