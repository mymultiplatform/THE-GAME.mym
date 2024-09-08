using UnityEngine;

public class gamelogicmym : MonoBehaviour
{
    // Public references for the cube (player), capsule (enemy), and cylinder (NPC)
    public GameObject playerCube;
    public GameObject enemyCapsule;
    public GameObject npcCylinder;

    // Distance threshold for brute-force collision detection
    public float collisionDistanceThreshold = 1.5f;

    // NPC dialogue-related variables
    private bool isEnemyDestroyed = false;
    private bool isDialogueShown = false;
    private GameObject npcLabel;
    private TextMesh dialogueTextMesh;

    // This function will be called at the start of the game
    void Start()
    {
        // Check if playerCube and enemyCapsule are assigned in the inspector
        if (playerCube != null && enemyCapsule != null && npcCylinder != null)
        {
            // Add a floating letter "A" above both objects for debug purposes
            AttachDebugLetter(playerCube);
            AttachDebugLetter(enemyCapsule);

            // NPC is initially hidden until the enemy is destroyed
            npcCylinder.SetActive(false);

            // Confirm components for brute force
            BruteForceColliderCheck(playerCube);
            BruteForceColliderCheck(enemyCapsule);
            BruteForceColliderCheck(npcCylinder);

            Debug.Log("Starting game logic with NPC interaction.");
        }
        else
        {
            Debug.LogError("Either playerCube, enemyCapsule, or npcCylinder is not attached in the inspector!");
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Check distance between playerCube and enemyCapsule to simulate brute-force collision detection
        if (!isEnemyDestroyed && Vector3.Distance(playerCube.transform.position, enemyCapsule.transform.position) < collisionDistanceThreshold)
        {
            // Log the collision detection
            Debug.Log("Brute-force collision detected between playerCube and enemyCapsule!");

            // Destroy the enemyCapsule
            Destroy(enemyCapsule);
            Debug.Log("Enemy capsule destroyed.");

            // Set NPC visible now that the enemy is destroyed
            npcCylinder.SetActive(true);
            AttachDebugLetter(npcCylinder);
            Debug.Log("NPC is now visible.");

            // Set the flag that the enemy has been destroyed
            isEnemyDestroyed = true;
        }

        // Check distance between playerCube and npcCylinder for NPC interaction
        if (isEnemyDestroyed && !isDialogueShown && Vector3.Distance(playerCube.transform.position, npcCylinder.transform.position) < collisionDistanceThreshold)
        {
            // Show dialogue next to the NPC
            ShowNPCDialogue();
            isDialogueShown = true;
            Debug.Log("NPC dialogue displayed.");
        }
    }

    // Function to attach a letter "A" above the assigned object for debugging
    void AttachDebugLetter(GameObject targetObject)
    {
        // Create a new TextMesh object for the floating "A"
        GameObject labelA = new GameObject("DebugLabel");
        TextMesh textMesh = labelA.AddComponent<TextMesh>();

        // Set the text to "A" and configure appearance
        textMesh.text = "A";
        textMesh.fontSize = 10;
        textMesh.color = Color.red;

        // Position the "A" slightly above the targetObject
        labelA.transform.position = targetObject.transform.position + new Vector3(0, 2, 0);  // Adjust height here
        labelA.transform.SetParent(targetObject.transform);  // Ensure the label follows the object

        // Save the label for the NPC so we can add dialogue later
        if (targetObject == npcCylinder)
        {
            npcLabel = labelA;
        }
    }

    // Function to show NPC dialogue when player collides with NPC
    void ShowNPCDialogue()
    {
        // Create a new TextMesh object for the dialogue text
        GameObject dialogueObject = new GameObject("NPCDialogue");
        dialogueTextMesh = dialogueObject.AddComponent<TextMesh>();

        // Set the dialogue text and configure appearance
        dialogueTextMesh.text = "Thank you for destroying the enemy";
        dialogueTextMesh.fontSize = 24;
        dialogueTextMesh.color = Color.white;

        // Position the dialogue next to the letter "A" above the NPC
        dialogueObject.transform.position = npcLabel.transform.position + new Vector3(1.5f, 0, 0);  // Adjust position here
        dialogueObject.transform.SetParent(npcCylinder.transform);  // Ensure the dialogue follows the NPC
    }

    // Brute-force check to ensure that colliders and rigidbodies are properly attached
    void BruteForceColliderCheck(GameObject obj)
    {
        // Check if the object has a collider
        if (obj.GetComponent<Collider>() == null)
        {
            Debug.LogWarning(obj.name + " does not have a Collider. Adding a BoxCollider.");
            obj.AddComponent<BoxCollider>();  // Add a default BoxCollider if none exists
        }

        // Check if the object has a Rigidbody (only the player needs this, typically)
        if (obj == playerCube && obj.GetComponent<Rigidbody>() == null)
        {
            Debug.LogWarning(playerCube.name + " does not have a Rigidbody. Adding a Rigidbody.");
            Rigidbody rb = obj.AddComponent<Rigidbody>();
            rb.isKinematic = true;  // Make it kinematic to ensure it's not affected by physics unless explicitly desired
        }
    }
}
