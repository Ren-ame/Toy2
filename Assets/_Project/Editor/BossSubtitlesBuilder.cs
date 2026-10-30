using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Tools/UI/Add Boss Subtitles : (Arena를 연 상태에서) 보스 전투 자막 UI(SubtitleGroup) 생성 + BossSubtitles 필드 연결
// - 위치: 화면 아래 가운데, 보스 이름·HP 바 바로 위 (아래에서 160)
// - 구성: 은은한 어두운 띠(UI_SubtitleBand) + 나눔명조 Bold 34, 종이색 한 줄
// - 처음엔 숨김(CanvasGroup alpha 0). 형제 순서는 HUD 쪽(NameCardGroup 바로 앞)
// - 같은 이름(SubtitleGroup)이 있으면 지우고 다시 만든다
// - BossSubtitles 스크립트가 있으면 SubtitleGroup에 붙이고 필드(참조·대사)를 채운다.
//   스크립트나 필드가 아직 없으면 경고만 하고 건너뜀 → 스크립트 작성 후 툴을 다시 실행
// - 조우 자막은 BossStateMachine.OnPlayerDetected를 BossSubtitles가 구독 (_boss 연결)
// - (예전 방식) IntroSequence에 _subtitles 필드가 있으면 연결, 없으면 건너뜀
public static class BossSubtitlesBuilder
{
    private const string GroupName = "SubtitleGroup";
    private const string BandPath = "Assets/_Project/UI/Subtitle/UI_SubtitleBand.png";
    private const string Undo_ = "Add Boss Subtitles";

    private static readonly Color Paper = new Color(0.92f, 0.89f, 0.83f);

    // 알데리크 대사 (빛 · 문 · 맹세)
    private const string BattleStartLine = "이 문은… 지나갈 수 없다.";
    private const string HalfHpLine      = "왕이시여… 문은 아직 닫혀 있습니다.";
    private const string GroggyLine      = "맹세가… 무겁구나.";
    private const string BossDeathLine   = "빛이… 꺼지는군. 이제야… 쉴 수 있겠어.";
    private const string PlayerDeathLine = "물러가라. 서약은 끝나지 않는다.";

    [MenuItem("Tools/UI/Add Boss Subtitles")]
    private static void Run()
    {
        Scene scene = SceneManager.GetActiveScene();
        Transform canvas = FindDeep(scene, "UI Canvas");
        if (canvas == null) { Debug.LogError("[Subtitles] 'UI Canvas'가 없습니다. Arena 씬을 열고 실행하세요."); return; }

        // 0) 띠 스프라이트 임포트 설정
        var importer = AssetImporter.GetAtPath(BandPath) as TextureImporter;
        if (importer == null) { Debug.LogError($"[Subtitles] 띠 이미지가 없습니다: {BandPath}"); return; }
        if (importer.textureType != TextureImporterType.Sprite || importer.alphaIsTransparency == false)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }
        Sprite band = AssetDatabase.LoadAssetAtPath<Sprite>(BandPath);

        // 1) 기존 것 지우기
        Transform old = FindDeep(scene, GroupName);
        if (old != null) Undo.DestroyObjectImmediate(old.gameObject);

        // 2) SubtitleGroup (화면 아래 가운데, 숨김)
        var group = new GameObject(GroupName, typeof(RectTransform), typeof(CanvasGroup));
        Undo.RegisterCreatedObjectUndo(group, Undo_);
        group.transform.SetParent(canvas, false);
        Transform nameCard = canvas.Find("NameCardGroup");
        if (nameCard != null) group.transform.SetSiblingIndex(nameCard.GetSiblingIndex());   // HUD 쪽, 이름 카드보다 아래

