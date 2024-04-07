using System.Collections;
using TMPro;
using UnityEngine;

public class UpgradeCheck : MonoBehaviour
{
    [SerializeField] private SO_Data_DayCycle dataDayCycle; //for saving information

    public GameObject[] upgradeObjects; // Reference to the GameObjects with the half-transparent sprite
    public int upgradeCost = 150; // Cost of the upgrade
    public Transform playerTransform; // Reference to the player's Transform
    public TextMeshProUGUI[] upgradeTexts;

    private SpriteRenderer[] spriteRenderers;
    private Collider2D[] colliders; // Array to hold the colliders of the upgrade objects
    private bool[] isBought;
    private int nextToBuyIndex = 0; // Start interaction from the second object
    private Color[] originalColors;

    private void Awake()
    {
        //set the indexes of the Incubation Pods for saving and loading.
        //For the InteractableObject_SeedPod Script
        
        for (int i = 0; i < upgradeObjects.Length; i++)
        {
            InteractableObject_SeedPod seedPod = upgradeObjects[i].GetComponent<InteractableObject_SeedPod>();
            seedPod.m_ThisIndex = i;

            //Initialize the data if not already initialized to avoid errors
            //Used to be just OnEnable, but onEnable is only true if the user has the SO selected in the Assets
            if (dataDayCycle.incubationPodPurchased == null) dataDayCycle.Initialize();

            //we already purchased the first incubation pod
            if (i == 0)
            {
                dataDayCycle.incubationPodPurchased[i] = true;
            }
            else
            {
                //only make the others false if they're not already true
                if (!dataDayCycle.incubationPodPurchased[i])
                {
                    
                    dataDayCycle.incubationPodPurchased[i] = false;
                }
                
            }


        }

        //update NextIndex to buy. Loop through all upgrade objects again 
        for (int i = 0; i < upgradeObjects.Length; i++)
        {
            if (dataDayCycle.incubationPodPurchased[i] == false)
            {
                nextToBuyIndex = i;
                break;
            }
        }

        isBought = dataDayCycle.incubationPodPurchased;


    }
    private void Start()
    {
        spriteRenderers = new SpriteRenderer[upgradeObjects.Length];
        colliders = new Collider2D[upgradeObjects.Length]; // Initialize the colliders array

        originalColors = new Color[upgradeObjects.Length];

        for (int i = 0; i < upgradeObjects.Length; i++)
        {
            spriteRenderers[i] = upgradeObjects[i].GetComponentInChildren<SpriteRenderer>();
            colliders[i] = upgradeObjects[i].GetComponentInChildren<Collider2D>(); // Get the collider component
            originalColors[i] = spriteRenderers[i].color; // Save the original color of each object

            ResetColorAndCollider(i);

        }
    }

    private void Update()
    {
        for (int i = 0; i < upgradeObjects.Length; i++)
        {
            bool playerInRange = IsPlayerInRange(upgradeObjects[i].transform);
            bool canAfford = PlayerWallet.Instance.walletAmount >= upgradeCost;

            if (playerInRange && !isBought[i] && i == nextToBuyIndex)
            {
                HandleUpgradeInteraction(i, canAfford);
                UpdateUpgradeText(i, true, canAfford); // Only show the text for the next purchasable upgrade
            }
            else
            {
                if (i == nextToBuyIndex) UpdateUpgradeText(i, false, canAfford); // Hide the text if out of range but it's the next purchasable upgrade
            }
        }
    }

    private void HandleUpgradeInteraction(int index, bool canAfford)
    {
        // Change color based on affordability
        spriteRenderers[index].color = canAfford ? Color.green : new Color(1f, 0f, 0f, 0.5f);

        // Buying logic
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (canAfford)
            {
                
                PlayerWallet.Instance.SubtractValue(upgradeCost, "Incubation Pod Purchased"); // Subtract the cost from the player's wallet
                isBought[index] = true; // Mark the object as bought
                nextToBuyIndex = Mathf.Min(nextToBuyIndex + 1, upgradeObjects.Length - 1); // Move to the next object in the list

                //Save the data when we purchased one
                dataDayCycle.incubationPodPurchased[index] = true;

                //Set isBought based on the save state
                isBought[index] = true;

                InteractableObject_SeedPod seedPod = upgradeObjects[index].GetComponent<InteractableObject_SeedPod>();
                seedPod.m_IncubationState = IncubationState.OBJ_AddSeed;
                dataDayCycle.incubationPodData[index].incubationState = IncubationState.OBJ_AddSeed;
                seedPod.SetColourOfPodLight();

                ResetColorAndCollider(index);
                
            }
            else
            {
                // Show "Insufficient Funds" temporarily
                upgradeTexts[index].text = "Insufficient Funds";
                StartCoroutine(ResetUpgradeTextAfterDelay(index));
            }
        }
    }

    private void ResetColorAndCollider(int index)
    {
        var color = originalColors[index];
        color.a = isBought[index] ? 1f : 0.5f; // Set opacity based on whether the object is bought
        spriteRenderers[index].color = color;

        // Assuming you have a way to get the InteractableObject_SeedPod component, e.g., through an array or GetComponent<>
        InteractableObject_SeedPod interactableObject = upgradeObjects[index].GetComponent<InteractableObject_SeedPod>();
        if (interactableObject != null)
        {
            interactableObject.SetInteractable(isBought[index]);
        }

        // Enable the collider for all purchased items
        if (colliders[index] != null)
        {
            colliders[index].enabled = isBought[index];
        }
    }
    IEnumerator ResetUpgradeTextAfterDelay(int index)
    {
        yield return new WaitForSeconds(2f); // Adjust delay as needed
        if (index == nextToBuyIndex) // Only reset text if it's still the next purchasable upgrade
        {
            UpdateUpgradeText(index, true, PlayerWallet.Instance.walletAmount >= upgradeCost);
        }
    }

    private void UpdateUpgradeText(int index, bool inRange, bool canAfford)
    {
        if (inRange && index == nextToBuyIndex) // Only update text for the next purchasable upgrade
        {
            upgradeTexts[index].gameObject.SetActive(true);
            upgradeTexts[index].text = $"Upgrade Cost: {upgradeCost}";
        }
        else
        {
            upgradeTexts[index].gameObject.SetActive(false);
        }
    }

    
    private bool IsPlayerInRange(Transform objectTransform)
    {
        Vector2 playerPosition = playerTransform.position;
        Vector2 objectPosition = objectTransform.position;
        float interactionRange = 3.0f;
        return Vector2.Distance(playerPosition, objectPosition) <= interactionRange;
    }

}
