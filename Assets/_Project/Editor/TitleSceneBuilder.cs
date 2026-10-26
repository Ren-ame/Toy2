using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

// Tools/UI/Build Title Scene : Arena를 복사해 전투 요소를 뺀 타이틀 씬 생성 + 타이틀/인트로 UI 배치 + 빌드 씬 순서(Title → Arena)
// Tools/UI/Add Boss Nameplate : (Arena를 연 상태에서) 보스 HP 바 위에 "성문의 서약기사 알데리크"
// 폰트: Nanum Myeongjo(한글, OFL), Cinzel(영문 제목, OFL) → TMP Dynamic 폰트 에셋 자동 생성
public static class TitleSceneBuilder
{
    private const string ArenaPath = "Assets/_Project/Scenes/Arena.unity";
    private const string TitlePath = "Assets/_Project/Scenes/Title.unity";
    private const string FontDir = "Assets/_Project/Fonts/";

    public const string GameTitle = "THE LAST WARDEN";
    public const string BossName = "성문의 서약기사 알데리크";

    // 타이틀 씬에서 지울 전투용 오브젝트 (이름 기준, 계층 전체 검색)
    private static readonly string[] RemoveNames =
        { "Player", "Boss", "Systems", "UI Canvas", "Katana_Prefab", "TrainingDummy_Prefab", "ArenaBoundary" };

    // 카메라: 아레나 동쪽에서 성문(서쪽)을 올려다봄
    private static readonly Vector3 CamPos = new Vector3(22f, 4f, 16f);
    private static readonly Vector3 CamLookAt = new Vector3(-45f, 24f, -0.5f);

    private static readonly Color Gold = new Color(0.95f, 0.86f, 0.62f);
    private static readonly Color Paper = new Color(0.92f, 0.89f, 0.83f);

    // ---------------------------------------------------------------- 폰트
    public static TMP_FontAsset KoreanFont(bool bold) =>
        MakeFont(FontDir + "NanumMyeongjo/NanumMyeongjo-" + (bold ? "Bold" : "Regular") + ".ttf",
                 FontDir + "NanumMyeongjo/NanumMyeongjo-" + (bold ? "Bold" : "Regular") + " SDF.asset");

    public static TMP_FontAsset TitleFont() =>
        MakeFont(FontDir + "Cinzel/Cinzel.ttf", FontDir + "Cinzel/Cinzel SDF.asset");

