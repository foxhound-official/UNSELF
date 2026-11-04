
using UnityEditor;
using UnityEngine;

public static class URPToHDRPMaterialConverter
{
    private const string MaterialFolder = "Assets/Material";
    private const string UrpShaderName = "Universal Render Pipeline/Lit";
    private const string HdrpShaderName = "HDRP/Lit";

    [MenuItem("Tools/UNSELF/HDRP/Convert URP Lit Materials")]
    private static void ConvertMaterials()
    {
        Shader hdrpShader = Shader.Find(HdrpShaderName);

        if (hdrpShader == null)
        {
            Debug.LogError("HDRP/Lit shader not found.");
            return;
        }

        if (!EditorUtility.DisplayDialog(
            "UNSELF HDRP Migration",
            "Convert opaque URP Lit materials in Assets/Material to HDRP Lit?",
            "Convert",
            "Cancel"))
            return;

        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { MaterialFolder });

        int converted = 0;
        int skipped = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material == null || material.shader == null)
                continue;

            if (material.shader.name != UrpShaderName)
                continue;

            // Прозрачные материалы и Alpha Clipping переносим отдельно.
            bool isTransparent = material.HasProperty("_Surface") && material.GetFloat("_Surface") > 0.5f;
            bool usesAlphaClip = material.HasProperty("_AlphaClip") && material.GetFloat("_AlphaClip") > 0.5f;

            if (isTransparent || usesAlphaClip)
            {
                Debug.Log($"HDRP migration skipped: {path}");
                skipped++;
                continue;
            }

            Color baseColor = material.GetColor("_BaseColor");
            float metallic = material.GetFloat("_Metallic");
            float smoothness = material.GetFloat("_Smoothness");

            Texture baseTexture = material.GetTexture("_BaseMap");
            Vector2 textureScale = material.GetTextureScale("_BaseMap");
            Vector2 textureOffset = material.GetTextureOffset("_BaseMap");

            Texture normalTexture = material.GetTexture("_BumpMap");
            float normalScale = material.GetFloat("_BumpScale");

            Undo.RecordObject(material, "Convert Material to HDRP");

            material.shader = hdrpShader;

            material.SetColor("_BaseColor", baseColor);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", smoothness);

            if (baseTexture != null)
            {
                material.SetTexture("_BaseColorMap", baseTexture);
                material.SetTextureScale("_BaseColorMap", textureScale);
                material.SetTextureOffset("_BaseColorMap", textureOffset);
            }

            if (normalTexture != null)
            {
                material.SetTexture("_NormalMap", normalTexture);
                material.SetFloat("_NormalScale", normalScale);
            }

            EditorUtility.SetDirty(material);
            converted++;
        }

        AssetDatabase.SaveAssets();

        Debug.Log($"UNSELF HDRP Migration: converted {converted}, skipped {skipped} materials.");
    }
}
