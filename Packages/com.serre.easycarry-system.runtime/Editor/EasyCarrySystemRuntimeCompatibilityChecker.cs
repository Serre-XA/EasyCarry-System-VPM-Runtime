using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Serre.EasyCarrySystem.Editor
{
    [InitializeOnLoad]
    internal static class EasyCarrySystemRuntimeCompatibilityWarning
    {
        private const string RuntimePackageName = "com.serre.easycarry-system.runtime";
        private const string CurrentAdvancedWarningTypeName =
            "Serre.EasyCarrySystem.Editor.EasyCarrySystemAdvancedCompatibilityWarning, "
            + "Serre.EasyCarrySystem.Advanced.Compatibility.Editor";
        private const string AdvancedEditorTypeName =
            "Serre.EasyCarrySystem.Editor.EasyCarrySystemItemReferenceAuthoringEditor, "
            + "Serre.EasyCarrySystem.Authoring.Editor";
        private const string AdvancedVersionFieldName = "AdvancedVersion";
        private const string ItemReferenceTypeName =
            "Serre.EasyCarrySystem.EasyCarrySystemItemReference";
        private const string SessionKeyPrefix =
            "Serre.EasyCarrySystem.RuntimeCompatibilityWarning.";

        private static readonly Regex AdvancedVersionPattern = new Regex(
            "AdvancedVersion\\s*=\\s*\"(?<version>[^\"]+)\"",
            RegexOptions.CultureInvariant);

        private static bool versionCached;
        private static string runtimeVersion;
        private static string advancedVersion;
        private static GUIStyle errorStyle;

        static EasyCarrySystemRuntimeCompatibilityWarning()
        {
            UnityEditor.Editor.finishedDefaultHeaderGUI += DrawVersionWarning;
            EditorApplication.projectChanged += ClearVersionCache;
            EditorApplication.delayCall += CheckOnEditorStartup;
        }

        private static void CheckOnEditorStartup()
        {
            if (Type.GetType(CurrentAdvancedWarningTypeName, false) != null)
            {
                return;
            }

            EnsureVersionCached();
            if (string.IsNullOrEmpty(advancedVersion)
                || string.Equals(runtimeVersion, advancedVersion, StringComparison.Ordinal))
            {
                return;
            }

            var runtimeVersionLabel = string.IsNullOrEmpty(runtimeVersion)
                ? "不明"
                : runtimeVersion;
            var sessionKey = SessionKeyPrefix + runtimeVersionLabel + "." + advancedVersion;
            if (SessionState.GetBool(sessionKey, false))
            {
                return;
            }

            SessionState.SetBool(sessionKey, true);
            var message = CreateVersionMismatchMessage(runtimeVersionLabel);
            Debug.LogError("[EasyCarry System] " + message.Replace('\n', ' '));
            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog("EasyCarry System バージョン不一致", message, "OK");
            }
        }

        private static void DrawVersionWarning(UnityEditor.Editor editor)
        {
            if (editor == null
                || editor.target == null
                || editor.target.GetType().FullName != ItemReferenceTypeName
                || Type.GetType(CurrentAdvancedWarningTypeName, false) != null)
            {
                return;
            }

            EnsureVersionCached();
            if (string.IsNullOrEmpty(advancedVersion)
                || string.Equals(runtimeVersion, advancedVersion, StringComparison.Ordinal))
            {
                return;
            }

            var runtimeVersionLabel = string.IsNullOrEmpty(runtimeVersion)
                ? "不明"
                : runtimeVersion;
            EditorGUILayout.Space(2f);
            EditorGUILayout.LabelField(
                "エラー: " + CreateVersionMismatchMessage(runtimeVersionLabel),
                GetErrorStyle());
            EditorGUILayout.Space(2f);
        }

        private static void EnsureVersionCached()
        {
            if (versionCached)
            {
                return;
            }

            runtimeVersion = FindRuntimeVersion();
            advancedVersion = FindAdvancedVersion();
            versionCached = true;
        }

        private static string FindRuntimeVersion()
        {
            foreach (var package in UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages())
            {
                if (package.name == RuntimePackageName)
                {
                    return package.version;
                }
            }

            return null;
        }

        private static string FindAdvancedVersion()
        {
            var advancedEditorType = Type.GetType(AdvancedEditorTypeName, false);
            var versionField = advancedEditorType?.GetField(
                AdvancedVersionFieldName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (versionField?.GetRawConstantValue() is string loadedVersion)
            {
                return loadedVersion;
            }

            foreach (var guid in AssetDatabase.FindAssets(
                         "EasyCarrySystemItemReferenceAuthoringEditor t:MonoScript"))
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
                if (string.IsNullOrEmpty(assetPath) || string.IsNullOrEmpty(projectRoot))
                {
                    continue;
                }

                var fullPath = Path.GetFullPath(Path.Combine(projectRoot, assetPath));
                if (!File.Exists(fullPath))
                {
                    continue;
                }

                try
                {
                    var match = AdvancedVersionPattern.Match(File.ReadAllText(fullPath));
                    if (match.Success)
                    {
                        return match.Groups["version"].Value;
                    }
                }
                catch (IOException)
                {
                    // Another editor process may be updating imported Advanced files.
                }
                catch (UnauthorizedAccessException)
                {
                    // Treat an unreadable source as an unknown Advanced installation.
                }
            }

            return null;
        }

        private static void ClearVersionCache()
        {
            versionCached = false;
            runtimeVersion = null;
            advancedVersion = null;
        }

        private static string CreateVersionMismatchMessage(string runtimeVersionLabel)
        {
            return "EasyCarry Systemのバージョンが一致していません。\n"
                + $"Basic: {runtimeVersionLabel} / Advanced: {advancedVersion}\n"
                + "BasicとAdvancedを同じバージョンへ更新してください。";
        }

        private static GUIStyle GetErrorStyle()
        {
            if (errorStyle != null)
            {
                return errorStyle;
            }

            errorStyle = new GUIStyle(EditorStyles.wordWrappedLabel)
            {
                fontStyle = FontStyle.Bold
            };
            errorStyle.normal.textColor = Color.red;
            return errorStyle;
        }
    }
}
