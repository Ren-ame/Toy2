using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Tools/UI/Build Upgrade Menu : (Arena를 연 상태에서) 상인 강화 창 UI를 만들고 UpgradeMenu · MerchantNpc 필드를 연결
// - 스타일은 일시정지 메뉴에서 복제 (패널 배경, 제목, 라벨, 값 글자, 버튼) → 같은 톤
// - 구성: 제목 / 보유 재화 / 강화 4줄(이름 · 현재→다음 · 비용 · [강화]) / [초기화 (+환불액)] / [닫기]
// - 처음엔 숨김 상태(CanvasGroup alpha 0). 에디터에서 모양을 보려면 UpgradeMenu의 CanvasGroup Alpha를 잠깐 1로
// - 이미 UpgradeMenu가 있으면 아무것도 하지 않음 (다시 만들려면 지우고 실행)
public static class UpgradeMenuBuilder
{
    private const string RootName = "UpgradeMenu";
    private const string Undo_ = "Build Upgrade Menu";

    // UpgradeMenu의 Row 필드 이름, 화면에 보일 이름
    private static readonly (string field, string label)[] Rows =
    {
        ("_atkRow", "공격력"),
        ("_hpRow", "체력"),
        ("_staminaRow", "최대 스태미나"),
        ("_stRegenRow", "스태미나 회복"),
    };

    // 레이아웃 (패널 중심 기준)
    private static readonly Vector2 PanelSize = new Vector2(820f, 660f);
    private const float HeaderY = 270f;
    private const float CurrencyY = 190f;
    private const float FirstRowY = 110f;
    private const float RowGap = 70f;
    private const float RespecY = -190f;
    private const float CloseY = -270f;
    private const float NameX = -260f, ValueX = -20f, CostX = 140f, ButtonX = 290f;

    [MenuItem("Tools/UI/Build Upgrade Menu")]
    private static void Run()
    {
        GameObject canvas = GameObject.Find("UI Canvas");
        if (canvas == null) { Debug.LogError("[UpgradeMenu] 'UI Canvas'가 없습니다. Arena 씬을 열고 실행하세요."); return; }

        if (canvas.transform.Find(RootName) != null)
        {
            Debug.Log("[UpgradeMenu] 이미 있습니다. 다시 만들려면 UI Canvas/UpgradeMenu를 지우고 실행하세요.");
            return;
        }

        Transform pausePanel = canvas.transform.Find("PauseGroup/Panel");
        Transform srcHeader = pausePanel != null ? pausePanel.Find("Header") : null;
        Transform srcLabel = pausePanel != null ? pausePanel.Find("SensitivityX_Label") : null;
        Transform srcValue = pausePanel != null ? pausePanel.Find("SensitivityX_Value") : null;
        Transform srcButton = pausePanel != null ? pausePanel.Find("ResumeButton") : null;
        if (srcHeader == null || srcLabel == null || srcValue == null || srcButton == null)
        {
            Debug.LogError("[UpgradeMenu] 스타일을 복제할 일시정지 메뉴 요소(Header / SensitivityX_Label / SensitivityX_Value / ResumeButton)를 찾지 못했습니다.");
            return;
        }

        // 1) 루트: 화면 전체 + CanvasGroup(숨김) + UpgradeMenu
        var root = new GameObject(RootName, typeof(RectTransform), typeof(CanvasGroup));
        Undo.RegisterCreatedObjectUndo(root, Undo_);
        root.transform.SetParent(canvas.transform, false);
        root.transform.SetAsLastSibling();   // 다른 UI 위에 그려지도록
        Stretch((RectTransform)root.transform);

        var group = root.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
        var menu = root.AddComponent<UpgradeMenu>();

        // 2) 어두운 배경 + 패널
        var dim = NewImage("Dim", root.transform, new Color(0f, 0f, 0f, 0.6f));
        Stretch((RectTransform)dim.transform);

        var panel = NewImage("Panel", root.transform, Color.white);
        var panelRect = (RectTransform)panel.transform;
        panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = PanelSize;
        CopyImage(pausePanel.GetComponent<Image>(), panel.GetComponent<Image>());

        // 3) 제목 · 보유 재화
        CloneText(srcHeader, panel.transform, "Header", new Vector2(0f, HeaderY), "상인", null, TextAlignmentOptions.Center);
        CloneText(srcLabel, panel.transform, "CurrencyLabel", new Vector2(-130f, CurrencyY), "보유 재화", new Vector2(240f, 44f), TextAlignmentOptions.Right);
        TMP_Text currency = CloneText(srcValue, panel.transform, "CurrencyValue", new Vector2(110f, CurrencyY), "0", new Vector2(200f, 44f), TextAlignmentOptions.Left);

        var so = new SerializedObject(menu);

        // 4) 강화 4줄
        float y = FirstRowY;
        foreach (var (field, label) in Rows)
        {
            string prefix = field.TrimStart('_');
            CloneText(srcLabel, panel.transform, prefix + "_Name", new Vector2(NameX, y), label, new Vector2(240f, 44f), TextAlignmentOptions.Left);
            TMP_Text value = CloneText(srcValue, panel.transform, prefix + "_Value", new Vector2(ValueX, y), "10 → 11", new Vector2(220f, 44f), TextAlignmentOptions.Center);
            TMP_Text cost = CloneText(srcValue, panel.transform, prefix + "_Cost", new Vector2(CostX, y), "100", new Vector2(120f, 44f), TextAlignmentOptions.Center);
            Button button = CloneButton(srcButton, panel.transform, prefix + "_Button", new Vector2(ButtonX, y), new Vector2(140f, 56f), "강화", out _);

            SerializedProperty row = so.FindProperty(field);
            row.FindPropertyRelative("Value").objectReferenceValue = value;
            row.FindPropertyRelative("Cost").objectReferenceValue = cost;
            row.FindPropertyRelative("Button").objectReferenceValue = button;

            y -= RowGap;
        }

        // 5) 초기화 · 닫기
        Button respec = CloneButton(srcButton, panel.transform, "RespecButton", new Vector2(0f, RespecY), new Vector2(320f, 64f), "초기화 (+0)", out TMP_Text respecText);
        Button close = CloneButton(srcButton, panel.transform, "CloseButton", new Vector2(0f, CloseY), new Vector2(320f, 64f), "닫기", out _);

        // 6) 필드 연결
        so.FindProperty("_group").objectReferenceValue = group;
        so.FindProperty("_currencyText").objectReferenceValue = currency;
        so.FindProperty("_respecButton").objectReferenceValue = respec;
        so.FindProperty("_respecText").objectReferenceValue = respecText;
        so.FindProperty("_closeButton").objectReferenceValue = close;

        PlayerCombatTarget player = Object.FindFirstObjectByType<PlayerCombatTarget>(FindObjectsInactive.Include);
        if (player != null) so.FindProperty("_player").objectReferenceValue = player;
        else Debug.LogWarning("[UpgradeMenu] PlayerCombatTarget을 찾지 못했습니다. _player 칸은 직접 연결하세요.");
        so.ApplyModifiedProperties();

        MerchantNpc npc = Object.FindFirstObjectByType<MerchantNpc>(FindObjectsInactive.Include);
        if (npc != null)
        {
            var npcSo = new SerializedObject(npc);
            npcSo.FindProperty("_menu").objectReferenceValue = menu;
            npcSo.ApplyModifiedProperties();
        }
        else Debug.LogWarning("[UpgradeMenu] MerchantNpc를 찾지 못했습니다. _menu 칸은 직접 연결하세요.");

        EditorSceneManager.MarkSceneDirty(canvas.scene);
        Selection.activeGameObject = root;
        Debug.Log("[UpgradeMenu] 강화 창 생성 완료 — 씬을 저장(Ctrl+S)하세요.");
    }

