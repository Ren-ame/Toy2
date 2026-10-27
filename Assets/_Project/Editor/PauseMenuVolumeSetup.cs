using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

// Tools/UI/Add Volume Sliders : (Arena를 연 상태에서) 일시정지 메뉴에 배경음·효과음 볼륨 슬라이더 2줄 추가 + PauseMenu 필드 연결
// - 상하 감도 줄(SensitivityY_Label/Value/Slider)을 복제해서 아래에 붙인다
// - 패널을 두 줄만큼 키우고, 제목·감도는 위로, 버튼은 아래로 민다
// - 이미 BGM_Slider가 있으면 아무것도 하지 않음 (여러 번 눌러도 안전)
public static class PauseMenuVolumeSetup
{
    private const string MixerPath = "Assets/_Project/Audio/MainMixer.mixer";
    private const float RowGap = 100f;        // 감도 한 줄(라벨 + 슬라이더)의 높이
    private const float DefaultVolume = 80f;  // VolumeSettings.DefaultVolume과 같은 값

    [MenuItem("Tools/UI/Add Volume Sliders")]
    private static void Run()
    {
        PauseMenu menu = Object.FindFirstObjectByType<PauseMenu>(FindObjectsInactive.Include);
        if (menu == null)
        {
            Debug.LogError("[VolumeSetup] 씬에 PauseMenu가 없습니다. Arena 씬을 열고 실행하세요.");
            return;
        }

        RectTransform panel = menu.transform.Find("Panel") as RectTransform;
        if (panel == null)
        {
            Debug.LogError("[VolumeSetup] PauseGroup 아래에 Panel이 없습니다.");
            return;
        }

        if (panel.Find("BGM_Slider") != null)
        {
            Debug.Log("[VolumeSetup] 볼륨 슬라이더가 이미 있습니다. 건너뜁니다.");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(panel.gameObject, "Add Volume Sliders");

        // 1) 패널을 두 줄만큼 키우고, 버튼은 아래로 / 나머지(제목·감도)는 위로 밀기
        panel.sizeDelta += new Vector2(0f, RowGap * 2f);
        foreach (RectTransform child in panel)
        {
            bool isButton = child.GetComponent<Button>() != null;
            child.anchoredPosition += new Vector2(0f, isButton ? -RowGap : RowGap);
        }

        // 2) 상하 감도 줄을 복제해 배경음·효과음 줄 만들기 (감도 Y 바로 아래 순서로)
        int siblingIndex = panel.Find("SensitivityY_Slider").GetSiblingIndex() + 1;
        (Slider bgmSlider, TMP_Text bgmText) = CloneRow(panel, "BGM", "배경음", -RowGap, ref siblingIndex);
        (Slider sfxSlider, TMP_Text sfxText) = CloneRow(panel, "SFX", "효과음", -RowGap * 2f, ref siblingIndex);

        // 3) PauseMenu 필드 연결
        var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
        if (mixer == null)
            Debug.LogWarning($"[VolumeSetup] 믹서를 찾지 못했습니다: {MixerPath} — _mixer 칸은 직접 넣어 주세요.");

        var so = new SerializedObject(menu);
        so.FindProperty("_mixer").objectReferenceValue = mixer;
        so.FindProperty("_bgmSlider").objectReferenceValue = bgmSlider;
        so.FindProperty("_sfxSlider").objectReferenceValue = sfxSlider;
        so.FindProperty("_bgmValueText").objectReferenceValue = bgmText;
        so.FindProperty("_sfxValueText").objectReferenceValue = sfxText;
        so.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(menu.gameObject.scene);
        Debug.Log("[VolumeSetup] 볼륨 슬라이더 추가 완료 — 씬을 저장(Ctrl+S)하세요.");
    }

    // 감도 Y 줄(라벨·값·슬라이더)을 복제해 yOffset만큼 아래에 놓고, 볼륨용(0~100 정수)으로 바꾼다
    private static (Slider, TMP_Text) CloneRow(RectTransform panel, string prefix, string labelText, float yOffset, ref int siblingIndex)
    {
        RectTransform label = Clone(panel.Find("SensitivityY_Label"), panel, prefix + "_Label", yOffset, ref siblingIndex);
        RectTransform value = Clone(panel.Find("SensitivityY_Value"), panel, prefix + "_Value", yOffset, ref siblingIndex);
        RectTransform sliderRect = Clone(panel.Find("SensitivityY_Slider"), panel, prefix + "_Slider", yOffset, ref siblingIndex);

        label.GetComponent<TMP_Text>().text = labelText;

        TMP_Text valueText = value.GetComponent<TMP_Text>();
        valueText.text = DefaultVolume.ToString("0");

        Slider slider = sliderRect.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 100f;
        slider.wholeNumbers = true;
        slider.value = DefaultVolume;

        return (slider, valueText);
    }

    private static RectTransform Clone(Transform source, RectTransform parent, string name, float yOffset, ref int siblingIndex)
    {
        GameObject go = Object.Instantiate(source.gameObject, parent);
        go.name = name;
        Undo.RegisterCreatedObjectUndo(go, "Add Volume Sliders");

        RectTransform rect = (RectTransform)go.transform;
        rect.anchoredPosition = ((RectTransform)source).anchoredPosition + new Vector2(0f, yOffset);
        rect.SetSiblingIndex(siblingIndex++);
        return rect;
    }
}
