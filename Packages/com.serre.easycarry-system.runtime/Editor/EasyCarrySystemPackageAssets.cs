using UnityEditor;
using UnityEngine;

namespace Serre.EasyCarrySystem.Editor
{
    internal static class EasyCarrySystemPackageAssets
    {
        internal const string RootPath = "Packages/com.serre.easycarry-system.runtime";

        internal static string GetAssetPath(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
            {
                return string.Empty;
            }

            return $"{RootPath}/{relativePath.Replace(System.IO.Path.DirectorySeparatorChar, '/').TrimStart('/')}";
        }

        internal static T LoadAsset<T>(string relativePath) where T : Object
        {
            var assetPath = GetAssetPath(relativePath);
            return string.IsNullOrEmpty(assetPath)
                ? null
                : AssetDatabase.LoadAssetAtPath<T>(assetPath);
        }
    }
}
