using TMPro;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Tools/UI/Add Prologue (Arena) : 프롤로그 → 보스 등장 → 조작 설명 연출에 필요한 오브젝트를 만들고 연결
//  1) PrologueGroup  : 검은 화면 + 가운데 문장(Line, 자체 CanvasGroup) + "Space 건너뛰기"
//  2) NameCardGroup  : 화면 아래쪽 띠 + THE LAST WARDEN / 성문의 서약기사 알데리크
//  3) IntroCam       : 보스 정면 아래에서 올려다보는 CinemachineCamera (Priority 100, 처음엔 꺼둠, 손떨림 노이즈)
//  3-1) CameraBlends: IntroCam ↔ 다른 카메라 전환은 Cut (돌아가는 2초 블렌드가 안 보이게)
//  4) BGM            : Play On Awake 끔 → 전투 시작 때 IntroSequence가 Play
//  5) IntroSequence  : 스크립트가 있으면 "Intro" 오브젝트에 붙이고 필드 연결(눈빛 스팅어 _revealSfx 포함) + 문장 채우기,
//                      예전 ControlsScreen 컴포넌트는 제거 (조작 설명도 IntroSequence가 띄움)
// 다시 실행하면 같은 이름 오브젝트를 지우고 새로 만듦
public static class PrologueBuilder
{
    private static readonly string[] Lines =
    {
        "숲의 끝에서, 방랑자는 무너진 성문을 보았다.",
        "오래전 사라진 신성왕국의 마지막 문.",
        "그 앞에는 빛바랜 흰 갑옷 하나가\n서 있었다.",
        "한 걸음 다가가자,\n투구 안에서 빛이 켜졌다.",
    };

    private const string RevealSfx = "Assets/_Project/Audio/SFX/Intro/SFX_Intro_Reveal.ogg";   // 눈빛이 켜질 때 브아앙

    private const string BlendsPath = "Assets/_Project/ScriptableObjects/CameraBlends.asset";
    private const string AnyCamera = "**ANY CAMERA**";               // Cinemachine 예약어

    private const string NoisePath =
        "Packages/com.unity.cinemachine/Presets/Noise/Handheld_normal_mild.asset";

    private static readonly Color Gold = new Color(0.95f, 0.86f, 0.62f);
    private static readonly Color Paper = new Color(0.92f, 0.89f, 0.83f);

    [MenuItem("Tools/UI/Add Prologue")]
    private static void Build()
    {
        Scene scene = SceneManager.GetActiveScene();
        Transform canvas = FindDeep(scene, "UI Canvas");
        var boss = Object.FindFirstObjectByType<BossStateMachine>(FindObjectsInactive.Include);
        if (canvas == null || boss == null) { Debug.LogError("[Prologue] UI Canvas / Boss를 찾지 못함 — Arena 씬을 열고 실행하세요"); return; }

        TMP_FontAsset kr = TitleSceneBuilder.KoreanFont(false), krBold = TitleSceneBuilder.KoreanFont(true);
        TMP_FontAsset title = TitleSceneBuilder.TitleFont();

        // ---- 1) 프롤로그 (검은 화면 + 문장)
        Remove(canvas, "PrologueGroup");
        RectTransform prologue = Stretch("PrologueGroup", canvas);
        Undo.RegisterCreatedObjectUndo(prologue.gameObject, "Prologue");
        CanvasGroup prologueGroup = Group(prologue.gameObject);
        var black = prologue.gameObject.AddComponent<Image>();
        black.color = Color.black;
        black.raycastTarget = false;

        RectTransform lineRt = Rect("Line", prologue, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1400, 220));
        CanvasGroup lineGroup = Group(lineRt.gameObject);
        TextMeshProUGUI line = AddText(lineRt, Lines[0], kr, 44, Paper, 4f);
        line.lineSpacing = 30f;

        RectTransform skipRt = Rect("SkipHint", prologue, new Vector2(1f, 0f), new Vector2(-48f, 40f), new Vector2(400, 40));
        skipRt.pivot = new Vector2(1f, 0f);                  // 오른쪽 아래 모서리 기준 → 화면 비율이 바뀌어도 안 잘림
        TextMeshProUGUI skip = AddText(skipRt, "Space  건너뛰기", kr, 22, new Color(0.85f, 0.82f, 0.75f, 0.45f), 4f);
        skip.alignment = TextAlignmentOptions.Right;

