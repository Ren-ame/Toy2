using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Tools/Map/Build Forest Terrain
// 아레나 바닥(Plane)을 Terrain으로 교체한다.
// - 레이어 4종(잔디/마른풀/흙/낙엽)을 노이즈로 섞어서 칠함 → 같은 무늬가 줄지어 반복되지 않음
// - 전투 구역과 성문 주변은 완전히 평평(y=0), 바깥 숲 쪽만 완만한 언덕
// - 동쪽(플레이어 시작 쪽)에서 성문 광장까지 흙길
// 재실행하면 기존 ForestTerrain을 지우고 다시 만든다. "(Random Seed)"는 무늬를 새로 뽑는다.
public static class ForestTerrainBuilder
{
    private const string TexDir = "Assets/_Project/Textures/Terrain/";
    private const string DataDir = "Assets/_Project/Terrain";
    private const string ObjName = "ForestTerrain";

    private static readonly Vector3 Center = new Vector3(-2.9f, 0f, -0.5f);   // 아레나 중심
    private static readonly Vector3 GatePos = new Vector3(-45f, 0f, -0.5f);   // HolyGatePlacer와 동일
    private const float Size = 200f;        // 지형 가로세로 (m)
    private const float Height = 30f;       // 지형 최대 높이 범위
    private const float Base = 3f;          // 이 높이가 월드 y=0 (아래로 살짝 파일 여유)
    private const int HeightRes = 257;
    private const int AlphaRes = 512;

    private const float FlatRadius = 50f;   // 이 안쪽은 평평
    private const float HillRamp = 25f;     // 평지 → 언덕 전환 거리
    private const float HillHeight = 5f;

    private static readonly (string name, float tile, float smooth)[] Layers =
    {
        ("Grass", 4f, 0.1f), ("DryGrass", 4f, 0.08f), ("Dirt", 3f, 0.12f), ("Leaves", 2.5f, 0.15f),
    };

    [MenuItem("Tools/Map/Build Forest Terrain")]
    private static void BuildDefault() => Build(1234);

    [MenuItem("Tools/Map/Build Forest Terrain (Random Seed)")]
    private static void BuildRandom() => Build(Random.Range(0, 100000));

    private static void Build(int seed)
    {
        if (!AssetDatabase.IsValidFolder(DataDir)) AssetDatabase.CreateFolder("Assets/_Project", "Terrain");
        TerrainLayer[] layers = SetupLayers();

        string dataPath = DataDir + "/ArenaTerrain.asset";
        var data = AssetDatabase.LoadAssetAtPath<TerrainData>(dataPath);
        if (data == null)
        {
            data = new TerrainData();
            AssetDatabase.CreateAsset(data, dataPath);
        }
        data.heightmapResolution = HeightRes;
        data.size = new Vector3(Size, Height, Size);
        data.alphamapResolution = AlphaRes;
        data.terrainLayers = layers;

        Vector3 origin = Center - new Vector3(Size / 2f, Base, Size / 2f);
        _gateBlockers = ForestPlanter.GateBlockers(3f);   // 성문·성벽 실제 위치 주변도 평평하게
        float ox = (seed * 0.731f) % 1000f, oz = (seed * 0.417f) % 1000f;   // 시드별 노이즈 위치

        // ---------- 높이
        var h = new float[HeightRes, HeightRes];
        for (int zi = 0; zi < HeightRes; zi++)
            for (int xi = 0; xi < HeightRes; xi++)
            {
                float wx = origin.x + xi / (HeightRes - 1f) * Size;
                float wz = origin.z + zi / (HeightRes - 1f) * Size;
                float hill = (Mathf.PerlinNoise(ox + wx * 0.02f, oz + wz * 0.02f) * 0.75f
                            + Mathf.PerlinNoise(ox + wx * 0.07f, oz + wz * 0.07f) * 0.25f) * HillHeight;
                h[zi, xi] = (Base + (1f - Flatness(wx, wz)) * hill) / Height;
            }
        data.SetHeights(0, 0, h);

        // ---------- 레이어 칠하기 (0 잔디, 1 마른풀, 2 흙, 3 낙엽)
        var a = new float[AlphaRes, AlphaRes, 4];
        for (int zi = 0; zi < AlphaRes; zi++)
            for (int xi = 0; xi < AlphaRes; xi++)
            {
                float wx = origin.x + (xi + 0.5f) / AlphaRes * Size;
                float wz = origin.z + (zi + 0.5f) / AlphaRes * Size;
                float rag = Mathf.PerlinNoise(ox + wx * 0.25f, oz + wz * 0.25f);             // 경계 너덜너덜
                float n1 = Mathf.PerlinNoise(ox + 100f + wx * 0.05f, oz + wz * 0.05f);
                float n2 = Mathf.PerlinNoise(ox + 200f + wx * 0.03f, oz + 50f + wz * 0.03f);

                float dirt = Smooth(0.6f, 0.68f, n1 * 0.75f + rag * 0.25f);
                dirt = Mathf.Max(dirt, PathMask(wx, wz, rag, ox));
                float dry = Smooth(0.55f, 0.68f, n2 * 0.8f + rag * 0.2f);

                float dc = Vector2.Distance(new Vector2(wx, wz), new Vector2(Center.x, Center.z));
                float leaves = Smooth(FlatRadius, FlatRadius + 15f, dc) * (0.6f + 0.4f * rag);
                float gs = HolyGatePlacer.GateScale;
                bool nearWall = Mathf.Abs(wx - GatePos.x) < 5f * gs && Mathf.Abs(wz - GatePos.z) > 11f * gs;   // 성벽 발치
                if (nearWall) leaves = Mathf.Max(leaves, Smooth(0.45f, 0.7f, rag) * 0.8f);

                float wLeaves = leaves;
                float wDirt = dirt * (1f - wLeaves);
                float wDry = dry * (1f - dirt) * (1f - wLeaves);
                float wGrass = Mathf.Max(0f, 1f - wLeaves - wDirt - wDry);
                float sum = wGrass + wDry + wDirt + wLeaves;
                a[zi, xi, 0] = wGrass / sum; a[zi, xi, 1] = wDry / sum; a[zi, xi, 2] = wDirt / sum; a[zi, xi, 3] = wLeaves / sum;
            }
        data.SetAlphamaps(0, 0, a);
        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();

        // ---------- 씬 오브젝트
        var scene = EditorSceneManager.GetActiveScene();
        var (oldParent, oldSibling) = RemoveExisting(ObjName);

        GameObject go = Terrain.CreateTerrainGameObject(data);
        go.name = ObjName;
        go.transform.position = origin;
        Undo.RegisterCreatedObjectUndo(go, "Build Forest Terrain");
        if (oldParent != null) { go.transform.SetParent(oldParent, true); go.transform.SetSiblingIndex(oldSibling); }
        go.transform.position = origin;
        var terrain = go.GetComponent<Terrain>();
        // 머티리얼은 비워 두면 URP 기본 Terrain 머티리얼(TerrainLit)이 쓰임
        terrain.basemapDistance = 250f;   // 멀리서도 저해상도 대체 텍스처로 안 바뀌게
        terrain.drawInstanced = true;
        GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);

