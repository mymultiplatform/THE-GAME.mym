using UnityEngine;

/// <summary>
/// Makes a cube behave like a living entity by applying gravity,
/// handling collisions, and moving randomly.
/// </summary>
[RequireComponent(typeof(Collider))]
public class AliveCube : MonoBehaviour
{
    [Header("Gravity Settings")]
    [Tooltip("Custom gravity force applied to the cube.")]
    public float gravity = -9.81f;

    [Header("Movement Settings")]
    [Tooltip("Force applied for movement.")]
    public float moveForce = 5f;

    [Tooltip("Time interval (in seconds) to change direction.")]
    public float changeDirectionInterval = 2f;

    private Rigidbody rb;
    private Vector3 currentDirection;
    private float timer;

    void Start()
    {
        // Ensure the cube has a Rigidbody component
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        // Configure Rigidbody
        rb.useGravity = false; // We'll apply custom gravity
        rb.constraints = RigidbodyConstraints.FreezeRotation; // Prevent unwanted rotation

        // Initialize movement direction
        SetRandomDirection();
        timer = 0f;
    }

    void Update()
    {
        // Update timer
        timer += Time.deltaTime;
        if (timer >= changeDirectionInterval)
        {
            SetRandomDirection();
            timer = 0f;
        }

        // Apply custom gravity
        rb.AddForce(Vector3.up * gravity, ForceMode.Acceleration);

        // Apply movement force
        rb.AddForce(currentDirection * moveForce);
    }

    /// <summary>
    /// Sets a new random direction on the XZ plane.
    /// </summary>
    void SetRandomDirection()
    {
        // Generate a random direction vector on the horizontal plane
        currentDirection = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
    }

    /// <summary>
    /// Optional: Handle collision events.
    /// </summary>
    /// <param name="collision">Collision data.</param>
    void OnCollisionEnter(Collision collision)
    {
        // Example: Change direction upon collision
        SetRandomDirection();
    }

    /// <summary>
    /// Optional: Visualize movement direction in the editor.
    /// </summary>
    void OnDrawGizmosSelected()
    {
        // Draw a line indicating the current movement direction
        if (currentDirection != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, transform.position + currentDirection);
        }
    }
}
