using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Serre.GrabSystem.Editor
{
    [InitializeOnLoad]
    internal static class GrabSystemGestureCheckerLifecycle
    {
        static GrabSystemGestureCheckerLifecycle()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode)
            {
                return;
            }

            var missingTargets = FindMissingGestureCheckerTargets();
            if (missingTargets.Count == 0)
            {
                return;
            }

            var message = missingTargets.Count == 1
                ? $"{GetAvatarName(missingTargets[0])} に共有 GestureChecker がありません。\n\n"
                    + "プレイモード開始前に自動生成してもよいですか？"
                : $"EasyCarry Systemを使用している {missingTargets.Count} 体のアバターに共有 GestureChecker がありません。\n\n"
                    + "プレイモード開始前に自動生成してもよいですか？";

            if (Application.isBatchMode
                || !EditorUtility.DisplayDialog("EasyCarry System", message, "生成する", "キャンセル"))
            {
                CancelPlayMode("GestureChecker がないため、プレイモードへの移行を中止しました。",
                    missingTargets[0]);
                return;
            }

            foreach (var targets in missingTargets)
            {
                if (GrabSystemGestureCheckerEditorUtility.EnsureFor(targets) != null)
                {
                    continue;
                }

                CancelPlayMode("GestureCheckerを生成できなかったため、プレイモードへの移行を中止しました。",
                    targets);
                return;
            }
        }

        private static List<GrabSystemItemReference> FindMissingGestureCheckerTargets()
        {
            var results = new List<GrabSystemItemReference>();
            var avatarRootIds = new HashSet<int>();
            foreach (var targets in Resources.FindObjectsOfTypeAll<GrabSystemItemReference>())
            {
                if (targets == null || EditorUtility.IsPersistent(targets)
                    || !targets.gameObject.scene.IsValid())
                {
                    continue;
                }

                var avatarRoot = GrabSystemGestureCheckerEditorUtility.ResolveAvatarRoot(targets.transform);
                if (avatarRoot == null || !avatarRootIds.Add(avatarRoot.GetInstanceID())
                    || GrabSystemGestureCheckerEditorUtility.FindFor(targets) != null)
                {
                    continue;
                }

                results.Add(targets);
            }

            return results;
        }

        private static string GetAvatarName(GrabSystemItemReference targets)
        {
            var avatarRoot = GrabSystemGestureCheckerEditorUtility.ResolveAvatarRoot(
                targets != null ? targets.transform : null);
            return avatarRoot != null ? avatarRoot.name : "アバター";
        }

        private static void CancelPlayMode(string message, Object context)
        {
            EditorApplication.isPlaying = false;
            Debug.LogError(message, context);
        }
    }
}
