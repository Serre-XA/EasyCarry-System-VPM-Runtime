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
    internal static class EasyCarrySystemSlotEditorUtility
    {
        private const string PrefabRelativePathFormat = "Prefabs/EasyCarrySystem_{0:00}.prefab";
        private const string MenuSettingsNameFormat = "CarryItem_{0:00}_Settings";
        private const string MenuResetNameFormat = "CI_{0:00}_Reset";
        private const string MenuSwitchHandsNameFormat = "CI_{0:00}_SwitchHands_Enable";
        private const string MenuFreezeNameFormat = "CI_{0:00}_Freeze_Enable";
        private const string SourceTransformPath = "Sources.source0.SourceTransform";
        private const string SourcePositionOffsetPath = "Sources.source0.ParentPositionOffset";
        private const string SourceRotationOffsetPath = "Sources.source0.ParentRotationOffset";
        private const string ContactShapeTypePath = "shapeType";
        private const string ContactRadiusPath = "radius";
        private const string ContactHeightPath = "height";
        private const string ContactSizePath = "size";
        private const string WorldFixedParameterName = "Item/CanFreeze";
        private const int SlotCount = 16;
        private const int MaxSourceCount = 16;
        private const float SlotButtonWidth = 28f;
        private const float SlotButtonHeight = 22f;
        private static readonly string[] AttachPointNames =
        {
            "AP_Hand_L", "AP_Hand_R", "AP_00", "AP_01", "AP_02", "AP_03", "AP_04", "AP_05", "AP_06",
        };

        private static readonly string[] AttachPointPropertyNames =
        {
            "apHandL", "apHandR", "ap00", "ap01", "ap02", "ap03", "ap04", "ap05", "ap06",
        };

        private static readonly string[] PositionOffsetPropertyNames =
        {
            "apHandLPositionOffset", "apHandRPositionOffset", "ap00PositionOffset", "ap01PositionOffset",
            "ap02PositionOffset", "ap03PositionOffset", "ap04PositionOffset", "ap05PositionOffset", "ap06PositionOffset",
        };

        private static readonly string[] RotationOffsetPropertyNames =
        {
            "apHandLRotationOffset", "apHandRRotationOffset", "ap00RotationOffset", "ap01RotationOffset",
            "ap02RotationOffset", "ap03RotationOffset", "ap04RotationOffset", "ap05RotationOffset", "ap06RotationOffset",
        };

        private static readonly string[] ContactNames =
        {
            "CI_ItemSize", "AP_Contact_Hand_L", "AP_Contact_Hand_R", "AP_Contact_00", "AP_Contact_01",
            "AP_Contact_02", "AP_Contact_03", "AP_Contact_04", "AP_Contact_05", "AP_Contact_06",
            "CI_Input_GrabBlocked", "CI_Input_ForceReturn",
            "CI_Output_IsGrabbed_L", "CI_Output_IsGrabbed_R",
        };

        private static readonly string[] ContactPropertyNames =
        {
            "ciItemSize", "apContactHandL", "apContactHandR", "apContact00", "apContact01",
            "apContact02", "apContact03", "apContact04", "apContact05", "apContact06",
        };
        private static readonly string[] HideWhenAttachedParameterNames =
        {
            "AttachPoint/00/HideWhenAttached", "AttachPoint/01/HideWhenAttached",
            "AttachPoint/02/HideWhenAttached", "AttachPoint/03/HideWhenAttached",
            "AttachPoint/04/HideWhenAttached", "AttachPoint/05/HideWhenAttached",
            "AttachPoint/06/HideWhenAttached",
        };

        internal static void DrawSlotSelector(EasyCarrySystemItemReference targets)
        {
            var activeSlot = ResolveSlot(targets);
            if (activeSlot >= 0 && targets.CISlot != activeSlot)
            {
                targets.SetCISlot(activeSlot);
                EditorUtility.SetDirty(targets);
            }

            var usedSlots = FindUsedSlots(targets);
            EditorGUILayout.Space(8f);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EasyCarrySystemEditorSharedUtility.DrawSectionHeader(
                    "\u30a2\u30a4\u30c6\u30e0\u30b9\u30ed\u30c3\u30c8");
                EasyCarrySystemEditorSharedUtility.DrawSectionDescription(
                    "\u540c\u3058\u30a2\u30d0\u30bf\u30fc\u5185\u3067\u91cd\u8907\u3057\u306a\u3044\u756a\u53f7\u3092\u9078\u3073\u307e\u3059\u3002\u7dd1\u306f\u9078\u629e\u4e2d\u3001\u8d64\u306f\u4f7f\u7528\u4e2d\u3067\u3059\u3002");
                using (new EasyCarrySystemEditorSharedUtility.HorizontalMarginScope())
                {
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.ExpandWidth(true)))
                    {
                        DrawSlotButtons(targets, activeSlot, usedSlots);
                    }
                }
            }
        }

        private static void DrawSlotButtons(EasyCarrySystemItemReference targets, int activeSlot, bool[] usedSlots)
        {
            const int slotsPerRow = 8;
            for (var rowStart = 0; rowStart < SlotCount; rowStart += slotsPerRow)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    var rowEnd = Mathf.Min(rowStart + slotsPerRow, SlotCount);
                    for (var slot = rowStart; slot < rowEnd; slot++)
                    {
                        DrawSlotButton(targets, slot, activeSlot, usedSlots[slot]);
                    }
                }
            }
        }

        private static void DrawSlotButton(EasyCarrySystemItemReference targets, int slot, int activeSlot, bool isUsed)
        {
            var isActive = slot == activeSlot;
            var style = EasyCarrySystemEditorSharedUtility.CreateReadableStyle(EditorStyles.miniButton);
            if (isActive)
            {
                style.onNormal.textColor = Color.green;
                style.onHover.textColor = Color.green;
                style.onActive.textColor = Color.green;
                style.onFocused.textColor = Color.green;
            }
            else if (isUsed)
            {
                style.normal.textColor = Color.red;
                style.hover.textColor = Color.red;
                style.active.textColor = Color.red;
                style.focused.textColor = Color.red;
            }

            var tooltip = isActive
                ? "\u73fe\u5728\u306e\u30a2\u30a4\u30c6\u30e0\u30b9\u30ed\u30c3\u30c8"
                : isUsed
                    ? "\u3053\u306e\u756a\u53f7\u306f\u73fe\u5728\u306e\u30b7\u30fc\u30f3\u5185\u3067\u4f7f\u7528\u3055\u308c\u3066\u3044\u307e\u3059"
                    : $"\u30a2\u30a4\u30c6\u30e0\u30b9\u30ed\u30c3\u30c8 {slot} \u306b\u5207\u308a\u66ff\u3048";

            var selected = GUILayout.Toggle(isActive, new GUIContent(slot.ToString(), tooltip), style,
                GUILayout.Width(SlotButtonWidth), GUILayout.Height(SlotButtonHeight));
            if (!selected || isActive)
            {
                return;
            }

            if (isUsed)
            {
                Debug.LogWarning($"EasyCarry System \u30b9\u30ed\u30c3\u30c8 {slot} \u306f\u73fe\u5728\u306e\u30b7\u30fc\u30f3\u5185\u3067\u4f7f\u7528\u6e08\u307f\u3067\u3059\u3002", targets);
                return;
            }

            ReplaceSlot(targets, slot);
            GUIUtility.ExitGUI();
        }

        private static bool[] FindUsedSlots(EasyCarrySystemItemReference current)
        {
            var usedSlots = new bool[SlotCount];
            var allTargets = Resources.FindObjectsOfTypeAll<EasyCarrySystemItemReference>();
            foreach (var candidate in allTargets)
            {
                if (candidate == null || candidate == current || EditorUtility.IsPersistent(candidate))
                {
                    continue;
                }

                if (candidate.gameObject.scene != current.gameObject.scene)
                {
                    continue;
                }

                var slot = ResolveSlot(candidate);
                if (slot >= 0 && slot < SlotCount)
                {
                    usedSlots[slot] = true;
                }
            }

            return usedSlots;
        }

        private static int ResolveSlot(EasyCarrySystemItemReference targets)
        {
            if (targets == null)
            {
                return -1;
            }

            var prefabPath = targets.GeneratedEasyCarrySystem != null
                ? PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(targets.GeneratedEasyCarrySystem)
                : string.Empty;
            for (var slot = 0; slot < SlotCount; slot++)
            {
                if (!string.IsNullOrEmpty(prefabPath) && prefabPath.EndsWith($"EasyCarrySystem_{slot:00}.prefab"))
                {
                    return slot;
                }
            }

            return targets.CISlot >= 0 && targets.CISlot < SlotCount ? targets.CISlot : -1;
        }

        private static void ReplaceSlot(EasyCarrySystemItemReference itemReference, int newSlot)
        {
            if (itemReference == null || Application.isPlaying)
            {
                return;
            }

            var oldRoot = itemReference.GeneratedEasyCarrySystem;
            if (oldRoot == null)
            {
                Debug.LogError("The generated EasyCarry System is missing.", itemReference);
                return;
            }

            var prefabPath = EasyCarrySystemAssetLocator.GetAssetPath(
                string.Format(PrefabRelativePathFormat, newSlot));
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError($"EasyCarry System prefab was not found: {prefabPath}", itemReference);
                return;
            }

            EasyCarrySystemEditorSharedUtility.PrepareForSlotReplacement(itemReference);
            var settings = CaptureSnapshot(itemReference);
            itemReference.SetItemSettings(settings);

            var oldTransform = oldRoot.transform;
            var undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName($"Switch EasyCarry System Slot To {newSlot}");

            var newRoot = PrefabUtility.InstantiatePrefab(prefab, oldTransform.parent) as GameObject;
            if (newRoot == null)
            {
                Debug.LogError($"Failed to instantiate EasyCarry System prefab: {prefabPath}", itemReference);
                return;
            }

            Undo.RegisterCreatedObjectUndo(newRoot, "Create Replacement EasyCarry System");
            newRoot.name = oldRoot.name;
            var newTransform = newRoot.transform;
            newTransform.SetSiblingIndex(oldTransform.GetSiblingIndex());
            newTransform.localPosition = oldTransform.localPosition;
            newTransform.localRotation = oldTransform.localRotation;
            newTransform.localScale = oldTransform.localScale;
            newRoot.SetActive(oldRoot.activeSelf);

            var newCIRoot = FindChildRecursive(newTransform, "CI_Root");
            if (newCIRoot == null)
            {
                Undo.DestroyObjectImmediate(newRoot);
                Debug.LogError("CI_Root was not found in the replacement EasyCarry System prefab.", itemReference);
                return;
            }

            Undo.RecordObject(itemReference, "Switch EasyCarry System Slot");
            itemReference.SetGeneratedEasyCarrySystem(newRoot);
            itemReference.SetCISlot(newSlot);
            RestoreSnapshot(itemReference, settings, true);
            EasyCarrySystemEditorSharedUtility.SyncNumberedAttachPointAvailability(itemReference, false);
            RetargetItem(itemReference, newCIRoot);

            Undo.DestroyObjectImmediate(oldRoot);
            PrefabUtility.RecordPrefabInstancePropertyModifications(itemReference);
            EditorUtility.SetDirty(itemReference);
            Undo.CollapseUndoOperations(undoGroup);
            Selection.activeObject = itemReference.gameObject;
        }

        private static void RetargetItem(EasyCarrySystemItemReference itemReference, Transform newCIRoot)
        {
#if VRC_SDK_VRCSDK3
            RetargetConstraint(itemReference.GetComponent<VRCParentConstraint>(), newCIRoot);
            RetargetConstraint(itemReference.GetComponent<VRCScaleConstraint>(), newCIRoot);
#endif
        }