    private static GameObject NewImage(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        return go;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private static void CopyImage(Image from, Image to)
    {
        if (from == null) { to.color = new Color(0.08f, 0.07f, 0.06f, 0.92f); return; }
        to.sprite = from.sprite;
        to.type = from.type;
        to.color = from.color;
        to.pixelsPerUnitMultiplier = from.pixelsPerUnitMultiplier;
    }

    // 일시정지 메뉴의 글자 오브젝트를 복제해 위치·크기·내용·정렬만 바꿈
    private static TMP_Text CloneText(Transform source, Transform parent, string name, Vector2 pos, string text,
                                      Vector2? size, TextAlignmentOptions align)
    {
        GameObject go = Object.Instantiate(source.gameObject, parent);
        go.name = name;
        var rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        if (size.HasValue) rect.sizeDelta = size.Value;

        TMP_Text tmp = go.GetComponent<TMP_Text>();
        tmp.text = text;
        tmp.alignment = align;
        return tmp;
    }

    // 일시정지 메뉴의 버튼을 복제해 위치·크기·글자만 바꿈 (클릭 연결은 UpgradeMenu.Start가 코드로 함)
    private static Button CloneButton(Transform source, Transform parent, string name, Vector2 pos, Vector2 size,
                                      string label, out TMP_Text labelText)
    {
        GameObject go = Object.Instantiate(source.gameObject, parent);
        go.name = name;
        var rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;

        Button button = go.GetComponent<Button>();
        button.onClick = new Button.ButtonClickedEvent();   // 복제 원본의 인스펙터 연결이 있더라도 비움

        labelText = go.GetComponentInChildren<TMP_Text>(true);
        if (labelText != null)
        {
            labelText.text = label;
            if (labelText.transform != go.transform)
                Stretch((RectTransform)labelText.transform);   // 버튼 크기가 바뀌어도 글자가 버튼 안에 맞게
        }
        return button;
    }
}
