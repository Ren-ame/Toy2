using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Tools/Map/Plant Forest
// 아레나(숲 속 공터) 바깥에 나무를 랜덤으로 심고, 플레이어가 숲으로 못 나가게 보이지 않는 경계벽을 세운다.
// - 공터 가장자리: 자작나무·물푸레나무(밝은 잎) / 안쪽 깊은 숲: 가문비나무(침엽수) 위주
// - 성벽 줄, 흙길 위에는 심지 않음.  높이는 ForestTerrain 표면에 맞춤
// - 낙엽 파티클은 일부 나무만 켜 둠(성능)
// 재실행하면 기존 Forest / ArenaBoundary를 지우고 같은 자리에 다시 만든다.
public static class ForestPlanter
{
    private const string PrefabDir = "Assets/Realistic Tree/Prefabs/URP/";
    private const string ForestName = "Forest";
    private const string BoundaryName = "ArenaBoundary";

    private static readonly Vector3 Center = new Vector3(-2.9f, 0f, -0.5f);   // ForestTerrainBuilder와 동일
    private const float HalfSize = 98f;          // 지형(200m) 안쪽까지만
    private const float ClearRadius = 54f;       // 공터 반경 (노이즈로 ±5m 들쭉날쭉)
    private const float Spacing = 6.5f;          // 나무 최소 간격
    private const float EdgeBand = 14f;          // 공터 가장자리에서 이 거리까지는 활엽수 위주
    private const float MinScale = 1.4f, MaxScale = 2.3f;
    private const float LeafParticleChance = 0.12f;
    private const float BoundaryRadius = 52f;    // 플레이어 이동 한계 (나무 줄보다 살짝 안쪽)

    [MenuItem("Tools/Map/Plant Forest")]
    private static void PlantDefault() => Plant(777);

    [MenuItem("Tools/Map/Plant Forest (Random Seed)")]
    private static void PlantRandom() => Plant(Random.Range(0, 100000));

    private static void Plant(int seed)
    {
        GameObject[] broadleaf = LoadPrefabs(("Birch/Birch", 10), ("Ash/Ash", 8));
        GameObject[] conifer = LoadPrefabs(("Spruce/Spruce", 10));
        if (broadleaf.Length == 0 || conifer.Length == 0)
        {
            Debug.LogError($"[Forest] 나무 프리팹을 못 찾음: {PrefabDir}");
            return;
        }

        Terrain terrain = FindTerrain();
        var rng = new System.Random(seed);
        float[,,] splat = null; int res = 0;
        if (terrain != null)
        {
            res = terrain.terrainData.alphamapResolution;
            splat = terrain.terrainData.GetAlphamaps(0, 0, res, res);
        }
        float ox = (seed * 0.613f) % 1000f;

        List<Bounds> blocked = GateBlockers(4f);   // 성문·성벽·광장·돌무더기 실제 위치 (사용자가 옮기거나 키워도 따라감)

        var (fParent, fSibling) = RemoveExisting(ForestName);
        var forest = new GameObject(ForestName);
        Undo.RegisterCreatedObjectUndo(forest, "Plant Forest");
        if (fParent != null) { forest.transform.SetParent(fParent, false); forest.transform.SetSiblingIndex(fSibling); }

        // 격자 + 흔들기(jitter)로 고르게 퍼진 랜덤 위치 만들기
        int count = 0;
        for (float gx = -HalfSize; gx <= HalfSize; gx += Spacing)
            for (float gz = -HalfSize; gz <= HalfSize; gz += Spacing)
            {
                float wx = Center.x + gx + (float)(rng.NextDouble() - 0.5) * Spacing * 0.8f;
                float wz = Center.z + gz + (float)(rng.NextDouble() - 0.5) * Spacing * 0.8f;
                float edge = DistanceFromClearing(wx, wz, ox);
                if (edge < 0f) continue;                                   // 공터 안
                if (IsBlocked(blocked, wx, wz)) continue;                  // 성문·성벽·광장 위
                if (DirtWeight(terrain, splat, res, wx, wz) > 0.5f) continue;   // 흙길·흙 패치 위는 비움
                if (rng.NextDouble() < 0.12) continue;                      // 듬성듬성한 빈자리

                // 가장자리일수록 활엽수, 깊을수록 침엽수
                float coniferChance = Mathf.Lerp(0.15f, 0.85f, Mathf.Clamp01(edge / EdgeBand));
                GameObject[] pool = rng.NextDouble() < coniferChance ? conifer : broadleaf;
                GameObject prefab = pool[rng.Next(pool.Length)];

                var pos = new Vector3(wx, 0f, wz);
                if (terrain != null) pos.y = terrain.SampleHeight(pos) + terrain.transform.position.y;

                var tree = (GameObject)PrefabUtility.InstantiatePrefab(prefab, forest.transform);
                tree.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f));
                float s = Mathf.Lerp(MinScale, MaxScale, (float)rng.NextDouble());
                tree.transform.localScale = Vector3.one * s;