#if VRC_SDK_VRCSDK3
        private static void RetargetConstraint(VRCConstraintBase constraint, Transform source)
        {
            if (constraint == null)
            {
                return;
            }

            Undo.RecordObject(constraint, "Retarget EasyCarry System Constraint");
            if (constraint.Sources.Count == 0)
            {
                constraint.Sources.Add(new VRCConstraintSource(source, 1f));
            }
            else
            {
                var constraintSource = constraint.Sources[0];
                constraintSource.SourceTransform = source;
                constraint.Sources[0] = constraintSource;
            }

            constraint.ApplyConfigurationChanges();
            PrefabUtility.RecordPrefabInstancePropertyModifications(constraint);
            EditorUtility.SetDirty(constraint);
        }
#endif

        internal static void CaptureAndStoreSettings(EasyCarrySystemItemReference targets)
        {
            if (targets == null || targets.EasyCarrySystemRoot == null)
            {
                return;
            }

            targets.SetItemSettings(CaptureSnapshot(targets));
            PrefabUtility.RecordPrefabInstancePropertyModifications(targets);
            EditorUtility.SetDirty(targets);
        }

        internal static void ApplyStoredSettings(EasyCarrySystemItemReference targets, bool restoreCIItemSizeScale)
        {
            if (targets == null || targets.EasyCarrySystemRoot == null)
            {
                return;
            }

            var settings = targets.ItemSettings;
            if (settings.Initialized)
            {
                RestoreSnapshot(targets, settings, restoreCIItemSizeScale);
            }
            else
            {
                CaptureAndStoreSettings(targets);
            }
        }
        internal static EasyCarrySystemItemSettings CaptureSnapshot(EasyCarrySystemItemReference targets)
        {
            if (targets == null || targets.EasyCarrySystemRoot == null)
            {
                return targets != null ? targets.ItemSettings : new EasyCarrySystemItemSettings();
            }

            EasyCarrySystemEditorSharedUtility.EnsureMenuObjectReferences(targets);
            var snapshot = new EasyCarrySystemItemSettings
            {
                Initialized = true,
                NumberedAttachPointOrder = targets.GetNumberedAttachPointOrder(),
                SourceSlot = Mathf.Clamp(targets.CISlot, 0, SlotCount - 1),
                MenuSettingsName = GetObjectName(targets.MenuSettingsRoot),
                MenuResetName = GetObjectName(targets.MenuResetItem),
                MenuSwitchHandsName = GetObjectName(targets.MenuSwitchHandsItem),
                MenuFreezeName = GetObjectName(targets.MenuFreezeItem),
            };
            for (var i = 0; i < AttachPointNames.Length; i++)
            {
                var attachPointName = AttachPointNames[i];
                var attachPointTransform = FindChildRecursive(targets.EasyCarrySystemRoot, attachPointName);
                var attachPointSnapshot = new EasyCarrySystemAttachPointSettings
                {
                    SourceTransform = GetAttachPointSource(targets, attachPointName),
                    PositionOffset = targets.GetAttachPointPositionOffset(attachPointName),
                    RotationOffset = targets.GetAttachPointRotationOffset(attachPointName),
                    AttachmentMethod = targets.GetAttachPointMethod(attachPointName),
                };

                if (attachPointTransform != null)
                {
                    attachPointSnapshot.HasLocalTransform = true;
                    attachPointSnapshot.LocalPosition = attachPointTransform.localPosition;
                    attachPointSnapshot.LocalRotation = attachPointTransform.localRotation;
                    attachPointSnapshot.LocalScale = attachPointTransform.localScale;

                    var boneProxy = attachPointTransform.GetComponent<ModularAvatarBoneProxy>();
                    if (boneProxy != null)
                    {
                        attachPointSnapshot.HasBoneProxy = true;
                        attachPointSnapshot.BoneReference = (int)boneProxy.boneReference;
                        attachPointSnapshot.BoneSubPath = boneProxy.subPath;
                        attachPointSnapshot.BoneAttachmentMode = (int)boneProxy.attachmentMode;
                    }
                }

                snapshot.AttachPoints[i] = attachPointSnapshot;
            }
            for (var i = 0; i < ContactNames.Length; i++)
            {
                snapshot.Contacts[i] = CaptureContact(targets.EasyCarrySystemRoot, ContactNames[i]);
            }

            var mainConstraint = FindParentConstraint(FindChildRecursive(targets.EasyCarrySystemRoot, "CI_MainConst"));
            if (mainConstraint != null)
            {
                var serializedConstraint = new SerializedObject(mainConstraint);
                for (var i = 0; i < Mathf.Min(MaxSourceCount, snapshot.MainWeights.Length); i++)
                {
                    var weightProperty = serializedConstraint.FindProperty($"Sources.source{i}.Weight");
                    snapshot.MainWeights[i] = weightProperty != null ? weightProperty.floatValue : 0f;
                }
            }

            CaptureOptionDefaults(targets, snapshot);
            return snapshot;
        }

        internal static void RestoreSnapshot(EasyCarrySystemItemReference targets, EasyCarrySystemItemSettings snapshot,
            bool restoreCIItemSizeScale)
        {
            if (targets == null || targets.EasyCarrySystemRoot == null || snapshot == null || !snapshot.Initialized)
            {
                return;
            }

            snapshot.EnsureInitialized();
            targets.SetNumberedAttachPointOrder(snapshot.NumberedAttachPointOrder);
            EasyCarrySystemEditorSharedUtility.EnsureMenuObjectReferences(targets);
            var targetSlot = Mathf.Clamp(targets.CISlot, 0, SlotCount - 1);
            RestoreObjectName(targets.MenuSettingsRoot, ResolveRestoredMenuName(
                snapshot.MenuSettingsName, snapshot.SourceSlot, targetSlot, MenuSettingsNameFormat));
            RestoreObjectName(targets.MenuResetItem, ResolveRestoredMenuName(
                snapshot.MenuResetName, snapshot.SourceSlot, targetSlot, MenuResetNameFormat));
            RestoreObjectName(targets.MenuSwitchHandsItem, ResolveRestoredMenuName(
                snapshot.MenuSwitchHandsName, snapshot.SourceSlot, targetSlot, MenuSwitchHandsNameFormat));
            RestoreObjectName(targets.MenuFreezeItem, ResolveRestoredMenuName(
                snapshot.MenuFreezeName, snapshot.SourceSlot, targetSlot, MenuFreezeNameFormat));
            for (var i = 0; i < AttachPointNames.Length; i++)
            {
                targets.SetAttachPointMethod(AttachPointNames[i], snapshot.AttachPoints[i].AttachmentMethod);
            }

            var serializedTargets = new SerializedObject(targets);
            for (var i = 0; i < AttachPointNames.Length; i++)
            {
                var attachPoint = snapshot.AttachPoints[i];
                serializedTargets.FindProperty(AttachPointPropertyNames[i]).objectReferenceValue = attachPoint.SourceTransform;
                serializedTargets.FindProperty(PositionOffsetPropertyNames[i]).vector3Value = attachPoint.PositionOffset;
                serializedTargets.FindProperty(RotationOffsetPropertyNames[i]).vector3Value = attachPoint.RotationOffset;
                ApplyAttachPoint(targets.EasyCarrySystemRoot, AttachPointNames[i], attachPoint);
                RestoreBoneProxyAttachPoint(targets.EasyCarrySystemRoot, AttachPointNames[i], attachPoint);
                RestoreAttachmentMethod(targets.EasyCarrySystemRoot, AttachPointNames[i], attachPoint.AttachmentMethod);
            }

            for (var i = 0; i < ContactNames.Length; i++)
            {
                var contactTransform = FindChildRecursive(targets.EasyCarrySystemRoot, ContactNames[i]);
                if (i < ContactPropertyNames.Length)
                {
                    var contactProperty = serializedTargets.FindProperty(ContactPropertyNames[i]);
                    if (contactProperty != null)
                    {
                        contactProperty.objectReferenceValue = contactTransform;
                    }
                }
                RestoreContact(contactTransform, snapshot.Contacts[i], i != 0 || restoreCIItemSizeScale);
            }

            serializedTargets.ApplyModifiedPropertiesWithoutUndo();
            RestoreOptionDefaults(targets, snapshot);

            var mainConstraint = FindParentConstraint(FindChildRecursive(targets.EasyCarrySystemRoot, "CI_MainConst"));
            if (mainConstraint != null)
            {
                var serializedConstraint = new SerializedObject(mainConstraint);
                Undo.RecordObject(mainConstraint, "Restore CI_MainConst Weights");
                for (var i = 0; i < Mathf.Min(MaxSourceCount, snapshot.MainWeights.Length); i++)
                {
                    var weightProperty = serializedConstraint.FindProperty($"Sources.source{i}.Weight");
                    if (weightProperty != null)
                    {
                        weightProperty.floatValue = snapshot.MainWeights[i];
                    }
                }
                serializedConstraint.ApplyModifiedProperties();
            }

            targets.SetItemSettings(CaptureSnapshot(targets));
            PrefabUtility.RecordPrefabInstancePropertyModifications(targets);
            EditorUtility.SetDirty(targets);
        }

        private static void CaptureOptionDefaults(EasyCarrySystemItemReference targets, EasyCarrySystemItemSettings snapshot)
        {
            var parametersComponent = targets.GeneratedEasyCarrySystem != null
                ? targets.GeneratedEasyCarrySystem.GetComponent<ModularAvatarParameters>()
                : null;
            if (parametersComponent == null || parametersComponent.parameters == null)
            {
                return;
            }

            for (var parameterIndex = 0; parameterIndex < parametersComponent.parameters.Count; parameterIndex++)
            {
                var parameter = parametersComponent.parameters[parameterIndex];
                if (parameter.isPrefix)
                {
                    continue;
                }

                if (parameter.nameOrPrefix == WorldFixedParameterName)
                {
                    snapshot.WorldFixedDefault = parameter.defaultValue > 0.5f;
                }

                for (var attachPointIndex = 0; attachPointIndex < HideWhenAttachedParameterNames.Length;
                     attachPointIndex++)
                {
                    if (parameter.nameOrPrefix == HideWhenAttachedParameterNames[attachPointIndex])
                    {
                        snapshot.HideWhenAttachedDefaults[attachPointIndex] = parameter.defaultValue > 0.5f;
                        break;
                    }
                }
            }
        }

        private static void RestoreOptionDefaults(EasyCarrySystemItemReference targets, EasyCarrySystemItemSettings snapshot)
        {
            var parametersComponent = targets.GeneratedEasyCarrySystem != null
                ? targets.GeneratedEasyCarrySystem.GetComponent<ModularAvatarParameters>()
                : null;
            if (parametersComponent == null || parametersComponent.parameters == null)
            {
                return;
            }

            Undo.RecordObject(parametersComponent, "Restore EasyCarry System Option Defaults");
            var changed = false;
            for (var parameterIndex = 0; parameterIndex < parametersComponent.parameters.Count; parameterIndex++)
            {
                var parameter = parametersComponent.parameters[parameterIndex];
                if (parameter.isPrefix)
                {
                    continue;
                }

                if (parameter.nameOrPrefix == WorldFixedParameterName)
                {
                    var defaultValue = snapshot.WorldFixedDefault ? 1f : 0f;
                    if (!Mathf.Approximately(parameter.defaultValue, defaultValue)
                        || !parameter.hasExplicitDefaultValue)
                    {
                        parameter.defaultValue = defaultValue;
                        parameter.hasExplicitDefaultValue = true;
                        parametersComponent.parameters[parameterIndex] = parameter;
                        changed = true;
                    }

                    continue;
                }

                for (var attachPointIndex = 0; attachPointIndex < HideWhenAttachedParameterNames.Length;
                     attachPointIndex++)
                {
                    if (parameter.nameOrPrefix != HideWhenAttachedParameterNames[attachPointIndex])
                    {
                        continue;
                    }

                    var defaultValue = snapshot.HideWhenAttachedDefaults[attachPointIndex] ? 1f : 0f;
                    if (!Mathf.Approximately(parameter.defaultValue, defaultValue)
                        || !parameter.hasExplicitDefaultValue)
                    {
                        parameter.defaultValue = defaultValue;
                        parameter.hasExplicitDefaultValue = true;
                        parametersComponent.parameters[parameterIndex] = parameter;
                        changed = true;
                    }

                    break;
                }
            }

            if (!changed)
            {
                return;
            }

            PrefabUtility.RecordPrefabInstancePropertyModifications(parametersComponent);
            EditorUtility.SetDirty(parametersComponent);
        }

        private static void RestoreBoneProxyAttachPoint(Transform root, string attachPointName, EasyCarrySystemAttachPointSettings snapshot)
        {
            var attachPoint = FindChildRecursive(root, attachPointName);
            if (attachPoint == null)
            {
                return;
            }

            if (snapshot.HasBoneProxy)
            {
                var boneProxy = attachPoint.GetComponent<ModularAvatarBoneProxy>();
                if (boneProxy != null)
                {
                    Undo.RecordObject(boneProxy, "Restore MA Bone Proxy Settings");
                    boneProxy.boneReference = (HumanBodyBones)snapshot.BoneReference;
                    boneProxy.subPath = snapshot.BoneSubPath;
                    boneProxy.attachmentMode = (BoneProxyAttachmentMode)snapshot.BoneAttachmentMode;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(boneProxy);
                    EditorUtility.SetDirty(boneProxy);
                }
            }

            if (snapshot.HasLocalTransform)
            {
                Undo.RecordObject(attachPoint, "Restore Attach Point Transform");
                attachPoint.localPosition = snapshot.LocalPosition;
                attachPoint.localRotation = snapshot.LocalRotation;
                attachPoint.localScale = snapshot.LocalScale;
                PrefabUtility.RecordPrefabInstancePropertyModifications(attachPoint);
                EditorUtility.SetDirty(attachPoint);
            }
        }

        private static void RestoreAttachmentMethod(Transform root, string attachPointName,
            EasyCarrySystemAttachPointMethod attachmentMethod)
        {
            var attachPoint = FindChildRecursive(root, attachPointName);
            if (attachPoint == null)
            {
                return;
            }

            var usesBoneProxy = attachPointName == "AP_Hand_L"
                || attachPointName == "AP_Hand_R"
                || attachmentMethod == EasyCarrySystemAttachPointMethod.BoneProxy;
            SetComponentEnabled(attachPoint.GetComponent<ModularAvatarBoneProxy>(), usesBoneProxy);
            SetComponentEnabled(FindParentConstraint(attachPoint), !usesBoneProxy);
        }

        private static void SetComponentEnabled(Component component, bool enabled)
        {
            if (component == null)
            {
                return;
            }

            var serializedComponent = new SerializedObject(component);
            var enabledProperty = serializedComponent.FindProperty("m_Enabled");
            if (enabledProperty == null || enabledProperty.boolValue == enabled)
            {
                return;
            }

            Undo.RecordObject(component, "Restore Attach Point Component");
            enabledProperty.boolValue = enabled;
            serializedComponent.ApplyModifiedProperties();
            PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            EditorUtility.SetDirty(component);
        }

        private static void ApplyAttachPoint(Transform root, string attachPointName, EasyCarrySystemAttachPointSettings snapshot)
        {
            var attachPoint = FindChildRecursive(root, attachPointName);
            var constraint = FindParentConstraint(attachPoint);
            if (constraint == null)
            {
                return;
            }

            var serializedConstraint = new SerializedObject(constraint);
            var sourceProperty = serializedConstraint.FindProperty(SourceTransformPath);
            var positionProperty = serializedConstraint.FindProperty(SourcePositionOffsetPath);
            var rotationProperty = serializedConstraint.FindProperty(SourceRotationOffsetPath);
            Undo.RecordObject(constraint, "Restore Attach Point Settings");
            if (sourceProperty != null) sourceProperty.objectReferenceValue = snapshot.SourceTransform;
            if (positionProperty != null) positionProperty.vector3Value = snapshot.PositionOffset;
            if (rotationProperty != null) rotationProperty.vector3Value = snapshot.RotationOffset;
            serializedConstraint.ApplyModifiedProperties();
        }

        private static Transform GetAttachPointSource(EasyCarrySystemItemReference targets, string attachPointName)
        {
            switch (attachPointName)
            {
                case "AP_Hand_L": return targets.APHandL;
                case "AP_Hand_R": return targets.APHandR;
                case "AP_00": return targets.AP00;
                case "AP_01": return targets.AP01;
                case "AP_02": return targets.AP02;
                case "AP_03": return targets.AP03;
                case "AP_04": return targets.AP04;
                case "AP_05": return targets.AP05;
                case "AP_06": return targets.AP06;
                default: return null;
            }
        }

        private static EasyCarrySystemContactSettings CaptureContact(Transform root, string contactName)
        {
            var contactTransform = FindChildRecursive(root, contactName);
            if (contactTransform == null)
            {
                return null;
            }

            var snapshot = new EasyCarrySystemContactSettings
            {
                Initialized = true,
                LocalPosition = contactTransform.localPosition,
                LocalRotation = contactTransform.localRotation,
                LocalScale = contactTransform.localScale,
            };

            var contactComponent = FindContactShapeComponent(contactTransform);
            if (contactComponent != null)
            {
                var serializedContact = new SerializedObject(contactComponent);
                var shapeProperty = serializedContact.FindProperty(ContactShapeTypePath);
                var radiusProperty = serializedContact.FindProperty(ContactRadiusPath);
                var heightProperty = serializedContact.FindProperty(ContactHeightPath);
                var sizeProperty = serializedContact.FindProperty(ContactSizePath);
                snapshot.HasShape = shapeProperty != null && radiusProperty != null;
                snapshot.ShapeType = shapeProperty != null ? shapeProperty.intValue : 0;
                snapshot.Radius = radiusProperty != null ? radiusProperty.floatValue : 0f;
                snapshot.Height = heightProperty != null ? heightProperty.floatValue : 0f;
                snapshot.Size = sizeProperty != null ? sizeProperty.vector3Value : Vector3.one;
            }

            return snapshot;
        }

        private static void RestoreContact(Transform contactTransform, EasyCarrySystemContactSettings snapshot, bool restoreScale)
        {
            if (contactTransform == null || snapshot == null || !snapshot.Initialized)
            {
                return;
            }

            Undo.RecordObject(contactTransform, "Restore Contact Transform");
            contactTransform.localPosition = snapshot.LocalPosition;
            contactTransform.localRotation = snapshot.LocalRotation;
            if (restoreScale)
            {
                contactTransform.localScale = snapshot.LocalScale;
            }
            PrefabUtility.RecordPrefabInstancePropertyModifications(contactTransform);
            EditorUtility.SetDirty(contactTransform);

            if (!snapshot.HasShape)
            {
                return;
            }

            foreach (var component in contactTransform.GetComponents<Component>())
            {
                if (!HasContactShapeProperties(component))
                {
                    continue;
                }

                var serializedContact = new SerializedObject(component);
                var shapeProperty = serializedContact.FindProperty(ContactShapeTypePath);
                var radiusProperty = serializedContact.FindProperty(ContactRadiusPath);
                var heightProperty = serializedContact.FindProperty(ContactHeightPath);
                var sizeProperty = serializedContact.FindProperty(ContactSizePath);
                Undo.RecordObject(component, "Restore Contact Shape");
                shapeProperty.intValue = snapshot.ShapeType;
                radiusProperty.floatValue = snapshot.Radius;
                if (heightProperty != null) heightProperty.floatValue = snapshot.Height;
                if (sizeProperty != null) sizeProperty.vector3Value = snapshot.Size;
                serializedContact.ApplyModifiedProperties();
                PrefabUtility.RecordPrefabInstancePropertyModifications(component);
                EditorUtility.SetDirty(component);
            }
        }

        private static Component FindParentConstraint(Transform target)
        {
            if (target == null)
            {
                return null;
            }

            foreach (var component in target.GetComponents<Component>())
            {
                if (component == null)
                {
                    continue;
                }
                var serializedComponent = new SerializedObject(component);
                if (serializedComponent.FindProperty(SourceTransformPath) != null)
                {
                    return component;
                }
            }
            return null;
        }

        private static Component FindContactShapeComponent(Transform target)
        {
            foreach (var component in target.GetComponents<Component>())
            {
                if (HasContactShapeProperties(component))
                {
                    return component;
                }
            }
            return null;
        }

        private static bool HasContactShapeProperties(Component component)
        {
            if (component == null)
            {
                return false;
            }
            var serializedComponent = new SerializedObject(component);
            return serializedComponent.FindProperty(ContactShapeTypePath) != null
                && serializedComponent.FindProperty(ContactRadiusPath) != null;
        }

        private static string GetObjectName(Transform targetTransform)
        {
            return targetTransform != null ? targetTransform.name : null;
        }

        private static string ResolveRestoredMenuName(
            string objectName, int sourceSlot, int targetSlot, string defaultNameFormat)
        {
            if (string.IsNullOrWhiteSpace(objectName))
            {
                return objectName;
            }

            var sourceDefaultName = string.Format(defaultNameFormat, sourceSlot);
            return objectName == sourceDefaultName
                ? string.Format(defaultNameFormat, targetSlot)
                : objectName;
        }

        private static void RestoreObjectName(Transform targetTransform, string objectName)
        {
            if (targetTransform == null
                || string.IsNullOrWhiteSpace(objectName)
                || targetTransform.name == objectName)
            {
                return;
            }

            Undo.RecordObject(targetTransform.gameObject, "Restore EasyCarry System Menu Name");
            targetTransform.name = objectName;
            PrefabUtility.RecordPrefabInstancePropertyModifications(targetTransform.gameObject);
            EditorUtility.SetDirty(targetTransform.gameObject);
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
                if (child.name == childName) return child;
                var found = FindChildRecursive(child, childName);
                if (found != null) return found;
            }
            return null;
        }


    }
}
