using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Tools/Models/Setup Merchant : 상인 NPC 모델(Medieval Civilian 3, Mixamo 리그) 임포트 설정
// - 노멀 맵 텍스처 타입 지정. SpecGloss 텍스처는 RGB = 스페큘러, A = 광택(원본 gloss 맵을 합친 것)
// - URP Lit(Specular 워크플로) 머티리얼 7개 생성 → FBX 머티리얼 슬롯에 리맵
// - 머리카락·수염·콧수염·몸(속눈썹)은 알파 클리핑, 머리카락·수염은 양면
// - FBX는 크기 0.01배(원본이 100배 크게 내보내짐) + Humanoid 아바타로 (플레이어 기사·보스와 같은 Mixamo 리그 → 기존 클립 재사용 가능)
// 여러 번 실행해도 안전: 머티리얼은 있으면 값만 다시 설정
public static class MerchantSetup
{
    private const string Root = "Assets/_Project/Models/Merchant/";
    private const string FbxPath = Root + "Merchant.fbx";

    // FBX 안의 머티리얼 슬롯 이름 = 텍스처 파일 이름의 부위 이름
    private static readonly string[] Parts = { "Top", "Bottom", "Body", "Shoes", "Hair", "Beard", "Moustache" };
    private static readonly HashSet<string> CutoutParts = new HashSet<string> { "Hair", "Beard", "Moustache", "Body" };
    private static readonly HashSet<string> TwoSidedParts = new HashSet<string> { "Hair", "Beard", "Moustache" };

    [MenuItem("Tools/Models/Setup Merchant")]
    private static void Run()
    {
        var importer = AssetImporter.GetAtPath(FbxPath) as ModelImporter;
        if (importer == null)
        {
            Debug.LogError($"[MerchantSetup] FBX를 찾지 못함: {FbxPath}");
            return;
        }

        // 1) 노멀 맵 텍스처 타입
        foreach (string part in Parts)
        {
            var normal = AssetImporter.GetAtPath(TexturePath(part, "Normal")) as TextureImporter;
            if (normal != null && normal.textureType != TextureImporterType.NormalMap)
            {
                normal.textureType = TextureImporterType.NormalMap;
                normal.SaveAndReimport();
            }
        }

        // 2) 머티리얼 (URP Lit, Specular 워크플로)
        if (!AssetDatabase.IsValidFolder(Root + "Materials"))
            AssetDatabase.CreateFolder(Root.TrimEnd('/'), "Materials");

        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        var materials = new Dictionary<string, Material>();
        foreach (string part in Parts)
        {
            string path = $"{Root}Materials/M_Merchant_{part}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(lit);
                AssetDatabase.CreateAsset(mat, path);
            }
            Configure(mat, lit, part);
            EditorUtility.SetDirty(mat);
            materials[part] = mat;
        }
        AssetDatabase.SaveAssets();

        // 3) FBX: Humanoid 아바타 + 머티리얼 리맵 (애니메이션·카메라·라이트는 가져오지 않음)
        // 원본이 cm 단위 수치를 m 단위 파일로 내보내서 100배 큼 (키 약 180m) → 0.01배로 1.8m
        importer.globalScale = 0.01f;
        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation = false;
        importer.importCameras = false;
        importer.importLights = false;
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        foreach (var pair in materials)
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), pair.Key), pair.Value);
        importer.SaveAndReimport();

        Debug.Log("[MerchantSetup] 완료 — Merchant.fbx 아바타(Humanoid)와 머티리얼 7개 설정");
    }

    private static void Configure(Material mat, Shader lit, string part)
    {
        if (mat.shader != lit) mat.shader = lit;

        // Specular 워크플로 (원본이 diffuse / specular / gloss 구성이라)
        mat.SetFloat("_WorkflowMode", 0f);
        mat.EnableKeyword("_SPECULAR_SETUP");

        mat.SetTexture("_BaseMap", Load(part, "Diffuse"));
        mat.SetColor("_BaseColor", Color.white);

        mat.SetTexture("_SpecGlossMap", Load(part, "SpecGloss"));
        mat.SetColor("_SpecColor", Color.white);
        mat.SetFloat("_Smoothness", 1f);                 // 맵 알파(광택)에 곱해지는 배율
        mat.SetFloat("_SmoothnessTextureChannel", 0f);   // 0 = 스페큘러 맵의 알파에서 광택을 읽음
        mat.EnableKeyword("_METALLICSPECGLOSSMAP");

        mat.SetTexture("_BumpMap", Load(part, "Normal"));
        mat.SetFloat("_BumpScale", 1f);
        mat.EnableKeyword("_NORMALMAP");

        bool cutout = CutoutParts.Contains(part);
        mat.SetFloat("_AlphaClip", cutout ? 1f : 0f);
        mat.SetFloat("_Cutoff", 0.5f);
        if (cutout) mat.EnableKeyword("_ALPHATEST_ON");
        else mat.DisableKeyword("_ALPHATEST_ON");
        mat.SetOverrideTag("RenderType", cutout ? "TransparentCutout" : "Opaque");
        mat.renderQueue = cutout ? (int)RenderQueue.AlphaTest : (int)RenderQueue.Geometry;

        mat.SetFloat("_Cull", TwoSidedParts.Contains(part) ? (float)CullMode.Off : (float)CullMode.Back);
    }

    private static string TexturePath(string part, string kind) => $"{Root}Textures/Merchant_{part}_{kind}.png";

    private static Texture2D Load(string part, string kind)
    {
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath(part, kind));
        if (tex == null) Debug.LogWarning($"[MerchantSetup] 텍스처 없음: {TexturePath(part, kind)}");
        return tex;
    }
}
