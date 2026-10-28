using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

// Tools/Audio/Setup SFX (Arena)
// 1) "SFX" 오브젝트 + SfxPlayer (출력 = MainMixer/SFX)
// 2) 플레이어 칼 / 보스 칼 / 보스 발 히트박스에 HitboxSfx + 3D AudioSource를 붙이고 클립 배정
//    (소리가 칼 위치에서 나도록 — HitboxSfx에 [SerializeField] AudioSource _source 필드가 있으면 자동 연결)
// 3) SFX 클립 Force To Mono (3D 방향감이 또렷해짐)
// 4) 아레나 전체에 AudioReverbZone(숲) → 마른 녹음 소리에 공간 울림을 더함
// 5) GameFlowController에 승리/패배 사운드 + BGM 소스 연결 (_winSfx / _loseSfx / _bgm 필드가 있으면)
//    휘두름 = Whoosh(바람) / 적중 = 플레이어→보스 갑옷음, 보스→플레이어 베는 소리 / 칼끼리 닿을 때만 금속 Clash: "팅"(02·05·10) = 패링, 긴 울림(06·07) = 가드
public static class SfxSetup
{
    private const string MixerPath = "Assets/_Project/Audio/MainMixer.mixer";
    private const string Clash = "Assets/_Project/Audio/SFX/Clash/SFX_Clash_";
    private const string WhooshLight = "Assets/_Project/Audio/SFX/Whoosh/SFX_WhooshLight_";   // 휘두름 (원본 피치 0.96~1.14)
    private const string WhooshHeavy = "Assets/_Project/Audio/SFX/Whoosh/SFX_WhooshHeavy_";   // 휘두름 (피치 0.72~0.9로 낮춰 묵직하게)
    private const string HitArmor = "Assets/_Project/Audio/SFX/Impact/SFX_HitArmor_";         // 플레이어 칼 → 갑옷 기사
    private const string VictoryClip = "Assets/_Project/Audio/SFX/Result/SFX_Result_Victory.ogg";  // 교회 종 3번
    private const string DefeatClip = "Assets/_Project/Audio/SFX/Result/SFX_Result_Defeat.ogg";    // 시네마틱 붐
    private const string Parry = "Assets/_Project/Audio/SFX/Parry/SFX_Parry_";              // Clash "팅" + 갑옷 타격 + 저음 쿵 레이어
    private const string HitFlesh = "Assets/_Project/Audio/SFX/Impact/SFX_HitFlesh_";         // 보스 공격 → 플레이어

    private static readonly int[] Four = { 1, 2, 3, 4 };
    private static readonly int[] Three = { 1, 2, 3 };
    private static readonly int[] BlockClash = { 6, 7 };

    [MenuItem("Tools/Audio/Setup SFX")]
    private static void Setup()
    {
        Scene scene = SceneManager.GetActiveScene();
        var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
        AudioMixerGroup sfxGroup = mixer != null ? mixer.FindMatchingGroups("SFX").FirstOrDefault() : null;
        if (sfxGroup == null) { Debug.LogError("[SFX] MainMixer의 SFX 그룹을 찾지 못함"); return; }

        // ---- SfxPlayer
        foreach (GameObject root in scene.GetRootGameObjects())
            if (root.name == "SFX") Undo.DestroyObjectImmediate(root);
        var go = new GameObject("SFX");
        Undo.RegisterCreatedObjectUndo(go, "Setup SFX");
        var player = go.AddComponent<SfxPlayer>();
        var so = new SerializedObject(player);
        so.FindProperty("_output").objectReferenceValue = sfxGroup;
        so.ApplyModifiedProperties();

        // ---- 히트박스 찾기
        HitboxComponent playerHitbox = null, bossSword = null, bossFoot = null;
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (HitboxComponent h in root.GetComponentsInChildren<HitboxComponent>(true))
            {
                if (root.name == "Player" && playerHitbox == null) playerHitbox = h;
                else if (h.name == "SwordHitbox") bossSword = h;
                else if (h.name == "FootHitbox") bossFoot = h;
            }

        ForceMono();

        int done = 0;
        done += Assign(sfxGroup, playerHitbox, Clips(WhooshLight, Four), Clips(HitArmor, Four), null, null);
        done += Assign(sfxGroup, bossSword, Clips(WhooshHeavy, Four), Clips(HitFlesh, Three), Clips(Parry, Three), Clips(Clash, BlockClash));
        done += Assign(sfxGroup, bossFoot, null, Clips(HitFlesh, Three), null, Clips(Clash, BlockClash));   // 발차기: 휘두르는 소리 없음, 패링 불가

        AssignResult(scene);
        SetupReverb(scene);

        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = go;
        Debug.Log($"[SFX] SfxPlayer 생성 + 히트박스 {done}/3개에 HitboxSfx 배정");
    }

