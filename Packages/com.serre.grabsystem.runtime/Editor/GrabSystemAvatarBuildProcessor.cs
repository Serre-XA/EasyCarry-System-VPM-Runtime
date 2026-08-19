#if VRC_SDK_VRCSDK3
using System.Collections.Generic;
using nadena.dev.modular_avatar.core;
using UnityEditor;
using UnityEngine;
using VRC.Dynamics;
using VRC.SDK3.Dynamics.Constraint.Components;
using VRC.SDKBase.Editor.BuildPipeline;

namespace Serre.GrabSystem.Editor
{
    internal sealed class GrabSystemAvatarBuildProcessor : IVRCSDKPreprocessAvatarCallback
    {
        public int callbackOrder => int.MinValue;

        public bool OnPreprocessAvatar(GameObject avatarGameObject)
        {
            if (avatarGameObject == null)
            {
                return true;
            }

            var targets = avatarGameObject.GetComponentsInChildren<GrabSystemItemReference>(true);
            var gestureSettings = avatarGameObject.GetComponentsInChildren<GrabSystemGestureSettings>(true);
            if (targets.Length == 0)
            {
                foreach (var settings in gestureSettings)
                {
                    if (settings != null)
                    {
                        Object.DestroyImmediate(settings.gameObject);
                    }
                }

                return true;
            }

            if (!EnsureGestureCheckerForBuild(avatarGameObject, targets))
            {
                return false;
            }

            gestureSettings = avatarGameObject.GetComponentsInChildren<GrabSystemGestureSettings>(true);

            if (!ValidateItemBoneProxies(avatarGameObject, targets))
            {
                return false;
            }

            if (!GrabSystemGestureCheckerEditorUtility.ValidateForAvatar(avatarGameObject, targets.Length))
            {
                return false;
            }

            foreach (var target in targets)
            {
                if (target != null)
                {
                    GrabSystemSlotEditorUtility.ApplyStoredSettings(target, true);
                    GrabSystemEditorSharedUtility.SetMainConstraintSourceWeights(
                        target.GrabSystemRoot,
                        "AP_00",
                        false);
                    GrabSystemEditorSharedUtility.PrepareForAvatarBuild(target);
                    Object.DestroyImmediate(target);
                }
            }


            foreach (var settings in gestureSettings)
            {
                if (settings != null)
                {
                    Object.DestroyImmediate(settings);
                }
            }

            return true;
        }

        private static bool EnsureGestureCheckerForBuild(
            GameObject avatarGameObject,
            GrabSystemItemReference[] targets)
        {
            if (targets == null || targets.Length == 0
                || GrabSystemGestureCheckerEditorUtility.FindFor(targets[0]) != null)
            {
                return true;
            }

            var message = $"{avatarGameObject.name} に共有 GestureChecker がありません。\n\n"
                + "ビルド開始前に自動生成してもよいですか？";
            if (Application.isBatchMode
                || !EditorUtility.DisplayDialog("GrabSystem", message, "生成して続行", "ビルドを中止"))
            {
                Debug.LogError(
                    "GestureChecker がないため、GrabSystemのビルドを中止しました。",
                    avatarGameObject);
                return false;
            }

            var settings = GrabSystemGestureCheckerEditorUtility.EnsureFor(targets[0]);
            if (settings != null)
            {
                Debug.Log("共有 GestureChecker を自動生成しました。", settings);
                return true;
            }

            Debug.LogError(
                "GestureCheckerを生成できなかったため、GrabSystemのビルドを中止しました。",
                avatarGameObject);
            return false;
        }

        private static bool ValidateItemBoneProxies(
            GameObject avatarGameObject,
            GrabSystemItemReference[] targets)
        {
            var invalidPaths = new List<string>();
            var foundBoneProxyIds = new HashSet<int>();
            ModularAvatarBoneProxy firstBoneProxy = null;
            foreach (var target in targets)
            {
                if (target == null)
                {
                    continue;
                }

                if (target.GeneratedGrabSystem == null)
                {
                    var itemPath = AnimationUtility.CalculateTransformPath(
                        target.transform,
                        avatarGameObject.transform);
                    invalidPaths.Add($"{itemPath}: 生成されたGrabSystemがありません");
                    continue;
                }

                var giRoot = GrabSystemEditorSharedUtility.FindChildRecursive(
                    target.GrabSystemRoot,
                    "GI_Root");
                if (giRoot == null)
                {
                    var itemPath = AnimationUtility.CalculateTransformPath(
                        target.transform,
                        avatarGameObject.transform);
                    invalidPaths.Add($"{itemPath}: GI_Rootがありません");
                    continue;
                }

                ValidateItemConstraintSource<VRCParentConstraint>(
                    target,
                    giRoot,
                    "VRC Parent Constraint",
                    avatarGameObject.transform,
                    invalidPaths);
                ValidateItemConstraintSource<VRCScaleConstraint>(
                    target,
                    giRoot,
                    "VRC Scale Constraint",
                    avatarGameObject.transform,
                    invalidPaths);

                foreach (var boneProxy in target.GetComponentsInChildren<ModularAvatarBoneProxy>(true))
                {
                    AddInvalidBoneProxy(
                        boneProxy,
                        avatarGameObject.transform,
                        invalidPaths,
                        foundBoneProxyIds,
                        ref firstBoneProxy);
                }
            }

            if (invalidPaths.Count == 0)
            {
                return true;
            }

            var message =
                "GrabSystem のビルドを中止しました。\n\n"
                + "GrabSystemが未生成、追従用Constraintが不正、または制御対象アイテムにMA Bone Proxyが含まれています。\n"
                + "GrabSystem Setupとアイテム階層を確認してから、再度ビルドしてください。\n\n"
                + string.Join("\n", invalidPaths);

            Debug.LogError(message, firstBoneProxy != null ? firstBoneProxy : avatarGameObject);
            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog(
                    "GrabSystem ビルドエラー",
                    message,
                    "OK");
            }

            return false;
        }

        private static void ValidateItemConstraintSource<TConstraint>(
            GrabSystemItemReference target,
            Transform expectedSource,
            string constraintName,
            Transform avatarTransform,
            ICollection<string> invalidPaths)
            where TConstraint : VRCConstraintBase
        {
            var constraint = target.GetComponent<TConstraint>();
            if (constraint != null && constraint.Sources.Count > 0
                && constraint.Sources[0].SourceTransform == expectedSource)
            {
                return;
            }

            var itemPath = AnimationUtility.CalculateTransformPath(
                target.transform,
                avatarTransform);
            invalidPaths.Add($"{itemPath}: {constraintName}のGI_Root参照が正しくありません");
        }

        private static void AddInvalidBoneProxy(
            ModularAvatarBoneProxy boneProxy,
            Transform avatarTransform,
            ICollection<string> invalidPaths,
            ISet<int> foundBoneProxyIds,
            ref ModularAvatarBoneProxy firstBoneProxy)
        {
            if (boneProxy == null || !foundBoneProxyIds.Add(boneProxy.GetInstanceID()))
            {
                return;
            }

            if (firstBoneProxy == null)
            {
                firstBoneProxy = boneProxy;
            }

            invalidPaths.Add(AnimationUtility.CalculateTransformPath(
                boneProxy.transform,
                avatarTransform));
        }

    }
}
#endif
