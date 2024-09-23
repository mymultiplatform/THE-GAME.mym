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

    // Public references for BOSS prefabs (Sphere)
    public GameObject bossSphereA, bossSphereB, bossSphereC, bossSphereD, bossSphereE;

    // Health and damage values
    private int playerHealth = 200;
    private const int playerMaxHealth = 200;
    private const int playerDamage = 1;
    private const int enemyHealthBase = 10;
    private int currentEnemyHealth, extraEnemy1Health, extraEnemy2Health;
    private int bossHealth = 10;

    // Cooldown between damage ticks (1 second)
    private float damageCooldown = 1.0f;
    private float lastDamageTime;

    // Distance threshold for brute-force collision detection
    public float collisionDistanceThreshold = 1.5f;

    // NPC dialogue-related variables
    private bool areAllEnemiesDestroyed = false;
    private bool isBossDestroyed = false;
    private bool isDialogueShown = false;
    private GameObject currentNpcLabel;
    private TextMesh dialogueTextMesh;

    // Reference to current enemy, NPC, and BOSS for the current level
    private GameObject currentEnemy;
    private GameObject currentNPC;
    private GameObject currentBoss;

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
        // Hide all enemies, NPCs, and bosses at the start
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
        if (!areAllEnemiesDestroyed)
        {
            if (currentEnemy != null)
            {
                FollowPlayer(currentEnemy); // Original enemy follows
            }

            if (extraEnemy1 != null)
            {
                FollowPlayerWithOffset(extraEnemy1, extraEnemyOffset, extraEnemyFollowDelay); // Extra enemy 1 follows with delay and offset
            }

            if (extraEnemy2 != null)
            {
                FollowPlayerWithOffset(extraEnemy2, -extraEnemyOffset, extraEnemyFollowDelay); // Extra enemy 2 follows with delay and opposite offset
            }

            // Check for collisions and handle enemy destruction
            HandleEnemyCollision(currentEnemy, ref currentEnemyHealth);
            HandleEnemyCollision(extraEnemy1, ref extraEnemy1Health);
            HandleEnemyCollision(extraEnemy2, ref extraEnemy2Health);

            // Check if all enemies are destroyed to spawn the BOSS
            if (currentEnemyHealth <= 0 && extraEnemy1Health <= 0 && extraEnemy2Health <= 0 && !areAllEnemiesDestroyed)
            {
                areAllEnemiesDestroyed = true;
                StartCoroutine(SpawnBossAfterDelay(5)); // Delay of 5 seconds to spawn the boss
            }
        }

        // Handle boss logic after enemies are destroyed
        if (areAllEnemiesDestroyed && currentBoss != null && !isBossDestroyed)
        {
            HandleBossCollision(currentBoss, ref bossHealth);

            // Once the boss is defeated, NPC appears
            if (bossHealth <= 0 && !isBossDestroyed)
            {
                isBossDestroyed = true;
                Destroy(currentBoss);
                Debug.Log("BOSS destroyed.");
                UnlockNPC();
            }
        }

        // Show NPC dialogue when the player reaches the NPC
        if (isBossDestroyed && !isDialogueShown && Vector3.Distance(playerCube.transform.position, currentNPC.transform.position) < collisionDistanceThreshold)
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
        Vector3 direction = (playerCube.transform.position - enemy.transform.position).normalized;
        enemy.transform.position += direction * enemyFollowSpeed * Time.deltaTime;
    }

    // Function to follow the player with an offset and delay for extra enemies
    void FollowPlayerWithOffset(GameObject enemy, float offset, float delay)
    {
        StartCoroutine(FollowWithDelay(enemy, offset, delay));
    }

    IEnumerator FollowWithDelay(GameObject enemy, float offset, float delay)
    {
        yield return new WaitForSeconds(delay);

        Vector3 direction = (playerCube.transform.position - enemy.transform.position).normalized;
        Vector3 offsetPosition = new Vector3(direction.z, 0, -direction.x) * offset;  // Perpendicular offset
        enemy.transform.position += (direction + offsetPosition) * enemyFollowSpeed * Time.deltaTime;
    }

    // Function to handle enemy collision and damage
    void HandleEnemyCollision(GameObject enemy, ref int enemyHealth)
    {
        if (enemy != null && Vector3.Distance(playerCube.transform.position, enemy.transform.position) < collisionDistanceThreshold && enemyHealth > 0)
        {
            if (Time.time - lastDamageTime >= damageCooldown)
            {
                ApplyDamageToEnemy(ref enemyHealth, enemy);
                ApplyDamageToPlayer();

                if (enemyHealth <= 0)
                {
                    Destroy(enemy);
                    Debug.Log($"Enemy destroyed.");
                }

                lastDamageTime = Time.time;
            }
        }
    }

    // Function to handle BOSS collision and damage
    void HandleBossCollision(GameObject boss, ref int bossHealth)
    {
        if (boss != null && Vector3.Distance(playerCube.transform.position, boss.transform.position) < collisionDistanceThreshold && bossHealth > 0)
        {
            if (Time.time - lastDamageTime >= damageCooldown)
            {
                ApplyDamageToEnemy(ref bossHealth, boss);
                ApplyDamageToPlayer();

                if (bossHealth <= 0)
                {
                    Debug.Log("Boss defeated.");
                }

                lastDamageTime = Time.time;
            }
        }
    }

    // Function to unlock the NPC after the boss is defeated
    void UnlockNPC()
    {
        currentNPC.SetActive(true);
        AttachDebugLetter(currentNPC, GetLabelForCurrentLevel());
        Debug.Log("Boss defeated. NPC unlocked.");
    }

    // Function to display floating damage numbers
    void DisplayDamageNumber(GameObject targetObject, int damageAmount)
    {
        GameObject damageTextObject = new GameObject("DamageText");
        TextMesh damageTextMesh = damageTextObject.AddComponent<TextMesh>();

        damageTextMesh.text = $"-{damageAmount}";
        damageTextMesh.fontSize = 24;
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
        textMesh.fontSize = 10;
        textMesh.color = Color.red;

        labelObject.transform.position = targetObject.transform.position + new Vector3(0, 2, 0);
        labelObject.transform.SetParent(targetObject.transform);

        if (targetObject == currentNPC)
        {
            currentNpcLabel = labelObject;
        }
    }

    // Coroutine to spawn the BOSS after a delay
    IEnumerator SpawnBossAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Debug.Log("Spawning BOSS.");

        // Activate the current BOSS
        currentBoss.SetActive(true);
        AttachDebugLetter(currentBoss, "B");
    }

    // Coroutine to handle the transition to the next level
    IEnumerator TransitionToNextLevel()
    {
        yield return new WaitForSeconds(5);  // Wait for 5 seconds after dialogue
        currentLevel++;

        if (currentLevel <= totalLevels)
        {
            StartLevel(currentLevel);
            Debug.Log($"Starting level {currentLevel}.");
        }
        else
        {
            Debug.Log("All levels completed. Game over!");
        }
    }

    // Function to apply damage to the player
    void ApplyDamageToPlayer()
    {
        playerHealth -= playerDamage;
        Debug.Log($"Player health: {playerHealth}");
    }

    // Function to apply damage to the enemy
    void ApplyDamageToEnemy(ref int health, GameObject enemy)
    {
        health -= playerDamage;
        DisplayDamageNumber(enemy, playerDamage);
        Debug.Log($"Enemy health: {health}");
    }

    // Function to start a level by setting up enemies, NPC, and BOSS
    void StartLevel(int level)
    {
        switch (level)
        {
            case 1:
                currentEnemy = enemyCapsuleA;
                currentNPC = npcCylinderA;
                extraEnemy1 = enemyCapsuleAExtra1;
                extraEnemy2 = enemyCapsuleAExtra2;
                currentBoss = bossSphereA;
                break;

            case 2:
                currentEnemy = enemyCapsuleB;
                currentNPC = npcCylinderB;
                extraEnemy1 = enemyCapsuleBExtra1;
                extraEnemy2 = enemyCapsuleBExtra2;
                currentBoss = bossSphereB;
                break;

            case 3:
                currentEnemy = enemyCapsuleC;
                currentNPC = npcCylinderC;
                extraEnemy1 = enemyCapsuleCExtra1;
                extraEnemy2 = enemyCapsuleCExtra2;
                currentBoss = bossSphereC;
                break;

            case 4:
                currentEnemy = enemyCapsuleD;
                currentNPC = npcCylinderD;
                extraEnemy1 = enemyCapsuleDExtra1;
                extraEnemy2 = enemyCapsuleDExtra2;
                currentBoss = bossSphereD;
                break;

            case 5:
                currentEnemy = enemyCapsuleE;
                currentNPC = npcCylinderE;
                extraEnemy1 = enemyCapsuleEExtra1;
                extraEnemy2 = enemyCapsuleEExtra2;
                currentBoss = bossSphereE;
                break;

            default:
                Debug.LogError("Invalid level.");
                return;
        }

        // Reset enemy healths
        currentEnemyHealth = enemyHealthBase;
        extraEnemy1Health = enemyHealthBase;
        extraEnemy2Health = enemyHealthBase;
        bossHealth = 10;

        // Reactivate enemies and deactivate NPC and BOSS at the start of the level
        currentEnemy.SetActive(true);
        extraEnemy1.SetActive(true);
        extraEnemy2.SetActive(true);
        currentNPC.SetActive(false);
        currentBoss.SetActive(false);

        AttachDebugLetter(currentEnemy, "E");
        AttachDebugLetter(extraEnemy1, "E1");
        AttachDebugLetter(extraEnemy2, "E2");

        // Reset level states
        areAllEnemiesDestroyed = false;
        isBossDestroyed = false;
        isDialogueShown = false;
    }

    // Function to hide all enemies, NPCs, and bosses
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

        bossSphereA.SetActive(false);
        bossSphereB.SetActive(false);
        bossSphereC.SetActive(false);
        bossSphereD.SetActive(false);
        bossSphereE.SetActive(false);
    }

    // Function to get the label for the current NPC
    string GetLabelForCurrentLevel()
    {
        switch (currentLevel)
        {
            case 1: return "A";
            case 2: return "B";
            case 3: return "C";
            case 4: return "D";
            case 5: return "E";
            default: return "?";
        }
    }

    // Function to show the dialogue for the current NPC
    void ShowNPCDialogue(string npcLabel)
    {
        if (currentNpcLabel != null)
        {
            dialogueTextMesh = currentNpcLabel.GetComponent<TextMesh>();
            if (dialogueTextMesh != null)
            {
                dialogueTextMesh.text = $"Thank you for saving me on level {npcLabel}!";
            }
        }
    }
}
