#if VRC_SDK_VRCSDK3
using System.Collections.Generic;
using nadena.dev.modular_avatar.core;
using nadena.dev.ndmf;
using nadena.dev.ndmf.builtin;
using nadena.dev.ndmf.localization;
using UnityEditor;
using UnityEngine;
using VRC.Dynamics;
using VRC.SDK3.Dynamics.Constraint.Components;

[assembly: ExportsPlugin(typeof(Serre.EasyCarrySystem.Editor.EasyCarrySystemBuildPlugin))]

namespace Serre.EasyCarrySystem.Editor
{
    internal sealed class EasyCarrySystemBuildPlugin : Plugin<EasyCarrySystemBuildPlugin>
    {
        private const string ModularAvatarPluginName = "nadena.dev.modular-avatar";

        public override string QualifiedName => "com.serre.easycarry-system";
        public override string DisplayName => "EasyCarry System";

        protected override void Configure()
        {
            InPhase(BuildPhase.Resolving)
                .Run("EasyCarry Systemの検証とビルド準備", Execute)
                .BeforePass(RemoveEditorOnlyPass.Instance)
                .BeforePlugin(ModularAvatarPluginName);
        }

        private static void Execute(BuildContext context)
        {
            var avatarGameObject = context.AvatarRootObject;
            if (avatarGameObject == null)
            {
                return;
            }

            var allTargets = avatarGameObject.GetComponentsInChildren<EasyCarrySystemItemReference>(true);
            if (allTargets.Length == 0)
            {
                return;
            }

            if (!ValidateEditorOnlyStates(avatarGameObject, allTargets))
            {
                return;
            }

            var buildTargets = new List<EasyCarrySystemItemReference>();
            foreach (var target in allTargets)
            {
                if (target != null
                    && EasyCarrySystemEditorSharedUtility.GetEditorOnlyState(target)
                    != EasyCarrySystemEditorOnlyState.Both)
                {
                    buildTargets.Add(target);
                }
            }

            var targets = buildTargets.ToArray();
            if (targets.Length == 0)
            {
                return;
            }

            if (!ValidateUniqueSlots(avatarGameObject, targets))
            {
                return;
            }

            if (!EasyCarrySystemGestureCheckerEditorUtility.ValidateMenuRootForAvatar(
                    avatarGameObject,
                    targets.Length))
            {
                ReportBuildError(
                    "EasyCarry Systemの共有メニューが見つからないか、複数存在しています。\n"
                    + "アバターの子階層に共有メニューを1つだけ配置してから、再度ビルドしてください。",
                    avatarGameObject);
                return;
            }

            var gestureSettings = avatarGameObject.GetComponentsInChildren<EasyCarrySystemGestureSettings>(true);
            if (!ValidateItemBoneProxies(avatarGameObject, targets))
            {
                return;
            }

            foreach (var target in targets)
            {
                if (target == null)
                {
                    continue;
                }

                EasyCarrySystemSlotEditorUtility.ApplyStoredSettings(target, true);
                var itemGestureSettings = EasyCarrySystemGestureCheckerEditorUtility.FindSettingsFor(target);
                if (itemGestureSettings == null
                    || !EasyCarrySystemGestureCheckerEditorUtility.ApplyParameterDefaults(itemGestureSettings))
                {
                    ReportBuildError(
                        $"{target.name} の握り判定設定またはMA Parametersが正しくありません。",
                        target);
                    return;
                }

                EasyCarrySystemEditorSharedUtility.SetMainConstraintSourceWeights(
                    target.EasyCarrySystemRoot,
                    "AP_00",
                    false);
                EasyCarrySystemEditorSharedUtility.PrepareForAvatarBuild(target);
            }

            // Re-resolve after all settings have been restored, including Play Mode builds.
            foreach (var target in targets)
            {
                var error = EasyCarrySystemEditorSharedUtility.RepairAndValidateScaleLinksForBuild(target);
                if (error == null) continue;
                ReportBuildError(error, target);
                return;
            }

            // Keep all item ownership references alive until every scale link is resolved.
            foreach (var target in targets)
            {
                if (target != null) Object.DestroyImmediate(target);
            }

            foreach (var settings in gestureSettings)
            {
                if (settings != null)
                {
                    Object.DestroyImmediate(settings);
                }
            }
        }

