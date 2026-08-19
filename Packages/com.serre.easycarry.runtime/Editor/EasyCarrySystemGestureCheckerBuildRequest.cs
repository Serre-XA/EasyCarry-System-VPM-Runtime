#if VRC_SDK_VRCSDK3
using UnityEditor;
using UnityEngine;
using VRC.SDKBase.Editor.BuildPipeline;

namespace Serre.EasyCarrySystem.Editor
{
    internal sealed class EasyCarrySystemGestureCheckerBuildRequest : IVRCSDKBuildRequestedCallback
    {
        public int callbackOrder => int.MinValue;

        public bool OnBuildRequested(VRCSDKRequestedBuildType requestedBuildType)
        {
            if (requestedBuildType != VRCSDKRequestedBuildType.Avatar)
            {
                return true;
            }

            var missingTargets = EasyCarrySystemGestureCheckerEditorUtility.FindMissingForLoadedAvatars();
            if (missingTargets.Count == 0)
            {
                return true;
            }

            var message = missingTargets.Count == 1
                ? $"{EasyCarrySystemGestureCheckerEditorUtility.GetAvatarName(missingTargets[0])} に共有 GestureChecker がありません。\n\n"
                    + "ビルド開始前に生成してもよいですか？"
                : $"EasyCarry Systemを使用している {missingTargets.Count} 体のアバターに共有 GestureChecker がありません。\n\n"
                    + "ビルド開始前に生成してもよいですか？";

            if (Application.isBatchMode
                || !EditorUtility.DisplayDialog(
                    "EasyCarry System",
                    message,
                    "生成してビルド",
                    "ビルドを中止"))
            {
                Debug.LogError(
                    "GestureChecker がないため、EasyCarry Systemのビルドを中止しました。",
                    missingTargets[0]);
                return false;
            }

            foreach (var targets in missingTargets)
            {
                var settings = EasyCarrySystemGestureCheckerEditorUtility.EnsureFor(targets);
                if (settings != null
                    && EasyCarrySystemGestureCheckerEditorUtility.FindFor(targets) != null)
                {
                    continue;
                }

                Debug.LogError(
                    "GestureCheckerを生成できなかったため、EasyCarry Systemのビルドを中止しました。",
                    targets);
                return false;
            }

            Debug.Log("共有 GestureCheckerを生成しました。ビルドを続行します。", missingTargets[0]);
            return true;
        }
    }
}
#endif