    private static TMP_FontAsset MakeFont(string ttfPath, string assetPath)
    {
        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
        if (existing != null) return existing;

        var font = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
        if (font == null) { Debug.LogError($"[Title] 폰트 없음: {ttfPath}"); return null; }

        // Dynamic: 쓰는 글자만 그때그때 아틀라스에 추가 → 한글 1만여 자를 미리 굽지 않아도 됨
        TMP_FontAsset fa = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 2048, 2048,
                                                         AtlasPopulationMode.Dynamic, true);
        fa.name = Path.GetFileNameWithoutExtension(assetPath);
        AssetDatabase.CreateAsset(fa, assetPath);
        fa.material.name = fa.name + " Material";
        fa.atlasTexture.name = fa.name + " Atlas";
        AssetDatabase.AddObjectToAsset(fa.material, fa);
        AssetDatabase.AddObjectToAsset(fa.atlasTexture, fa);
        AssetDatabase.SaveAssets();
        return fa;
    }

    // ---------------------------------------------------------------- 타이틀 씬
    [MenuItem("Tools/UI/Build Title Scene")]
    private static void BuildTitle()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(TitlePath) != null &&
            !EditorUtility.DisplayDialog("Title 씬", "Title.unity가 이미 있어요. Arena를 다시 복사해서 새로 만들까요?", "새로 만들기", "취소"))
            return;

        TMP_FontAsset kr = KoreanFont(false), krBold = KoreanFont(true), title = TitleFont();

        AssetDatabase.DeleteAsset(TitlePath);
        AssetDatabase.CopyAsset(ArenaPath, TitlePath);
        Scene scene = EditorSceneManager.OpenScene(TitlePath, OpenSceneMode.Single);

        // 전투 요소 제거
        foreach (string n in RemoveNames)
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                    if (t != null && t.name == n) Object.DestroyImmediate(t.gameObject);

        // 카메라: Cinemachine Brain 제거(따라갈 가상 카메라가 없음) 후 고정 구도
        Camera cam = null;
        foreach (GameObject root in scene.GetRootGameObjects())
            if (root.name == "Main Camera") cam = root.GetComponent<Camera>();
        if (cam != null)
        {
            foreach (Component c in cam.GetComponents<Component>())
                if (c != null && c.GetType().Name.Contains("Cinemachine")) Object.DestroyImmediate(c);
            cam.transform.position = CamPos;
            cam.transform.rotation = Quaternion.LookRotation(CamLookAt - CamPos);
            cam.fieldOfView = 50f;
            cam.farClipPlane = 1000f;
        }

        BuildTitleUI(kr, krBold, title);

        EditorSceneManager.SaveScene(scene);
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(TitlePath, true),
            new EditorBuildSettingsScene(ArenaPath, true),
        };
        Debug.Log("[Title] 타이틀 씬 생성 완료. 빌드 순서: Title(0) → Arena(1)");
    }

    private static void BuildTitleUI(TMP_FontAsset kr, TMP_FontAsset krBold, TMP_FontAsset titleFont)
    {
        var canvasGO = new GameObject("Title Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // --- 타이틀 그룹 (제목 + 버튼)
        RectTransform titleGroup = Stretch("TitleGroup", canvasGO.transform);
        titleGroup.gameObject.AddComponent<CanvasGroup>();

        // 아래쪽을 어둡게 깔아 글자가 배경에 묻히지 않게
        var shade = Rect("BottomShade", titleGroup, new Vector2(0, 0), new Vector2(1, 0.55f), new Vector2(0.5f, 0), Vector2.zero, Vector2.zero);
        var shadeImg = shade.gameObject.AddComponent<Image>();
        shadeImg.color = new Color(0f, 0f, 0f, 0.45f);
        shadeImg.raycastTarget = false;

        Text(titleGroup, "Title", GameTitle, titleFont, 120, Gold, new Vector2(0.5f, 0.66f), new Vector2(1600, 160), 12f);
        Text(titleGroup, "Subtitle", "마지막 수문장", kr, 34, Paper, new Vector2(0.5f, 0.56f), new Vector2(800, 60), 6f);

        Button(titleGroup, "StartButton", "게임 시작", krBold, new Vector2(0.5f, 0.34f));
        Button(titleGroup, "QuitButton", "종료", krBold, new Vector2(0.5f, 0.26f));

        // --- 인트로 그룹 (검은 화면 + 대사). 시작 버튼을 누르면 TitleMenu가 보여 줌
        RectTransform intro = Stretch("IntroGroup", canvasGO.transform);
        var introGroup = intro.gameObject.AddComponent<CanvasGroup>();
        introGroup.alpha = 0f;
        introGroup.blocksRaycasts = false;
        var bg = intro.gameObject.AddComponent<Image>();
        bg.color = Color.black;
        TextMeshProUGUI line = Text(intro, "IntroText", "", kr, 40, Paper, new Vector2(0.5f, 0.5f), new Vector2(1400, 300), 2f);
        line.alpha = 0f;
        line.lineSpacing = 30f;
    }

    // ---------------------------------------------------------------- 보스 이름판 (Arena)
    [MenuItem("Tools/UI/Add Boss Nameplate")]
    private static void AddNameplate()
    {
        Scene scene = SceneManager.GetActiveScene();
        Transform bar = null;
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == "BossHP_BG") bar = t;
        if (bar == null) { Debug.LogError("[Title] BossHP_BG를 찾지 못함 — Arena 씬을 열고 실행하세요"); return; }

        Transform old = bar.Find("BossName");
        if (old != null) Undo.DestroyObjectImmediate(old.gameObject);

        // HP 바 왼쪽 위에 (소울류 배치). 체간 바(가운데, 높이 8~18)보다 위
        RectTransform rt = Rect("BossName", bar, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 0), new Vector2(4, 22), new Vector2(900, 44));
        Undo.RegisterCreatedObjectUndo(rt.gameObject, "Boss Nameplate");
        var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.font = KoreanFont(true);
        tmp.text = BossName;
        tmp.fontSize = 30;
        tmp.color = Paper;
        tmp.alignment = TextAlignmentOptions.BottomLeft;
        tmp.characterSpacing = 2f;
        tmp.raycastTarget = false;

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("[Title] 보스 이름판 추가 완료");
    }

    // ---------------------------------------------------------------- 결과 화면 (Arena)
    // 족자·붓글씨(勝利/死) → 화면 가운데 어두운 가로 띠 + Cinzel 큰 글자 + 한글 한 줄 + 재시작 안내
    // GameFlowController가 참조하는 오브젝트(ResultPanel / You Win / You Died)는 그대로 두고 모양만 바꿈
    [MenuItem("Tools/UI/Restyle Result Screen")]
    private static void RestyleResult()
    {
        Scene scene = SceneManager.GetActiveScene();
        Transform panel = FindDeep(scene, "ResultPanel"), band = FindDeep(scene, "Scroll") ?? FindDeep(scene, "Band");
        Transform win = FindDeep(scene, "You Win"), lose = FindDeep(scene, "You Died");
        if (panel == null || band == null || win == null || lose == null)
        { Debug.LogError("[Title] ResultPanel/Scroll/You Win/You Died 중 일부를 찾지 못함 — Arena 씬을 열고 실행하세요"); return; }

        TMP_FontAsset title = TitleFont(), kr = KoreanFont(false);
        Undo.RegisterFullObjectHierarchyUndo(panel.gameObject, "Restyle Result");

        var panelImg = panel.GetComponent<Image>();          // 화면 전체를 살짝 어둡게
        panelImg.sprite = null;
        panelImg.color = new Color(0f, 0f, 0f, 0.35f);

        band.name = "Band";                                   // 족자 → 가운데 가로 띠
        var bandImg = band.GetComponent<Image>();
        bandImg.sprite = null;
        bandImg.color = new Color(0f, 0f, 0f, 0.78f);
        SetRect((RectTransform)band, new Vector2(0, 0.5f), new Vector2(1, 0.5f), Vector2.zero, new Vector2(0, 280));

        StyleResult(win, "WARDEN SLAIN", Gold, "서약의 기사 알데리크, 마침내 잠들다", title, kr);
        StyleResult(lose, "FALLEN", new Color(0.75f, 0.13f, 0.1f), "서약의 빛 앞에 쓰러졌다", title, kr);

        Transform oldHint = band.Find("Hint");
        if (oldHint != null) Object.DestroyImmediate(oldHint.gameObject);
        TextMeshProUGUI hint = Text(band, "Hint", "R  다시 도전", kr, 24, new Color(0.85f, 0.82f, 0.75f, 0.8f),
                                    new Vector2(0.5f, 0.5f), new Vector2(800, 40), 6f);
        ((RectTransform)hint.transform).anchoredPosition = new Vector2(0, -112);

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("[Title] 결과 화면 스타일 변경 완료");
    }

    private static void StyleResult(Transform t, string text, Color color, string sub, TMP_FontAsset font, TMP_FontAsset subFont)
    {
        var tmp = t.GetComponent<TextMeshProUGUI>();
        tmp.font = font;
        tmp.text = text;
        tmp.fontSize = 110;
        tmp.color = color;
        tmp.characterSpacing = 16f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        SetRect((RectTransform)t, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0, 35), new Vector2(0, 140));

        Transform old = t.Find("Sub");                      // 한글 한 줄 — 제목의 자식이라 같이 켜지고 꺼짐
        if (old != null) Object.DestroyImmediate(old.gameObject);
        TextMeshProUGUI s = Text(t, "Sub", sub, subFont, 32, Paper, new Vector2(0.5f, 0.5f), new Vector2(1200, 50), 4f);
        ((RectTransform)s.transform).anchoredPosition = new Vector2(0, -95);
    }

    private static void SetRect(RectTransform rt, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = size;
    }

    private static Transform FindDeep(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
        return null;
    }

    // ---------------------------------------------------------------- UI 도우미
    private static RectTransform Rect(string name, Transform parent, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = pivot;
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        return rt;
    }

    private static RectTransform Stretch(string name, Transform parent) =>
        Rect(name, parent, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

    private static TextMeshProUGUI Text(Transform parent, string name, string text, TMP_FontAsset font, float size, Color color,
                                       Vector2 anchor, Vector2 box, float spacing)
    {
        RectTransform rt = Rect(name, parent, anchor, anchor, new Vector2(0.5f, 0.5f), Vector2.zero, box);
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

    private static void Button(Transform parent, string name, string label, TMP_FontAsset font, Vector2 anchor)
    {
        RectTransform rt = Rect(name, parent, anchor, anchor, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(360, 64));
        var img = rt.gameObject.AddComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 1f);
        var btn = rt.gameObject.AddComponent<Button>();
        ColorBlock cb = btn.colors;                     // 평소엔 거의 투명, 마우스를 올리면 금빛 띠
        cb.normalColor = new Color(0f, 0f, 0f, 0.25f);
        cb.highlightedColor = new Color(0.95f, 0.8f, 0.45f, 0.35f);
        cb.pressedColor = new Color(0.95f, 0.8f, 0.45f, 0.6f);
        cb.selectedColor = cb.normalColor;
        cb.fadeDuration = 0.15f;
        btn.colors = cb;
        Text(rt, "Label", label, font, 36, Paper, new Vector2(0.5f, 0.5f), new Vector2(360, 64), 8f);
    }
}
