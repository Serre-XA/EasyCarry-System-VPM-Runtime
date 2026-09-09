using nadena.dev.modular_avatar.core;
using Serre.EasyCarrySystem;
using UnityEditor;
using UnityEngine;
#if VRC_SDK_VRCSDK3
using VRC.Dynamics;
using VRC.SDK3.Dynamics.Constraint.Components;
#endif

namespace Serre.EasyCarrySystem.Editor
{
    internal static class EasyCarrySystemSetupEditorUtility
    {
        private const string PrefabRelativePathFormat = "Prefabs/EasyCarrySystem_{0:00}.prefab";
        private const string GeneratedObjectNamePrefix = "ECS_";
        private const int SlotCount = 16;

        internal static void Setup(GameObject itemObject)
        {
            if (itemObject == null)
            {
                return;
            }

            if (EditorUtility.IsPersistent(itemObject) || !itemObject.scene.IsValid())
            {
                Debug.LogWarning(
                    "EasyCarry System Setupを中止しました。HierarchyまたはPrefab Mode内のオブジェクトを選択してください。",
                    itemObject);
                return;
            }

            if (RejectSetupInProtectedHierarchy(itemObject))
            {
                return;
            }

            var boneProxy = itemObject.GetComponentInChildren<ModularAvatarBoneProxy>(true);
            if (boneProxy != null)
            {
                Debug.LogError(
                    $"EasyCarry System setup cannot be applied because MA Bone Proxy is attached to {boneProxy.gameObject.name} within the item hierarchy. Remove it before setup.",
                    boneProxy);
                return;
            }

            var itemReference = itemObject.GetComponent<EasyCarrySystemItemReference>();
            var preferredSlot = itemReference != null ? itemReference.CISlot : -1;
            var slot = FindFirstAvailableSlot(itemObject.transform, preferredSlot, itemReference);
            if (slot < 0)
            {
                Debug.LogError("No available EasyCarry System slot was found in this avatar.", itemObject);
                return;
            }

            Setup(itemObject, slot, itemObject.transform.parent);
        }

        private static void Setup(GameObject itemObject, int slot, Transform installParent)
        {
            if (itemObject == null || Application.isPlaying)
            {
                return;
            }

            if (RejectSetupInProtectedHierarchy(itemObject))
            {
                return;
            }

            var itemReference = itemObject.GetComponent<EasyCarrySystemItemReference>();
            if (itemReference != null && itemReference.GeneratedEasyCarrySystem != null)
            {
                Debug.LogWarning("EasyCarry System Setup was canceled because the selected object is already set up.", itemObject);
                return;
            }

#if VRC_SDK_VRCSDK3
            var canReuseEasyCarrySystemConstraints = itemReference != null
                && itemReference.ItemSettings.Initialized;
            if (!canReuseEasyCarrySystemConstraints
                && (itemObject.GetComponent<VRCParentConstraint>() != null
                    || itemObject.GetComponent<VRCScaleConstraint>() != null))
            {
                Debug.LogError(
                    "EasyCarry System Setupを中止しました。選択したオブジェクトには既存のVRC Parent ConstraintまたはVRC Scale Constraintがあります。",
                    itemObject);
                return;
            }
#endif

            var itemName = itemObject.name;
            var itemTransform = itemObject.transform;
            if (installParent != null && IsChildOf(installParent, itemTransform))
            {
                Debug.LogError("Install Root cannot be the item itself or a child of the item.", itemObject);
                return;
            }

            var prefabPath = EasyCarrySystemPackageAssets.GetAssetPath(
                string.Format(PrefabRelativePathFormat, Mathf.Clamp(slot, 0, SlotCount - 1)));
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError($"EasyCarry System prefab was not found: {prefabPath}", itemObject);
                return;
            }

            Undo.IncrementCurrentGroup();
            var undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Setup EasyCarry System");

            var instanceObject = (installParent != null
                ? PrefabUtility.InstantiatePrefab(prefab, installParent)
                : PrefabUtility.InstantiatePrefab(prefab, itemObject.scene)) as GameObject;
            if (instanceObject == null)
            {
                Debug.LogError($"Failed to instantiate EasyCarry System prefab: {prefabPath}", itemObject);
                return;
            }

