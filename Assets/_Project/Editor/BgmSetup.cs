using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

// Tools/Audio/Setup BGM : 현재 씬에 BGM 오브젝트(AudioSource) 배치
//  - Title → 잔잔한 메뉴 곡 / Arena → 보스전 루프
//  - 출력은 MainMixer의 BGM 그룹 → 일시정지 메뉴의 BGM 볼륨 슬라이더가 적용됨
//  - 임포트 설정: Streaming(긴 곡은 메모리에 통째로 안 올림), Vorbis 압축
// 다시 실행하면 기존 "BGM" 오브젝트를 지우고 새로 만듦
public static class BgmSetup
{
    private const string Dir = "Assets/_Project/Audio/BGM/";
    private const string TitleClip = Dir + "BGM_Title_RaviDeTeRevoir.mp3";        // YannZ — Ravi de te Revoir (Contemplative Fantasy)
    private const string BossClip = Dir + "BGM_Boss_ShadowforgeConvergence_Loop.ogg"; // alkakrab — Shadowforge Convergence (Loop)
    private const string MixerPath = "Assets/_Project/Audio/MainMixer.mixer";
    private const float Volume = 0.6f;   // 소스 자체 볼륨 (전체 크기는 Mixer/슬라이더로)

    [MenuItem("Tools/Audio/Setup BGM")]
    private static void Setup()
    {
        Scene scene = SceneManager.GetActiveScene();
        string clipPath = scene.name == "Title" ? TitleClip : scene.name == "Arena" ? BossClip : null;
        if (clipPath == null) { Debug.LogError("[BGM] Title 또는 Arena 씬에서 실행하세요"); return; }

        ConfigureImport(TitleClip);
        ConfigureImport(BossClip);

        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
        var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
        AudioMixerGroup[] groups = mixer != null ? mixer.FindMatchingGroups("BGM") : null;
        if (clip == null || groups == null || groups.Length == 0)
        { Debug.LogError("[BGM] 클립 또는 MainMixer의 BGM 그룹을 찾지 못함"); return; }

        foreach (GameObject root in scene.GetRootGameObjects())
            if (root.name == "BGM") Undo.DestroyObjectImmediate(root);

        var go = new GameObject("BGM");
        Undo.RegisterCreatedObjectUndo(go, "Setup BGM");
        var src = go.AddComponent<AudioSource>();
        src.clip = clip;
        src.outputAudioMixerGroup = groups[0];
        src.loop = true;
        src.playOnAwake = true;
        src.spatialBlend = 0f;   // 2D: 카메라 위치와 상관없이 같은 크기
        src.volume = Volume;
        src.priority = 0;        // 가장 높은 우선순위 (효과음이 많아도 끊기지 않게)
        src.reverbZoneMix = 0f;  // 아레나 리버브 존(숲 울림)은 효과음에만

        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = go;
        Debug.Log($"[BGM] {scene.name}: {clip.name} 배치 완료 → Mixer/BGM");
    }

    private static void ConfigureImport(string path)
    {
        if (!(AssetImporter.GetAtPath(path) is AudioImporter imp)) return;
        AudioImporterSampleSettings s = imp.defaultSampleSettings;
        s.loadType = AudioClipLoadType.Streaming;
        s.compressionFormat = AudioCompressionFormat.Vorbis;
        s.quality = 0.7f;
        imp.defaultSampleSettings = s;
        imp.loadInBackground = true;
        imp.SaveAndReimport();
    }
}
