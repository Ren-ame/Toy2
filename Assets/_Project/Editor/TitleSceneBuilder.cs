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

    // ---------------------------------------------------------------- 조작 안내 (Arena)
    // 화면 오른쪽 아래 반투명 박스. 코드 없이 항상 표시 (보스 HP 바와 안 겹치는 위치)
    private static readonly string[] Controls =
    {
        "WASD|이동", "마우스|시점", "좌클릭|공격  (길게: 강공격)", "우클릭|패링  (길게: 가드)",
        "Shift|구르기", "Tab|락온", "R|다시 도전 (결과 화면)",
    };

    [MenuItem("Tools/UI/Add Controls Guide")]
    private static void AddControlsGuide()
    {
        Scene scene = SceneManager.GetActiveScene();
        Transform canvas = FindDeep(scene, "UI Canvas");
        if (canvas == null) { Debug.LogError("[Title] UI Canvas를 찾지 못함 — Arena 씬을 열고 실행하세요"); return; }

        Transform old = canvas.Find("ControlsGuide");
        if (old != null) Undo.DestroyObjectImmediate(old.gameObject);

        RectTransform box = Rect("ControlsGuide", canvas, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0),
                                 new Vector2(-30, 30), new Vector2(380, 40 + Controls.Length * 34));
        Undo.RegisterCreatedObjectUndo(box.gameObject, "Controls Guide");
        var bg = box.gameObject.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.35f);
        bg.raycastTarget = false;

        // 키 이름은 금색, 설명은 흰색 (TMP 리치 텍스트)
        var sb = new System.Text.StringBuilder();
        foreach (string c in Controls)
        {
            string[] kv = c.Split('|');
            sb.AppendLine($"<color=#F2DB9E><b>{kv[0]}</b></color><pos=32%>{kv[1]}");
        }
        TextMeshProUGUI tmp = Text(box, "Text", sb.ToString().TrimEnd(), KoreanFont(false), 22, Paper,
                                   new Vector2(0.5f, 0.5f), new Vector2(340, Controls.Length * 34), 0f);
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.lineSpacing = 18f;
        tmp.richText = true;

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("[Title] 조작 안내 추가 완료");
    }

    // ---------------------------------------------------------------- 조작법 화면 (Arena)
    // Arena 시작 시 조작법 화면(키캡·마우스 그림) → Space로 닫고 전투 시작
    // 처음엔 숨김(alpha 0). 보여 주고 숨기고 게임을 멈추는 건 런타임 스크립트가 담당
    private const string UiDir = "Assets/_Project/UI/Controls/";
    private static readonly Color KeyText = new Color(0.16f, 0.14f, 0.12f);

    [MenuItem("Tools/UI/Add Controls Screen")]
    private static void AddControlsScreen()
    {
        Scene scene = SceneManager.GetActiveScene();
        Transform canvas = FindDeep(scene, "UI Canvas");
        if (canvas == null) { Debug.LogError("[Title] UI Canvas를 찾지 못함 — Arena 씬을 열고 실행하세요"); return; }

        Sprite key = UiSprite("UI_Keycap.png", new Vector4(24, 30, 24, 24));
        Sprite card = UiSprite("UI_Card.png", new Vector4(44, 44, 44, 44));
        Sprite frame = UiSprite("UI_ButtonFrame.png", new Vector4(24, 24, 24, 24));
        Sprite mouse = UiSprite("UI_Mouse.png", Vector4.zero);
        TMP_FontAsset kr = KoreanFont(false), krBold = KoreanFont(true);

        Transform old = canvas.Find("ControlsGroup");
        if (old != null) Undo.DestroyObjectImmediate(old.gameObject);

        RectTransform root = Stretch("ControlsGroup", canvas);
        Undo.RegisterCreatedObjectUndo(root.gameObject, "Controls Screen");
        root.SetAsLastSibling();   // HUD·결과 화면보다 위에 그려지게
        var group = root.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f; group.interactable = false; group.blocksRaycasts = false;
        var dim = root.gameObject.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.88f);

        Text(root, "Header", "조작법", krBold, 56, Paper, new Vector2(0.5f, 0.9f), new Vector2(600, 80), 12f);

        // ---- 왼쪽 카드: 이동·회피
        RectTransform left = Card(root, "MoveCard", card, new Vector2(0.3f, 0.52f), new Vector2(660, 600));
        Label(left, "이동", krBold);
        Key(left, key, krBold, "W", new Vector2(0, 150), 96);
        Key(left, key, krBold, "A", new Vector2(-110, 40), 96);
        Key(left, key, krBold, "S", new Vector2(0, 40), 96);
        Key(left, key, krBold, "D", new Vector2(110, 40), 96);
        Arrow(left, kr, "▲", new Vector2(0, 220)); Arrow(left, kr, "▼", new Vector2(0, -24));
        Arrow(left, kr, "◀", new Vector2(-180, 46)); Arrow(left, kr, "▶", new Vector2(180, 46));
        Label(left, "회피", krBold, -88);
        Key(left, key, krBold, "Shift", new Vector2(-110, -150), 200, 84, 30);
        Caption(left, kr, "구르기", new Vector2(-110, -215));
        Key(left, key, krBold, "Tab", new Vector2(130, -150), 140, 84, 30);
        Caption(left, kr, "락온", new Vector2(130, -215));

        // ---- 오른쪽 카드: 전투(마우스)
        RectTransform right = Card(root, "CombatCard", card, new Vector2(0.7f, 0.52f), new Vector2(660, 600));
        Label(right, "전투", krBold);
        RectTransform m = Rect("Mouse", right, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                               new Vector2(0, 20), new Vector2(170, 255));
        var mi = m.gameObject.AddComponent<Image>(); mi.sprite = mouse; mi.preserveAspect = true; mi.raycastTarget = false;
        Desc(right, krBold, kr, "좌클릭", "공격\n<size=80%>길게 누르면 강공격</size>", new Vector2(-200, 90), TextAlignmentOptions.Right);
        Desc(right, krBold, kr, "우클릭", "패링\n<size=80%>길게 누르면 가드</size>", new Vector2(200, 90), TextAlignmentOptions.Left);
        // 공략 팁: 패링 신호 → 체간 → 그로기 중 강공격 치명타 (핵심 단어만 금색)
        const string g = "<color=#F2DB9E>", e = "</color>";
        string tips = $"{g}눈이 빛나는 공격{e}은 패링할 수 있다\n" +
                      $"패링·공격으로 {g}체간{e}을 채우면 보스가 무릎을 꿇는다\n" +
                      $"무릎 꿇은 보스에게 {g}강공격{e}은 치명타";
        TextMeshProUGUI tip = Text(right, "Tips", tips, kr, 23, Paper, new Vector2(0.5f, 0.5f), new Vector2(620, 140), 0f);
        tip.richText = true;
        tip.lineSpacing = 20f;
        ((RectTransform)tip.transform).anchoredPosition = new Vector2(0, -205);

        // ---- 시작 안내 (버튼 모양이지만 클릭이 아니라 Space로 닫음 — 전투 중 커서가 잠겨 있으므로)
        RectTransform btn = Rect("StartHint", root, new Vector2(0.5f, 0.1f), new Vector2(0.5f, 0.1f), new Vector2(0.5f, 0.5f),
                                 Vector2.zero, new Vector2(420, 80));
        var bi = btn.gameObject.AddComponent<Image>(); bi.sprite = frame; bi.type = Image.Type.Sliced; bi.raycastTarget = false;
        Text(btn, "Label", "Space  전투 시작", krBold, 34, KeyText, new Vector2(0.5f, 0.5f), new Vector2(420, 80), 6f);

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("[Title] 조작법 화면 추가 완료 (ControlsGroup, 처음엔 숨김)");
    }

    private static Sprite UiSprite(string file, Vector4 border)
    {
        string path = UiDir + file;
        if (AssetImporter.GetAtPath(path) is TextureImporter ti)
        {
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spriteBorder = border;                  // 9-slice: 모서리는 그대로, 가운데만 늘어남
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = false;
            ti.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static RectTransform Card(Transform parent, string name, Sprite sprite, Vector2 anchor, Vector2 size)
    {
        RectTransform rt = Rect(name, parent, anchor, anchor, new Vector2(0.5f, 0.5f), Vector2.zero, size);
        var img = rt.gameObject.AddComponent<Image>(); img.sprite = sprite; img.type = Image.Type.Sliced; img.raycastTarget = false;
        return rt;
    }

    private static void Label(RectTransform card, string text, TMP_FontAsset font, float y = 230f)
    {
        TextMeshProUGUI t = Text(card, "Label_" + text, text, font, 40, Gold, new Vector2(0.5f, 0.5f), new Vector2(200, 60), 4f);
        t.alignment = TextAlignmentOptions.Left;
        ((RectTransform)t.transform).anchoredPosition = new Vector2(-210, y);
    }

    private static void Key(RectTransform parent, Sprite sprite, TMP_FontAsset font, string label, Vector2 pos,
                            float w, float h = 96f, float fontSize = 40f)
    {
        RectTransform k = Rect("Key_" + label, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(w, h));
        var img = k.gameObject.AddComponent<Image>(); img.sprite = sprite; img.type = Image.Type.Sliced; img.raycastTarget = false;
        TextMeshProUGUI t = Text(k, "Label", label, font, fontSize, KeyText, new Vector2(0.5f, 0.5f), new Vector2(w, h), 0f);
        ((RectTransform)t.transform).anchoredPosition = new Vector2(0, 4);   // 윗면 중앙 (아래 그림자 10px 보정)
    }

    private static void Arrow(RectTransform parent, TMP_FontAsset font, string glyph, Vector2 pos)
    {
        TextMeshProUGUI t = Text(parent, "Arrow", glyph, font, 22, Gold, new Vector2(0.5f, 0.5f), new Vector2(40, 40), 0f);
        ((RectTransform)t.transform).anchoredPosition = pos;
    }

    private static void Caption(RectTransform parent, TMP_FontAsset font, string text, Vector2 pos)
    {
        TextMeshProUGUI t = Text(parent, "Caption", text, font, 24, Paper, new Vector2(0.5f, 0.5f), new Vector2(240, 36), 2f);
        ((RectTransform)t.transform).anchoredPosition = pos;
    }

    private static void Desc(RectTransform parent, TMP_FontAsset bold, TMP_FontAsset font, string title, string body, Vector2 pos,
                             TextAlignmentOptions align)
    {
        TextMeshProUGUI h = Text(parent, "Desc_" + title, title, bold, 30, Gold, new Vector2(0.5f, 0.5f), new Vector2(200, 40), 2f);
        h.alignment = align;
        ((RectTransform)h.transform).anchoredPosition = pos;
        TextMeshProUGUI d = Text(parent, "Body_" + title, body, font, 28, Paper, new Vector2(0.5f, 0.5f), new Vector2(200, 90), 0f);
        d.alignment = align;
        d.richText = true;
        ((RectTransform)d.transform).anchoredPosition = pos + new Vector2(0, -62);
    }

    // ---------------------------------------------------------------- 일시정지 메뉴 (Arena)
    // Esc → 일시정지 창: 마우스 감도 슬라이더 + 계속하기 / 타이틀로 / 게임 종료. 처음엔 숨김(alpha 0)
    // 열고 닫기·감도 저장/적용은 런타임 스크립트(PauseMenu)가 담당
    [MenuItem("Tools/UI/Add Pause Menu")]
    private static void AddPauseMenu()
    {
        Scene scene = SceneManager.GetActiveScene();
        Transform canvas = FindDeep(scene, "UI Canvas");
        if (canvas == null) { Debug.LogError("[Title] UI Canvas를 찾지 못함 — Arena 씬을 열고 실행하세요"); return; }

        Sprite card = UiSprite("UI_Card.png", new Vector4(44, 44, 44, 44));
        Sprite frame = UiSprite("UI_ButtonFrame.png", new Vector4(24, 24, 24, 24));
        Sprite key = UiSprite("UI_Keycap.png", new Vector4(24, 30, 24, 24));
        TMP_FontAsset kr = KoreanFont(false), krBold = KoreanFont(true);

        Transform old = canvas.Find("PauseGroup");
        if (old != null) Undo.DestroyObjectImmediate(old.gameObject);

        RectTransform root = Stretch("PauseGroup", canvas);
        Undo.RegisterCreatedObjectUndo(root.gameObject, "Pause Menu");
        root.SetAsLastSibling();
        var group = root.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f; group.interactable = false; group.blocksRaycasts = false;
        var dim = root.gameObject.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.7f);

        RectTransform panel = Card(root, "Panel", card, new Vector2(0.5f, 0.5f), new Vector2(640, 680));
        TextMeshProUGUI header = Text(panel, "Header", "일시정지", krBold, 48, Paper, new Vector2(0.5f, 0.5f), new Vector2(400, 70), 10f);
        ((RectTransform)header.transform).anchoredPosition = new Vector2(0, 270);

        // ---- 마우스 감도: 좌우(X) / 상하(Y) 각각 라벨 + 현재 값 + 슬라이더
        SensitivityRow(panel, key, kr, krBold, "X", "좌우 감도", 190);
        SensitivityRow(panel, key, kr, krBold, "Y", "상하 감도", 90);

        // ---- 버튼 3개
        MenuButton(panel, frame, krBold, "ResumeButton", "계속하기", new Vector2(0, -50));
        MenuButton(panel, frame, krBold, "TitleButton", "타이틀로", new Vector2(0, -140));
        MenuButton(panel, frame, krBold, "QuitButton", "게임 종료", new Vector2(0, -230));

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("[Title] 일시정지 메뉴 추가 완료 (PauseGroup, 처음엔 숨김)");
    }

    private static void SensitivityRow(RectTransform panel, Sprite handle, TMP_FontAsset font, TMP_FontAsset bold,
                                       string axis, string title, float y)
    {
        TextMeshProUGUI label = Text(panel, $"Sensitivity{axis}_Label", title, font, 30, Gold, new Vector2(0.5f, 0.5f), new Vector2(240, 44), 2f);
        label.alignment = TextAlignmentOptions.Left;
        ((RectTransform)label.transform).anchoredPosition = new Vector2(-130, y);
        TextMeshProUGUI value = Text(panel, $"Sensitivity{axis}_Value", "1.0", bold, 30, Paper, new Vector2(0.5f, 0.5f), new Vector2(120, 44), 0f);
        value.alignment = TextAlignmentOptions.Right;
        ((RectTransform)value.transform).anchoredPosition = new Vector2(190, y);
        BuildSlider(panel, handle, $"Sensitivity{axis}_Slider", new Vector2(0, y - 50), new Vector2(500, 28));
    }

    private static void BuildSlider(RectTransform parent, Sprite handleSprite, string name, Vector2 pos, Vector2 size)
    {
        RectTransform rt = Rect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);
        var slider = rt.gameObject.AddComponent<Slider>();

        RectTransform bg = Rect("Background", rt, new Vector2(0, 0.25f), new Vector2(1, 0.75f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        bg.gameObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.15f);

        RectTransform fillArea = Rect("Fill Area", rt, new Vector2(0, 0.25f), new Vector2(1, 0.75f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        RectTransform fill = Rect("Fill", fillArea, Vector2.zero, new Vector2(0, 1), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        fill.gameObject.AddComponent<Image>().color = Gold;

        RectTransform handleArea = Rect("Handle Slide Area", rt, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-20, 0));
        RectTransform handle = Rect("Handle", handleArea, Vector2.zero, new Vector2(0, 1), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(24, 16));
        var hImg = handle.gameObject.AddComponent<Image>(); hImg.sprite = handleSprite; hImg.type = Image.Type.Sliced;

        slider.fillRect = fill;
        slider.handleRect = handle;
        slider.targetGraphic = hImg;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0.2f;     // 감도 배율 (기본 Gain × 이 값)
        slider.maxValue = 3f;
        slider.value = 1f;
    }

    private static void MenuButton(RectTransform parent, Sprite frame, TMP_FontAsset font, string name, string label, Vector2 pos)
    {
        RectTransform rt = Rect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(360, 72));
        var img = rt.gameObject.AddComponent<Image>(); img.sprite = frame; img.type = Image.Type.Sliced;
        var b = rt.gameObject.AddComponent<Button>();
        ColorBlock cb = b.colors;
        cb.highlightedColor = new Color(1f, 0.92f, 0.75f);
        cb.pressedColor = new Color(0.85f, 0.75f, 0.55f);
        b.colors = cb;
        Text(rt, "Label", label, font, 32, KeyText, new Vector2(0.5f, 0.5f), new Vector2(360, 72), 6f);
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
