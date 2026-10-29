using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Tools/UI/Restyle Bars (Arena) : HP·스태미나·보스 HP·체간 바를 세키로식(B안)으로
//  - 얇은 바 + 검정/청동 1px 테두리(Frame) + 뒤쪽 부드러운 그림자(Shadow)
//  - 채움: 세로 그라데이션 + 가로 결 텍스처, 색은 Image.color
//  - HP·스태미나·보스 HP: 금빛 잔상(Trail) → GaugeBarUI에 _trail 필드가 있으면 자동 연결
//  - 체간: 가운데 마름모 문장(Emblem) / 보스 HP: 양끝 창끝 장식(CapL·CapR)
//  - 오브젝트 이름·GaugeBarUI 참조는 그대로. 다시 실행해도 추가 오브젝트는 하나씩만 유지
public static class BarStyler
{
    private const string Dir = "Assets/_Project/UI/Bars/";

    private static readonly Color HpColor = new Color(0.76f, 0.12f, 0.09f);
    private static readonly Color StaminaColor = new Color(0.35f, 0.64f, 0.25f);
    private static readonly Color BossHpColor = new Color(0.69f, 0.09f, 0.07f);
    private static readonly Color PoiseColor = new Color(1f, 0.67f, 0.18f);
    private static readonly Color TrailColor = new Color(1f, 0.9f, 0.52f);
    private static readonly Color BackColor = new Color(0.04f, 0.035f, 0.03f, 0.85f);

    [MenuItem("Tools/UI/Restyle Bars")]
    private static void Restyle()
    {
        Scene scene = SceneManager.GetActiveScene();
        Transform hp = Find(scene, "PlayerHP_BG"), st = Find(scene, "PlayerStamina_BG");
        Transform boss = Find(scene, "BossHP_BG"), poise = Find(scene, "BossPoise_BG");
        if (hp == null || st == null || boss == null || poise == null)
        { Debug.LogError("[Bars] PlayerHP_BG / PlayerStamina_BG / BossHP_BG / BossPoise_BG 중 일부를 찾지 못함 — Arena 씬에서 실행하세요"); return; }

        Sprite frame = LoadSprite("UI_BarFrame.png", new Vector4(6, 6, 6, 6));
        Sprite shadow = LoadSprite("UI_BarShadow.png", new Vector4(40, 40, 40, 40));
        Sprite fill = LoadSprite("UI_BarFill.png", Vector4.zero);
        Sprite emblem = LoadSprite("UI_PoiseEmblem.png", Vector4.zero);
        Sprite cap = LoadSprite("UI_BarCap.png", Vector4.zero);

        Undo.RegisterFullObjectHierarchyUndo(hp.parent.gameObject, "Restyle Bars");

        // 위치·크기 (1920×1080 기준, B안 미리보기와 동일)
        Place(hp, new Vector2(60, -48), new Vector2(460, 10));
        Place(st, new Vector2(60, -66), new Vector2(280, 7));
        Place(poise, new Vector2(0, -72), new Vector2(520, 9));
        Place(boss, new Vector2(0, 69), new Vector2(960, 11));

        StyleBar(hp, hp.Find("PlayerHP_Fill"), HpColor, fill, frame, shadow, true);
        StyleBar(st, st.Find("PlayerStamina_Fill"), StaminaColor, fill, frame, shadow, true);
        StyleBar(boss, boss.Find("BossHP_Fill"), BossHpColor, fill, frame, shadow, true);
        StyleBar(poise, null, PoiseColor, fill, frame, shadow, false);
        StyleFill(poise.Find("BossPoise_Fill_L"), PoiseColor, fill);
        StyleFill(poise.Find("BossPoise_Fill_R"), PoiseColor, fill);

        // 체간 가운데 문장 (채움보다 위)
        Image em = Child(poise, "Emblem", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(28, 28));
        em.sprite = emblem;
        em.transform.SetAsLastSibling();

        // 보스 HP 양끝 창끝
        Image capL = Child(boss, "CapL", new Vector2(0f, 0.5f), new Vector2(-9f, 0f), new Vector2(16, 20));
        capL.sprite = cap;
        Image capR = Child(boss, "CapR", new Vector2(1f, 0.5f), new Vector2(9f, 0f), new Vector2(16, 20));
        capR.sprite = cap;
        capR.rectTransform.localScale = new Vector3(-1f, 1f, 1f);   // 좌우 반전

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("[Bars] 바 4개 세키로식 스타일 적용 완료");
    }