    // 플레이어 칼은 보스가 막거나 패링하지 않으므로 parried/blocked 비움
    private static int Assign(AudioMixerGroup group, HitboxComponent hitbox, AudioClip[] swing, AudioClip[] hit, AudioClip[] parried, AudioClip[] blocked)
    {
        if (hitbox == null) return 0;
        var sfx = hitbox.GetComponent<HitboxSfx>();
        if (sfx == null) sfx = Undo.AddComponent<HitboxSfx>(hitbox.gameObject);

        // 칼에 다는 작은 스피커
        var src = hitbox.GetComponent<AudioSource>();
        if (src == null) src = Undo.AddComponent<AudioSource>(hitbox.gameObject);
        Undo.RecordObject(src, "Setup SFX Source");
        src.playOnAwake = false;
        src.outputAudioMixerGroup = group;
        src.spatialBlend = 1f;                              // 완전 3D
        src.rolloffMode = AudioRolloffMode.Logarithmic;
        src.minDistance = 3f;                               // 카메라(약 5m)보다 가깝게 → 거리감이 생김 (6이면 귀 바로 앞처럼 들림)
        src.maxDistance = 35f;
        src.dopplerLevel = 0f;                              // 빠른 휘두르기에 음 휘는 것 방지

        var so = new SerializedObject(sfx);
        SerializedProperty srcProp = so.FindProperty("_source");
        if (srcProp != null) srcProp.objectReferenceValue = src;
        else Debug.LogWarning($"[SFX] {hitbox.name}: HitboxSfx에 _source 필드가 없어 AudioSource 연결 생략");
        Fill(so.FindProperty("_swing"), swing);
        Fill(so.FindProperty("_hit"), hit);
        Fill(so.FindProperty("_parried"), parried);
        Fill(so.FindProperty("_blocked"), blocked);
        so.ApplyModifiedProperties();
        return 1;
    }

    private static void SetupReverb(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
            if (root.name == "SFX_Reverb") Undo.DestroyObjectImmediate(root);
        var go = new GameObject("SFX_Reverb");
        Undo.RegisterCreatedObjectUndo(go, "Setup SFX Reverb");
        go.transform.position = Vector3.zero;               // 아레나 중심
        var zone = go.AddComponent<AudioReverbZone>();
        zone.reverbPreset = AudioReverbPreset.Forest;       // 마음에 안 들면 인스펙터에서 Plain(약하게) / Mountains(크게)
        zone.minDistance = 60f;                             // 리스너(카메라)가 이 안에 있으면 울림 100%
        zone.maxDistance = 80f;
    }

    private static void AssignResult(Scene scene)
    {
        GameFlowController flow = null;
        AudioSource bgm = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            flow ??= root.GetComponentInChildren<GameFlowController>(true);
            if (root.name == "BGM") bgm = root.GetComponent<AudioSource>();
        }
        if (bgm != null) { Undo.RecordObject(bgm, "BGM Reverb Off"); bgm.reverbZoneMix = 0f; }   // 음악까지 숲 울림 먹지 않게
        if (flow == null) { Debug.LogWarning("[SFX] GameFlowController를 찾지 못해 결과 사운드 연결 생략"); return; }

        var so = new SerializedObject(flow);
        SetRef(so, "_winSfx", AssetDatabase.LoadAssetAtPath<AudioClip>(VictoryClip));
        SetRef(so, "_loseSfx", AssetDatabase.LoadAssetAtPath<AudioClip>(DefeatClip));
        SetRef(so, "_bgm", bgm);
        so.ApplyModifiedProperties();
    }

    private static void SetRef(SerializedObject so, string field, Object value)
    {
        SerializedProperty prop = so.FindProperty(field);
        if (prop == null) { Debug.LogWarning($"[SFX] GameFlowController에 {field} 필드가 없어 연결 생략"); return; }
        if (value == null) Debug.LogWarning($"[SFX] {field}에 넣을 대상을 찾지 못함 (BGM은 Setup BGM 먼저)");
        prop.objectReferenceValue = value;
    }

    private static void ForceMono()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/_Project/Audio/SFX" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("/Result/") || path.Contains("/Intro/")) continue;   // 결과·인트로 사운드는 2D로 트니까 스테레오 유지
            var importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null || importer.forceToMono) continue;
            importer.forceToMono = true;
            importer.SaveAndReimport();
        }
    }

    private static void Fill(SerializedProperty prop, AudioClip[] clips)
    {
        clips ??= new AudioClip[0];
        prop.arraySize = clips.Length;
        for (int i = 0; i < clips.Length; i++) prop.GetArrayElementAtIndex(i).objectReferenceValue = clips[i];
    }

    private static AudioClip[] Clips(string prefix, int[] numbers) =>
        numbers.Select(n => AssetDatabase.LoadAssetAtPath<AudioClip>($"{prefix}{n:00}.ogg")).Where(c => c != null).ToArray();
}
