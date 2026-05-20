using System.Collections;
using UnityEngine;

public class ChoppableTree : MonoBehaviour
{
    private const float CaloriesCostPerHit = 20f;

    public bool playerInRange;
    public bool canBeChopped;

    public float chopMaxHealth;
    public float chopCurrentHealth;

    private Animator animator;
    private void Start()
    {
        chopCurrentHealth = chopMaxHealth;
        animator = transform.parent.transform.parent.GetComponent<Animator>();
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
        }
    }

    public void GetHit()
    {
        if (!canBeChopped)
        {
            return;
        }

        PlayerState playerState = PlayerState.Instance;
        if (playerState != null && playerState.currentCalories <= 0f)
        {
            return;
        }

        if (!TrySpendCaloriesForHit())
        {
            return;
        }

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayChopItemSound();
        }

        chopCurrentHealth -= 1f;
        if (chopCurrentHealth <= 0)
        {
            TreeIsDead();
        }

        animator.SetTrigger("shake");
    }

    private bool TrySpendCaloriesForHit()
    {
        PlayerState playerState = PlayerState.Instance;
        if (playerState == null)
        {
            Debug.LogWarning("PlayerState instance missing. Cannot spend calories for chopping.");
            return false;
        }

        return playerState.TrySpendCaloriesForAction(CaloriesCostPerHit);
    }

    private void Update()
    {
        if (canBeChopped)
        {
            GlobalState.Instance.resourceHealth = chopCurrentHealth;
            GlobalState.Instance.resourceMaxHealth = chopMaxHealth;
        }
    }

    private void TreeIsDead()
    {
        canBeChopped = false;
        Destroy(transform.parent.transform.parent.gameObject);
        SelectionManager.Instance.chopHolder.gameObject.SetActive(false);
        SelectionManager.Instance.selectedTree = null;

        Vector3 treePosition = transform.position;
        GameObject brokenTree = 
            Instantiate(Resources.Load<GameObject>("choppedTree"), 
                new Vector3(treePosition.x, treePosition.y + 1, treePosition.z), Quaternion.Euler(0,0,0));
    }
}
