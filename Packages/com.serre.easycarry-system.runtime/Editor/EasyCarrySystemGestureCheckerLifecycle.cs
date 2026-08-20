using UnityEditor;
using UnityEngine;

namespace Serre.EasyCarrySystem.Editor
{
    [InitializeOnLoad]
    internal static class EasyCarrySystemGestureCheckerLifecycle
    {
        static EasyCarrySystemGestureCheckerLifecycle()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode)
            {
                return;
            }

            var missingTargets = EasyCarrySystemGestureCheckerEditorUtility.FindMissingForLoadedAvatars();
            if (missingTargets.Count == 0)
            {
                return;
            }

            var message = missingTargets.Count == 1
                ? $"{EasyCarrySystemGestureCheckerEditorUtility.GetAvatarName(missingTargets[0])} に共有 GestureChecker がありません。\n\n"
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
                if (EasyCarrySystemGestureCheckerEditorUtility.EnsureFor(targets) != null)
                {
                    continue;
                }

                CancelPlayMode("GestureCheckerを生成できなかったため、プレイモードへの移行を中止しました。",
                    targets);
                return;
            }
        }

        private static void CancelPlayMode(string message, Object context)
        {
            EditorApplication.isPlaying = false;
            Debug.LogError(message, context);
        }
    }
}
