using UnityEngine;
using System.Collections;

public class GameLogicMym : MonoBehaviour
{
    // Public references for the player cube (Player "P")
    public GameObject playerCube;

    // Public references for each enemy and NPC for each level
    public GameObject enemyCapsuleA, npcCylinderA;
    public GameObject enemyCapsuleB, npcCylinderB;
    public GameObject enemyCapsuleC, npcCylinderC;
    public GameObject enemyCapsuleD, npcCylinderD;
    public GameObject enemyCapsuleE, npcCylinderE;

    // Additional enemies for each level
    public GameObject enemyCapsuleAExtra1, enemyCapsuleAExtra2;
    public GameObject enemyCapsuleBExtra1, enemyCapsuleBExtra2;
    public GameObject enemyCapsuleCExtra1, enemyCapsuleCExtra2;
    public GameObject enemyCapsuleDExtra1, enemyCapsuleDExtra2;
    public GameObject enemyCapsuleEExtra1, enemyCapsuleEExtra2;

    // Health and damage values
    private int playerHealth = 200;
    private const int playerMaxHealth = 200;
    private const int playerDamage = 1;
    private const int enemyHealthBase = 10;
    private int currentEnemyHealth;

    // Cooldown between damage ticks (1 second)
    private float damageCooldown = 1.0f;
    private float lastDamageTime;

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

    // References for the additional enemies of the current level
    private GameObject extraEnemy1, extraEnemy2;

    // Current level and total levels
    private int currentLevel = 1;
    private const int totalLevels = 5;

    // Enemy follow speed
    public float enemyFollowSpeed = 2.0f;
    public float extraEnemyOffset = 1.0f;  // Offset for the extra enemies
    public float extraEnemyFollowDelay = 0.5f;  // Delay in movement for extra enemies

    // This function will be called at the start of the game
    void Start()
    {
        // Hide all enemies and NPCs at the start
        HideAllEnemiesAndNPCs();

        // Initialize the first level
        StartLevel(1);

        // Attach the player label "P"
        AttachDebugLetter(playerCube, "P");
        Debug.Log("Starting game logic with level progression.");
    }

    // Update is called once per frame
    void Update()
    {
        // Handle enemy following logic for main and extra enemies
        if (currentEnemy != null && !isEnemyDestroyed)
        {
            FollowPlayer(currentEnemy); // Original enemy follows
        }

        if (extraEnemy1 != null && !isEnemyDestroyed)
        {
            FollowPlayerWithOffset(extraEnemy1, extraEnemyOffset, extraEnemyFollowDelay); // Extra enemy 1 follows with delay and offset
        }

        if (extraEnemy2 != null && !isEnemyDestroyed)
        {
            FollowPlayerWithOffset(extraEnemy2, -extraEnemyOffset, extraEnemyFollowDelay); // Extra enemy 2 follows with delay and opposite offset
        }

        // Check for collisions and handle enemy destruction
        if (currentEnemy != null && Vector3.Distance(playerCube.transform.position, currentEnemy.transform.position) < collisionDistanceThreshold && !isEnemyDestroyed)
        {
            // Apply damage if cooldown has passed
            if (Time.time - lastDamageTime >= damageCooldown)
            {
                ApplyDamageToEnemy();
                ApplyDamageToPlayer();

                // Check if the original enemy is destroyed
                if (currentEnemyHealth <= 0)
                {
                    Destroy(currentEnemy);
                    Destroy(extraEnemy1);
                    Destroy(extraEnemy2);
                    Debug.Log($"Enemy {GetLabelForCurrentLevel()} and its extras destroyed.");

                    // Set NPC visible now that the enemies are destroyed
                    currentNPC.SetActive(true);
                    AttachDebugLetter(currentNPC, GetLabelForCurrentLevel());
                    isEnemyDestroyed = true;
                }

                // Update last damage time
                lastDamageTime = Time.time;
            }
        }

        // Show NPC dialogue when the player reaches the NPC
        if (isEnemyDestroyed && !isDialogueShown && Vector3.Distance(playerCube.transform.position, currentNPC.transform.position) < collisionDistanceThreshold)
        {
            ShowNPCDialogue(GetLabelForCurrentLevel());
            isDialogueShown = true;
            Debug.Log($"NPC {GetLabelForCurrentLevel()} dialogue displayed.");
            StartCoroutine(TransitionToNextLevel());
        }
    }

    // Function to follow the player
    void FollowPlayer(GameObject enemy)
    {
        // Calculate the direction towards the playerCube and move the enemy
        Vector3 direction = (playerCube.transform.position - enemy.transform.position).normalized;
        enemy.transform.position += direction * enemyFollowSpeed * Time.deltaTime;
    }

    // Function to follow the player with an offset and delay for extra enemies
    void FollowPlayerWithOffset(GameObject enemy, float offset, float delay)
    {
        // Wait for the delay before the extra enemy starts moving
        StartCoroutine(FollowWithDelay(enemy, offset, delay));
    }

    IEnumerator FollowWithDelay(GameObject enemy, float offset, float delay)
    {
        yield return new WaitForSeconds(delay);

        // Calculate the direction towards the playerCube and move the enemy with an offset
        Vector3 direction = (playerCube.transform.position - enemy.transform.position).normalized;
        Vector3 offsetPosition = new Vector3(direction.z, 0, -direction.x) * offset;  // Perpendicular offset
        enemy.transform.position += (direction + offsetPosition) * enemyFollowSpeed * Time.deltaTime;
    }

