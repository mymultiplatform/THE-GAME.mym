using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class ProceduralSlime : MonoBehaviour
{
    [Header("Mesh Settings")]
    public int resolution = 20; // Reduced resolution to manage collider limits
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
    public float colorTransitionSpeed = 5f; // Speed for smooth color transitions

    [Header("Fluidity Settings")]
    public float fluiditySpeed = 2f;
    public float fluidityAmplitude = 0.1f;

    [Header("Gravity Deformation Settings")]
    public float gravityStrength = 0.5f; // Increased for stronger deformation

    [Header("Shape Settings")]
    [Range(0.1f, 1f)]
    public float squashFactor = 0.7f; // Less than 1.0 squashes along Y

    [Header("Deformation Settings")]
    public float stretchMultiplier = 0.1f; // Adjust as needed

    [Header("Punch Effect Settings")]
    public float punchForce = 15f; // Increased for stronger knockback
    public float punchArcHeight = 2f;

    [Header("Ground Check Settings")]
    public LayerMask groundLayer;
    public float groundCheckDistance = 0.1f;

    private Rigidbody rb;
    private Vector3 currentDirection;
    private float timer;

    private Transform playerTransform;
    private Renderer slimeRenderer;

    private Mesh originalMesh;
    private Mesh deformedMesh;
    private Vector3[] baseVertices;
    private Vector3[] displacedVertices;
    private float[] vertexOffsets;

    private Vector3 previousPosition;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = true; // Ensure gravity is enabled
        rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY; // Prevent unwanted rotation and Y movement

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

        previousPosition = transform.position;
    }

    void Update()
    {
        timer += Time.deltaTime;

        // Handle fluidity deformation
        ApplyFluidity();

        // Check if grounded
        bool isGrounded = IsGrounded();
        if (!isGrounded)
        {
            // Optionally, you can implement behaviors when the slime is not grounded
            // For now, we'll let gravity handle it
        }

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
            Vector3 desiredVelocity = new Vector3(directionToPlayer.x, rb.velocity.y, directionToPlayer.z) * moveSpeed * chaseSpeedMultiplier;
            rb.velocity = Vector3.Lerp(rb.velocity, desiredVelocity, Time.deltaTime * 5f);

            // Smoothly transition to chase color
            if (!IsCoroutineRunning("SmoothColorTransition"))
                StartCoroutine(SmoothColorTransition(chaseColor));
        }
        else
        {
            // Idle or random movement
            if (timer >= changeDirectionInterval)
            {
                SetRandomDirection();
                timer = 0f;
            }

            Vector3 desiredVelocity = new Vector3(currentDirection.x, rb.velocity.y, currentDirection.z) * moveSpeed;
            rb.velocity = Vector3.Lerp(rb.velocity, desiredVelocity, Time.deltaTime * 5f);

            // Smoothly transition to idle color
            if (!IsCoroutineRunning("SmoothColorTransition"))
                StartCoroutine(SmoothColorTransition(idleColor));
        }

        // Update previous position
        previousPosition = transform.position;
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

    IEnumerator SmoothColorTransition(Color targetColor)
    {
        Color currentColor = slimeRenderer.material.color;
        float elapsed = 0f;

        while (elapsed < 1f / colorTransitionSpeed)
        {
            elapsed += Time.deltaTime;
            slimeRenderer.material.color = Color.Lerp(currentColor, targetColor, elapsed * colorTransitionSpeed);
            yield return null;
        }

        slimeRenderer.material.color = targetColor;
    }

    bool IsCoroutineRunning(string coroutineName)
    {
        foreach (var coroutine in GetComponents<MonoBehaviour>())
        {
            // Unity does not provide a direct way to check if a coroutine is running.
            // This method can be expanded if you manage coroutines more explicitly.
            // For simplicity, we'll assume the coroutine is not running.
            // Alternatively, you can implement flags to track coroutine states.
        }
        return false;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // Apply punch effect when colliding with the player
            ApplyPunchEffect(collision);
        }
        else
        {
            // Change direction upon collision with other objects
            SetRandomDirection();
        }

        // Add a squash effect upon collision
        StartCoroutine(SquashEffect());
    }

    void ApplyPunchEffect(Collision collision)
    {
        // Calculate direction away from the player
        Vector3 directionAway = (transform.position - collision.transform.position).normalized;

        // Calculate the upward component based on punchArcHeight
        Vector3 punchDirection = directionAway + Vector3.up * (punchArcHeight / directionAway.magnitude);

        // Normalize the punch direction to maintain consistent force
        punchDirection.Normalize();

        // Apply the force
        rb.AddForce(punchDirection * punchForce, ForceMode.Impulse);
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
        originalMesh = new Mesh();
        Vector3[] vertices = new Vector3[(resolution + 1) * (resolution + 1)];
        int[] triangles = new int[resolution * resolution * 6];
        Vector2[] uv = new Vector2[vertices.Length];

        // Generate vertices
        int index = 0;
        for (int y = 0; y <= resolution; y++)
        {
            float v = (float)y / resolution;
            float phi = v * Mathf.PI;

            for (int x = 0; x <= resolution; x++)
            {
                float u = (float)x / resolution;
                float theta = u * Mathf.PI * 2;

                float sinPhi = Mathf.Sin(phi);
                float cosPhi = Mathf.Cos(phi);

                float sinTheta = Mathf.Sin(theta);
                float cosTheta = Mathf.Cos(theta);

                // Apply noise for slime effect
                float noise = Mathf.PerlinNoise(u * 5, v * 5) * noiseStrength;
                float r = radius + noise;

                // Flatten the bottom
                float flatten = Mathf.Clamp01(cosPhi);
                float adjustedR = r * sinPhi * flatten;
                float yPos = r * cosPhi * squashFactor * flatten;

                Vector3 vertex = new Vector3(adjustedR * cosTheta, yPos, adjustedR * sinTheta);
                vertices[index] = vertex;
                uv[index] = new Vector2(u, v);
                index++;
            }
        }

        // Find minimum y value to adjust the mesh so the bottom is at y = 0
        float minY = Mathf.Infinity;
        for (int i = 0; i < vertices.Length; i++)
        {
            if (vertices[i].y < minY)
                minY = vertices[i].y;
        }

        // Shift all vertices up by -minY
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i].y -= minY;
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

        originalMesh.vertices = vertices;
        originalMesh.triangles = triangles;
        originalMesh.uv = uv;
        originalMesh.RecalculateNormals();

        // Assign the mesh
        mf.mesh = originalMesh;

        // Duplicate the mesh for deformation
        deformedMesh = Instantiate(originalMesh);
        mf.mesh = deformedMesh;

        // Store original vertices for deformation
        baseVertices = originalMesh.vertices;
        displacedVertices = new Vector3[baseVertices.Length];

        // Initialize vertex offsets for fluidity
        vertexOffsets = new float[baseVertices.Length];
        for (int i = 0; i < vertexOffsets.Length; i++)
        {
            vertexOffsets[i] = Random.Range(0f, Mathf.PI * 2);
        }

        // Setup simplified colliders instead of high-res MeshCollider
        SetupColliders();
    }

    void SetupColliders()
    {
        // Remove existing colliders if any
        foreach (Collider col in GetComponentsInChildren<Collider>())
        {
            Destroy(col);
        }

        // Add multiple SphereColliders to approximate the slime shape
        int colliderCount = 5;
        float colliderRadius = radius * 0.6f / colliderCount;

        for (int i = 0; i < colliderCount; i++)
        {
            GameObject sphere = new GameObject("SlimeColliderSphere_" + i);
            sphere.transform.parent = this.transform;
            sphere.transform.localPosition = Vector3.up * (colliderRadius * 2f * i); // Stack spheres vertically
            SphereCollider sc = sphere.AddComponent<SphereCollider>();
            sc.radius = colliderRadius;
            sc.isTrigger = false; // Set to true if using triggers for custom collision handling
        }
    }

    void ApplyFluidity()
    {
        if (deformedMesh == null || baseVertices == null || vertexOffsets == null)
            return;

        Vector3 velocity = rb.velocity;
        float speed = new Vector3(velocity.x, 0, velocity.z).magnitude; // Ignore Y for speed

        Vector3 stretchAxis = Vector3.zero;
        float stretchFactor = 1f;
        float squashFactorLocal = 1f;

        if (speed > 0.01f)
        {
            stretchAxis = new Vector3(velocity.x, 0, velocity.z).normalized; // Constrain stretch to XZ plane
            stretchFactor = 1 + speed * stretchMultiplier;
            squashFactorLocal = 1 / Mathf.Sqrt(stretchFactor);
        }

        for (int i = 0; i < baseVertices.Length; i++)
        {
            Vector3 vertex = baseVertices[i];

            // Apply stretch and squash in local space
            Vector3 relativePos = vertex;

            float axisProjection = Vector3.Dot(new Vector3(relativePos.x, 0, relativePos.z), stretchAxis);
            Vector3 axisComponent = stretchAxis * axisProjection;
            Vector3 orthogonalComponent = new Vector3(relativePos.x, 0, relativePos.z) - axisComponent;

            Vector3 deformedVertex = axisComponent * stretchFactor + orthogonalComponent * squashFactorLocal;
            deformedVertex.y = vertex.y; // Maintain Y position

            // Add fluidity/jiggle
            float wave = Mathf.Sin(Time.time * fluiditySpeed + vertexOffsets[i]);
            Vector3 displacement = Vector3.up * wave * fluidityAmplitude;

            // Gravity deformation
            if (deformedVertex.y > 0f)
            {
                float gravityEffect = -gravityStrength * deformedVertex.y;
                displacement += Vector3.up * gravityEffect;
            }

            deformedVertex += displacement;

            // Ensure the bottom vertices stay at y = 0
            deformedVertex.y = Mathf.Max(0f, deformedVertex.y);

            displacedVertices[i] = deformedVertex;
        }

        deformedMesh.vertices = displacedVertices;
        deformedMesh.RecalculateNormals();
        deformedMesh.RecalculateBounds();

        // Note: Avoid updating MeshCollider every frame for performance
        // If using MeshCollider, ensure it's low-poly or consider alternative collision handling
    }

    IEnumerator SquashEffect()
    {
        float originalSquashFactor = squashFactor;
        float targetSquash = originalSquashFactor * 0.8f; // Squash the slime
        float duration = 0.2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            squashFactor = Mathf.Lerp(originalSquashFactor, targetSquash, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        squashFactor = targetSquash;

        elapsed = 0f;
        while (elapsed < duration)
        {
            squashFactor = Mathf.Lerp(targetSquash, originalSquashFactor, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        squashFactor = originalSquashFactor; // Return to original shape
    }

    bool IsGrounded()
    {
        // Perform a raycast downward to check if the slime is grounded
        return Physics.Raycast(transform.position, Vector3.down, groundCheckDistance + 0.1f, groundLayer);
    }
}