        // 기존 바닥 Plane 끄기 (삭제 X). 이제 Terrain Collider가 바닥 충돌 담당
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == "Plane")
                {
                    Undo.RecordObject(t.gameObject, "Disable Plane");
                    t.gameObject.SetActive(false);
                }

        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = go;
        Debug.Log($"[ForestTerrain] 생성 완료 (seed {seed})");
    }

    // 같은 이름 오브젝트를 계층 전체(자식 포함)에서 찾아 모두 지우고, 첫 번째의 부모·순서를 돌려준다
    // → 사용자가 Environment 같은 폴더 오브젝트 아래로 옮겨 둬도 중복 생성되지 않고 그 자리에 다시 만들어짐
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

    private static System.Collections.Generic.List<Bounds> _gateBlockers;

    // 1 = 평평하게 유지할 곳 (전투 구역 원 + 성문·성벽·광장 주변)
    private static float Flatness(float wx, float wz)
    {
        float dc = Vector2.Distance(new Vector2(wx, wz), new Vector2(Center.x, Center.z));
        float flat = 1f - Smooth(FlatRadius, FlatRadius + HillRamp, dc);
        if (_gateBlockers != null)
            foreach (Bounds b in _gateBlockers)
            {
                float dx = Mathf.Max(0f, Mathf.Abs(wx - b.center.x) - b.extents.x);
                float dz = Mathf.Max(0f, Mathf.Abs(wz - b.center.z) - b.extents.z);
                flat = Mathf.Max(flat, 1f - Smooth(0f, HillRamp, Mathf.Sqrt(dx * dx + dz * dz)));
            }
        return flat;
    }

    // 동쪽 끝 → 성문 광장까지 구불구불한 흙길
    private static float PathMask(float wx, float wz, float rag, float ox)
    {
        if (wx < GatePos.x + 5f * HolyGatePlacer.GateScale) return 0f;   // 성문 계단 앞에서 끝남
        float pz = Center.z + 5f * Mathf.Sin(wx * 0.045f + ox) + 2f * Mathf.Sin(wx * 0.11f + ox * 2f);
        float width = 2.2f + 1.2f * rag;
        return 1f - Smooth(width * 0.55f, width, Mathf.Abs(wz - pz));
    }

    private static float Smooth(float a, float b, float x)
    {
        float t = Mathf.Clamp01((x - a) / (b - a));
        return t * t * (3f - 2f * t);
    }

    private static TerrainLayer[] SetupLayers()
    {
        var result = new TerrainLayer[Layers.Length];
        for (int i = 0; i < Layers.Length; i++)
        {
            var (name, tile, smooth) = Layers[i];
            string nrmPath = TexDir + $"Terrain_{name}_Normal.png";
            if (AssetImporter.GetAtPath(nrmPath) is TextureImporter ti && ti.textureType != TextureImporterType.NormalMap)
            {
                ti.textureType = TextureImporterType.NormalMap;
                ti.SaveAndReimport();
            }
            string path = DataDir + $"/Layer_{name}.terrainlayer";
            var layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
            if (layer == null)
            {
                layer = new TerrainLayer();
                AssetDatabase.CreateAsset(layer, path);
            }
            layer.diffuseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexDir + $"Terrain_{name}_BaseColor.png");
            layer.normalMapTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(nrmPath);
            layer.normalScale = 0.8f;
            layer.tileSize = new Vector2(tile, tile);
            layer.metallic = 0f;
            layer.smoothness = smooth;
            EditorUtility.SetDirty(layer);
            result[i] = layer;
        }
        return result;
    }
}
