using UnityEditor;
using UnityEngine;
using System.IO;
using System.Text.RegularExpressions;

namespace FPSToolkit.Editor
{
    /// <summary>
    /// Detects Unity StarterAssets FirstPersonController and patches known issues.
    /// Runs automatically on editor load and after asset imports.
    /// </summary>
    public class StarterAssetsPatcher : AssetPostprocessor
    {
        private const string ControllerSearchPattern = "FirstPersonController.cs";

        // The stock bug: threshold is applied to mouse input, eating slow movements.
        // Stock line:   if (_input.look.sqrMagnitude >= _threshold)
        // Fixed line:   if (_input.look.sqrMagnitude >= (IsCurrentDeviceMouse ? 0f : _threshold))
        private const string BuggyPattern =
            "_input.look.sqrMagnitude >= _threshold";
        private const string FixedPattern =
            "_input.look.sqrMagnitude >= (IsCurrentDeviceMouse ? 0f : _threshold)";

        [InitializeOnLoadMethod]
        private static void OnEditorLoad()
        {
            // Delay so the editor is fully ready before we scan.
            EditorApplication.delayCall += ScanAndPatch;
        }

        private static void OnPostprocessAllAssets(
            string[] importedAssets, string[] deletedAssets,
            string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (var asset in importedAssets)
            {
                if (asset.EndsWith(ControllerSearchPattern))
                {
                    PatchFile(asset);
                    return;
                }
            }
        }

        private static void ScanAndPatch()
        {
            // Find all FirstPersonController.cs files in the project.
            string[] guids = AssetDatabase.FindAssets("FirstPersonController t:MonoScript");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(ControllerSearchPattern))
                {
                    PatchFile(path);
                }
            }
        }

        private static void PatchFile(string assetPath)
        {
            string fullPath = Path.GetFullPath(assetPath);
            if (!File.Exists(fullPath))
                return;

            string contents = File.ReadAllText(fullPath);

            // Already patched or doesn't contain the buggy pattern — nothing to do.
            if (contents.Contains(FixedPattern) || !contents.Contains(BuggyPattern))
                return;

            string patched = contents.Replace(BuggyPattern, FixedPattern);
            File.WriteAllText(fullPath, patched);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

            Debug.Log($"[FPS Toolkit] Patched mouse dead-zone bug in {assetPath}");
        }
    }
}
