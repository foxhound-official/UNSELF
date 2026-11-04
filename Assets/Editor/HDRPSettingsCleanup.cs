
using UnityEditor;
using UnityEngine;

public static class HDRPSettingsCleanup
{
    [MenuItem("Tools/UNSELF/HDRP/Remove Missing URP Resource")]
    private static void RemoveMissingURPResource()
    {
        Object asset = Selection.activeObject;

        if (asset == null || asset.GetType().Name != "HDRenderPipelineGlobalSettings")
        {
            Debug.LogWarning("Select the HDRenderPipelineGlobalSettings asset first.");
            return;
        }

        var missingReferences = SerializationUtility.GetManagedReferencesWithMissingTypes(asset);
        int removed = 0;

        foreach (var reference in missingReferences)
        {
            // Удаляем исключительно ссылку на ресурс старого URP.
            if (reference.className != "MipGenRenderPipelineRuntimeResources" ||
                reference.assemblyName != "Unity.RenderPipelines.Universal.Runtime")
                continue;

            bool confirmed = EditorUtility.DisplayDialog(
                "Remove Missing URP Resource",
                "Remove the obsolete URP resource reference from HDRP Global Settings?",
                "Remove",
                "Cancel"
            );

            if (!confirmed)
                return;

            Undo.RecordObject(asset, "Remove Missing URP Resource");

            if (SerializationUtility.ClearManagedReferenceWithMissingType(asset, reference.referenceId))
                removed++;
        }

        if (removed > 0)
        {
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
        }

        Debug.Log($"HDRP cleanup: removed {removed} obsolete URP references.");
    }
}
