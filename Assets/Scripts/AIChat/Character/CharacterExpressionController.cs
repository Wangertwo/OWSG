using UnityEngine;

public class CharacterExpressionController : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private string emotionParameter = "EmotionState";
    [SerializeField] private bool logFallback = true;

    public void ApplyEmotion(string emotion)
    {
        int mappedEmotion = MapEmotionToInt(emotion);

        if (animator == null)
        {
            if (logFallback)
            {
                Debug.Log("Emotion fallback: " + emotion);
            }

            return;
        }

        if (!HasIntParameter(animator, emotionParameter))
        {
            if (logFallback)
            {
                Debug.LogWarning("Animator missing int parameter: " + emotionParameter);
            }

            return;
        }

        animator.SetInteger(emotionParameter, mappedEmotion);
    }

    private int MapEmotionToInt(string emotion)
    {
        if (string.IsNullOrWhiteSpace(emotion))
        {
            return 0;
        }

        string normalizedEmotion = emotion.Trim().ToLowerInvariant();

        switch (normalizedEmotion)
        {
            case "happy":
                return 1;
            case "sad":
                return 2;
            case "angry":
                return 3;
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