        var rect = (RectTransform)group.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, 160f);
        rect.sizeDelta = new Vector2(1100f, 64f);

        var canvasGroup = group.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        // 3) 어두운 띠 (가장자리가 흐려지는 이미지라 그대로 늘려도 자연스러움)
        var bandGo = new GameObject("Band", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bandGo.transform.SetParent(group.transform, false);
        var bandRect = (RectTransform)bandGo.transform;
        bandRect.anchorMin = Vector2.zero;
        bandRect.anchorMax = Vector2.one;
        bandRect.offsetMin = new Vector2(0f, -10f);
        bandRect.offsetMax = new Vector2(0f, 10f);
        var bandImage = bandGo.GetComponent<Image>();
        bandImage.sprite = band;
        bandImage.color = Color.white;
        bandImage.raycastTarget = false;

        // 4) 대사 한 줄
        var textGo = new GameObject("Line", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(group.transform, false);
        var textRect = (RectTransform)textGo.transform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = textRect.offsetMax = Vector2.zero;
        var text = textGo.GetComponent<TextMeshProUGUI>();
        text.font = TitleSceneBuilder.KoreanFont(true);
        text.fontSize = 34f;
        text.color = Paper;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        text.text = HalfHpLine;   // 에디터에서 모양 확인용 (실행하면 코드가 바꿈)

        // 5) BossSubtitles 연결 (스크립트가 있을 때만)
        WireBossSubtitles(scene, group, canvasGroup, text);

        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = group;
        Debug.Log("[Subtitles] SubtitleGroup 생성 완료 — 씬을 저장(Ctrl+S)하세요. (모양 확인: CanvasGroup Alpha를 잠깐 1로)");
    }

    private static void WireBossSubtitles(Scene scene, GameObject group, CanvasGroup canvasGroup, TMP_Text text)
    {
        System.Type type = System.Type.GetType("BossSubtitles, Assembly-CSharp");
        if (type == null)
        {
            Debug.LogWarning("[Subtitles] BossSubtitles 스크립트가 아직 없습니다. 작성 후 툴을 다시 실행하면 연결됩니다.");
            return;
        }

        Component comp = group.AddComponent(type);
        var so = new SerializedObject(comp);

        Transform boss = FindDeep(scene, "Boss");
        Transform player = FindDeep(scene, "Player");

        Set(so, "_group", canvasGroup);
        Set(so, "_text", text);
        Set(so, "_bossHealth", boss != null ? boss.GetComponent<HealthComponent>() : null);
        Set(so, "_bossPoise", boss != null ? boss.GetComponent<PoiseComponent>() : null);
        Set(so, "_playerHealth", player != null ? player.GetComponent<HealthComponent>() : null);
        Set(so, "_boss", boss != null ? boss.GetComponent<BossStateMachine>() : null);   // 조우(발견) 자막용

        SetString(so, "_battleStartLine", BattleStartLine);
        SetString(so, "_halfHpLine", HalfHpLine);
        SetString(so, "_groggyLine", GroggyLine);
        SetString(so, "_bossDeathLine", BossDeathLine);
        SetString(so, "_playerDeathLine", PlayerDeathLine);
        so.ApplyModifiedProperties();

        // IntroSequence._subtitles (전투 시작 자막을 StartBattle에서 부르는 경우)
        System.Type introType = System.Type.GetType("IntroSequence, Assembly-CSharp");
        if (introType != null)
        {
            Object intro = Object.FindFirstObjectByType(introType, FindObjectsInactive.Include);
            if (intro != null)
            {
                var introSo = new SerializedObject(intro);
                Set(introSo, "_subtitles", comp);
                introSo.ApplyModifiedProperties();
            }
        }
    }

    private static void Set(SerializedObject so, string field, Object value)
    {
        SerializedProperty p = so.FindProperty(field);
        if (p == null) { Debug.LogWarning($"[Subtitles] {so.targetObject.GetType().Name}에 {field} 필드가 없습니다. 건너뜀"); return; }
        if (value == null) Debug.LogWarning($"[Subtitles] {field}에 넣을 대상을 찾지 못했습니다.");
        p.objectReferenceValue = value;
    }

    private static void SetString(SerializedObject so, string field, string value)
    {
        SerializedProperty p = so.FindProperty(field);
        if (p == null) { Debug.LogWarning($"[Subtitles] {so.targetObject.GetType().Name}에 {field} 필드가 없습니다. 건너뜀"); return; }
        p.stringValue = value;
    }

    private static Transform FindDeep(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
        return null;
    }
}
