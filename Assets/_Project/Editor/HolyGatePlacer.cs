using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Tools/Map/Place Holy Gate
// 성문 + 성벽 모듈을 현재 씬(Arena)의 보스 뒤쪽에 배치한다.
// 1) 대리석 텍스처 임포트 설정  2) URP Lit 머티리얼 생성  3) FBX 머티리얼 리맵
// 4) 배치(재실행하면 기존 HolyGate_Env를 지우고 다시 배치)  5) 기존 Cylinder 비활성화
public static class HolyGatePlacer
{
    private const string ModelDir = "Assets/_Project/Models/HolyGate/";
    private const string TexDir = ModelDir + "Textures/";
    private const string GroundTexDir = "Assets/_Project/Textures/Ground/";
    private const float GroundTiling = 20f;      // 100m 바닥 / 5m 타일
    private const string MatParent = "Assets/_Project/Materials";
    private const string MatDir = MatParent + "/HolyGate/";
    private const string EnvName = "HolyGate_Env";

    // 아레나 중심(-2.9, 0, -0.5) 기준, 보스(X=-10) 뒤쪽 35m 지점. 정면은 +X(아레나 쪽)
    private static readonly Vector3 GatePos = new Vector3(-45f, 0f, -0.5f);
    private const float GateHalfWidth = 10.9f;   // 성문 중심 → 탑 바깥 끝 (모델 기준, 스케일 전)
    public const float GateScale = 2f;
    private const float PlazaScale = 1.3f;       // 광장 실제 배율 (반경 약 20m)           // 성문·성벽·광장 전체 크기 배율 (HolyGate_Env 스케일)

    private static readonly string[] ModelFiles =
    {
        "HolyGate.fbx", "HolyGate_Plaza.fbx",
        "Walls/HolyWall_Intact.fbx", "Walls/HolyWall_BrokenA.fbx", "Walls/HolyWall_BrokenB.fbx",
        "Walls/HolyWall_Stub.fbx", "Walls/HolyWall_Rubble.fbx",
    };

    [MenuItem("Tools/Map/Place Holy Gate")]
    private static void Place()
    {
        SetupTextures();
        Dictionary<string, Material> mats = SetupMaterials();
        foreach (string f in ModelFiles) RemapMaterials(ModelDir + f, mats);

        var scene = EditorSceneManager.GetActiveScene();
        var (oldParent, oldSibling) = RemoveExisting(EnvName);

        var env = new GameObject(EnvName);
        Undo.RegisterCreatedObjectUndo(env, "Place Holy Gate");
        if (oldParent != null) { env.transform.SetParent(oldParent, false); env.transform.SetSiblingIndex(oldSibling); }
        // 크기는 HolyGate_Env 하나의 스케일로 조절 → 자식은 전부 "성문 중심 기준 로컬 좌표"로 배치
        // (스케일을 키워도 성문이 제자리에서 커지고, 멀리 밀려나지 않음)
        env.transform.SetPositionAndRotation(GatePos, Quaternion.identity);
        float parentScale = env.transform.parent != null ? env.transform.parent.lossyScale.x : 1f;
        env.transform.localScale = Vector3.one * (GateScale / parentScale);

        // 성문: 정면이 아레나(+X)를 보게. FBX 축 변환 결과와 상관없이 바운즈로 확인해서 맞춘다
        GameObject gate = Spawn("HolyGate.fbx", env.transform, Vector3.zero, Quaternion.Euler(0f, 90f, 0f));
        if (LocalCenter(gate).x < 0f) gate.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
        Quaternion rot = gate.transform.localRotation;

        // 성문 앞 포석 광장 (성문과 같은 피벗/방향으로 만들어져 있음). 바닥 Terrain이 충돌을 담당하므로 콜라이더 없음
        GameObject plaza = Spawn("HolyGate_Plaza.fbx", env.transform, Vector3.zero, rot);
        plaza.transform.localScale = Vector3.one * (PlazaScale / GateScale);   // 광장은 성문만큼 키우면 아레나를 다 덮어서 따로 조절

        // 오른쪽(+Z): 온전 → 무너짐A → (틈) → 그루터기
        PlaceWall("Walls/HolyWall_Intact.fbx", env.transform, rot, +1f, GateHalfWidth);
        PlaceWall("Walls/HolyWall_BrokenA.fbx", env.transform, rot, +1f, GateHalfWidth + 8f);
        PlaceWall("Walls/HolyWall_Stub.fbx", env.transform, rot, +1f, GateHalfWidth + 19f);
        PlaceRubble(env.transform, +1f, GateHalfWidth + 15.5f, 2.5f, 0f);
        PlaceRubble(env.transform, +1f, GateHalfWidth + 21f, 2.2f, 70f);

        // 왼쪽(-Z): 온전 → 무너짐B → 무너짐A
        PlaceWall("Walls/HolyWall_Intact.fbx", env.transform, rot, -1f, GateHalfWidth);
        PlaceWall("Walls/HolyWall_BrokenB.fbx", env.transform, rot, -1f, GateHalfWidth + 8f);
        PlaceWall("Walls/HolyWall_BrokenA.fbx", env.transform, rot, -1f, GateHalfWidth + 16f);
        PlaceRubble(env.transform, -1f, GateHalfWidth + 12f, 2.4f, 40f);
        PlaceRubble(env.transform, -1f, GateHalfWidth + 21f, 2.6f, 160f);

        // 충돌 + 정적 배칭
        foreach (MeshFilter mf in env.GetComponentsInChildren<MeshFilter>())
        {
            if (mf.transform.IsChildOf(plaza.transform)) continue;
            var mc = Undo.AddComponent<MeshCollider>(mf.gameObject);
            mc.sharedMesh = mf.sharedMesh;
        }
        foreach (Transform t in env.GetComponentsInChildren<Transform>())
            GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic);