        // ---- 2) 보스 이름 카드 (아래쪽 1/4 지점의 어두운 띠)
        Remove(canvas, "NameCardGroup");
        RectTransform card = Stretch("NameCardGroup", canvas);
        Undo.RegisterCreatedObjectUndo(card.gameObject, "Name Card");
        CanvasGroup cardGroup = Group(card.gameObject);
        RectTransform band = Rect("Band", card, new Vector2(0.5f, 0.2f), Vector2.zero, Vector2.zero);
        band.anchorMin = new Vector2(0f, 0.2f); band.anchorMax = new Vector2(1f, 0.2f);
        band.sizeDelta = new Vector2(0f, 200f);
        var bandImg = band.gameObject.AddComponent<Image>();
        bandImg.color = new Color(0f, 0f, 0f, 0.6f);
        bandImg.raycastTarget = false;
        TextMeshProUGUI nameEn = AddText(Rect("Title", band, new Vector2(0.5f, 0.5f), new Vector2(0, 25), new Vector2(1600, 110)),
                                         "THE LAST WARDEN", title, 84, Gold, 16f);
        nameEn.textWrappingMode = TextWrappingModes.NoWrap;
        AddText(Rect("Sub", band, new Vector2(0.5f, 0.5f), new Vector2(0, -55), new Vector2(1200, 50)),
                "성문의 서약기사 알데리크", kr, 32, Paper, 6f);

        // 순서: HUD < 이름 카드 < 프롤로그 < 조작 설명 < 일시정지
        card.SetAsLastSibling();
        prologue.SetAsLastSibling();
        Transform controls = canvas.Find("ControlsGroup");
        if (controls != null) controls.SetAsLastSibling();
        Transform pause = canvas.Find("PauseGroup");
        if (pause != null) pause.SetAsLastSibling();

        // ---- 3) 보스 등장 카메라
        CinemachineCamera introCam = BuildIntroCam(scene, boss);
        SetupCutBlends();

        // ---- 4) BGM은 전투 시작 때 재생
        AudioSource bgm = null;
        foreach (GameObject root in scene.GetRootGameObjects())
            if (root.name == "BGM") bgm = root.GetComponent<AudioSource>();
        if (bgm != null) { Undo.RecordObject(bgm, "BGM"); bgm.playOnAwake = false; }
        else Debug.LogWarning("[Prologue] BGM 오브젝트가 없음 — Tools/Audio/Setup BGM 먼저");

