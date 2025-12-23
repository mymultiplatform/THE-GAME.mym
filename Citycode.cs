using UnityEngine;

public class ProceduralCityGenerator : MonoBehaviour
{
    [Header("City Grid")]
    public int cityCols = 8;
    public int cityRows = 8;
    public float blockSpacing = 80f;      // distance between intersections
    public float roadWidth = 24f;
    public float margin = 6f;             // inset inside each block
    public float roadThickness = 0.2f;

    [Header("Building Heights")]
    public float floorHeight = 3.6f;      // ~12ft in meters-ish
    public int minLevels = 3;
    public int maxLevels = 6;
    [Range(0f, 1f)] public float rowTallProb = 0.15f; // 5-6
    [Range(0f, 1f)] public float rowMidProb = 0.40f;  // 4-5

    [Header("Block Fill / Subdivision")]
    [Range(0f, 1f)] public float blockFillChance = 0.85f;  // whether a block gets buildings
    [Range(0f, 1f)] public float sliceSkipChance = 0.20f;  // skip individual slice
    public int minSlices = 1;
    public int maxSlices = 4;

    [Header("Materials (optional, auto-generated if null)")]
    public Material roadMaterial;
    public Material[] buildingMaterials;

    [Header("Spawn/Centering")]
    public bool centerCityOnThisObject = true; // city centered around this transform
    public bool spawnPlayerAtCityCenter = false;
    public Transform player; // optional (tag-based fallback below)
    public float playerSpawnHeight = 2f;

    [Header("Debug")]
    public bool addColliders = true;
    public bool clearPreviousOnPlay = true;
    public int randomSeed = 0; // 0 = random each play

    Transform _root;
    int[] _rowHeights;

    void Start()
    {
        if (randomSeed != 0) Random.InitState(randomSeed);

        if (clearPreviousOnPlay) ClearPrevious();
        EnsureMaterials();

        _root = new GameObject("CityRoot").transform;
        _root.SetParent(transform, false);

        _rowHeights = BuildRowHeightProfile(cityRows);

        Vector3 cityOrigin = GetCityOrigin();
        BuildRoads(cityOrigin);
        BuildBuildings(cityOrigin);

        if (spawnPlayerAtCityCenter)
            SpawnPlayerAtCenter(cityOrigin);
    }

    Vector3 GetCityOrigin()
    {
        // City spans (cols * spacing) by (rows * spacing) in X/Z between intersections
        float width = cityCols * blockSpacing;
        float depth = cityRows * blockSpacing;

        // We build from a bottom-left origin; optionally shift so it’s centered around this object
        if (!centerCityOnThisObject)
            return Vector3.zero;

        Vector3 center = transform.position;
        return new Vector3(center.x - width * 0.5f, center.y, center.z - depth * 0.5f);
    }

    int[] BuildRowHeightProfile(int rows)
    {
        var heights = new int[rows];

        for (int r = 0; r < rows; r++)
        {
            float roll = Random.value;
            if (roll < rowTallProb) heights[r] = Random.Range(5, 7);        // 5-6
            else if (roll < rowMidProb) heights[r] = Random.Range(4, 6);    // 4-5
            else heights[r] = Random.Range(3, 5);                            // 3-4
        }

        return heights;
    }

    void BuildRoads(Vector3 origin)
    {
        Transform roadsRoot = new GameObject("Roads").transform;
        roadsRoot.SetParent(_root, false);

        float cityWidth = cityCols * blockSpacing;
        float cityDepth = cityRows * blockSpacing;

        // Horizontal roads: rows+1
        for (int r = 0; r <= cityRows; r++)
        {
            float z = origin.z + r * blockSpacing;
            Vector3 pos = new Vector3(origin.x + cityWidth * 0.5f, origin.y - roadThickness * 0.5f, z);
            Vector3 size = new Vector3(cityWidth + roadWidth, roadThickness, roadWidth);

            CreateSlab("Road_H_" + r, pos, size, roadMaterial, roadsRoot);
        }

        // Vertical roads: cols+1
        for (int c = 0; c <= cityCols; c++)
        {
            float x = origin.x + c * blockSpacing;
            Vector3 pos = new Vector3(x, origin.y - roadThickness * 0.5f, origin.z + cityDepth * 0.5f);
            Vector3 size = new Vector3(roadWidth, roadThickness, cityDepth + roadWidth);

            CreateSlab("Road_V_" + c, pos, size, roadMaterial, roadsRoot);
        }
    }

    void BuildBuildings(Vector3 origin)
    {
        Transform bldgRoot = new GameObject("Buildings").transform;
        bldgRoot.SetParent(_root, false);

        for (int c = 0; c < cityCols; c++)
        {
            for (int r = 0; r < cityRows; r++)
            {
                if (Random.value > blockFillChance)
                    continue;

                // Block bounds between roads
                float xMin = origin.x + c * blockSpacing + roadWidth * 0.5f;
                float xMax = origin.x + (c + 1) * blockSpacing - roadWidth * 0.5f;
                float zMin = origin.z + r * blockSpacing + roadWidth * 0.5f;
                float zMax = origin.z + (r + 1) * blockSpacing - roadWidth * 0.5f;

                // Inset by margin
                xMin += margin; xMax -= margin;
                zMin += margin; zMax -= margin;

                float w = xMax - xMin;
                float d = zMax - zMin;
                if (w < 2f || d < 2f) continue;

                // Row-dominant height + small variation (-1..+1), clamped
                int baseLevels = _rowHeights[r];
                int variation = Random.Range(-1, 2);
                int levels = Mathf.Clamp(baseLevels + variation, minLevels, maxLevels);

                // Optional extra small chance of variation like your Tkinter
                if (Random.value < 0.2f)
                    levels = Mathf.Clamp(baseLevels + (Random.value < 0.5f ? -1 : 1), minLevels, maxLevels);

                bool splitAlongX = Random.value < 0.5f;
                int n = Random.Range(minSlices, maxSlices + 1);

                if (splitAlongX)
                {
                    float step = w / n;
                    for (int i = 0; i < n; i++)
                    {
                        if (Random.value < sliceSkipChance) continue;

                        float bx1 = xMin + i * step;
                        float bx2 = xMin + (i + 1) * step;
                        CreateBuildingBox(bldgRoot, bx1, bx2, zMin, zMax, levels);
                    }
                }
                else
                {
                    float step = d / n;
                    for (int i = 0; i < n; i++)
                    {
                        if (Random.value < sliceSkipChance) continue;

                        float bz1 = zMin + i * step;
                        float bz2 = zMin + (i + 1) * step;
                        CreateBuildingBox(bldgRoot, xMin, xMax, bz1, bz2, levels);
                    }
                }
            }
        }
    }

    void CreateBuildingBox(Transform parent, float x1, float x2, float z1, float z2, int levels)
    {
        float w = Mathf.Abs(x2 - x1);
        float d = Mathf.Abs(z2 - z1);
        if (w < 1.5f || d < 1.5f) return;

        float height = levels * floorHeight;

        Vector...

<...etc...>
