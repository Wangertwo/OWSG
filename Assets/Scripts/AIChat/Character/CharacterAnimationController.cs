using UnityEngine;

public class CharacterAnimationController : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private string animationParameter = "ChatAnimationState";
    [SerializeField] private bool logFallback = true;

    public void ApplyAnimation(string animationState)
    {
        int mappedAnimation = MapAnimationToInt(animationState);

        if (animator == null)
        {
            if (logFallback)
            {
                Debug.Log("Animation fallback: " + animationState);
            }

            return;
        }

        if (!HasIntParameter(animator, animationParameter))
        {
            if (logFallback)
            {
                Debug.LogWarning("Animator missing int parameter: " + animationParameter);
            }

            return;
        }

        animator.SetInteger(animationParameter, mappedAnimation);
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
}