                bool leaves = rng.NextDouble() < LeafParticleChance;
                foreach (ParticleSystem ps in tree.GetComponentsInChildren<ParticleSystem>(true))
                    ps.gameObject.SetActive(leaves);
                count++;
            }

        BuildBoundary();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = forest;
        Debug.Log($"[Forest] 나무 {count}그루 배치 (seed {seed})");
    }

    // 공터 가장자리로부터의 거리 (음수 = 공터 안쪽)
    private static float DistanceFromClearing(float wx, float wz, float ox)
    {
        float d = Vector2.Distance(new Vector2(wx, wz), new Vector2(Center.x, Center.z));
        float angle = Mathf.Atan2(wz - Center.z, wx - Center.x);
        float wobble = (Mathf.PerlinNoise(ox + Mathf.Cos(angle) * 1.5f + 5f, ox + Mathf.Sin(angle) * 1.5f + 5f) - 0.5f) * 10f;
        return d - (ClearRadius + wobble);
    }

    // Terrain에 칠해진 흙 레이어(2번) 비율 → 길이나 흙바닥 위에는 나무를 안 심음 (지형 시드가 바뀌어도 자동으로 맞음)
    private static float DirtWeight(Terrain t, float[,,] splat, int res, float wx, float wz)
    {
        if (splat == null || splat.GetLength(2) < 3) return 0f;
        Vector3 local = new Vector3(wx, 0f, wz) - t.transform.position;
        Vector3 size = t.terrainData.size;
        int xi = Mathf.Clamp((int)(local.x / size.x * res), 0, res - 1);
        int zi = Mathf.Clamp((int)(local.z / size.z * res), 0, res - 1);
        return splat[zi, xi, 2];
    }

    // 원형으로 박스 콜라이더를 둘러 보이지 않는 벽 만들기
    private static void BuildBoundary()
    {
        var (bParent, bSibling) = RemoveExisting(BoundaryName);
        var root = new GameObject(BoundaryName);
        Undo.RegisterCreatedObjectUndo(root, "Arena Boundary");
        if (bParent != null) { root.transform.SetParent(bParent, false); root.transform.SetSiblingIndex(bSibling); }

        const int segments = 48;
        float segLen = 2f * Mathf.PI * BoundaryRadius / segments + 0.5f;
        for (int i = 0; i < segments; i++)
        {
            float a = i * Mathf.PI * 2f / segments;
            var pos = Center + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * BoundaryRadius + Vector3.up * 5f;
            var wall = new GameObject($"Wall_{i:00}");
            wall.transform.SetParent(root.transform, false);
            wall.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(Center - new Vector3(pos.x, Center.y, pos.z)));
            var col = wall.AddComponent<BoxCollider>();
            col.size = new Vector3(segLen, 10f, 1f);
        }
    }

    // HolyGate_Env 자식(성문, 각 성벽, 광장, 돌무더기)마다 바운즈를 따로 잡음 → 벽 사이 틈 뒤에는 나무가 들어갈 수 있음
    public static List<Bounds> GateBlockers(float margin)
    {
        var list = new List<Bounds>();
        GameObject env = FindByName("HolyGate_Env");
        if (env == null) return list;
        foreach (Transform child in env.transform)
        {
            Renderer[] rs = child.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) continue;
            Bounds b = rs[0].bounds;
            foreach (Renderer r in rs) b.Encapsulate(r.bounds);
            b.Expand(new Vector3(margin * 2f, 0f, margin * 2f));
            list.Add(b);
        }
        return list;
    }

    private static bool IsBlocked(List<Bounds> list, float wx, float wz)
    {
        foreach (Bounds b in list)
            if (wx >= b.min.x && wx <= b.max.x && wz >= b.min.z && wz <= b.max.z) return true;
        return false;
    }

    private static GameObject FindByName(string name)
    {
        foreach (GameObject root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t.gameObject;
        return null;
    }

    private static GameObject[] LoadPrefabs(params (string path, int n)[] sets)
    {
        var list = new List<GameObject>();
        foreach (var (path, n) in sets)
            for (int i = 1; i <= n; i++)
            {
                var p = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}{path} {i}.prefab");
                if (p != null) list.Add(p);
            }
        return list.ToArray();
    }

    private static Terrain FindTerrain()
    {
        foreach (Terrain t in Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None))
            if (t.name == "ForestTerrain") return t;
        return Terrain.activeTerrain;
    }

    private static (Transform parent, int sibling) RemoveExisting(string name)
    {
        Transform parent = null; int sibling = -1;
        foreach (GameObject root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t == null || t.name != name) continue;
                if (sibling < 0) { parent = t.parent; sibling = t.GetSiblingIndex(); }
                Undo.DestroyObjectImmediate(t.gameObject);
            }
        return (parent, sibling);
    }
}
