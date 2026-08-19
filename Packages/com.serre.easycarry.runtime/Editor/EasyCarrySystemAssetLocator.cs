using UnityEditor;
using UnityEngine;

namespace Serre.EasyCarrySystem.Editor
{
    public static class EasyCarrySystemAssetLocator
    {
        private const string RootMarkerGuid = "4d8c0e51f3ac4fa7a48d9b9af447c916";
        private static bool markerMissingLogged;

        public static string RootPath
        {
            get
            {
                var markerPath = AssetDatabase.GUIDToAssetPath(RootMarkerGuid);
                if (!string.IsNullOrEmpty(markerPath))
                {
                    markerMissingLogged = false;
                    var separatorIndex = markerPath.LastIndexOf('/');
                    return separatorIndex >= 0 ? markerPath.Substring(0, separatorIndex) : string.Empty;
                }

                if (!markerMissingLogged)
                {
                    Debug.LogError(
                        "EasyCarry System root marker was not found. "
                        + "Keep EasyCarrySystemRootMarker.txt and its .meta file when moving or packaging EasyCarry System.");
                    markerMissingLogged = true;
                }

                return string.Empty;
            }
        }

        public static string GetAssetPath(string relativePath)
        {
            var rootPath = RootPath;
            if (string.IsNullOrEmpty(rootPath) || string.IsNullOrEmpty(relativePath))
            {
                return string.Empty;
            }

            return $"{rootPath}/{relativePath.Replace('\\', '/').TrimStart('/')}";
        }

        public static T LoadAsset<T>(string relativePath) where T : Object
        {
            var assetPath = GetAssetPath(relativePath);
            return string.IsNullOrEmpty(assetPath)
                ? null
                : AssetDatabase.LoadAssetAtPath<T>(assetPath);
        }
    }
}
