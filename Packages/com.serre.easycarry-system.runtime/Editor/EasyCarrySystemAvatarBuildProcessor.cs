#if VRC_SDK_VRCSDK3
using System.Collections.Generic;
using nadena.dev.modular_avatar.core;
using UnityEditor;
using UnityEngine;
using VRC.Dynamics;
using VRC.SDK3.Dynamics.Constraint.Components;
using VRC.SDKBase.Editor.BuildPipeline;

namespace Serre.EasyCarrySystem.Editor
{
    internal sealed class EasyCarrySystemAvatarBuildProcessor : IVRCSDKPreprocessAvatarCallback
    {
        public int callbackOrder => int.MinValue;

        public bool OnPreprocessAvatar(GameObject avatarGameObject)
        {
            if (avatarGameObject == null)
            {
                return true;
            }

            var targets = avatarGameObject.GetComponentsInChildren<EasyCarrySystemItemReference>(true);
            if (targets.Length == 0)
            {
                var orphanedGestureSettings = avatarGameObject.GetComponentsInChildren<EasyCarrySystemGestureSettings>(true);
                foreach (var settings in orphanedGestureSettings)
                {
                    if (settings != null)
                    {
                        Object.DestroyImmediate(settings.gameObject);
                    }
                }

                return true;
            }

            if (!EasyCarrySystemGestureCheckerEditorUtility.ValidateForAvatar(avatarGameObject, targets.Length))
            {
                if (!Application.isBatchMode)
                {
                    EditorUtility.DisplayDialog(
                        "EasyCarry System ビルドエラー",
                        "共有 GestureChecker が見つからないか、複数存在しています。\n"
                        + "アバター直下に1つだけ生成してから、再度ビルドしてください。",
                        "OK");
                }

                return false;
            }

            var gestureSettings = avatarGameObject.GetComponentsInChildren<EasyCarrySystemGestureSettings>(true);
            if (!ValidateItemBoneProxies(avatarGameObject, targets))
            {
                return false;
            }
            foreach (var target in targets)
            {
                if (target != null)
                {
                    EasyCarrySystemSlotEditorUtility.ApplyStoredSettings(target, true);
                    EasyCarrySystemEditorSharedUtility.SetMainConstraintSourceWeights(
                        target.EasyCarrySystemRoot,
                        "AP_00",
                        false);
                    EasyCarrySystemEditorSharedUtility.PrepareForAvatarBuild(target);
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

        private static bool ValidateItemBoneProxies(
            GameObject avatarGameObject,
            EasyCarrySystemItemReference[] targets)
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

                if (target.GeneratedEasyCarrySystem == null)
                {
                    var itemPath = AnimationUtility.CalculateTransformPath(
                        target.transform,
                        avatarGameObject.transform);
                    invalidPaths.Add($"{itemPath}: 生成されたEasyCarry Systemがありません");
                    continue;
                }

                var ciRoot = EasyCarrySystemEditorSharedUtility.FindChildRecursive(
                    target.EasyCarrySystemRoot,
                    "CI_Root");
                if (ciRoot == null)
                {
                    var itemPath = AnimationUtility.CalculateTransformPath(
                        target.transform,
                        avatarGameObject.transform);
                    invalidPaths.Add($"{itemPath}: CI_Rootがありません");
                    continue;
                }

                ValidateItemConstraintSource<VRCParentConstraint>(
                    target,
                    ciRoot,
                    "VRC Parent Constraint",
                    avatarGameObject.transform,
                    invalidPaths);
                ValidateItemConstraintSource<VRCScaleConstraint>(
                    target,
                    ciRoot,
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
                "EasyCarry System のビルドを中止しました。\n\n"
                + "EasyCarry Systemが未生成、追従用Constraintが不正、または制御対象アイテムにMA Bone Proxyが含まれています。\n"
                + "EasyCarry System Setupとアイテム階層を確認してから、再度ビルドしてください。\n\n"
                + string.Join("\n", invalidPaths);

            Debug.LogError(message, firstBoneProxy != null ? firstBoneProxy : avatarGameObject);
            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog(
                    "EasyCarry System ビルドエラー",
                    message,
                    "OK");
            }

            return false;
        }

        private static void ValidateItemConstraintSource<TConstraint>(
            EasyCarrySystemItemReference target,
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
            invalidPaths.Add($"{itemPath}: {constraintName}のCI_Root参照が正しくありません");
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