        // ---- 5) IntroSequence 연결
        WireIntro(scene, prologueGroup, lineGroup, line, cardGroup, introCam, boss, controls, bgm);

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("[Prologue] 프롤로그 UI·IntroCam 생성 완료");
    }

    private static CinemachineCamera BuildIntroCam(Scene scene, BossStateMachine boss)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
            if (root.name == "IntroCam") Undo.DestroyObjectImmediate(root);

        // 보스 크기(렌더러 바운즈)로 거리·높이를 정함 → 스케일이 바뀌어도 같은 구도
        Bounds b = new Bounds(boss.transform.position, Vector3.one);
        bool first = true;
        foreach (Renderer r in boss.GetComponentsInChildren<Renderer>())
        {
            if (r is ParticleSystemRenderer) continue;
            if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds);
        }
        float h = Mathf.Max(b.size.y, 1f);
        Vector3 feet = new Vector3(boss.transform.position.x, b.min.y, boss.transform.position.z);

        Vector3 head = feet + Vector3.up * h * 0.9f;
        var animator = boss.GetComponentInChildren<Animator>();
        if (animator != null && animator.isHuman)
        {
            Transform hb = animator.GetBoneTransform(HumanBodyBones.Head);
            if (hb != null) head = hb.position;
        }

        Vector3 fwd = boss.transform.forward; fwd.y = 0f; fwd.Normalize();
        Vector3 right = Vector3.Cross(Vector3.up, fwd);
        // 정면에서 살짝 옆, 가슴 아래 높이 → 올려다보는 구도 (뒤로 성문이 보임)
        Vector3 camPos = feet + fwd * (h * 1.15f) + right * (h * 0.25f) + Vector3.up * (h * 0.4f);
        Vector3 lookAt = Vector3.Lerp(feet + Vector3.up * h * 0.6f, head, 0.6f);

        var go = new GameObject("IntroCam");
        Undo.RegisterCreatedObjectUndo(go, "Intro Cam");
        go.transform.SetPositionAndRotation(camPos, Quaternion.LookRotation(lookAt - camPos));
        var cam = go.AddComponent<CinemachineCamera>();
        cam.Priority = 100;                                  // 켜지면 플레이어 카메라보다 우선
        LensSettings lens = cam.Lens;
        lens.FieldOfView = 38f;                              // 좁은 화각 → 묵직한 인물 샷
        cam.Lens = lens;

        var noise = go.AddComponent<CinemachineBasicMultiChannelPerlin>();
        noise.NoiseProfile = AssetDatabase.LoadAssetAtPath<NoiseSettings>(NoisePath);
        noise.AmplitudeGain = 0.4f;                          // 아주 약한 손떨림 → 정지 화면처럼 안 보이게
        noise.FrequencyGain = 0.5f;
        if (noise.NoiseProfile == null) Debug.LogWarning("[Prologue] 노이즈 프리셋을 찾지 못함 (흔들림 없이 진행)");

        go.SetActive(false);                                 // IntroSequence가 켜고 끔
        return cam;
    }

    private static void SetupCutBlends()
    {
        var brain = Object.FindFirstObjectByType<CinemachineBrain>();
        if (brain == null) { Debug.LogWarning("[Prologue] CinemachineBrain을 찾지 못해 Cut 전환 설정 생략"); return; }

        var blends = AssetDatabase.LoadAssetAtPath<CinemachineBlenderSettings>(BlendsPath);
        if (blends == null)
        {
            blends = ScriptableObject.CreateInstance<CinemachineBlenderSettings>();
            AssetDatabase.CreateAsset(blends, BlendsPath);
        }
        var cut = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0f);
        blends.CustomBlends = new[]
        {
            new CinemachineBlenderSettings.CustomBlend { From = AnyCamera, To = "IntroCam", Blend = cut },
            new CinemachineBlenderSettings.CustomBlend { From = "IntroCam", To = AnyCamera, Blend = cut },
        };
        EditorUtility.SetDirty(blends);
        AssetDatabase.SaveAssets();

        Undo.RecordObject(brain, "Camera Blends");
        brain.CustomBlends = blends;                        // 나머지 전환(락온 등)은 기존 Default Blend 그대로
    }

    private static void WireIntro(Scene scene, CanvasGroup prologue, CanvasGroup lineGroup, TMP_Text line,
                                  CanvasGroup card, CinemachineCamera introCam, BossStateMachine boss,
                                  Transform controls, AudioSource bgm)
    {
        System.Type type = System.Type.GetType("IntroSequence, Assembly-CSharp");
        if (type == null) { Debug.Log("[Prologue] IntroSequence 스크립트가 아직 없음 — 작성 후 다시 실행하면 자동 연결"); return; }

        GameObject host = null;
        foreach (GameObject root in scene.GetRootGameObjects())
            if (root.name == "Intro") host = root;
        if (host == null) { host = new GameObject("Intro"); Undo.RegisterCreatedObjectUndo(host, "Intro"); }
        Component intro = host.GetComponent(type) ?? Undo.AddComponent(host, type);

        var brain = Object.FindFirstObjectByType<CinemachineBrain>();
        var so = new SerializedObject(intro);
        Set(so, "_prologueGroup", prologue);
        Set(so, "_lineGroup", lineGroup);
        Set(so, "_lineText", line);
        Set(so, "_nameCardGroup", card);
        Set(so, "_introCam", introCam);
        Set(so, "_brain", brain);
        Set(so, "_boss", boss);
        Set(so, "_controlsGroup", controls != null ? controls.GetComponent<CanvasGroup>() : null);
        Set(so, "_bgm", bgm);
        Set(so, "_revealSfx", AssetDatabase.LoadAssetAtPath<AudioClip>(RevealSfx));
        Transform freeLook = FindDeep(scene, "FreeLook Camera");   // 컷씬 → 플레이어 전환 때 Inherit Position 잠시 끄기용
        Set(so, "_playerCam", freeLook != null ? freeLook.GetComponent<CinemachineCamera>() : null);

        SerializedProperty lines = so.FindProperty("_lines");
        if (lines != null && lines.isArray)
        {
            lines.arraySize = Lines.Length;
            for (int i = 0; i < Lines.Length; i++) lines.GetArrayElementAtIndex(i).stringValue = Lines[i];
        }
        else Debug.LogWarning("[Prologue] IntroSequence에 string[] _lines 필드가 없어 문장 채우기 생략");
        so.ApplyModifiedProperties();

        // 조작 설명은 이제 IntroSequence가 띄움 → 예전 ControlsScreen 컴포넌트 제거 (스크립트 파일은 직접 삭제)
        System.Type old = System.Type.GetType("ControlsScreen, Assembly-CSharp");
        if (old != null)
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (Component c in root.GetComponentsInChildren(old, true))
                {
                    Debug.Log($"[Prologue] {c.gameObject.name}의 ControlsScreen 제거");
                    Undo.DestroyObjectImmediate(c);
                }

        Debug.Log("[Prologue] IntroSequence 연결 완료");
    }

    private static void Set(SerializedObject so, string field, Object value)
    {
        SerializedProperty p = so.FindProperty(field);
        if (p == null) { Debug.LogWarning($"[Prologue] IntroSequence에 {field} 필드가 없어 연결 생략"); return; }
        if (value == null) Debug.LogWarning($"[Prologue] {field}에 넣을 대상을 찾지 못함");
        p.objectReferenceValue = value;
    }

    // ---------------------------------------------------------------- 헬퍼
    private static CanvasGroup Group(GameObject go)
    {
        var g = go.AddComponent<CanvasGroup>();
        g.alpha = 0f; g.interactable = false; g.blocksRaycasts = false;   // 처음엔 숨김 (재시작 때 안 보이게)
        return g;
    }

    private static void Remove(Transform parent, string name)
    {
        Transform old = parent.Find(name);
        if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    private static RectTransform Stretch(string name, Transform parent)
    {
        RectTransform rt = Rect(name, parent, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        return rt;
    }

    private static TextMeshProUGUI AddText(RectTransform rt, string text, TMP_FontAsset font, float size, Color color, float spacing)
    {
        var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.font = font;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.characterSpacing = spacing;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static Transform FindDeep(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
        return null;
    }
}