        private static bool ValidateEditorOnlyStates(
            GameObject avatarGameObject,
            EasyCarrySystemItemReference[] targets)
        {
            var invalidPaths = new List<string>();
            EasyCarrySystemItemReference firstInvalidTarget = null;
            foreach (var target in targets)
            {
                if (target == null)
                {
                    continue;
                }

                var state = EasyCarrySystemEditorSharedUtility.GetEditorOnlyState(target);
                if (state != EasyCarrySystemEditorOnlyState.ItemOnly
                    && state != EasyCarrySystemEditorOnlyState.GeneratedSystemOnly)
                {
                    continue;
                }

                var itemPath = AnimationUtility.CalculateTransformPath(
                    target.transform,
                    avatarGameObject.transform);
                var detail = state == EasyCarrySystemEditorOnlyState.ItemOnly
                    ? "制御対象アイテムだけがEditorOnlyです。生成されたEasyCarry SystemもEditorOnlyにしてください"
                    : "生成されたEasyCarry SystemだけがEditorOnlyです。制御対象アイテムもEditorOnlyにするか、両方からEditorOnlyを外してください";
                invalidPaths.Add($"{itemPath}: {detail}");
                firstInvalidTarget ??= target;
            }

            if (invalidPaths.Count == 0)
            {
                return true;
            }

            ReportBuildError(
                "EasyCarry System のビルドを中止しました。\n\n"
                + "制御対象アイテムと生成されたEasyCarry SystemのEditorOnly設定が一致していません。\n"
                + "両方をEditorOnlyにするか、両方からEditorOnlyを外してから再度ビルドしてください。\n\n"
                + string.Join("\n", invalidPaths),
                firstInvalidTarget != null ? firstInvalidTarget : avatarGameObject);
            return false;
        }

        private static bool ValidateUniqueSlots(
            GameObject avatarGameObject,
            EasyCarrySystemItemReference[] targets)
        {
            var targetsBySlot = new Dictionary<int, List<EasyCarrySystemItemReference>>();
            foreach (var target in targets)
            {
                if (target == null)
                {
                    continue;
                }

                var slot = target.CISlot;
                if (!targetsBySlot.TryGetValue(slot, out var slotTargets))
                {
                    slotTargets = new List<EasyCarrySystemItemReference>();
                    targetsBySlot.Add(slot, slotTargets);
                }

                slotTargets.Add(target);
            }

            var duplicateMessages = new List<string>();
            Object firstDuplicate = null;
            for (var slot = 0; slot <= 15; slot++)
            {
                if (!targetsBySlot.TryGetValue(slot, out var slotTargets) || slotTargets.Count < 2)
                {
                    continue;
                }

                duplicateMessages.Add($"アイテムスロット {slot:00} が重複しています。");
                foreach (var target in slotTargets)
                {
                    var itemPath = AnimationUtility.CalculateTransformPath(
                        target.transform,
                        avatarGameObject.transform);
                    duplicateMessages.Add($"  - {itemPath}");
                    firstDuplicate ??= target;
                }
            }

            if (duplicateMessages.Count == 0)
            {
                return true;
            }

            ReportBuildError(
                "EasyCarry System のビルドを中止しました。\n\n"
                + "同じアバター内でアイテムスロットが重複しています。"
                + "各アイテムに異なるスロットを指定してから、再度ビルドしてください。\n\n"
                + string.Join("\n", duplicateMessages),
                firstDuplicate != null ? firstDuplicate : avatarGameObject);
            return false;
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

            ReportBuildError(message, firstBoneProxy != null ? firstBoneProxy : avatarGameObject);
            return false;
        }

        private static void ReportBuildError(string message, Object contextObject)
        {
            using (ErrorReport.WithContextObject(contextObject))
            {
                ErrorReport.ReportError(new EasyCarrySystemBuildError(message));
            }
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

    internal sealed class EasyCarrySystemBuildError : SimpleError
    {
        private readonly string message;

        internal EasyCarrySystemBuildError(string message)
        {
            this.message = message;
        }

        public override Localizer Localizer => null;
        public override string TitleKey => "EasyCarrySystemBuildError";
        public override ErrorSeverity Severity => ErrorSeverity.Error;

        public override string FormatTitle()
        {
            return "EasyCarry System ビルドエラー";
        }

        public override string FormatDetails()
        {
            return message;
        }

        public override string FormatHint()
        {
            return "EasyCarry SystemのInspectorと、エラーに表示されたオブジェクトを確認してください。";
        }
    }
}
#endif
