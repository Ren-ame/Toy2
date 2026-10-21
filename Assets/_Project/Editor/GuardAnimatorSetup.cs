using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// 일회성 에디터 도구: 가드 이동용 블렌드 트리 + 상체 레이어를 PlayerAnimator에 만든다.
// 메뉴: Tools > Animator > Setup Guard Move (여러 번 눌러도 중복 생성되지 않음)
public static class GuardAnimatorSetup
{
    private const string ControllerPath = "Assets/_Project/Animations/Player/PlayerAnimator.controller";
    private const string MaskPath = "Assets/_Project/Animations/Player/UpperBody.mask";
    private const string MixamoFolder = "Assets/_Project/Animations/Player/Pro Sword and Shield Pack/";
    private const string GuardPosePath = "Assets/_Project/Animations/Player/EEJANAI_Team/FreeSwordAnimations/Animations/deffensive stance.anim";

    private const string BlendStateName = "GuardMove";
    private const string UpperLayerName = "UpperBody";
    private const string UpperStateName = "Guard";

    [MenuItem("Tools/Animator/Setup Guard Move")]
    private static void Setup()
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null) { Debug.LogError($"[GuardAnimatorSetup] 컨트롤러 없음: {ControllerPath}"); return; }

        AnimationClip idle = LoadFbxClip("sword and shield idle.fbx");
        AnimationClip forward = LoadFbxClip("sword and shield walk.fbx");
        AnimationClip backward = LoadFbxClip("sword and shield walk (2).fbx");
        AnimationClip right = LoadFbxClip("sword and shield strafe.fbx");
        AnimationClip left = LoadFbxClip("sword and shield strafe (2).fbx");
        var guardPose = AssetDatabase.LoadAssetAtPath<AnimationClip>(GuardPosePath);
        if (new[] { idle, forward, backward, right, left, guardPose }.Any(c => c == null)) return;

        // 1. 파라미터
        AddFloatParameter(controller, "MoveX");
        AddFloatParameter(controller, "MoveY");

        // 2. Base Layer: GuardMove 블렌드 트리 (있으면 지우고 새로)
        AnimatorStateMachine baseMachine = controller.layers[0].stateMachine;
        AnimatorState old = baseMachine.states.Select(s => s.state).FirstOrDefault(s => s.name == BlendStateName);
        if (old != null)
        {
            if (old.motion is BlendTree oldTree) Object.DestroyImmediate(oldTree, true);
            baseMachine.RemoveState(old);
        }

        AnimatorState blendState = controller.CreateBlendTreeInController(BlendStateName, out BlendTree tree, 0);
        tree.blendType = BlendTreeType.FreeformDirectional2D;
        tree.blendParameter = "MoveX";
        tree.blendParameterY = "MoveY";
        tree.AddChild(idle, Vector2.zero);
        tree.AddChild(forward, new Vector2(0f, 1f));
        tree.AddChild(backward, new Vector2(0f, -1f));
        tree.AddChild(right, new Vector2(1f, 0f));
        tree.AddChild(left, new Vector2(-1f, 0f));

        // 상태 박스 위치를 기존 Guard 옆으로
        ChildAnimatorState[] states = baseMachine.states;
        for (int i = 0; i < states.Length; i++)
        {
            if (states[i].state == blendState)
            {
                ChildAnimatorState guard = states.FirstOrDefault(s => s.state.name == "Guard");
                states[i].position = guard.state != null ? guard.position + new Vector3(0f, -60f, 0f) : new Vector3(300f, 300f, 0f);
            }
        }
        baseMachine.states = states;

        // 3. 상체 마스크
        AvatarMask mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(MaskPath);
        if (mask == null)
        {
            mask = new AvatarMask();
            AssetDatabase.CreateAsset(mask, MaskPath);
        }
        for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++)
        {
            var part = (AvatarMaskBodyPart)i;
            bool upper = part == AvatarMaskBodyPart.Body || part == AvatarMaskBodyPart.Head
                      || part == AvatarMaskBodyPart.LeftArm || part == AvatarMaskBodyPart.RightArm
                      || part == AvatarMaskBodyPart.LeftFingers || part == AvatarMaskBodyPart.RightFingers
                      || part == AvatarMaskBodyPart.LeftHandIK || part == AvatarMaskBodyPart.RightHandIK;
            mask.SetHumanoidBodyPartActive(part, upper);
        }
        EditorUtility.SetDirty(mask);

        // 4. UpperBody 레이어 (없을 때만 생성) — 기본 Weight 0, 상태는 Guard 하나
        if (controller.layers.All(l => l.name != UpperLayerName))
        {
            controller.AddLayer(UpperLayerName);
        }
        AnimatorControllerLayer[] layers = controller.layers;
        int upperIndex = System.Array.FindIndex(layers, l => l.name == UpperLayerName);
        layers[upperIndex].avatarMask = mask;
        layers[upperIndex].defaultWeight = 0f;
        layers[upperIndex].blendingMode = AnimatorLayerBlendingMode.Override;
        controller.layers = layers;

        AnimatorStateMachine upperMachine = controller.layers[upperIndex].stateMachine;
        AnimatorState upperGuard = upperMachine.states.Select(s => s.state).FirstOrDefault(s => s.name == UpperStateName)
                                   ?? upperMachine.AddState(UpperStateName);
        upperGuard.motion = guardPose;
        upperMachine.defaultState = upperGuard;

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        Debug.Log($"[GuardAnimatorSetup] 완료: '{BlendStateName}' 블렌드 트리(Base), '{UpperLayerName}' 레이어(인덱스 {upperIndex}, Weight 0)");
    }

    private static void AddFloatParameter(AnimatorController controller, string name)
    {
        if (controller.parameters.Any(p => p.name == name)) return;
        controller.AddParameter(name, AnimatorControllerParameterType.Float);
    }

    private static AnimationClip LoadFbxClip(string fileName)
    {
        string path = MixamoFolder + fileName;
        AnimationClip clip = AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<AnimationClip>()
            .FirstOrDefault(c => !c.name.StartsWith("__preview__"));
        if (clip == null) Debug.LogError($"[GuardAnimatorSetup] 클립 없음: {path}");
        return clip;
    }
}