            var instanceTransform = instanceObject.transform;
            instanceObject.name = GetGeneratedObjectName(instanceTransform.parent, itemName);
            Undo.RegisterCreatedObjectUndo(instanceObject, "Create EasyCarry System");
            instanceTransform.localPosition = Vector3.zero;
            instanceTransform.localRotation = Quaternion.identity;
            instanceTransform.localScale = Vector3.one;

            var ciRoot = FindChildRecursive(instanceTransform, "CI_Root");
            if (ciRoot == null)
            {
                Debug.LogError("CI_Root was not found in the instantiated EasyCarry System prefab.", instanceObject);
                Undo.RevertAllDownToGroup(undoGroup);
                return;
            }

            var ciItemSize = FindChildRecursive(instanceTransform, "CI_ItemSize");
            if (ciItemSize == null)
            {
                Debug.LogError("CI_ItemSize was not found in the instantiated EasyCarry System prefab.", instanceObject);
                Undo.RevertAllDownToGroup(undoGroup);
                return;
            }

            if (itemReference == null)
            {
                itemReference = Undo.AddComponent<EasyCarrySystemItemReference>(itemObject);
            }
            else
            {
                Undo.RecordObject(itemReference, "Setup EasyCarry System Item Reference");
            }

            itemReference.SetGeneratedEasyCarrySystem(instanceObject);
            itemReference.SetCISlot(slot);
            itemReference.SetCIItemSize(ciItemSize);
            EasyCarrySystemSlotEditorUtility.ApplyStoredSettings(itemReference, true);
            EasyCarrySystemEditorSharedUtility.ResetMenuDisplayName(itemReference);
            EditorUtility.SetDirty(itemReference);

#if VRC_SDK_VRCSDK3
            SetupItemConstraints(itemObject, ciRoot);
#else
            Debug.LogError("VRC SDK was not found, so the EasyCarry System constraints could not be created.", itemObject);
            Undo.RevertAllDownToGroup(undoGroup);
            return;
#endif

            var gestureSettings =
                EasyCarrySystemGestureCheckerEditorUtility.FindSettingsFor(itemReference);
            if (gestureSettings == null
                || !EasyCarrySystemGestureCheckerEditorUtility.ApplyParameterDefaults(gestureSettings))
            {
                Debug.LogError(
                    "EasyCarry System内の握り判定設定またはMA Parametersが正しくありません。Prefab構成を確認してください。",
                    instanceObject);
                Undo.RevertAllDownToGroup(undoGroup);
                return;
            }

            EasyCarrySystemGestureCheckerEditorUtility.EnsureMenuRootFor(itemReference);
            Undo.CollapseUndoOperations(undoGroup);
            Selection.activeObject = itemObject;
        }

        internal static void DrawMissingItemConstraints(EasyCarrySystemItemReference target)
        {
#if VRC_SDK_VRCSDK3
            if (target == null || target.GeneratedEasyCarrySystem == null)
            {
                return;
            }

            var missingParent = target.GetComponent<VRCParentConstraint>() == null;
            var missingScale = target.GetComponent<VRCScaleConstraint>() == null;
            if (!missingParent && !missingScale)
            {
                return;
            }

            var missing = missingParent && missingScale
                ? "VRC Parent Constraint / VRC Scale Constraint"
                : missingParent ? "VRC Parent Constraint" : "VRC Scale Constraint";
            EditorGUILayout.HelpBox($"必須の{missing}がありません。再生成してください。", MessageType.Error);
            var source = FindChildRecursive(target.GeneratedEasyCarrySystem.transform, "CI_Root");
            if (source == null)
            {
                EditorGUILayout.HelpBox("参照先のCI_Rootが見つからないため再生成できません。生成されたECSの構成を確認してください。", MessageType.Error);
            }

            using (new EditorGUI.DisabledScope(Application.isPlaying || source == null
                || EditorUtility.IsPersistent(target) || !target.gameObject.scene.IsValid()))
            {
                if (GUILayout.Button("不足している必須Constraintを再生成"))
                {
                    RestoreMissingItemConstraints(target);
                    GUIUtility.ExitGUI();
                }
            }
#endif
        }

#if VRC_SDK_VRCSDK3
        internal static void RestoreMissingItemConstraints(EasyCarrySystemItemReference target)
        {
            if (target == null || target.GeneratedEasyCarrySystem == null || Application.isPlaying
                || EditorUtility.IsPersistent(target) || !target.gameObject.scene.IsValid())
            {
                return;
            }

            var source = FindChildRecursive(target.GeneratedEasyCarrySystem.transform, "CI_Root");
            if (source == null)
            {
                Debug.LogError("必須Constraintを再生成できません。CI_Rootが見つかりません。", target);
                return;
            }

            var missingParent = target.GetComponent<VRCParentConstraint>() == null;
            var missingScale = target.GetComponent<VRCScaleConstraint>() == null;
            if (!missingParent && !missingScale)
            {
                return;
            }

            Undo.IncrementCurrentGroup();
            var group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Restore EasyCarry System Constraints");
            // Capture scale before adding a constraint can evaluate the item transform.
            var scale = target.transform.localScale;
            try
            {
                if (missingParent)
                {
                    ConfigureItemParentConstraint(Undo.AddComponent<VRCParentConstraint>(target.gameObject), source);
                }
                if (missingScale)
                {
                    ConfigureItemScaleConstraint(Undo.AddComponent<VRCScaleConstraint>(target.gameObject), source, scale);
                }
                Undo.CollapseUndoOperations(group);
            }
            catch (System.Exception exception)
            {
                Undo.RevertAllDownToGroup(group);
                Debug.LogException(exception, target);
            }
        }
#endif

