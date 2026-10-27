using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// 일회성 에디터 도구: 보스 패턴 1~3에 쓰는 클립에 Animation Event를 한 번에 넣는다.
// 메뉴: Tools > Boss > Apply Pattern Events (여러 번 눌러도 기존 이벤트를 지우고 다시 넣음)
//
// 이벤트 규칙 (Function = OnAnimEvent, Int = 패턴 안에서 몇 번째 구간인지)
//   GlowOn          : 눈빛 신호 = "다음 타는 패링 가능". 패턴마다 한 타에만 넣는다.
//   SwordHitboxOn/Off: 칼 판정
//   FootHitboxOn/Off: 발 판정 (발차기)
//   AttackEnd       : 이 구간 끝 → 다음 구간으로, 마지막 구간이면 패턴 종료
public static class BossPatternEventSetup
{
    private const string Pattern = "Assets/_Project/Animations/Boss/Pattern/";

    private struct Ev
    {
        public float Time; public string Name; public int Index;
        public Ev(float time, string name, int index) { Time = time; Name = name; Index = index; }
    }

    // ---- 패턴 1: 전진 3연타 (패링 가능 = 3타)
    // ---- 패턴 2: 지연 변칙 (패링 가능 = 1타 지연 베기)
    private static readonly Dictionary<string, Ev[]> AnimEvents = new Dictionary<string, Ev[]>
    {
        // ---- 단일 베기 (패링 가능)
        ["Single/Enemy_Attack_1_InPlace.anim"] = new[] { new Ev(0.133f, "GlowOn", 0), new Ev(0.433f, "SwordHitboxOn", 0), new Ev(0.667f, "SwordHitboxOff", 0), new Ev(0.933f, "AttackEnd", 0) },

        ["P1_Advance/OneHand_Up_Attack_1.anim"] = new[] { new Ev(0.38f, "SwordHitboxOn", 0), new Ev(0.62f, "SwordHitboxOff", 0), new Ev(0.80f, "AttackEnd", 0) },
        ["P1_Advance/OneHand_Up_Attack_2.anim"] = new[] { new Ev(0.75f, "SwordHitboxOn", 1), new Ev(1.00f, "SwordHitboxOff", 1), new Ev(1.15f, "AttackEnd", 1) },
        ["P1_Advance/OneHand_Up_Attack_3.anim"] = new[] { new Ev(0.33f, "GlowOn", 2), new Ev(0.58f, "SwordHitboxOn", 2), new Ev(0.82f, "SwordHitboxOff", 2), new Ev(1.60f, "AttackEnd", 2) },

        ["P2_Delay/OneHand_Up_Attack_B_1.anim"] = new[] { new Ev(0.85f, "GlowOn", 0), new Ev(1.10f, "SwordHitboxOn", 0), new Ev(1.35f, "SwordHitboxOff", 0), new Ev(1.50f, "AttackEnd", 0) },
        ["P2_Delay/OneHand_Up_Attack_B_2.anim"] = new[] { new Ev(0.55f, "SwordHitboxOn", 1), new Ev(0.78f, "SwordHitboxOff", 1), new Ev(0.95f, "AttackEnd", 1) },
        ["P2_Delay/OneHand_Up_Walk_B.anim"] = new[] { new Ev(0.55f, "AttackEnd", 2) },
        ["P2_Delay/OneHand_Up_Attack_B_3.anim"] = new[] { new Ev(0.88f, "SwordHitboxOn", 3), new Ev(1.12f, "SwordHitboxOff", 3), new Ev(1.80f, "AttackEnd", 3) },
    };

    // ---- 패턴 3: 베기 → 발차기 → 옆걸음 → 돌진 회전베기 (패링 가능 = 1타 베기)
    private static readonly Dictionary<string, Ev[]> FbxEvents = new Dictionary<string, Ev[]>
    {
        ["P3_GreatSword/great sword slash.fbx"] = new[] { new Ev(0.28f, "GlowOn", 0), new Ev(0.53f, "SwordHitboxOn", 0), new Ev(0.77f, "SwordHitboxOff", 0), new Ev(0.85f, "AttackEnd", 0) },
        ["P3_GreatSword/great sword kick.fbx"] = new[] { new Ev(0.53f, "FootHitboxOn", 1), new Ev(0.70f, "FootHitboxOff", 1), new Ev(0.95f, "AttackEnd", 1) },
        ["P3_GreatSword/great sword strafe.fbx"] = new[] { new Ev(0.50f, "AttackEnd", 2) },
        ["P3_GreatSword/great sword high spin attack.fbx"] = new[] { new Ev(0.60f, "SwordHitboxOn", 3), new Ev(1.05f, "SwordHitboxOff", 3), new Ev(1.80f, "AttackEnd", 3) },
    };

    [MenuItem("Tools/Boss/Apply Pattern Events")]
    private static void Apply()
    {
        int done = 0;

        // .anim 클립: 클립에 직접 이벤트 (시간 = 초)
        foreach (var pair in AnimEvents)
        {
            string path = Pattern + pair.Key;
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null) { Debug.LogError($"[BossPatternEventSetup] 클립 없음: {path}"); continue; }

            AnimationEvent[] events = pair.Value.Select(e => Make(Mathf.Min(e.Time, clip.length), e)).ToArray();
            AnimationUtility.SetAnimationEvents(clip, events);

            // 루트 모션: 앞뒤 이동량을 포즈에 굽지 않고 뽑아냄 (Center of Mass 기준) → 보스가 코드로 실제 이동
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopBlendPositionXZ = false;
            settings.keepOriginalPositionXZ = false;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            done++;
        }

        // FBX 클립: Import 설정에 이벤트 (시간 = 0~1 정규화)
        foreach (var pair in FbxEvents)
        {
            string path = Pattern + pair.Key;
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) { Debug.LogError($"[BossPatternEventSetup] FBX 없음: {path}"); continue; }

            ModelImporterClipAnimation[] clips = importer.clipAnimations.Length > 0 ? importer.clipAnimations : importer.defaultClipAnimations;
            if (clips.Length == 0) continue;

            ModelImporterClipAnimation c = clips[0];
            float fps = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                .FirstOrDefault(a => !a.name.StartsWith("__preview__"))?.frameRate ?? 30f;
            float length = Mathf.Max(0.01f, (c.lastFrame - c.firstFrame) / fps);

            c.events = pair.Value.Select(e => Make(Mathf.Clamp01(e.Time / length), e)).ToArray();
            c.lockRootPositionXZ = false;      // 루트 모션 (위와 같은 이유)
            c.keepOriginalPositionXZ = false;
            clips[0] = c;
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
            done++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[BossPatternEventSetup] 클립 {done}개에 패턴 이벤트 적용 완료");
    }

    private static AnimationEvent Make(float time, Ev e)
    {
        return new AnimationEvent { time = time, functionName = "OnAnimEvent", stringParameter = e.Name, intParameter = e.Index };
    }
}
