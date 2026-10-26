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
    private const string D = "Assets/_Project/Animations/Boss/DoubleL/Demo/Anim/";

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

        // 후딜(간 보기): 플레이어를 바라본 채 옆·뒤로 걷기. 이동은 코드가 하므로 제자리(InPlace) 클립
        ("StrafeL",   D + "OneHand_Up_Walk_L_InPlace.anim"),
        ("StrafeR",   D + "OneHand_Up_Walk_R_InPlace.anim"),
        ("StrafeB",   D + "OneHand_Up_Walk_B_InPlace.anim"),
        ("StrafeF",   D + "OneHand_Up_Walk_F_InPlace.anim"),   // 후딜 중 거리 좁히기(천천히 걷기)
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
                state = sm.AddState(stateName, GridPosition(stateName, i));
                added++;
            }
            else updated++;
            state.motion = clip;
        }

        // 화면 밖(이전 버전 버그로 y=5020 등)에 놓인 상태는 제자리로 옮김
        ChildAnimatorState[] children = sm.states;
        for (int c = 0; c < children.Length; c++)
        {
            int idx = System.Array.FindIndex(States, s => s.state == children[c].state.name);
            if (idx >= 0 && children[c].position.y > 2000f) children[c].position = GridPosition(States[idx].state, idx);
        }
        sm.states = children;

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        Debug.Log($"[BossAnimatorSetup] 상태 추가 {added}개, 클립 재연결 {updated}개");
    }

    // 패턴 상태(P1_~P3_)는 패턴별로 한 줄씩, Attack1은 맨 위 줄, 그 밖(Groggy 등)은 Stagger·Dead 아래 열
    private static Vector3 GridPosition(string stateName, int index)
    {
        if (stateName == "Attack1") return new Vector3(250f, 400f, 0f);
        bool isPattern = stateName.Length > 3 && stateName[0] == 'P' && char.IsDigit(stateName[1]) && stateName[2] == '_';
        if (isPattern)
            return new Vector3(250f + CountBefore(stateName, index) * 210f, 400f + (stateName[1] - '0') * 70f, 0f);
        int misc = States.Take(index).Count(s => !(s.state.Length > 3 && s.state[0] == 'P' && char.IsDigit(s.state[1])) && s.state != "Attack1");
        return new Vector3(10f, 400f + misc * 60f, 0f);
    }

    // 같은 패턴(P1_, P2_ ...) 안에서 몇 번째인지
    private static int CountBefore(string stateName, int index)
    {
        string prefix = stateName.Substring(0, 3);
        return States.Take(index).Count(s => s.state.StartsWith(prefix));
    }
}
