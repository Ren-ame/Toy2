using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Tools/UI/Add Interact Prompt : (Arena를 연 상태에서) 화면 아래 가운데 "[F] 대화" 안내 UI를 만들고 MerchantNpc._prompt에 연결
// - 반투명 검은 띠(Image) + 금색 글자(TMP, Nanum Myeongjo) — 일시정지 메뉴 글자색과 같은 톤
// - 처음엔 꺼진 상태 (MerchantNpc가 범위에 들어오면 켬)
// - 이미 있으면 새로 만들지 않고 연결만 다시 함
public static class InteractPromptSetup
{
    private const string PromptName = "InteractPrompt";
    private const string PromptText = "[F] 대화";
    private const string FontPath = "Assets/_Project/Fonts/NanumMyeongjo/NanumMyeongjo-Regular SDF.asset";
    private static readonly Color TextColor = new Color(0.95f, 0.86f, 0.62f, 1f);   // 일시정지 메뉴 라벨과 같은 금색
    private static readonly Color BackColor = new Color(0f, 0f, 0f, 0.55f);

    [MenuItem("Tools/UI/Add Interact Prompt")]
    private static void Run()
    {
        GameObject canvas = GameObject.Find("UI Canvas");
        if (canvas == null)
        {
            Debug.LogError("[InteractPrompt] 'UI Canvas'를 찾지 못했습니다. Arena 씬을 열고 실행하세요.");
            return;
        }

        Transform existing = canvas.transform.Find(PromptName);
        GameObject prompt = existing != null ? existing.gameObject : Create(canvas.transform);

        // MerchantNpc._prompt 연결
        MerchantNpc npc = Object.FindFirstObjectByType<MerchantNpc>(FindObjectsInactive.Include);
        if (npc != null)
        {
            var so = new SerializedObject(npc);
            so.FindProperty("_prompt").objectReferenceValue = prompt;
            so.ApplyModifiedProperties();
        }
        else
        {
            Debug.LogWarning("[InteractPrompt] 씬에 MerchantNpc가 없습니다. _prompt 칸은 직접 연결하세요.");
        }

        EditorSceneManager.MarkSceneDirty(canvas.scene);
        Selection.activeGameObject = prompt;
        Debug.Log("[InteractPrompt] 완료 — 씬을 저장(Ctrl+S)하세요.");
    }

    private static GameObject Create(Transform canvas)
    {
        // 배경 띠 (화면 아래 가운데)
        var root = new GameObject(PromptName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        Undo.RegisterCreatedObjectUndo(root, "Add Interact Prompt");
        root.transform.SetParent(canvas, false);
        root.transform.SetSiblingIndex(0);   // 다른 UI(일시정지 메뉴 등)보다 아래에 그려지도록

        var rect = (RectTransform)root.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, 180f);
        rect.sizeDelta = new Vector2(300f, 64f);

        var back = root.GetComponent<Image>();
        back.color = BackColor;
        back.raycastTarget = false;   // 마우스 클릭을 막지 않게

        // 글자
        var label = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        label.transform.SetParent(root.transform, false);
        var labelRect = (RectTransform)label.transform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;

        var text = label.GetComponent<TextMeshProUGUI>();
        text.text = PromptText;
        text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        text.fontSize = 32f;
        text.color = TextColor;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;

        root.SetActive(false);
        return root;
    }
}
