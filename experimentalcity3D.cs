using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MasterCityGame : MonoBehaviour
{
    // =========================================================
    // CITY GENERATION
    // =========================================================
    [Header("City Grid")]
    public int cityCols = 8;
    public int cityRows = 8;
    public float blockSpacing = 80f;      // distance between intersections
    public float roadWidth = 24f;
    public float margin = 6f;             // inset inside each block
    public float roadThickness = 0.2f;
    public bool centerCityOnThisObject = true;

    [Header("Building Heights")]
    public float floorHeight = 3.6f;      // ~12ft
    public int minLevels = 3;
    public int maxLevels = 6;
    [Range(0f, 1f)] public float rowTallProb = 0.15f; // 5-6
    [Range(0f, 1f)] public float rowMidProb = 0.40f;  // 4-5

    [Header("Block Fill / Subdivision")]
    [Range(0f, 1f)] public float blockFillChance = 0.85f;
    [Range(0f, 1f)] public float sliceSkipChance = 0.20f;
    public int minSlices = 1;
    public int maxSlices = 4;

    [Header("Materials (optional, auto-generated if null)")]
    public Material roadMaterial;
    public Material[] buildingMaterials;

    [Header("City Debug")]
    public bool addColliders = true;
    public bool clearPreviousOnPlay = true;
    public int randomSeed = 0; // 0 = random each play

    // =========================================================
    // COINS (ELLIPSE CLUSTERING)
    // =========================================================
    [Header("Coins (Ellipse-Clustered On Roads)")]
    public GameObject coinPrefab;
    public int coinsPerRoad = 160;                 // per road slab (each horizontal & vertical)
    public int coinsPerCluster = 12;               // how many coins in each cluster
    public float coinHeight = 0.6f;
    public float coinPickupRadius = 1.6f;
    public float coinCheckInterval = 0.10f;

    [Tooltip("Ellipse 'a' radius (along road length). Uses ellipse equation: (x^2/a^2) + (y^2/b^2) <= 1")]
    public float ellipseA = 18f;

    [Tooltip("Ellipse 'b' radius (across road width). Keep <= roadWidth/2.")]
    public float ellipseB = 6f;

    [Tooltip("Perlin noise scale for cluster placement")]
    public float coinNoiseScale = 0.035f;

    [Range(0f, 1f)]
    [Tooltip("Higher = fewer clusters")]
    public float coinNoiseThreshold = 0.55f;

    public bool coinsRespawnEachLevel = false;

    // =========================================================
    // GAME LOGIC (LEVELS / ENEMIES / NPC)
    // =========================================================
    [Header("Game Actors (drag your scene objects here)")]
    public GameObject playerCube;

    public GameObject enemyCapsuleA, npcCylinderA;
    public GameObject enemyCapsuleB, npcCylinderB;
    public GameObject enemyCapsuleC, npcCylinderC;
    public GameObject enemyCapsuleD, npcCylinderD;
    public GameObject enemyCapsuleE, npcCylinderE;

    public GameObject enemyCapsuleAExtra1, enemyCapsuleAExtra2;
    public GameObject enemyCapsuleBExtra1, enemyCapsuleBExtra2;
    public GameObject enemyCapsuleCExtra1, enemyCapsuleCExtra2;
    public GameObject enemyCapsuleDExtra1, enemyCapsuleDExtra2;
    public GameObject enemyCapsuleEExtra1, enemyCapsuleEExtra2;

    [Header("Combat / Movement")]
    public float enemyFollowSpeed = 2.0f;
    public float extraEnemyOffset = 1.0f;
    public float extraEnemyFollowDelay = 0.5f;
    public float collisionDistanceThreshold = 1.5f;

    // Health and damage values
    private int playerHealth = 200;
    private const int playerMaxHealth = 200;
    private const int playerDamage = 1;
    private const int enemyHealthBase = 10;
    private int currentEnemyHealth;

    // Cooldown between damage ticks (1 second)
    private float damageCooldown = 1.0f;
    private float lastDamageTime;

    // NPC dialogue-related
    private bool isEnemyDestroyed = false;
    private bool isDialogueShown = false;
    private GameObject currentNpcLabel;
    private TextMesh dialogueTextMesh;

    // Current enemy/NPC
    private GameObject currentEnemy;
    private GameObject currentNPC;
    private GameObject extraEnemy1, extraEnemy2;

    // Leveling
    private int currentLevel = 1;
    private const int totalLevels = 5;

    // =========================================================
    // INTERNALS
    // =========================================================
    private Transform _root;
    private int[] _rowHeights;
    private Vector3 _cityOrigin;

    private struct RoadArea
    {
        public Vector3 center;
        public Vector3 size;
        public bool horizontal; // true => length along X, width along Z. false => length along Z, width along X.
    }

    private readonly List<RoadArea> _roads = new List<RoadArea>();
    private readonly List<Transform> _coins = new List<Transform>();
    private float _coinNoiseOffset;
    private float _nextCoinCheckTime;

    // Player history for delayed-follow extras (fixes “StartCoroutine every frame” problem)
    private readonly List<(float t, Vector3 p)> _playerHistory = new List<(float, Vector3)>();
    private const float _historySampleEvery = 0.05f;
    private float _nextHistorySampleTime;

    void Start()
    {
        if (randomSeed != 0) Random.InitState(randomSeed);
        _coinNoiseOffset = Random.value * 1000f;

        if (clearPreviousOnPlay) ClearPrevious();
        EnsureMaterials();
        EnsurePlayer();

        // Build city
        _root = new GameObject("CityRoot").transform;
        _root.SetParent(transform, false);

        _rowHeights = BuildRowHeightProfile(cityRows);
        _cityOrigin = GetCityOrigin();

        BuildRoads(_cityOrigin);
        SpawnCoinsOnRoads();     // COINS HERE (ellipse clustered)
        BuildBuildings(_cityOrigin);

        // Start game logic
        HideAllEnemiesAndNPCs();
        StartLevel(1);
        AttachDebugLetter(playerCube, "P");

        Debug.Log("MasterCityGame started (City + Coins + Levels).");
    }

    void Update()
    {
        if (!playerCube) return;

        // Record player position history for extra enemies with delay
        UpdatePlayerHistory();

        // Follow logic
        if (currentEnemy != null && !isEnemyDestroyed)
            FollowPlayer(currentEnemy, playerCube.transform.position);

        if (extraEnemy1 != null && !isEnemyDestroyed)
            FollowPlayerWithOffset(extraEnemy1, extraEnemyOffset, extraEnemyFollowDelay);

        if (extraEnemy2 != null && !isEnemyDestroyed)
            FollowPlayerWithOffset(extraEnemy2, -extraEnemyOffset, extraEnemyFollowDelay);

        // Combat: if close to any enemy, damage tick applies (main health pool)
        if (!isEnemyDestroyed && IsPlayerCloseToAnyEnemy())
        {
            if (Time.time - lastDamageTime >= damageCooldown)
            {
                GameObject hitEnemy = GetClosestEnemyToPlayer();
                ApplyDamageToEnemy(hitEnemy);
                ApplyDamageToPlayer();

                if (currentEnemyHealth <= 0)
                {
                    SafeDestroy(currentEnemy);
                    SafeDestroy(extraEnemy1);
                    SafeDestroy(extraEnemy2);

                    Debug.Log($"Enemy {GetLabelForCurrentLevel()} and extras destroyed.");

                    if (currentNPC != null)
                    {
                        currentNPC.SetActive(true);
                        AttachDebugLetter(currentNPC, GetLabelForCurrentLevel());
                    }

                    isEnemyDestroyed = true;
                }

                lastDamageTime = Time.time;
            }
        }

        // NPC dialogue and level transition
        if (isEnemyDestroyed && !isDialogueShown && currentNPC != null && currentNPC.activeSelf)
        {
            if (Vector3.Distance(playerCube.transform.position, currentNPC.transform.position) < collisionDistanceThreshold)
            {
                ShowNPCDialogue(GetLabelForCurrentLevel());
                isDialogueShown = true;
                Debug.Log($"NPC {GetLabelForCurrentLevel()} dialogue displayed.");
                StartCoroutine(TransitionToNextLevel());
            }
        }

        // Coins pickup
        if (Time.time >= _nextCoinCheckTime)
        {
            _nextCoinCheckTime = Time.time + coinCheckInterval;
            CheckCoinPickup();
        }
    }

    // =========================================================
    // CITY
    // =========================================================
    Vector3 GetCityOrigin()
    {
        float width = cityCols * blockSpacing;
        float depth = cityRows * blockSpacing;

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
            if (roll < rowTallProb) heights[r] = Random.Range(5, 7);
            else if (roll < rowMidProb) heights[r] = Random.Range(4, 6);
            else heights[r] = Random.Range(3, 5);
        }
        return heights;
    }

    void BuildRoads(Vector3 origin)
    {
        _roads.Clear();

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

            _roads.Add(new RoadArea { center = pos, size = size, horizontal = true });
        }

        // Vertical roads: cols+1
        for (int c = 0; c <= cityCols; c++)
        {
            float x = origin.x + c * blockSpacing;
            Vector3 pos = new Vector3(x, origin.y - roadThickness * 0.5f, origin.z + cityDepth * 0.5f);
            Vector3 size = new Vector3(roadWidth, roadThickness, cityDepth + roadWidth);

            CreateSlab("Road_V_" + c, pos, size, roadMaterial, roadsRoot);

            _roads.Add(new RoadArea { center = pos, size = size, horizontal = false });
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

                float xMin = origin.x + c * blockSpacing + roadWidth * 0.5f;
                float xMax = origin.x + (c + 1) * blockSpacing - roadWidth * 0.5f;
                float zMin = origin.z + r * blockSpacing + roadWidth * 0.5f;
                float zMax = origin.z + (r + 1) * blockSpacing - roadWidth * 0.5f;

                xMin += margin; xMax -= margin;
                zMin += margin; zMax -= margin;

                float w = xMax - xMin;
                float d = zMax - zMin;
                if (w < 2f || d < 2f) continue;

                int baseLevels = _rowHeights[r];
                int variation = Random.Range(-1, 2);
                int levels = Mathf.Clamp(baseLevels + variation, minLevels, maxLevels);

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
        Vector3 center = new Vector3((x1 + x2) * 0.5f, height * 0.5f, (z1 + z2) * 0.5f);
        Vector3 size = new Vector3(w, height, d);

        Material mat = (buildingMaterials != null && buildingMaterials.Length > 0)
            ? buildingMaterials[Random.Range(0, buildingMaterials.Length)]
            : null;

        GameObject b = CreateSlab("Building_" + levels, center, size, mat, parent);
        b.name = $"Building_{levels}F";
    }

    GameObject CreateSlab(string name, Vector3 pos, Vector3 size, Material mat, Transform parent)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.localScale = size;

        if (!addColliders)
        {
            Collider col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);
        }

        if (mat != null)
        {
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = mat;
        }

        return go;
    }

    void EnsureMaterials()
    {
        if (roadMaterial == null)
        {
            roadMaterial = new Material(Shader.Find("Standard"));
            roadMaterial.name = "Road_Asphalt_Runtime";
            roadMaterial.color = new Color(0.27f, 0.27f, 0.27f, 1f);
            roadMaterial.SetFloat("_Glossiness", 0.1f);
        }

        if (buildingMaterials == null || buildingMaterials.Length == 0)
        {
            buildingMaterials = new Material[4];
            buildingMaterials[0] = MakeMat("B1_Concrete", new Color32(200, 200, 200, 255), 0.0f, 0.2f);
            buildingMaterials[1] = MakeMat("B2_Brick", new Color32(170, 120, 100, 255), 0.0f, 0.1f);
            buildingMaterials[2] = MakeGlassMat("B3_Glass", new Color32(180, 230, 255, 180), 0.0f, 0.8f);
            buildingMaterials[3] = MakeMat("B4_Steel", new Color32(150, 160, 170, 255), 0.6f, 0.7f);
        }
    }

    Material MakeMat(string name, Color color, float metallic, float smooth)
    {
        var m = new Material(Shader.Find("Standard"));
        m.name = name + "_Runtime";
        m.color = color;
        m.SetFloat("_Metallic", metallic);
        m.SetFloat("_Glossiness", smooth);
        return m;
    }

    Material MakeGlassMat(string name, Color color, float metallic, float smooth)
    {
        var m = MakeMat(name, color, metallic, smooth);
        SetMaterialTransparent(m);
        return m;
    }

    void SetMaterialTransparent(Material mat)
    {
        mat.SetFloat("_Mode", 3);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
    }

    void ClearPrevious()
    {
        Transform existing = transform.Find("CityRoot");
        if (existing != null)
        {
#if UNITY_EDITOR
            DestroyImmediate(existing.gameObject);
#else
            Destroy(existing.gameObject);
#endif
        }
    }

    void EnsurePlayer()
    {
        if (playerCube != null) return;

        GameObject tagged = GameObject.FindGameObjectWithTag("Player");
        if (tagged != null)
        {
            playerCube = tagged;
            return;
        }

        // fallback: spawn a player cube
        playerCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        playerCube.name = "Player";
        playerCube.transform.position = _cityOrigin + new Vector3((cityCols * blockSpacing) * 0.5f, 1f, (cityRows * blockSpacing) * 0.5f);
    }

    // =========================================================
    // COINS (Ellipse clustering using formula from your image)
    //   Ellipse equation: (x^2/a^2) + (y^2/b^2) <= 1
    // =========================================================
    void SpawnCoinsOnRoads()
    {
        ClearCoins();

        Transform coinRoot = new GameObject("Coins").transform;
        coinRoot.SetParent(_root, false);

        // Clamp ellipseB so it doesn't exceed road width
        float maxB = Mathf.Max(0.25f, (roadWidth * 0.5f) - 0.75f);
        float b = Mathf.Min(ellipseB, maxB);
        float a = Mathf.Max(0.5f, ellipseA);

        int clustersPerRoad = Mathf.Max(1, Mathf.CeilToInt(coinsPerRoad / Mathf.Max(1, coinsPerCluster)));

        foreach (var road in _roads)
        {
            for (int c = 0; c < clustersPerRoad; c++)
            {
                Vector3 clusterCenter = RandomPointInRoad(road);

                // Noise gate for cluster placement (creates "patchy" distribution)
                float n = Mathf.PerlinNoise((clusterCenter.x + _coinNoiseOffset) * coinNoiseScale,
                                            (clusterCenter.z + _coinNoiseOffset) * coinNoiseScale);
                if (n < coinNoiseThreshold) continue;

                int spawned = 0;
                int attempts = 0;
                while (spawned < coinsPerCluster && attempts < coinsPerCluster * 12)
                {
                    attempts++;

                    // Rejection sample inside ellipse in road-local coords:
                    // (x^2/a^2) + (y^2/b^2) <= 1
                    float lx = Random.Range(-a, a);
                    float ly = Random.Range(-b, b);
                    float eq = (lx * lx) / (a * a) + (ly * ly) / (b * b);
                    if (eq > 1f) continue;

                    Vector3 worldPos;
                    if (road.horizontal)
                    {
                        // length along X, width along Z
                        worldPos = new Vector3(clusterCenter.x + lx, _cityOrigin.y + coinHeight, clusterCenter.z + ly);
                    }
                    else
                    {
                        // length along Z, width along X
                        worldPos = new Vector3(clusterCenter.x + ly, _cityOrigin.y + coinHeight, clusterCenter.z + lx);
                    }

                    // Must still be inside road bounds
                    if (!PointInsideRoadXZ(road, worldPos)) continue;

                    Transform coin = CreateCoin(worldPos, coinRoot);
                    _coins.Add(coin);
                    spawned++;
                }
            }
        }
    }

    Transform CreateCoin(Vector3 pos, Transform parent)
    {
        GameObject go;

        if (coinPrefab != null)
        {
            go = Instantiate(coinPrefab, pos, Quaternion.identity, parent);
        }
        else
        {
            // fallback coin (simple cylinder)
            go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "Coin_Fallback";
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = new Vector3(0.7f, 0.12f, 0.7f);
        }

        // Make sure it has a collider (for visuals we still do radius checks)
        Collider col = go.GetComponent<Collider>();
        if (col == null) col = go.AddComponent<SphereCollider>();
        col.isTrigger = true;

        return go.transform;
    }

    Vector3 RandomPointInRoad(RoadArea road)
    {
        float halfX = road.size.x * 0.5f;
        float halfZ = road.size.z * 0.5f;

        // Small inset so cluster centers aren’t right at the edges
        float insetX = Mathf.Min(2f, halfX * 0.1f);
        float insetZ = Mathf.Min(2f, halfZ * 0.1f);

        float x = Random.Range(road.center.x - halfX + insetX, road.center.x + halfX - insetX);
        float z = Random.Range(road.center.z - halfZ + insetZ, road.center.z + halfZ - insetZ);
        return new Vector3(x, road.center.y, z);
    }

    bool PointInsideRoadXZ(RoadArea road, Vector3 p)
    {
        float halfX = road.size.x * 0.5f;
        float halfZ = road.size.z * 0.5f;
        return (p.x >= road.center.x - halfX && p.x <= road.center.x + halfX &&
                p.z >= road.center.z - halfZ && p.z <= road.center.z + halfZ);
    }

    void ClearCoins()
    {
        _coins.Clear();

        Transform existing = _root != null ? _root.Find("Coins") : null;
        if (existing != null)
        {
#if UNITY_EDITOR
            DestroyImmediate(existing.gameObject);
#else
            Destroy(existing.gameObject);
#endif
        }
    }

    void CheckCoinPickup()
    {
        if (_coins.Count == 0) return;

        Vector3 p = playerCube.transform.position;
        float r2 = coinPickupRadius * coinPickupRadius;

        for (int i = _coins.Count - 1; i >= 0; i--)
        {
            Transform c = _coins[i];
            if (c == null)
            {
                _coins.RemoveAt(i);
                continue;
            }

            Vector3 d = c.position - p;
            d.y = 0f;
            if (d.sqrMagnitude <= r2)
            {
                // pickup
                Destroy(c.gameObject);
                _coins.RemoveAt(i);

                // tiny reward example (optional)
                playerHealth = Mathf.Min(playerMaxHealth, playerHealth + 1);
            }
        }
    }

    // =========================================================
    // GAME LOGIC
    // =========================================================
    void UpdatePlayerHistory()
    {
        float now = Time.time;

        if (now >= _nextHistorySampleTime)
        {
            _nextHistorySampleTime = now + _historySampleEvery;
            _playerHistory.Add((now, playerCube.transform.position));
        }

        float keepAfter = now - Mathf.Max(1.0f, extraEnemyFollowDelay + 0.5f);
        while (_playerHistory.Count > 0 && _playerHistory[0].t < keepAfter)
            _playerHistory.RemoveAt(0);
    }

    Vector3 GetDelayedPlayerPos(float delay)
    {
        float targetTime = Time.time - Mathf.Max(0f, delay);
        for (int i = _playerHistory.Count - 1; i >= 0; i--)
        {
            if (_playerHistory[i].t <= targetTime)
                return _playerHistory[i].p;
        }
        return playerCube.transform.position;
    }

    void FollowPlayer(GameObject enemy, Vector3 targetPos)
    {
        Vector3 direction = (targetPos - enemy.transform.position);
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return;

        direction.Normalize();
        enemy.transform.position += direction * enemyFollowSpeed * Time.deltaTime;
    }

    void FollowPlayerWithOffset(GameObject enemy, float offset, float delay)
    {
        Vector3 delayedPos = GetDelayedPlayerPos(delay);

        Vector3 direction = (delayedPos - enemy.transform.position);
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return;

        direction.Normalize();
        Vector3 perp = new Vector3(direction.z, 0, -direction.x) * offset;
        enemy.transform.position += (direction + perp).normalized * enemyFollowSpeed * Time.deltaTime;
    }

    bool IsPlayerCloseToAnyEnemy()
    {
        Vector3 p = playerCube.transform.position;

        if (currentEnemy != null && Vector3.Distance(p, currentEnemy.transform.position) < collisionDistanceThreshold) return true;
        if (extraEnemy1 != null && Vector3.Distance(p, extraEnemy1.transform.position) < collisionDistanceThreshold) return true;
        if (extraEnemy2 != null && Vector3.Distance(p, extraEnemy2.transform.position) < collisionDistanceThreshold) return true;

        return false;
    }

    GameObject GetClosestEnemyToPlayer()
    {
        Vector3 p = playerCube.transform.position;
        GameObject best = currentEnemy;
        float bestD = best != null ? Vector3.Distance(p, best.transform.position) : float.MaxValue;

        if (extraEnemy1 != null)
        {
            float d = Vector3.Distance(p, extraEnemy1.transform.position);
            if (d < bestD) { bestD = d; best = extraEnemy1; }
        }
        if (extraEnemy2 != null)
        {
            float d = Vector3.Distance(p, extraEnemy2.transform.position);
            if (d < bestD) { bestD = d; best = extraEnemy2; }
        }
        return best != null ? best : currentEnemy;
    }

    void ApplyDamageToEnemy(GameObject enemyForPopup)
    {
        currentEnemyHealth -= playerDamage;

        if (enemyForPopup != null)
            DisplayDamageNumber(enemyForPopup, playerDamage);

        Debug.Log($"Enemy {GetLabelForCurrentLevel()} takes {playerDamage} damage. Health now: {currentEnemyHealth}");
    }

    void ApplyDamageToPlayer()
    {
        playerHealth -= 1;
        DisplayDamageNumber(playerCube, 1);

        Debug.Log($"Player takes 1 damage. Health now: {playerHealth}");

        if (playerHealth <= 0)
        {
            Debug.Log("Player health reached 0. Resetting to full health for debugging.");
            playerHealth = playerMaxHealth;
        }
    }

    void DisplayDamageNumber(GameObject targetObject, int damageAmount)
    {
        if (targetObject == null) return;

        GameObject damageTextObject = new GameObject("DamageText");
        TextMesh damageTextMesh = damageTextObject.AddComponent<TextMesh>();

        damageTextMesh.text = $"-{damageAmount}";
        damageTextMesh.fontSize = 32;
        damageTextMesh.color = Color.red;

        damageTextObject.transform.position = targetObject.transform.position + new Vector3(0, 2, 0);
        Destroy(damageTextObject, 1.0f);
    }

    void AttachDebugLetter(GameObject targetObject, string label)
    {
        if (targetObject == null) return;

        GameObject labelObject = new GameObject("DebugLabel");
        TextMesh textMesh = labelObject.AddComponent<TextMesh>();

        textMesh.text = label;
        textMesh.fontSize = 32;
        textMesh.color = Color.red;

        labelObject.transform.SetParent(targetObject.transform);
        labelObject.transform.localPosition = new Vector3(0, 2, 0);

        if (targetObject == currentNPC)
            currentNpcLabel = labelObject;
    }

    void ShowNPCDialogue(string label)
    {
        if (currentNPC == null) return;

        if (currentNpcLabel == null)
            AttachDebugLetter(currentNPC, label);

        GameObject dialogueObject = new GameObject("NPCDialogue");
        dialogueTextMesh = dialogueObject.AddComponent<TextMesh>();

        dialogueTextMesh.text = $"Thank you for destroying the enemy {label}";
        dialogueTextMesh.fontSize = 24;
        dialogueTextMesh.color = Color.white;

        dialogueObject.transform.SetParent(currentNPC.transform);
        dialogueObject.transform.position = currentNpcLabel.transform.position + new Vector3(1.5f, 0, 0);
    }

    IEnumerator TransitionToNextLevel()
    {
        yield return new WaitForSeconds(10);

        if (dialogueTextMesh != null)
            Destroy(dialogueTextMesh.gameObject);

        if (currentLevel < totalLevels)
        {
            currentLevel++;
            StartLevel(currentLevel);
            isEnemyDestroyed = false;
            isDialogueShown = false;

            if (coinsRespawnEachLevel)
                SpawnCoinsOnRoads();
        }
        else
        {
            Debug.Log("All levels complete. Game finished.");
        }
    }

    void StartLevel(int level)
    {
        Debug.Log($"Starting Level {level}");

        // Pick refs
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

        // Safety: make sure objects exist
        if (currentEnemy == null || currentNPC == null || extraEnemy1 == null || extraEnemy2 == null)
        {
            Debug.LogWarning("Some enemy/NPC references are missing for this level. Assign them in Inspector.");
        }

        currentEnemyHealth = enemyHealthBase;

        if (currentNPC != null) currentNPC.SetActive(false);

        if (currentEnemy != null) currentEnemy.SetActive(true);
        if (extraEnemy1 != null) extraEnemy1.SetActive(true);
        if (extraEnemy2 != null) extraEnemy2.SetActive(true);

        AttachDebugLetter(currentEnemy, GetLabelForCurrentLevel());
        AttachDebugLetter(extraEnemy1, GetLabelForCurrentLevel() + " Extra1");
        AttachDebugLetter(extraEnemy2, GetLabelForCurrentLevel() + " Extra2");

        lastDamageTime = Time.time;
    }

    void HideAllEnemiesAndNPCs()
    {
        SafeSetActive(enemyCapsuleA, false); SafeSetActive(npcCylinderA, false);
        SafeSetActive(enemyCapsuleB, false); SafeSetActive(npcCylinderB, false);
        SafeSetActive(enemyCapsuleC, false); SafeSetActive(npcCylinderC, false);
        SafeSetActive(enemyCapsuleD, false); SafeSetActive(npcCylinderD, false);
        SafeSetActive(enemyCapsuleE, false); SafeSetActive(npcCylinderE, false);

        SafeSetActive(enemyCapsuleAExtra1, false); SafeSetActive(enemyCapsuleAExtra2, false);
        SafeSetActive(enemyCapsuleBExtra1, false); SafeSetActive(enemyCapsuleBExtra2, false);
        SafeSetActive(enemyCapsuleCExtra1, false); SafeSetActive(enemyCapsuleCExtra2, false);
        SafeSetActive(enemyCapsuleDExtra1, false); SafeSetActive(enemyCapsuleDExtra2, false);
        SafeSetActive(enemyCapsuleEExtra1, false); SafeSetActive(enemyCapsuleEExtra2, false);
    }

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

    void SafeSetActive(GameObject go, bool active)
    {
        if (go != null) go.SetActive(active);
    }

    void SafeDestroy(GameObject go)
    {
        if (go != null) Destroy(go);
    }
}
