using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

// 일회성 에디터 도구: Mixamo 팩 FBX들의 클립 Import 설정을 한 번에 적용한다.
// 메뉴: Tools > Mixamo > Apply Clip Settings
public static class MixamoClipSetup
{
    private const string Folder = "Assets/_Project/Animations/Player/Pro Sword and Shield Pack";
    private const string ReportPath = "Library/MixamoClipReport.txt";
    // 기사 모델(스킨 포함, T포즈 기준) — 모든 Mixamo 모션이 이 아바타를 공유하도록
    private const string KnightModelPath = "Assets/_Project/Models/medieval-knight-sculpture-game-ready/source/Dwarf Idle.fbx";

    // 반복 재생할 클립 (파일 이름에 포함된 단어)
    private static readonly string[] LoopKeywords = { "idle", "walk", "run", "strafe" };
    // 이름에 loop 단어가 있어도 반복하지 않을 클립
    private static readonly string[] NoLoopKeywords = { "roll" };
    // 실제로 앞으로 나아가는 이동 클립 → XZ를 포즈에 굽지 않음(루트 모션으로 빠져서 버려짐 = 제자리)
    private static readonly string[] LocomotionKeywords = { "walk", "run", "strafe", "roll" };

    [MenuItem("Tools/Mixamo/Apply Clip Settings")]
    private static void Apply()
    {
        string[] paths = AssetDatabase.FindAssets("t:Model", new[] { Folder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => p.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Avatar knightAvatar = AssetDatabase.LoadAllAssetsAtPath(KnightModelPath).OfType<Avatar>().FirstOrDefault();
        if (knightAvatar == null)
        {
            Debug.LogError($"[MixamoClipSetup] 기사 아바타를 찾지 못함: {KnightModelPath}");
            return;
        }

        foreach (string path in paths)
        {
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) continue;

            // 스킨 없이 받은 모션은 첫 프레임 자세로 아바타가 만들어져 팔다리가 틀어짐 → 기사 아바타 복사
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
            importer.sourceAvatar = knightAvatar;

            string file = Path.GetFileNameWithoutExtension(path);
            string lower = file.ToLowerInvariant();
            bool loop = LoopKeywords.Any(lower.Contains) && !NoLoopKeywords.Any(lower.Contains);
            bool locomotion = LocomotionKeywords.Any(lower.Contains);

            ModelImporterClipAnimation[] clips = importer.clipAnimations.Length > 0
                ? importer.clipAnimations
                : importer.defaultClipAnimations;
            if (clips.Length == 0) continue;

            for (int i = 0; i < clips.Length; i++)
            {
                ModelImporterClipAnimation clip = clips[i];
                clip.name = clips.Length == 1 ? file : $"{file}_{i}";
                clip.loopTime = loop;
                clip.loopPose = loop;

                clip.lockRootRotation = true;          // Rotation: Bake Into Pose
                clip.keepOriginalOrientation = true;   //   Based Upon: Original
                clip.lockRootHeightY = true;           // Y: Bake Into Pose (쓰러짐·구르기 높이 유지)
                clip.keepOriginalPositionY = true;     //   Based Upon: Original
                clip.lockRootPositionXZ = !locomotion; // XZ: 이동 클립만 굽지 않음
                clip.keepOriginalPositionXZ = true;
                clips[i] = clip;
            }

            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }

        WriteReport(paths);
        Debug.Log($"[MixamoClipSetup] {paths.Length}개 FBX 설정 완료. 리포트: {ReportPath}");
    }

    private static void WriteReport(string[] paths)
    {
        var sb = new StringBuilder();
        foreach (string path in paths)
        {
            foreach (AnimationClip clip in AssetDatabase.LoadAllAssetsAtPath(path)
                         .OfType<AnimationClip>()
                         .Where(c => !c.name.StartsWith("__preview__")))
            {
                Vector3 v = clip.averageSpeed;
                sb.AppendLine($"{clip.name}\tlen={clip.length:F2}\tloop={clip.isLooping}\tvel=({v.x:F2},{v.y:F2},{v.z:F2})\tangY={clip.averageAngularSpeed:F1}");
            }
        }
        File.WriteAllText(ReportPath, sb.ToString());
    }
}