        // 숲 속 공터 컨셉 → 기존 기둥 4개는 끔(삭제 X)
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name.StartsWith("Cylinder"))
                {
                    Undo.RecordObject(t.gameObject, "Disable Cylinder");
                    t.gameObject.SetActive(false);
                }

        // 아레나 바닥(Plane)에 숲 바닥 머티리얼
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (MeshRenderer r in root.GetComponentsInChildren<MeshRenderer>(true))
                if (r.name == "Plane")
                {
                    Undo.RecordObject(r, "Ground Material");
                    r.sharedMaterial = mats["Ground_Forest"];
                }

        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = env;
        Debug.Log($"[HolyGate] 배치 완료: 성문 회전 {rot.eulerAngles.y:F0}°, 오브젝트 {env.transform.childCount}개");
    }

    // ---------- 배치
    private static void PlaceWall(string file, Transform parent, Quaternion rot, float side, float edge)
    {
        Vector3 pos = new Vector3(0f, 0f, side * edge);   // 벽의 피벗 = 모듈 시작점(아래, 벽 두께 중앙)
        GameObject w = Spawn(file, parent, pos, rot);

        // 정면(버트레스 쪽)이 아레나를 보게
        if (LocalCenter(w).x < 0f) w.transform.localRotation = rot * Quaternion.Euler(0f, 180f, 0f);
        // 성문에서 바깥쪽으로 뻗게 (반대면 X 미러 → 좌우 반전된 모양이 돼서 반복감도 줄어듦)
        if ((LocalCenter(w).z - pos.z) * side < 0f)
        {
            Vector3 s = w.transform.localScale; s.x *= -1f; w.transform.localScale = s;
        }
    }

    private static void PlaceRubble(Transform parent, float side, float edge, float forward, float yaw)
    {
        Vector3 pos = new Vector3(forward, 0f, side * edge);
        Spawn("Walls/HolyWall_Rubble.fbx", parent, pos, Quaternion.Euler(0f, yaw, 0f));
    }

    private static GameObject Spawn(string file, Transform parent, Vector3 pos, Quaternion rot)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelDir + file);
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.transform.localPosition = pos;
        go.transform.localRotation = rot;
        return go;
    }

    // 렌더러 바운즈 중심을 부모(HolyGate_Env) 로컬 좌표로
    private static Vector3 LocalCenter(GameObject go) => go.transform.parent.InverseTransformPoint(WorldBounds(go).center);

    private static Bounds WorldBounds(GameObject go)
    {
        Renderer[] rs = go.GetComponentsInChildren<Renderer>();
        Bounds b = rs[0].bounds;
        foreach (Renderer r in rs) b.Encapsulate(r.bounds);
        return b;
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

    // ---------- 텍스처 / 머티리얼
    private static void SetupTextures()
    {
        foreach (string g in new[] { "Ground_Forest", "Ground_Flagstone" })
        {
            SetTexture(GroundTexDir + g + "_Normal.png", t => t.textureType = TextureImporterType.NormalMap);
            SetTexture(GroundTexDir + g + "_MetallicSmoothness.png", t =>
            {
                t.sRGBTexture = false;
                t.alphaSource = TextureImporterAlphaSource.FromInput;
            });
        }
        SetTexture(TexDir + "HolyGate_Marble_Normal.png", t => t.textureType = TextureImporterType.NormalMap);
        SetTexture(TexDir + "HolyGate_Marble_MetallicSmoothness.png", t =>
        {
            t.sRGBTexture = false;                                   // 데이터 텍스처
            t.alphaSource = TextureImporterAlphaSource.FromInput;    // 알파 = Smoothness
        });
    }

    private static void SetTexture(string path, System.Action<TextureImporter> setup)
    {
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp == null) { Debug.LogError($"[HolyGate] 텍스처 없음: {path}"); return; }
        setup(imp);
        imp.SaveAndReimport();
    }

    private static Dictionary<string, Material> SetupMaterials()
    {
        if (!AssetDatabase.IsValidFolder(MatDir.TrimEnd('/'))) AssetDatabase.CreateFolder(MatParent, "HolyGate");

        var baseMap = AssetDatabase.LoadAssetAtPath<Texture2D>(TexDir + "HolyGate_Marble_BaseColor.png");
        var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(TexDir + "HolyGate_Marble_Normal.png");
        var metal = AssetDatabase.LoadAssetAtPath<Texture2D>(TexDir + "HolyGate_Marble_MetallicSmoothness.png");

        System.Action<Material, Color> marble = (m, tint) =>
        {
            m.SetTexture("_BaseMap", baseMap); m.SetColor("_BaseColor", tint);
            m.SetTexture("_BumpMap", normal); m.SetFloat("_BumpScale", 0.6f); m.EnableKeyword("_NORMALMAP");
            m.SetTexture("_MetallicGlossMap", metal); m.EnableKeyword("_METALLICSPECGLOSSMAP");
            m.SetFloat("_SmoothnessTextureChannel", 0f);   // Metallic Alpha
            m.SetFloat("_Smoothness", 1f);                  // 맵 알파에 곱해지는 배율
        };

        System.Action<Material, string, float> ground = (m, pre, bump) =>
        {
            m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(GroundTexDir + pre + "_BaseColor.png"));
            m.SetColor("_BaseColor", Color.white);
            m.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(GroundTexDir + pre + "_Normal.png"));
            m.SetFloat("_BumpScale", bump); m.EnableKeyword("_NORMALMAP");
            m.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(GroundTexDir + pre + "_MetallicSmoothness.png"));
            m.EnableKeyword("_METALLICSPECGLOSSMAP");
            m.SetFloat("_SmoothnessTextureChannel", 0f); m.SetFloat("_Smoothness", 1f);
        };

        return new Dictionary<string, Material>
        {
            // 키 = FBX 안의 머티리얼 이름 (리맵용). Ground_Forest는 Plane에 직접 넣음
            { "Plaza_Flagstone", MakeMat("M_GroundFlagstone", m => ground(m, "Ground_Flagstone", 1f)) },
            { "Ground_Forest",   MakeMat("M_GroundForest",    m =>
                {
                    ground(m, "Ground_Forest", 0.8f);
                    m.SetTextureScale("_BaseMap", new Vector2(GroundTiling, GroundTiling));
                }) },
            { "HolyGate_Marble", MakeMat("M_HolyMarble", m => marble(m, Color.white)) },
            { "HolyGate_Door",   MakeMat("M_HolyDoor",   m => marble(m, new Color(0.86f, 0.84f, 0.8f))) },
            { "HolyGate_Gold",   MakeMat("M_HolyGold",   m =>
                {
                    m.SetColor("_BaseColor", new Color(0.85f, 0.63f, 0.24f));
                    m.SetFloat("_Metallic", 1f); m.SetFloat("_Smoothness", 0.75f);
                }) },
        };
    }

    private static Material MakeMat(string name, System.Action<Material> setup)
    {
        string path = MatDir + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(m, path);
        }
        setup(m);
        EditorUtility.SetDirty(m);
        return m;
    }

    private static void RemapMaterials(string path, Dictionary<string, Material> mats)
    {
        var imp = AssetImporter.GetAtPath(path) as ModelImporter;
        if (imp == null) { Debug.LogError($"[HolyGate] 모델 없음: {path}"); return; }
        imp.animationType = ModelImporterAnimationType.None;
        imp.importAnimation = false;
        foreach (var kv in mats)
            imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), kv.Key), kv.Value);
        imp.SaveAndReimport();
    }
}