        private static string GetGeneratedObjectName(Transform parent, string itemName)
        {
            var prefixedName = itemName.StartsWith(GeneratedObjectNamePrefix, System.StringComparison.Ordinal)
                ? itemName
                : GeneratedObjectNamePrefix + itemName;
            return GameObjectUtility.GetUniqueNameForSibling(parent, prefixedName);
        }

#if VRC_SDK_VRCSDK3
        private static void SetupItemConstraints(GameObject itemObject, Transform ciRoot)
        {
            var itemTransform = itemObject.transform;
            var originalLocalScale = itemTransform.localScale;

            Undo.RecordObject(itemTransform, "Reset EasyCarry System Item Transform");
            itemTransform.localPosition = Vector3.zero;
            itemTransform.localRotation = Quaternion.identity;
            itemTransform.localScale = originalLocalScale;
            EditorUtility.SetDirty(itemTransform);

            var parentConstraint = itemObject.GetComponent<VRCParentConstraint>()
                ?? Undo.AddComponent<VRCParentConstraint>(itemObject);
            ConfigureItemParentConstraint(parentConstraint, ciRoot);

            var scaleConstraint = itemObject.GetComponent<VRCScaleConstraint>()
                ?? Undo.AddComponent<VRCScaleConstraint>(itemObject);
            ConfigureItemScaleConstraint(scaleConstraint, ciRoot, originalLocalScale);
        }

        private static void ConfigureItemParentConstraint(VRCParentConstraint parentConstraint, Transform ciRoot)
        {
            Undo.RecordObject(parentConstraint, "Configure EasyCarry System Parent Constraint");
            ConfigureConstraintSource(parentConstraint, ciRoot);
            parentConstraint.PositionAtRest = Vector3.zero;
            parentConstraint.RotationAtRest = Vector3.zero;
            parentConstraint.AffectsPositionX = true;
            parentConstraint.AffectsPositionY = true;
            parentConstraint.AffectsPositionZ = true;
            parentConstraint.AffectsRotationX = true;
            parentConstraint.AffectsRotationY = true;
            parentConstraint.AffectsRotationZ = true;
            parentConstraint.ApplyConfigurationChanges();
            EditorUtility.SetDirty(parentConstraint);
            PrefabUtility.RecordPrefabInstancePropertyModifications(parentConstraint);
        }

