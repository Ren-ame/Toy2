using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Tools/Map/Apply Dusk Lighting
// 황혼 분위기: 낮게 깔린 주황빛 해(성문 정면을 비춤) + 노을 하늘 + 따뜻한 안개 + 보랏빛 환경광
// 값은 아래 상수만 바꿔서 다시 실행하면 됨 (씬 RenderSettings에 저장됨)
public static class DuskLightingSetup
{
    private const string SkyMatPath = "Assets/_Project/Materials/Sky/M_DuskSky.mat";

    // 해: 성문이 +X(아레나 쪽)를 보고 있으므로 +X 쪽에서 낮게 비춰야 흰 성문이 금빛으로 물듦
    private const float SunElevation = 11f;     // 지평선 위 각도 (낮을수록 그림자 길고 붉음)
    private const float SunAzimuth = 25f;       // +X 기준 +Z 쪽으로 틀어진 각도 (정면이면 밋밋해서 살짝 측면)
    private static readonly Color SunColor = new Color(1f, 0.62f, 0.36f);
    private const float SunIntensity = 1.7f;

    // 환경광 (Trilight): 하늘은 보랏빛, 수평은 주황, 바닥은 어두운 갈색 → 그림자 쪽이 차갑게 떨어져 대비가 생김
    private static readonly Color AmbientSky = new Color(0.42f, 0.40f, 0.56f);
    private static readonly Color AmbientEquator = new Color(0.52f, 0.38f, 0.30f);
    private static readonly Color AmbientGround = new Color(0.16f, 0.13f, 0.11f);

    // 안개: 지형 끝(약 100m~)이 안 보이게 서서히 묻힘. 색은 노을 지평선과 비슷하게
    private static readonly Color FogColor = new Color(0.66f, 0.50f, 0.44f);
    private const float FogStart = 25f;
    private const float FogEnd = 170f;

    private const float ShadowDistance = 120f;  // 기본 50m → 숲 그림자까지 보이게

    [MenuItem("Tools/Map/Apply Dusk Lighting")]
    private static void Apply()
    {
        // ---------- 하늘 (Procedural Skybox)
        if (!AssetDatabase.IsValidFolder("Assets/_Project/Materials/Sky"))
            AssetDatabase.CreateFolder("Assets/_Project/Materials", "Sky");
        var sky = AssetDatabase.LoadAssetAtPath<Material>(SkyMatPath);
        if (sky == null)
        {
            sky = new Material(Shader.Find("Skybox/Procedural"));
            AssetDatabase.CreateAsset(sky, SkyMatPath);
        }
        sky.SetFloat("_SunDisk", 2f);                 // High Quality 해 원반
        sky.SetFloat("_SunSize", 0.05f);
        sky.SetFloat("_SunSizeConvergence", 4f);
        sky.SetFloat("_AtmosphereThickness", 1.9f);   // 두꺼울수록 붉은 노을
        sky.SetColor("_SkyTint", new Color(0.60f, 0.50f, 0.62f));
        sky.SetColor("_GroundColor", new Color(0.30f, 0.24f, 0.22f));   // 지평선 아래 = 안개색과 비슷하게
        sky.SetFloat("_Exposure", 1.05f);
        EditorUtility.SetDirty(sky);

        // ---------- 해 (Directional Light)
        Light sun = RenderSettings.sun;
        if (sun == null)
            foreach (Light l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional) { sun = l; break; }
        if (sun == null) { Debug.LogError("[Dusk] Directional Light를 찾을 수 없음"); return; }

        Undo.RecordObject(sun.transform, "Dusk Sun");
        Undo.RecordObject(sun, "Dusk Sun");
        float e = SunElevation * Mathf.Deg2Rad, a = SunAzimuth * Mathf.Deg2Rad;
        var toSun = new Vector3(Mathf.Cos(e) * Mathf.Cos(a), Mathf.Sin(e), Mathf.Cos(e) * Mathf.Sin(a));
        sun.transform.rotation = Quaternion.LookRotation(-toSun);
        sun.color = SunColor;
        sun.intensity = SunIntensity;
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 0.85f;

        // ---------- 씬 렌더 설정 (씬 파일에 저장됨)
        var scene = EditorSceneManager.GetActiveScene();
        RenderSettings.skybox = sky;
        RenderSettings.sun = sun;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = AmbientSky;
        RenderSettings.ambientEquatorColor = AmbientEquator;
        RenderSettings.ambientGroundColor = AmbientGround;
        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;   // 금색 장식에 노을 하늘이 비침
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = FogColor;
        RenderSettings.fogStartDistance = FogStart;
        RenderSettings.fogEndDistance = FogEnd;
        DynamicGI.UpdateEnvironment();

        // ---------- 그림자 거리 (URP 에셋 = 프로젝트 전체 설정)
        if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp && urp.shadowDistance < ShadowDistance)
        {
            Undo.RecordObject(urp, "Shadow Distance");
            urp.shadowDistance = ShadowDistance;
            EditorUtility.SetDirty(urp);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[Dusk] 황혼 조명 적용 완료");
    }
}
