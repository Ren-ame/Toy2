using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Tools/UI/Add Title Reset Button : (Title 씬을 연 상태에서) 오른쪽 아래 [기록 초기화] 버튼 + 확인 창 생성, TitleMenu 필드 연결
// - 버튼·글자 스타일은 타이틀의 [종료] 버튼과 부제목에서 복제
// - 버튼은 TitleGroup 안에 둠 → 시작을 눌러 타이틀이 사라질 때 같이 사라짐
// - 확인 창은 처음엔 숨김(CanvasGroup alpha 0), TitleMenu가 열고 닫음
// - 이미 ResetButton이 있으면 아무것도 하지 않음
public static class TitleResetSetup
{
    private const string Undo_ = "Add Title Reset Button";
    private const string Message = "강화와 재화가 모두 사라집니다.\n기록을 초기화할까요?";

    [MenuItem("Tools/UI/Add Title Reset Button")]
    private static void Run()
    {
        GameObject canvas = GameObject.Find("Title Canvas");
        if (canvas == null) { Debug.LogError("[TitleReset] 'Title Canvas'가 없습니다. Title 씬을 열고 실행하세요."); return; }

        Transform titleGroup = canvas.transform.Find("TitleGroup");
        Transform srcButton = titleGroup != null ? titleGroup.Find("QuitButton") : null;
        Transform srcText = titleGroup != null ? titleGroup.Find("Subtitle") : null;
        if (srcButton == null || srcText == null)
        {
            Debug.LogError("[TitleReset] 복제할 TitleGroup/QuitButton 또는 TitleGroup/Subtitle을 찾지 못했습니다.");
            return;
        }
        if (titleGroup.Find("ResetButton") != null)
        {
            Debug.Log("[TitleReset] 이미 있습니다. 다시 만들려면 ResetButton과 ResetConfirm을 지우고 실행하세요.");
            return;
        }

        // 1) 오른쪽 아래 구석의 작은 [기록 초기화] 버튼
        Button reset = CloneButton(srcButton, titleGroup, "ResetButton", "기록 초기화", 26f);
        var resetRect = (RectTransform)reset.transform;
        resetRect.anchorMin = resetRect.anchorMax = resetRect.pivot = new Vector2(1f, 0f);
        resetRect.anchoredPosition = new Vector2(-40f, 40f);
        resetRect.sizeDelta = new Vector2(220f, 52f);

        // 2) 확인 창 (화면 전체 + 숨김)
        var confirm = new GameObject("ResetConfirm", typeof(RectTransform), typeof(CanvasGroup));
        Undo.RegisterCreatedObjectUndo(confirm, Undo_);
        confirm.transform.SetParent(canvas.transform, false);
        confirm.transform.SetAsLastSibling();
        Stretch((RectTransform)confirm.transform);
        var group = confirm.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        var dim = NewImage("Dim", confirm.transform, new Color(0f, 0f, 0f, 0.6f));
        Stretch((RectTransform)dim.transform);

        var panel = NewImage("Panel", confirm.transform, new Color(0.08f, 0.07f, 0.06f, 0.92f));
        var panelRect = (RectTransform)panel.transform;
        panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(680f, 320f);

        GameObject message = Object.Instantiate(srcText.gameObject, panel.transform);
        message.name = "Message";
        var messageRect = (RectTransform)message.transform;
        messageRect.anchorMin = messageRect.anchorMax = messageRect.pivot = new Vector2(0.5f, 0.5f);
        messageRect.anchoredPosition = new Vector2(0f, 50f);
        messageRect.sizeDelta = new Vector2(620f, 140f);
        var messageText = message.GetComponent<TMP_Text>();
        messageText.text = Message;
        messageText.fontSize = 32f;
        messageText.alignment = TextAlignmentOptions.Center;

        Button yes = CloneButton(srcButton, panel.transform, "YesButton", "초기화", 32f);
        Place(yes, new Vector2(-140f, -90f), new Vector2(220f, 60f));
        Button no = CloneButton(srcButton, panel.transform, "NoButton", "취소", 32f);
        Place(no, new Vector2(140f, -90f), new Vector2(220f, 60f));

        // 3) TitleMenu 필드 연결
        TitleMenu menu = Object.FindFirstObjectByType<TitleMenu>(FindObjectsInactive.Include);
        if (menu != null)
        {
            var so = new SerializedObject(menu);
            so.FindProperty("_resetButton").objectReferenceValue = reset;
            so.FindProperty("_confirmGroup").objectReferenceValue = group;
            so.FindProperty("_confirmYesButton").objectReferenceValue = yes;
            so.FindProperty("_confirmNoButton").objectReferenceValue = no;
            so.ApplyModifiedProperties();
        }
        else Debug.LogWarning("[TitleReset] TitleMenu를 찾지 못했습니다. 칸 4개는 직접 연결하세요.");

        EditorSceneManager.MarkSceneDirty(canvas.scene);
        Selection.activeGameObject = reset.gameObject;
        Debug.Log("[TitleReset] 완료 — 씬을 저장(Ctrl+S)하세요.");
    }

    private static Button CloneButton(Transform source, Transform parent, string name, string label, float fontSize)
    {
        GameObject go = Object.Instantiate(source.gameObject, parent);
        go.name = name;
        Undo.RegisterCreatedObjectUndo(go, Undo_);

        Button button = go.GetComponent<Button>();
        button.onClick = new Button.ButtonClickedEvent();   // 클릭 연결은 TitleMenu.Start가 코드로 함

        TMP_Text text = go.GetComponentInChildren<TMP_Text>(true);
        if (text != null)
        {
            text.text = label;
            text.fontSize = fontSize;
            if (text.transform != go.transform) Stretch((RectTransform)text.transform);
        }
        return button;
    }

    private static void Place(Button button, Vector2 pos, Vector2 size)
    {
        var rect = (RectTransform)button.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;
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
}