    // Function to apply damage to the enemy
    void ApplyDamageToEnemy()
    {
        currentEnemyHealth -= playerDamage;
        DisplayDamageNumber(currentEnemy, playerDamage);

        Debug.Log($"Enemy {GetLabelForCurrentLevel()} takes {playerDamage} damage. Health now: {currentEnemyHealth}");
    }

    // Function to apply damage to the player
    void ApplyDamageToPlayer()
    {
        playerHealth -= 1;  // Player receives -1 damage from the enemy
        DisplayDamageNumber(playerCube, 1);

        Debug.Log($"Player takes 1 damage. Health now: {playerHealth}");

        if (playerHealth <= 0)
        {
            Debug.Log("Player health reached 0. Resetting to full health for debugging.");
            playerHealth = playerMaxHealth;
        }
    }

    // Function to display floating damage numbers
    void DisplayDamageNumber(GameObject targetObject, int damageAmount)
    {
        GameObject damageTextObject = new GameObject("DamageText");
        TextMesh damageTextMesh = damageTextObject.AddComponent<TextMesh>();

        damageTextMesh.text = $"-{damageAmount}";
        damageTextMesh.fontSize = 32;
        damageTextMesh.color = Color.red;

        damageTextObject.transform.position = targetObject.transform.position + new Vector3(0, 2, 0);
        Destroy(damageTextObject, 1.0f);
    }

    // Function to attach a letter above the assigned object for debugging
    void AttachDebugLetter(GameObject targetObject, string label)
    {
        GameObject labelObject = new GameObject("DebugLabel");
        TextMesh textMesh = labelObject.AddComponent<TextMesh>();

        textMesh.text = label;
        textMesh.fontSize = 32;
        textMesh.color = Color.red;

        labelObject.transform.position = targetObject.transform.position + new Vector3(0, 2, 0);
        labelObject.transform.SetParent(targetObject.transform);

        if (targetObject == currentNPC)
        {
            currentNpcLabel = labelObject;
        }
    }

    // Function to show NPC dialogue when player collides with NPC
    void ShowNPCDialogue(string label)
    {
        GameObject dialogueObject = new GameObject("NPCDialogue");
        dialogueTextMesh = dialogueObject.AddComponent<TextMesh>();

        dialogueTextMesh.text = $"Thank you for destroying the enemy {label}";
        dialogueTextMesh.fontSize = 24;
        dialogueTextMesh.color = Color.white;

        dialogueObject.transform.position = currentNpcLabel.transform.position + new Vector3(1.5f, 0, 0);
        dialogueObject.transform.SetParent(currentNPC.transform);
    }

    // Coroutine to handle transition to the next level
    IEnumerator TransitionToNextLevel()
    {
        yield return new WaitForSeconds(10);

        if (dialogueTextMesh != null)
        {
            Destroy(dialogueTextMesh.gameObject);
        }

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
                extraEnemy1 = enemyCapsuleAExtra1;
                extraEnemy2 = enemyCapsuleAExtra2;
                break;
            case 2:
                currentEnemy = enemyCapsuleB;
                currentNPC = npcCylinderB;
                extraEnemy1 = enemyCapsuleBExtra1;
                extraEnemy2 = enemyCapsuleBExtra2;
                break;
            case 3:
                currentEnemy = enemyCapsuleC;
                currentNPC = npcCylinderC;
                extraEnemy1 = enemyCapsuleCExtra1;
                extraEnemy2 = enemyCapsuleCExtra2;
                break;
            case 4:
                currentEnemy = enemyCapsuleD;
                currentNPC = npcCylinderD;
                extraEnemy1 = enemyCapsuleDExtra1;
                extraEnemy2 = enemyCapsuleDExtra2;
                break;
            case 5:
                currentEnemy = enemyCapsuleE;
                currentNPC = npcCylinderE;
                extraEnemy1 = enemyCapsuleEExtra1;
                extraEnemy2 = enemyCapsuleEExtra2;
                break;
        }

        // Set initial enemy health and deactivate NPC at the start of each level
        currentEnemyHealth = enemyHealthBase;
        currentNPC.SetActive(false);

        // Activate the current enemy and additional enemies for the level
        currentEnemy.SetActive(true);
        extraEnemy1.SetActive(true);
        extraEnemy2.SetActive(true);

        AttachDebugLetter(currentEnemy, GetLabelForCurrentLevel());
        AttachDebugLetter(extraEnemy1, GetLabelForCurrentLevel() + " Extra1");
        AttachDebugLetter(extraEnemy2, GetLabelForCurrentLevel() + " Extra2");

        lastDamageTime = Time.time;
    }

    // Function to hide all enemies and NPCs at the start of the game
    void HideAllEnemiesAndNPCs()
    {
        enemyCapsuleA.SetActive(false);
        npcCylinderA.SetActive(false);
        enemyCapsuleB.SetActive(false);
        npcCylinderB.SetActive(false);
        enemyCapsuleC.SetActive(false);
        npcCylinderC.SetActive(false);
        enemyCapsuleD.SetActive(false);
        npcCylinderD.SetActive(false);
        enemyCapsuleE.SetActive(false);
        npcCylinderE.SetActive(false);

        // Hide all extra enemies
        enemyCapsuleAExtra1.SetActive(false);
        enemyCapsuleAExtra2.SetActive(false);
        enemyCapsuleBExtra1.SetActive(false);
        enemyCapsuleBExtra2.SetActive(false);
        enemyCapsuleCExtra1.SetActive(false);
        enemyCapsuleCExtra2.SetActive(false);
        enemyCapsuleDExtra1.SetActive(false);
        enemyCapsuleDExtra2.SetActive(false);
        enemyCapsuleEExtra1.SetActive(false);
        enemyCapsuleEExtra2.SetActive(false);
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
