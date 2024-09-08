using UnityEngine;
using System.Collections;

public class gamelogicmym : MonoBehaviour
{
    // Public references for the player cube (Player "P")
    public GameObject playerCube;

    // Public references for each enemy and NPC for each level
    public GameObject enemyCapsuleA;
    public GameObject npcCylinderA;

    public GameObject enemyCapsuleB;
    public GameObject npcCylinderB;

    public GameObject enemyCapsuleC;
    public GameObject npcCylinderC;

    public GameObject enemyCapsuleD;
    public GameObject npcCylinderD;

    public GameObject enemyCapsuleE;
    public GameObject npcCylinderE;

    // Current level and total levels
    private int currentLevel = 1;
    private const int totalLevels = 5;

    // Distance threshold for brute-force collision detection
    public float collisionDistanceThreshold = 1.5f;

    // NPC dialogue-related variables
    private bool isEnemyDestroyed = false;
    private bool isDialogueShown = false;
    private GameObject currentNpcLabel;
    private TextMesh dialogueTextMesh;

    // Reference to current enemy and NPC for the current level
    private GameObject currentEnemy;
    private GameObject currentNPC;

    // This function will be called at the start of the game
    void Start()
    {
        // Initialize the first level
        StartLevel(1);
        
        // Attach the player label "P"
        AttachDebugLetter(playerCube, "P");

        Debug.Log("Starting game logic with level progression.");
    }

    // Update is called once per frame
    void Update()
    {
        if (currentEnemy != null && Vector3.Distance(playerCube.transform.position, currentEnemy.transform.position) < collisionDistanceThreshold && !isEnemyDestroyed)
        {
            // Destroy the current enemy and show NPC
            Destroy(currentEnemy);
            Debug.Log($"Enemy {GetLabelForCurrentLevel()} destroyed.");

            // Set NPC visible now that the enemy is destroyed
            currentNPC.SetActive(true);
            AttachDebugLetter(currentNPC, GetLabelForCurrentLevel());
            isEnemyDestroyed = true;
        }

        if (isEnemyDestroyed && !isDialogueShown && Vector3.Distance(playerCube.transform.position, currentNPC.transform.position) < collisionDistanceThreshold)
        {
            // Show NPC dialogue
            ShowNPCDialogue(GetLabelForCurrentLevel());
            isDialogueShown = true;
            Debug.Log($"NPC {GetLabelForCurrentLevel()} dialogue displayed.");

            // Start coroutine to transition to the next level
            StartCoroutine(TransitionToNextLevel());
        }
    }

    // Function to attach a letter above the assigned object for debugging
    void AttachDebugLetter(GameObject targetObject, string label)
    {
        // Create a new TextMesh object for the label
        GameObject labelObject = new GameObject("DebugLabel");
        TextMesh textMesh = labelObject.AddComponent<TextMesh>();

        // Set the text to the specified label and configure appearance
        textMesh.text = label;
        textMesh.fontSize = 32;
        textMesh.color = Color.red;

        // Position the label slightly above the targetObject
        labelObject.transform.position = targetObject.transform.position + new Vector3(0, 2, 0);  // Adjust height here
        labelObject.transform.SetParent(targetObject.transform);  // Ensure the label follows the object

        // Save the label for the NPC so we can add dialogue later
        if (targetObject == currentNPC)
        {
            currentNpcLabel = labelObject;
        }
    }

    // Function to show NPC dialogue when player collides with NPC
    void ShowNPCDialogue(string label)
    {
        // Create a new TextMesh object for the dialogue text
        GameObject dialogueObject = new GameObject("NPCDialogue");
        dialogueTextMesh = dialogueObject.AddComponent<TextMesh>();

        // Set the dialogue text and configure appearance
        if (label == "E")
        {
            dialogueTextMesh.text = "Thank you for destroying the enemy";
        }
        else
        {
            dialogueTextMesh.text = $"Thank you for destroying the enemy {label}";
        }
        dialogueTextMesh.fontSize = 24;
        dialogueTextMesh.color = Color.white;

        // Position the dialogue next to the label above the NPC
        dialogueObject.transform.position = currentNpcLabel.transform.position + new Vector3(1.5f, 0, 0);  // Adjust position here
        dialogueObject.transform.SetParent(currentNPC.transform);  // Ensure the dialogue follows the NPC
    }

    // Coroutine to handle transition to the next level
    IEnumerator TransitionToNextLevel()
    {
        // Wait for 10 seconds after showing dialogue
        yield return new WaitForSeconds(10);

        // Clear the current dialogue
        if (dialogueTextMesh != null)
        {
            Destroy(dialogueTextMesh.gameObject);
        }

        // Increment to the next level, or finish the game if the final level is completed
        if (currentLevel < totalLevels)
        {
            currentLevel++;
            StartLevel(currentLevel);
            isEnemyDestroyed = false;
            isDialogueShown = false;
        }
        else
        {
            Debug.Log("All levels complete. Game finished.");
        }
    }

    // Function to start a specific level
    void StartLevel(int level)
    {
        Debug.Log($"Starting Level {level}");

        // Activate the appropriate enemy and NPC for the given level
        switch (level)
        {
            case 1:
                currentEnemy = enemyCapsuleA;
                currentNPC = npcCylinderA;
                break;
            case 2:
                currentEnemy = enemyCapsuleB;
                currentNPC = npcCylinderB;
                break;
            case 3:
                currentEnemy = enemyCapsuleC;
                currentNPC = npcCylinderC;
                break;
            case 4:
                currentEnemy = enemyCapsuleD;
                currentNPC = npcCylinderD;
                break;
            case 5:
                currentEnemy = enemyCapsuleE;
                currentNPC = npcCylinderE;
                break;
        }

        // Make sure to deactivate NPC at the start of each level
        currentNPC.SetActive(false);

        // Activate the current enemy for the level
        currentEnemy.SetActive(true);
        AttachDebugLetter(currentEnemy, GetLabelForCurrentLevel());
    }

    // Function to get the label for the current level (A, B, C, D, or E)
    string GetLabelForCurrentLevel()
    {
        switch (currentLevel)
        {
            case 1: return "A";
            case 2: return "B";
            case 3: return "C";
            case 4: return "D";
            case 5: return "E";
            default: return "";
        }
    }
}
