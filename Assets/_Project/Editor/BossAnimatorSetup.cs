using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// 일회성 에디터 도구: BossAnimator에 패턴용 상태들을 만든다 (화살표 없이 상태만).
// 메뉴: Tools > Boss > Setup Pattern States (여러 번 눌러도 중복 생성되지 않고 클립만 다시 연결)
public static class BossAnimatorSetup
{
    private const string ControllerPath = "Assets/_Project/Animations/Boss/BossAnimator.controller";
    private const string P = "Assets/_Project/Animations/Boss/Pattern/";

    // (상태 이름, 클립 경로) — 상태 이름은 BossPattern 에셋의 StateName과 같아야 한다
    private static readonly (string state, string clip)[] States =
    {
        ("Attack1",   P + "Single/Enemy_Attack_1_InPlace.anim"),

        ("P1_Hit1",   P + "P1_Advance/OneHand_Up_Attack_1.anim"),
        ("P1_Hit2",   P + "P1_Advance/OneHand_Up_Attack_2.anim"),
        ("P1_Hit3",   P + "P1_Advance/OneHand_Up_Attack_3.anim"),

        ("P2_Hit1",   P + "P2_Delay/OneHand_Up_Attack_B_1.anim"),
        ("P2_Hit2",   P + "P2_Delay/OneHand_Up_Attack_B_2.anim"),
        ("P2_Step",   P + "P2_Delay/OneHand_Up_Walk_B.anim"),
        ("P2_Hit3",   P + "P2_Delay/OneHand_Up_Attack_B_3.anim"),

        ("P3_Slash",  P + "P3_GreatSword/great sword slash.fbx"),
        ("P3_Kick",   P + "P3_GreatSword/great sword kick.fbx"),
        ("P3_Strafe", P + "P3_GreatSword/great sword strafe.fbx"),
        ("P3_Spin",   P + "P3_GreatSword/great sword high spin attack.fbx"),

        ("Groggy",    "Assets/_Project/Animations/Boss/Groggy/Kneeling_Idle.fbx"),   // 체간 붕괴: 한쪽 무릎 꿇고 대기(반복)
    };

    [MenuItem("Tools/Boss/Setup Pattern States")]
    private static void Setup()
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null) { Debug.LogError($"[BossAnimatorSetup] 컨트롤러 없음: {ControllerPath}"); return; }
        AnimatorStateMachine sm = controller.layers[0].stateMachine;

        int added = 0, updated = 0;
        for (int i = 0; i < States.Length; i++)
        {
            (string stateName, string clipPath) = States[i];
            AnimationClip clip = AssetDatabase.LoadAllAssetsAtPath(clipPath).OfType<AnimationClip>()
                .FirstOrDefault(c => !c.name.StartsWith("__preview__"));
            if (clip == null) { Debug.LogError($"[BossAnimatorSetup] 클립 없음: {clipPath}"); continue; }

            AnimatorState state = sm.states.Select(s => s.state).FirstOrDefault(s => s.name == stateName);
            if (state == null)
            {
                // 패턴별로 한 줄씩 배치 (Single은 맨 위)
                int row = stateName == "Attack1" ? 0 : stateName[1] - '0';
                int col = stateName == "Attack1" ? 0 : CountBefore(stateName, i);
                state = sm.AddState(stateName, new Vector3(250f + col * 210f, 400f + row * 70f, 0f));
                added++;
            }
            else updated++;
            state.motion = clip;
        }

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        Debug.Log($"[BossAnimatorSetup] 상태 추가 {added}개, 클립 재연결 {updated}개");
    }

    // 같은 패턴(P1_, P2_ ...) 안에서 몇 번째인지
    private static int CountBefore(string stateName, int index)
    {
        string prefix = stateName.Substring(0, 3);
        return States.Take(index).Count(s => s.state.StartsWith(prefix));
    }
}