    private static void StyleBar(Transform bg, Transform fillT, Color color, Sprite fill, Sprite frame, Sprite shadow, bool trail)
    {
        var img = bg.GetComponent<Image>();
        img.sprite = null;                               // 단색 반투명 배경
        img.type = Image.Type.Simple;
        img.color = BackColor;
        img.raycastTarget = false;

        // 그림자: 첫 번째 자식 → 배경 위·채움 아래, 바깥으로 번짐
        Image sh = Child(bg, "Shadow", Vector2.zero, Vector2.zero, Vector2.zero);
        Stretch(sh.rectTransform, 10f);
        sh.sprite = shadow;
        sh.type = Image.Type.Sliced;
        sh.pixelsPerUnitMultiplier = 3f;
        sh.transform.SetAsFirstSibling();

        if (fillT != null)
        {
            StyleFill(fillT, color, fill);
            if (trail) AddTrail(bg, fillT, fill);
        }

        // 테두리: 바 바깥 2 (검정 1 + 청동 1)
        Image fr = Child(bg, "Frame", Vector2.zero, Vector2.zero, Vector2.zero);
        Stretch(fr.rectTransform, 2f);
        fr.sprite = frame;
        fr.type = Image.Type.Sliced;
        fr.fillCenter = false;
        fr.pixelsPerUnitMultiplier = 2f;
        Transform name = bg.Find("BossName");            // 이름 텍스트보다는 아래
        fr.transform.SetSiblingIndex(name != null ? name.GetSiblingIndex() : bg.childCount - 1);
    }

    private static void StyleFill(Transform t, Color color, Sprite fill)
    {
        if (t == null) return;
        var img = t.GetComponent<Image>();
        img.sprite = fill;                               // Filled 타입·방향·원점은 그대로
        img.color = color;
        img.raycastTarget = false;
    }

    private static void AddTrail(Transform bg, Transform fillT, Sprite fill)
    {
        Transform old = bg.Find("Trail");
        if (old != null) Object.DestroyImmediate(old.gameObject);

        var src = fillT.GetComponent<Image>();
        var srt = (RectTransform)fillT;
        Image img = Child(bg, "Trail", srt.anchorMin, srt.anchoredPosition, srt.sizeDelta);
        img.rectTransform.anchorMax = srt.anchorMax;
        img.rectTransform.pivot = srt.pivot;
        img.transform.SetSiblingIndex(fillT.GetSiblingIndex());   // 채움 바로 앞 → 채움이 위
        img.sprite = fill;
        img.type = Image.Type.Filled;
        img.fillMethod = src.fillMethod;
        img.fillOrigin = src.fillOrigin;
        img.fillAmount = src.fillAmount;
        img.color = TrailColor;

        var gauge = bg.GetComponent("GaugeBarUI");
        if (gauge == null) return;
        var so = new SerializedObject(gauge);
        SerializedProperty p = so.FindProperty("_trail");
        if (p == null)
        {
            // 코드가 없으면 잔상이 가득 찬 채로 남아 깎인 부분이 금색으로 보임 → 아예 만들지 않음
            Object.DestroyImmediate(img.gameObject);
            Debug.Log($"[Bars] {bg.name}: GaugeBarUI에 _trail 필드가 없어 잔상 생략 (필드 추가 후 다시 실행하면 생성)");
            return;
        }
        p.objectReferenceValue = img;
        so.ApplyModifiedProperties();
    }

    // 같은 이름 자식이 있으면 지우고 새로 (Image 하나 달린 RectTransform)
    private static Image Child(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        Transform old = parent.Find(name);
        if (old != null) Object.DestroyImmediate(old.gameObject);
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var img = go.AddComponent<Image>();
        img.raycastTarget = false;
        return img;
    }

    private static void Stretch(RectTransform rt, float outset)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(-outset, -outset); rt.offsetMax = new Vector2(outset, outset);
    }

    private static void Place(Transform t, Vector2 pos, Vector2 size)
    {
        var rt = (RectTransform)t;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    private static Sprite LoadSprite(string file, Vector4 border)
    {
        string path = Dir + file;
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) { Debug.LogError($"[Bars] {path} 없음"); return null; }
        if (ti.textureType != TextureImporterType.Sprite || ti.spriteBorder != border || ti.mipmapEnabled)
        {
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spriteBorder = border;
            ti.mipmapEnabled = false;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.filterMode = FilterMode.Bilinear;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.alphaIsTransparency = true;
            ti.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static Transform Find(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
        return null;
    }
}
