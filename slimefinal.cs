using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SlimyEnemy : MonoBehaviour
{
    public int resolution = 32;
    public float radius = 1f;
    public float noiseScale = 0.5f;
    public float smoothness = 1.5f;
    public float wobbleIntensity = 0.2f;
    public float wobbleSpeed = 2.0f;
    public Material slimeMaterial;

    private Mesh mesh;
    private Vector3[] vertices;
    private Vector3[] normals;
    private Vector3[] originalVertices;

    void Start()
    {
        GenerateSlimeMesh();
        ApplySlimeMaterial();
    }

    void Update()
    {
        ApplyWobbleEffect();
    }

    void GenerateSlimeMesh()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;

        vertices = new Vector3[resolution * resolution];
        normals = new Vector3[resolution * resolution];
        originalVertices = new Vector3[resolution * resolution];
        Vector2[] uv = new Vector2[resolution * resolution];
        int[] triangles = new int[(resolution - 1) * (resolution - 1) * 6];

        float step = 2f / (resolution - 1);
        int triIndex = 0;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                int i = x + y * resolution;
                Vector2 percent = new Vector2(x, y) / (resolution - 1);
                Vector3 spherePoint = new Vector3(
                    Mathf.Cos(percent.x * Mathf.PI * 2) * Mathf.Sin(percent.y * Mathf.PI),
                    Mathf.Cos(percent.y * Mathf.PI),
                    Mathf.Sin(percent.x * Mathf.PI * 2) * Mathf.Sin(percent.y * Mathf.PI)
                );

                // Add Perlin noise for the slimy effect
                float noise = Mathf.PerlinNoise(percent.x * noiseScale, percent.y * noiseScale);
                spherePoint *= radius + noise * 0.2f; // Increase displacement

                vertices[i] = spherePoint;
                originalVertices[i] = spherePoint; // Store original for wobble effect
                normals[i] = spherePoint.normalized;
                uv[i] = percent;

                if (x != resolution - 1 && y != resolution - 1)
                {
                    triangles[triIndex] = i;
                    triangles[triIndex + 1] = i + resolution + 1;
                    triangles[triIndex + 2] = i + resolution;
                    triangles[triIndex + 3] = i;
                    triangles[triIndex + 4] = i + 1;
                    triangles[triIndex + 5] = i + resolution + 1;
                    triIndex += 6;
                }
            }
        }

        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uv;
        mesh.triangles = triangles;
    }

    void ApplyWobbleEffect()
    {
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 offset = originalVertices[i];
            float wobble = Mathf.Sin(Time.time * wobbleSpeed + offset.x * 2.0f + offset.y * 2.0f) * wobbleIntensity;
            vertices[i] = originalVertices[i] + normals[i] * wobble;
        }

        mesh.vertices = vertices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    void ApplySlimeMaterial()
    {
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        renderer.material = slimeMaterial;
    }
}