        private static void ConfigureItemScaleConstraint(VRCScaleConstraint scaleConstraint, Transform ciRoot,
            Vector3 originalLocalScale)
        {
            Undo.RecordObject(scaleConstraint, "Configure EasyCarry System Scale Constraint");
            ConfigureConstraintSource(scaleConstraint, ciRoot);
            scaleConstraint.ScaleAtRest = originalLocalScale;
            scaleConstraint.ScaleOffset = originalLocalScale;
            scaleConstraint.AffectsScaleX = true;
            scaleConstraint.AffectsScaleY = true;
            scaleConstraint.AffectsScaleZ = true;
            scaleConstraint.ApplyConfigurationChanges();
            EditorUtility.SetDirty(scaleConstraint);
            PrefabUtility.RecordPrefabInstancePropertyModifications(scaleConstraint);
        }

        private static void ConfigureConstraintSource(VRCConstraintBase constraint, Transform source)
        {
            constraint.IsActive = true;
            constraint.GlobalWeight = 1f;
            constraint.TargetTransform = null;
            constraint.SolveInLocalSpace = false;
            constraint.FreezeToWorld = false;
            constraint.RebakeOffsetsWhenUnfrozen = false;
            constraint.Locked = true;
            constraint.Sources.Clear();
            constraint.Sources.Add(new VRCConstraintSource(source, 1f)
            {
                ParentPositionOffset = Vector3.zero,
                ParentRotationOffset = Vector3.zero,
            });
        }
#endif


        private static int FindFirstAvailableSlot(Transform itemTransform, int preferredSlot, EasyCarrySystemItemReference ignoredReference)
        {
            var usedSlots = EasyCarrySystemSlotEditorUtility.FindUsedSlots(itemTransform, ignoredReference);

            if (preferredSlot >= 0 && preferredSlot < usedSlots.Length && !usedSlots[preferredSlot])
            {
                return preferredSlot;
            }

            for (var i = 0; i < usedSlots.Length; i++)
            {
                if (!usedSlots[i])
                {
                    return i;
                }
            }

            return -1;
        }

        private static bool RejectSetupInProtectedHierarchy(GameObject itemObject)
        {
            if (itemObject != null
                && EasyCarrySystemGestureCheckerEditorUtility.IsMenuRootHierarchy(itemObject.transform))
            {
                Debug.LogWarning(
                    "EasyCarry System Setupを中止しました。共有メニューとその子階層にはSetupできません。",
                    itemObject);
                return true;
            }

            var current = itemObject != null ? itemObject.transform : null;
            while (current != null)
            {
                if (current.GetComponent<EasyCarrySystemGestureSettings>() != null)
                {
                    Debug.LogWarning(
                        "EasyCarry System Setupを中止しました。握り判定用の内部オブジェクトとその子階層にはSetupできません。",
                        itemObject);
                    return true;
                }

                var itemReference = current.GetComponent<EasyCarrySystemItemReference>();
                if (itemReference != null
                    && (current.gameObject != itemObject || itemReference.GeneratedEasyCarrySystem != null))
                {
                    var message = current.gameObject == itemObject
                        ? "EasyCarry System Setupを中止しました。選択したオブジェクトには既にEasyCarry Systemが設定されています。"
                        : $"EasyCarry System Setupを中止しました。選択したオブジェクトはEasyCarry Systemアイテム「{current.name}」の子階層にあります。";
                    Debug.LogWarning(message, itemObject);
                    return true;
                }

                current = current.parent;
            }

            foreach (var itemReference in Resources.FindObjectsOfTypeAll<EasyCarrySystemItemReference>())
            {
                if (itemReference == null || EditorUtility.IsPersistent(itemReference)
                    || itemReference.GeneratedEasyCarrySystem == null
                    || itemReference.gameObject.scene != itemObject.scene)
                {
                    continue;
                }

                if (IsChildOf(itemObject.transform, itemReference.GeneratedEasyCarrySystem.transform))
                {
                    Debug.LogWarning(
                        $"EasyCarry System Setupを中止しました。選択したオブジェクトは生成済みEasyCarry System「{itemReference.GeneratedEasyCarrySystem.name}」の子階層にあります。",
                        itemObject);
                    return true;
                }
            }

            return false;
        }

        private static Transform FindChildRecursive(Transform parent, string childName)
        {
            if (parent == null)
            {
                return null;
            }

            for (var i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child.name == childName)
                {
                    return child;
                }

                var found = FindChildRecursive(child, childName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static bool IsChildOf(Transform candidate, Transform parent)
        {
            var current = candidate;
            while (current != null)
            {
                if (current == parent)
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }
    }
}

