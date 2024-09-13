using UnityEngine;
using System.Collections;

public class GameLogic : MonoBehaviour
{
    // Public references for the player cube (Player "P")
    public GameObject playerCube;

    // Array to hold enemies per level
    public GameObject[] enemiesLevelA;
    public GameObject[] enemiesLevelB;
    public GameObject[] enemiesLevelC;
    public GameObject[] enemiesLevelD;
    public GameObject[] enemiesLevelE;

    // Public references for NPCs per level
    public GameObject npcA;
    public GameObject npcB;
    public GameObject npcC;
    public GameObject npcD;
    public GameObject npcE;

    // Bosses per level
    public GameObject bossB;
    public GameObject bossC;
    public GameObject bossD;
    public GameObject bossE;

    // Player stats
    private int playerHealth = 100;
    public int enemyDamage = 1;
    public int playerDamage = 1;

    // Current level, total levels, and current enemies remaining
    private int currentLevel = 1;
    private const int totalLevels = 5;
    private int enemiesRemaining;

    // NPC dialogue-related variables
    private bool isDialogueShown = false;

    // Distance threshold for brute-force collision detection
    public float collisionDistanceThreshold = 1.5f;

    // Cinematic transition time
    public float cinematicDuration = 10.0f;

    // This function will be called at the start of the game
    void Start()
    {
        StartLevel(1);
        AttachDebugLetter(playerCube, "P");

        Debug.Log("Starting game logic with level progression.");
    }

    // Update is called once per frame
    void Update()
    {
        CheckCollisions();
    }

    // Function to check for collisions and handle enemy/player interaction
    void CheckCollisions()
    {
        foreach (GameObject enemy in GetCurrentEnemies())
        {
            if (enemy != null && Vector3.Distance(playerCube.transform.position, enemy.transform.position) < collisionDistanceThreshold)
            {
                // Damage the enemy
                enemy.GetComponent<Enemy>().TakeDamage(playerDamage);

                // Player also takes damage
                playerHealth -= enemyDamage;

                if (enemy.GetComponent<Enemy>().IsDead())
                {
                    Destroy(enemy);
                    enemiesRemaining--;
                    Debug.Log("Enemy destroyed.");

                    if (enemiesRemaining == 0 && IsBossLevel())
                    {
                        HandleBossEncounter();
                    }
                    else if (enemiesRemaining == 0)
                    {
                        ShowNPCDialogue();
                    }
                }
            }
        }

        if (IsBossLevel() && Vector3.Distance(playerCube.transform.position, GetBoss().transform.position) < collisionDistanceThreshold)
        {
            GetBoss().GetComponent<Enemy>().TakeDamage(playerDamage);
            playerHealth -= enemyDamage;

            if (GetBoss().GetComponent<Enemy>().IsDead())
            {
                Destroy(GetBoss());
                ShowNPCDialogue();
            }
        }

        if (playerHealth <= 0)
        {
            Debug.Log("Player died.");
            // Handle player death here (restart level or show game over screen)
        }
    }

    // Function to start a specific level
    void StartLevel(int level)
    {
        Debug.Log($"Starting Level {level}");
        currentLevel = level;

        switch (level)
        {
            case 1:
                enemiesRemaining = enemiesLevelA.Length;
                ActivateEnemies(enemiesLevelA);
                npcA.SetActive(false);
                break;
            case 2:
                enemiesRemaining = enemiesLevelB.Length;
                ActivateEnemies(enemiesLevelB);
                npcB.SetActive(false);
                break;
            case 3:
                enemiesRemaining = enemiesLevelC.Length;
                ActivateEnemies(enemiesLevelC);
                npcC.SetActive(false);
                break;
            case 4:
                enemiesRemaining = enemiesLevelD.Length;
                ActivateEnemies(enemiesLevelD);
                npcD.SetActive(false);
                break;
            case 5:
                enemiesRemaining = enemiesLevelE.Length;
                ActivateEnemies(enemiesLevelE);
                npcE.SetActive(false);
                break;
        }
    }

    // Function to show NPC dialogue when player collides with NPC
    void ShowNPCDialogue()
    {
        Debug.Log("Displaying NPC dialogue.");
        // Show NPC dialogue after all enemies are defeated
        GameObject npc = GetCurrentNPC();
        npc.SetActive(true);
        AttachDebugLetter(npc, "NPC");
        StartCoroutine(TransitionToNextLevel());
    }

    // Coroutine to handle transition to the next level
    IEnumerator TransitionToNextLevel()
    {
        yield return new WaitForSeconds(cinematicDuration);
        if (currentLevel < totalLevels)
        {
            StartLevel(currentLevel + 1);
        }
        else
        {
            Debug.Log("All levels complete. Game finished.");
        }
    }

    // Activate all enemies for the level
    void ActivateEnemies(GameObject[] enemies)
    {
        foreach (GameObject enemy in enemies)
        {
            enemy.SetActive(true);
        }
    }

    // Helper function to get current enemies for the level
    GameObject[] GetCurrentEnemies()
    {
        switch (currentLevel)
        {
            case 1: return enemiesLevelA;
            case 2: return enemiesLevelB;
            case 3: return enemiesLevelC;
            case 4: return enemiesLevelD;
            case 5: return enemiesLevelE;
            default: return new GameObject[0];
        }
    }

    // Helper function to get the current NPC
    GameObject GetCurrentNPC()
    {
        switch (currentLevel)
        {
            case 1: return npcA;
            case 2: return npcB;
            case 3: return npcC;
            case 4: return npcD;
            case 5: return npcE;
            default: return null;
        }
    }

    // Helper function to determine if the level has a boss
    bool IsBossLevel()
    {
        return currentLevel >= 2;
    }

    // Helper function to get the boss for the current level
    GameObject GetBoss()
    {
        switch (currentLevel)
        {
            case 2: return bossB;
            case 3: return bossC;
            case 4: return bossD;
            case 5: return bossE;
            default: return null;
        }
    }

    // Function to handle boss encounters
    void HandleBossEncounter()
    {
        GameObject boss = GetBoss();
        if (boss != null)
        {
            boss.SetActive(true);
            Debug.Log("Boss encounter started.");
        }
    }

    // Attach a debug letter for enemy, NPC, or player
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
        labelObject.transform.position = targetObject.transform.position + new Vector3(0, 2, 0);
        labelObject.transform.SetParent(targetObject.transform);
    }
}
