using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class ProceduralSlime : MonoBehaviour
{
    [Header("Mesh Settings")]
    public int resolution = 20;
    public float radius = 1f;
    public float noiseStrength = 0.2f;

    [Header("Movement Settings")]
    public float moveSpeed = 3f;
    public float changeDirectionInterval = 3f;
    public float detectionRadius = 5f;

    [Header("Chase Settings")]
    public float chaseSpeedMultiplier = 1.5f;

    [Header("Visual Feedback")]
    public Color idleColor = Color.green;
    public Color chaseColor = Color.red;

    private Rigidbody rb;
    private Vector3 currentDirection;
    private float timer;

    private Transform playerTransform;
    private Renderer slimeRenderer;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        slimeRenderer = GetComponent<Renderer>();

        GenerateSlimeMesh();

        // Find the player by tag. Ensure your player GameObject has the tag "Player".
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerTransform = player.transform;
        else
            Debug.LogWarning("Player not found! Make sure the player has the tag 'Player'.");

        // Initialize movement direction
        SetRandomDirection();
        timer = 0f;

        // Set initial color
        SetColor(idleColor);
    }

    void Update()
    {
        timer += Time.deltaTime;

        // Check distance to player
        bool isPlayerNearby = false;
        float distanceToPlayer = Mathf.Infinity;
        if (playerTransform != null)
        {
            distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            isPlayerNearby = distanceToPlayer <= detectionRadius;
        }

        if (isPlayerNearby)
        {
            // Chase the player
            Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;
            rb.velocity = directionToPlayer * moveSpeed * chaseSpeedMultiplier;

            // Change color to indicate chasing
            SetColor(chaseColor);
        }
        else
        {
            // Idle or random movement
            if (timer >= changeDirectionInterval)
            {
                SetRandomDirection();
                timer = 0f;
            }

            rb.velocity = currentDirection * moveSpeed;

            // Change color to indicate idle
            SetColor(idleColor);
        }
    }

    void SetRandomDirection()
    {
        Vector2 randomDir = Random.insideUnitCircle.normalized;
        currentDirection = new Vector3(randomDir.x, 0, randomDir.y);
    }

    void SetColor(Color color)
    {
        if (slimeRenderer != null)
        {
            slimeRenderer.material.color = color;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Change direction upon collision
        SetRandomDirection();
    }

    void OnDrawGizmosSelected()
    {
        // Draw detection radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }

    void GenerateSlimeMesh()
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[(resolution + 1) * (resolution + 1)];
        int[] triangles = new int[resolution * resolution * 6];
        Vector2[] uv = new Vector2[vertices.Length];

        // Generate vertices
        for (int y = 0; y <= resolution; y++)
        {
            for (int x = 0; x <= resolution; x++)
            {
                int index = y * (resolution + 1) + x;
                float u = (float)x / resolution;
                float v = (float)y / resolution;
                float theta = u * Mathf.PI * 2;
                float phi = v * Mathf.PI;

                float sinPhi = Mathf.Sin(phi);
                float cosPhi = Mathf.Cos(phi);
                float sinTheta = Mathf.Sin(theta);
                float cosTheta = Mathf.Cos(theta);

                // Apply noise for slime effect
                float noise = Mathf.PerlinNoise(u * 5, v * 5) * noiseStrength;
                float r = radius + noise;

                Vector3 vertex = new Vector3(r * sinPhi * cosTheta, r * cosPhi, r * sinPhi * sinTheta);
                vertices[index] = vertex;
                uv[index] = new Vector2(u, v);
            }
        }

        // Generate triangles
        int tri = 0;
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                int current = y * (resolution + 1) + x;
                int next = current + resolution + 1;

                triangles[tri++] = current;
                triangles[tri++] = next;
                triangles[tri++] = current + 1;

                triangles[tri++] = current + 1;
                triangles[tri++] = next;
                triangles[tri++] = next + 1;
            }
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        mesh.RecalculateNormals();

        mf.mesh = mesh;

        // Add a spherical collider
        SphereCollider collider = gameObject.AddComponent<SphereCollider>();
        collider.radius = radius + noiseStrength;
    }
}
