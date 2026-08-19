using System;
using System.Reflection;
using nadena.dev.modular_avatar.core;
using UnityEditor;
using UnityEngine;

namespace Serre.GrabSystem.Editor
{
    [InitializeOnLoad]
    internal static class GrabSystemEditorSharedUtility
    {
        private const string RuntimeLogoRelativePath = "Editor/Images/GrabSystem_Logo_Runtime.png";
        private const string AuthoringLogoRelativePath = "Editor/Authoring/GrabSystem_Logo_Authoring.png";
        private const string FallbackLogoRelativePath = "Editor/Images/GrabSystem_Logo.png";
        private const string ComponentIconRelativePath = "Editor/Images/GrabSystem_Icon.png";
        private const string ItemReferenceScriptRelativePath = "Scripts/GrabSystemItemReference.cs";
        private static Texture2D runtimeLogo;
        private static Texture2D authoringLogo;
        private static Texture2D grabSystemComponentIcon;
        private static bool componentIconApplied;
        private static double nextComponentIconLoadTime;
        private static bool hasAttachPointTransformClipboard;
        private static Vector3 attachPointClipboardPosition;
        private static Vector3 attachPointClipboardRotation;

        internal const string SourceTransformPath = "Sources.source0.SourceTransform";
        internal const string SourcePositionOffsetPath = "Sources.source0.ParentPositionOffset";
        internal const string SourceRotationOffsetPath = "Sources.source0.ParentRotationOffset";
        internal const string ContactShapeTypePath = "shapeType";
        internal const string ContactRadiusPath = "radius";
        internal const string ContactHeightPath = "height";
        internal const string ContactSizePath = "size";
        private static bool inspectorWasLocked;
        private static bool inspectorLockedByGrabSystem;
        private static readonly Type SceneHierarchyWindowType =
            typeof(EditorWindow).Assembly.GetType("UnityEditor.SceneHierarchyWindow");
        private static readonly MethodInfo SetExpandedRecursiveMethod =
            SceneHierarchyWindowType?.GetMethod(
                "SetExpandedRecursive",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new[] { typeof(int), typeof(bool) },
                null);


        internal static readonly string[] AttachPointNames =
        {
            "AP_Hand_L", "AP_Hand_R", "AP_00", "AP_01", "AP_02", "AP_03", "AP_04", "AP_05", "AP_06",
        };

        internal static readonly string[] NumberedAttachPointNames =
        {
            "AP_00", "AP_01", "AP_02", "AP_03", "AP_04", "AP_05", "AP_06",
        };

        internal static readonly string[] ContactNames =
        {
            "AP_Contact_Hand_L", "AP_Contact_Hand_R", "AP_Contact_00", "AP_Contact_01", "AP_Contact_02",
            "AP_Contact_03", "AP_Contact_04", "AP_Contact_05", "AP_Contact_06",
        };

        internal static readonly string[] ItemInputContactNames =
        {
            "GI_Input_GrabBlocked", "GI_Input_ForceReturn",
        };

        internal static readonly string[] ItemOutputContactNames =
        {
            "GI_Output_IsGrabbed_L", "GI_Output_IsGrabbed_R",
        };

        private static readonly string[] ItemAttachedParameterNames =
        {
            "AttachPoint/00/ItemAttached", "AttachPoint/01/ItemAttached", "AttachPoint/02/ItemAttached",
            "AttachPoint/03/ItemAttached", "AttachPoint/04/ItemAttached", "AttachPoint/05/ItemAttached",
            "AttachPoint/06/ItemAttached",
        };

        static GrabSystemEditorSharedUtility()
        {
            EditorApplication.playModeStateChanged += ResetAllAttachPointsToAP00OnPlayMode;
        }

        internal sealed class HorizontalMarginScope : GUI.Scope
        {
            private readonly float margin;

            internal HorizontalMarginScope(float margin = 12f)
            {
                this.margin = margin;
                GUILayout.BeginHorizontal();
                GUILayout.Space(margin);
            }

            protected override void CloseScope()
            {
                GUILayout.Space(margin);
                GUILayout.EndHorizontal();
            }
        }

        internal static void DrawRuntimeLogoHeader()
        {
            DrawLogoHeader(RuntimeLogoRelativePath, ref runtimeLogo);
        }

        internal static void DrawAuthoringLogoHeader()
        {
            DrawLogoHeader(AuthoringLogoRelativePath, ref authoringLogo);
        }

        private static void DrawLogoHeader(string logoRelativePath, ref Texture2D logo)
        {
            ApplyComponentIcon();

            var logoAssetPath = GrabSystemAssetLocator.GetAssetPath(logoRelativePath);
            var loadedLogoPath = logo != null ? AssetDatabase.GetAssetPath(logo) : string.Empty;
            if (logo == null || loadedLogoPath != logoAssetPath)
            {
                var editionLogo = GrabSystemAssetLocator.LoadAsset<Texture2D>(logoRelativePath);
                if (editionLogo != null)
                {
                    logo = editionLogo;
                }
                else if (logo == null)
                {
                    logo = GrabSystemAssetLocator.LoadAsset<Texture2D>(FallbackLogoRelativePath);
                }
            }

            if (logo == null)
            {
                return;
            }

            var availableWidth = Mathf.Max(160f, EditorGUIUtility.currentViewWidth - 36f);
            var height = Mathf.Clamp(availableWidth / 4f, 64f, 112f);
            using (new HorizontalMarginScope())
            {
                var rect = GUILayoutUtility.GetRect(0f, height, GUILayout.ExpandWidth(true));
                GUI.DrawTexture(rect, logo, ScaleMode.ScaleToFit, true);
            }

            EditorGUILayout.Space(6f);
        }

        internal static void DrawGrabSystemReference(GrabSystemItemReference targets)
        {
            var grabSystemObject = targets != null ? targets.GeneratedGrabSystem : null;
            using (new HorizontalMarginScope())
            {
                var rect = EditorGUILayout.GetControlRect();
                var label = new GUIContent(
                    "参照中のGrabSystem",
                    "現在このアイテムが参照しているGrabSystemです。クリックするとHierarchyで選択します。");
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUI.ObjectField(rect, label, grabSystemObject, typeof(GameObject), true);
                }

                if (grabSystemObject != null)
                {
                    var clickableRect = EditorGUI.IndentedRect(rect);
                    clickableRect.xMin += EditorGUIUtility.labelWidth;
                    EditorGUIUtility.AddCursorRect(clickableRect, MouseCursor.Link);

                    var currentEvent = Event.current;
                    if (currentEvent.type == EventType.MouseDown
                        && currentEvent.button == 0
                        && clickableRect.Contains(currentEvent.mousePosition))
                    {
                        Selection.activeObject = grabSystemObject;
                        EditorGUIUtility.PingObject(grabSystemObject);
                        currentEvent.Use();
                    }
                }
            }

            EditorGUILayout.Space(2f);
        }

        private static void ApplyComponentIcon()
        {
            if (componentIconApplied || EditorApplication.timeSinceStartup < nextComponentIconLoadTime)
            {
                return;
            }

            nextComponentIconLoadTime = EditorApplication.timeSinceStartup + 1d;
            if (grabSystemComponentIcon == null)
            {
                grabSystemComponentIcon =
                    GrabSystemAssetLocator.LoadAsset<Texture2D>(ComponentIconRelativePath);
            }

            if (grabSystemComponentIcon == null)
            {
                return;
            }

            var itemReferenceScript =
                GrabSystemAssetLocator.LoadAsset<MonoScript>(ItemReferenceScriptRelativePath);
            if (itemReferenceScript == null)
            {
                return;
            }

            EditorGUIUtility.SetIconForObject(itemReferenceScript, grabSystemComponentIcon);
            componentIconApplied = true;
        }

        internal static void DrawSectionHeader(string title)
        {
            var style = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                padding = new RectOffset(6, 6, 4, 4),
            };

            var rect = EditorGUILayout.GetControlRect(false, 25f);
            EditorGUI.DrawRect(rect, new Color(0.16f, 0.16f, 0.16f, 1f));
            EditorGUI.LabelField(rect, title, style);
        }

        internal static void DrawSectionDescription(string description)
        {
            EditorGUILayout.LabelField(
                description,
                CreateReadableStyle(EditorStyles.wordWrappedMiniLabel));
            EditorGUILayout.Space(3f);
        }

        internal static GUIStyle CreateReadableStyle(GUIStyle baseStyle)
        {
            var style = new GUIStyle(baseStyle);
            var buttonFontSize = GUI.skin.button.fontSize > 0
                ? GUI.skin.button.fontSize
                : 12;
            style.fontSize = Mathf.Max(style.fontSize, buttonFontSize);
            return style;
        }

        internal static void CollapseGrabSystemHierarchy(GrabSystemItemReference targets)
        {
            var grabSystemObject = targets != null ? targets.GeneratedGrabSystem : null;
            if (grabSystemObject == null || SceneHierarchyWindowType == null
                || SetExpandedRecursiveMethod == null)
            {
                return;
            }

            EditorApplication.delayCall += () =>
            {
                if (grabSystemObject == null)
                {
                    return;
                }

                foreach (var hierarchyWindow in Resources.FindObjectsOfTypeAll(SceneHierarchyWindowType))
                {
                    SetExpandedRecursiveMethod.Invoke(
                        hierarchyWindow,
                        new object[] { grabSystemObject.GetInstanceID(), false });
                }
            };
        }

        internal static void PrepareForSlotReplacement(GrabSystemItemReference targets)
        {
            if (targets == null)
            {
                return;
            }

            Undo.RecordObject(targets, "Prepare GrabSystem Slot Replacement");
            foreach (var attachPointName in AttachPointNames)
            {
                targets.SetAttachPointEditing(attachPointName, false);
                DestroyEditObjects(targets, attachPointName);
            }

            targets.SetGIItemSizeEditing(false);
            targets.SetGIItemSizeContactEditing(false);
            targets.SetGIInputContactEditing(false);
            targets.SetGIOutputContactEditing(false);
            SetItemOutputContactVisibility(targets, false);
            foreach (var contactName in ContactNames)
            {
                targets.SetContactEditing(contactName, false);
            }

            EditorUtility.SetDirty(targets);
            UnlockInspector();
        }

        internal static Transform GetItemContactGroupPrimary(GrabSystemItemReference targets, bool input)
        {
            if (targets == null || targets.GrabSystemRoot == null)
            {
                return null;
            }

            var contactNames = input ? ItemInputContactNames : ItemOutputContactNames;
            return FindChildRecursive(targets.GrabSystemRoot, contactNames[0]);
        }

        internal static void SetItemOutputContactVisibility(
            GrabSystemItemReference targets,
            bool visible)
        {
            if (targets == null || targets.GrabSystemRoot == null)
            {
                return;
            }

            foreach (var contactName in ItemOutputContactNames)
            {
                var contactTransform = FindChildRecursive(targets.GrabSystemRoot, contactName);
                if (contactTransform == null || contactTransform.gameObject.activeSelf == visible)
                {
                    continue;
                }

                contactTransform.gameObject.SetActive(visible);
                EditorUtility.SetDirty(contactTransform.gameObject);
            }
        }
        internal static bool IsItemContactGroupSelection(GrabSystemItemReference targets, bool input, Transform selected)
        {
            if (targets == null || targets.GrabSystemRoot == null || selected == null)
            {
                return false;
            }

            var contactNames = input ? ItemInputContactNames : ItemOutputContactNames;
            foreach (var contactName in contactNames)
            {
                if (FindChildRecursive(targets.GrabSystemRoot, contactName) == selected)
                {
                    return true;
                }
            }

            return false;
        }

        internal static void SyncItemContactGroupTransforms(GrabSystemItemReference targets)
        {
            if (targets == null || targets.GrabSystemRoot == null)
            {
                return;
            }

            SetItemOutputContactVisibility(targets, targets.GIOutputContactEditing);

            if (targets.GIInputContactEditing)
            {
                SyncItemContactGroupTransform(targets, true);
            }

            if (targets.GIOutputContactEditing)
            {
                SyncItemContactGroupTransform(targets, false);
            }
        }

        internal static void SyncItemContactGroupTransform(GrabSystemItemReference targets, bool input)
        {
            var primary = GetItemContactGroupPrimary(targets, input);
            if (primary == null)
            {
                return;
            }

            var contactNames = input ? ItemInputContactNames : ItemOutputContactNames;
            for (var index = 1; index < contactNames.Length; index++)
            {
                var contactTransform = FindChildRecursive(targets.GrabSystemRoot, contactNames[index]);
                if (contactTransform == null
                    || (Approximately(contactTransform.localPosition, primary.localPosition)
                        && Quaternion.Angle(contactTransform.localRotation, primary.localRotation) < 0.01f))
                {
                    continue;
                }

                Undo.RecordObject(contactTransform, input
                    ? "Sync GrabSystem Input Contact Transform"
                    : "Sync GrabSystem Output Contact Transform");
                contactTransform.localPosition = primary.localPosition;
                contactTransform.localRotation = primary.localRotation;
                PrefabUtility.RecordPrefabInstancePropertyModifications(contactTransform);
                EditorUtility.SetDirty(contactTransform);
            }
        }

        internal static void LockInspector()
        {
            if (inspectorLockedByGrabSystem)
            {
                return;
            }

            var tracker = ActiveEditorTracker.sharedTracker;
            inspectorWasLocked = tracker.isLocked;
            tracker.isLocked = true;
            tracker.ForceRebuild();
            inspectorLockedByGrabSystem = true;
        }

        internal static void UnlockInspector()
        {
            if (!inspectorLockedByGrabSystem)
            {
                return;
            }

            var tracker = ActiveEditorTracker.sharedTracker;
            tracker.isLocked = inspectorWasLocked;
            tracker.ForceRebuild();
            inspectorLockedByGrabSystem = false;
        }

        internal static void EnsureNumberedAttachPointListInitialized(GrabSystemItemReference targets)
        {
            if (targets == null || targets.NumberedAttachPointListInitialized)
            {
                return;
            }

            var existingOrder = targets.GetNumberedAttachPointOrder();
            if (existingOrder.Length > 0)
            {
                targets.SetNumberedAttachPointOrder(existingOrder);
            }
            else
            {
                var count = 0;
                for (var index = 0; index < NumberedAttachPointNames.Length; index++)
                {
                    if (IsNumberedAttachPointAssigned(targets, NumberedAttachPointNames[index]))
                    {
                        count = index + 1;
                    }
                }

                targets.InitializeNumberedAttachPointList(count);
            }
            PrefabUtility.RecordPrefabInstancePropertyModifications(targets);
            EditorUtility.SetDirty(targets);
        }

        internal static string GetNumberedAttachPointDisplayName(int attachPointIndex)
        {
            return attachPointIndex == 0
                ? "装備位置00（初期位置）"
                : $"装備位置 {attachPointIndex:00}";
        }

        internal static void SyncNumberedAttachPointAvailability(GrabSystemItemReference targets, bool recordUndo)
        {
            if (targets == null)
            {
                return;
            }

            EnsureNumberedAttachPointListInitialized(targets);
            for (var index = 0; index < NumberedAttachPointNames.Length; index++)
            {
                var attachPointName = NumberedAttachPointNames[index];
                var available = targets.IsNumberedAttachPointEnabled(index)
                    && IsNumberedAttachPointAssigned(targets, attachPointName);
                SetAttachPointAvailability(targets, attachPointName, available, recordUndo);
            }

            SyncParameterLocalOnly(targets, recordUndo);
        }

        internal static void PrepareForAvatarBuild(GrabSystemItemReference targets)
        {
            if (targets == null)
            {
                return;
            }

            // Output contacts are visible only while authoring and must never leak into a build.
            SetItemOutputContactVisibility(targets, false);
            SyncAllAttachmentMethodComponents(targets, false);
            SyncNumberedAttachPointAvailability(targets, false);

            foreach (var attachPointName in NumberedAttachPointNames)
            {
                if (targets.GetAttachPointMethod(attachPointName) != GrabSystemAttachPointMethod.ParentConstraint)
                {
                    continue;
                }

                var boneProxy = FindChildRecursive(targets.GrabSystemRoot, attachPointName)
                    ?.GetComponent<ModularAvatarBoneProxy>();
                if (boneProxy != null)
                {
                    UnityEngine.Object.DestroyImmediate(boneProxy);
                }
            }
        }

        internal static void SyncAllAttachmentMethodComponents(GrabSystemItemReference targets, bool recordUndo)
        {
            if (targets == null)
            {
                return;
            }

            foreach (var attachPointName in AttachPointNames)
            {
                var attachPoint = FindChildRecursive(targets.GrabSystemRoot, attachPointName);
                if (attachPoint == null)
                {
                    continue;
                }

                var usesBoneProxy = UsesBoneProxy(targets, attachPointName);
                SetComponentEnabled(attachPoint.GetComponent<ModularAvatarBoneProxy>(), usesBoneProxy, recordUndo);
                SetComponentEnabled(FindVrcParentConstraint(attachPoint), !usesBoneProxy, recordUndo);
            }
        }

        internal static bool EnsureMenuObjectReferences(GrabSystemItemReference targets)
        {
            if (targets == null)
            {
                return false;
            }

            var slot = Mathf.Clamp(targets.GISlot, 0, 15);
            var settingsRoot = IsTransformUnder(targets.MenuSettingsRoot, targets.GrabSystemRoot)
                ? targets.MenuSettingsRoot
                : FindChildRecursive(targets.GrabSystemRoot, $"GrabItem_{slot:00}_Settings");
            settingsRoot ??= FindChildRecursiveByNamePattern(targets.GrabSystemRoot, "GrabItem_", "_Settings");

            if (settingsRoot == null)
            {
                var resetCandidate = FindChildRecursive(targets.GrabSystemRoot, $"GI_{slot:00}_Reset")
                    ?? FindChildRecursiveByNamePattern(targets.GrabSystemRoot, "GI_", "_Reset");
                settingsRoot = resetCandidate != null ? resetCandidate.parent : null;
            }

            var resetItem = IsDirectChildOf(targets.MenuResetItem, settingsRoot)
                ? targets.MenuResetItem
                : FindDirectChild(settingsRoot, $"GI_{slot:00}_Reset");
            resetItem ??= FindDirectChildBySuffix(settingsRoot, "_Reset");

            var switchHandsItem = IsDirectChildOf(targets.MenuSwitchHandsItem, settingsRoot)
                ? targets.MenuSwitchHandsItem
                : FindDirectChild(settingsRoot, $"GI_{slot:00}_SwitchHands_Enable");
            switchHandsItem ??= FindDirectChildBySuffix(settingsRoot, "_SwitchHands_Enable");

            var freezeItem = IsDirectChildOf(targets.MenuFreezeItem, settingsRoot)
                ? targets.MenuFreezeItem
                : FindDirectChild(settingsRoot, $"GI_{slot:00}_Freeze_Enable");
            freezeItem ??= FindDirectChildBySuffix(settingsRoot, "_Freeze_Enable");

            if (targets.MenuSettingsRoot != settingsRoot || targets.MenuResetItem != resetItem
                || targets.MenuSwitchHandsItem != switchHandsItem || targets.MenuFreezeItem != freezeItem)
            {
                targets.SetMenuObjects(settingsRoot, resetItem, switchHandsItem, freezeItem);
                PrefabUtility.RecordPrefabInstancePropertyModifications(targets);
                EditorUtility.SetDirty(targets);
            }

            return settingsRoot != null && resetItem != null && switchHandsItem != null && freezeItem != null;
        }

        internal static bool UsesBoneProxy(GrabSystemItemReference targets, string attachPointName)
        {
            return !IsNumberedAttachPoint(attachPointName)
                || targets.GetAttachPointMethod(attachPointName) == GrabSystemAttachPointMethod.BoneProxy;
        }

        internal static void HandleAttachPointTransformClipboardContext(
            Rect rect,
            GrabSystemItemReference targets,
            string attachPointName)
        {
            var currentEvent = Event.current;
            if (currentEvent == null
                || currentEvent.type != EventType.ContextClick
                || !rect.Contains(currentEvent.mousePosition))
            {
                return;
            }

            var attachPoint = targets != null
                ? FindChildRecursive(targets.GrabSystemRoot, attachPointName)
                : null;
            var menu = new GenericMenu();
            if (attachPoint != null)
            {
                menu.AddItem(new GUIContent("位置・回転をコピー"), false,
                    () => CopyAttachPointTransform(targets, attachPointName));
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("位置・回転をコピー"));
            }

            if (attachPoint != null && hasAttachPointTransformClipboard)
            {
                menu.AddItem(new GUIContent("位置・回転をペースト"), false,
                    () => PasteAttachPointTransform(targets, attachPointName));
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("位置・回転をペースト"));
            }

            menu.ShowAsContext();
            currentEvent.Use();
        }

        private static void CopyAttachPointTransform(
            GrabSystemItemReference targets,
            string attachPointName)
        {
            var attachPoint = FindChildRecursive(targets.GrabSystemRoot, attachPointName);
            if (attachPoint == null)
            {
                return;
            }

            if (UsesBoneProxy(targets, attachPointName))
            {
                attachPointClipboardPosition = attachPoint.localPosition;
                attachPointClipboardRotation = NormalizeEulerAngles(attachPoint.localEulerAngles);
            }
            else
            {
                attachPointClipboardPosition = targets.GetAttachPointPositionOffset(attachPointName);
                attachPointClipboardRotation = targets.GetAttachPointRotationOffset(attachPointName);
            }

            hasAttachPointTransformClipboard = true;
        }

        private static void PasteAttachPointTransform(
            GrabSystemItemReference targets,
            string attachPointName)
        {
            if (!hasAttachPointTransformClipboard || targets == null)
            {
                return;
            }

            var attachPoint = FindChildRecursive(targets.GrabSystemRoot, attachPointName);
            if (attachPoint == null)
            {
                return;
            }

            var undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName($"Paste {attachPointName} Transform");
            Undo.RecordObject(targets, $"Paste {attachPointName} Transform");

            if (UsesBoneProxy(targets, attachPointName))
            {
                Undo.RecordObject(attachPoint, $"Paste {attachPointName} Transform");
                attachPoint.localPosition = attachPointClipboardPosition;
                attachPoint.localRotation = Quaternion.Euler(attachPointClipboardRotation);
                PrefabUtility.RecordPrefabInstancePropertyModifications(attachPoint);
                EditorUtility.SetDirty(attachPoint);
            }
            else
            {
                targets.SetAttachPointOffsets(
                    attachPointName,
                    attachPointClipboardPosition,
                    attachPointClipboardRotation);
                ApplyAttachPointConstraintOffset(
                    attachPoint,
                    attachPointName,
                    attachPointClipboardPosition,
                    attachPointClipboardRotation);
            }

            GrabSystemSlotEditorUtility.CaptureAndStoreSettings(targets);
            PrefabUtility.RecordPrefabInstancePropertyModifications(targets);
            EditorUtility.SetDirty(targets);
            Undo.CollapseUndoOperations(undoGroup);
            SceneView.RepaintAll();
        }

        private static void ApplyAttachPointConstraintOffset(
            Transform attachPoint,
            string attachPointName,
            Vector3 position,
            Vector3 rotation)
        {
            var constraint = FindVrcParentConstraint(attachPoint);
            if (constraint == null)
            {
                return;
            }

            var serializedConstraint = new SerializedObject(constraint);
            var positionProperty = serializedConstraint.FindProperty(SourcePositionOffsetPath);
            var rotationProperty = serializedConstraint.FindProperty(SourceRotationOffsetPath);
            if (positionProperty == null || rotationProperty == null)
            {
                return;
            }

            Undo.RecordObject(constraint, $"Paste {attachPointName} Transform");
            positionProperty.vector3Value = position;
            rotationProperty.vector3Value = rotation;
            serializedConstraint.ApplyModifiedProperties();
            PrefabUtility.RecordPrefabInstancePropertyModifications(constraint);
            EditorUtility.SetDirty(constraint);
        }

        internal static bool IsAttachPointAssigned(GrabSystemItemReference targets, string attachPointName)
        {
            if (!IsNumberedAttachPoint(attachPointName))
            {
                return FindChildRecursive(targets.GrabSystemRoot, attachPointName) != null;
            }

            return IsNumberedAttachPointAssigned(targets, attachPointName);
        }

        internal static Transform GetAttachPointReference(GrabSystemItemReference targets, string attachPointName)
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

        internal static Transform FindChildRecursive(Transform parent, string childName)
        {
            if (parent == null)
            {
                return null;
            }

            for (var index = 0; index < parent.childCount; index++)
            {
                var child = parent.GetChild(index);
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

        internal static Transform FindDirectChild(Transform parent, string childName)
        {
            if (parent == null)
            {
                return null;
            }

            for (var index = 0; index < parent.childCount; index++)
            {
                var child = parent.GetChild(index);
                if (child.name == childName)
                {
                    return child;
                }
            }

            return null;
        }

        internal static Component FindVrcParentConstraint(Transform targetTransform)
        {
            if (targetTransform == null)
            {
                return null;
            }

            foreach (var component in targetTransform.GetComponents<Component>())
            {
                if (component == null || !component.GetType().Name.Contains("ParentConstraint"))
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

        internal static Component FindContactComponent(Transform targetTransform, string typeName)
        {
            if (targetTransform == null)
            {
                return null;
            }

            Component fallback = null;
            foreach (var component in targetTransform.GetComponents<Component>())
            {
                if (!HasContactShapeProperties(component))
                {
                    continue;
                }

                fallback ??= component;
                if (component.GetType().Name.Contains(typeName))
                {
                    return component;
                }
            }

            return fallback;
        }

        internal static void SetMainConstraintSourceWeights(
            Transform root,
            string activeSourceName,
            bool recordUndo = true)
        {
            var mainConstraint = FindVrcParentConstraint(FindChildRecursive(root, "GI_MainConst"));
            if (mainConstraint == null)
            {
                return;
            }

            var activeIndex = -1;
            var serializedConstraint = new SerializedObject(mainConstraint);
            for (var index = 0; index < 16; index++)
            {
                var sourceProperty = serializedConstraint.FindProperty($"Sources.source{index}.SourceTransform");
                var source = sourceProperty?.objectReferenceValue as Transform;
                if (!string.IsNullOrEmpty(activeSourceName) && source != null && source.name == activeSourceName)
                {
                    activeIndex = index;
                }
            }

            if (recordUndo)
            {
                Undo.RecordObject(mainConstraint, "Set GI_MainConst Weights");
            }
            for (var index = 0; index < 16; index++)
            {
                var weight = serializedConstraint.FindProperty($"Sources.source{index}.Weight");
                if (weight != null)
                {
                    weight.floatValue = index == activeIndex ? 1f : 0f;
                }
            }

            if (recordUndo)
            {
                serializedConstraint.ApplyModifiedProperties();
                PrefabUtility.RecordPrefabInstancePropertyModifications(mainConstraint);
            }
            else
            {
                serializedConstraint.ApplyModifiedPropertiesWithoutUndo();
            }
            EditorUtility.SetDirty(mainConstraint);
        }

        private static void ResetAllAttachPointsToAP00OnPlayMode(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode)
            {
                return;
            }

            var targets = Resources.FindObjectsOfTypeAll<GrabSystemItemReference>();
            var undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Reset GrabSystem Attach Points To AP 00");

            foreach (var target in targets)
            {
                if (target == null || EditorUtility.IsPersistent(target)
                    || !target.gameObject.scene.IsValid() || !target.gameObject.scene.isLoaded)
                {
                    continue;
                }

                SetMainConstraintSourceWeights(target.GrabSystemRoot, "AP_00");
            }

            Undo.CollapseUndoOperations(undoGroup);
        }

        internal static Vector3 NormalizeEulerAngles(Vector3 eulerAngles)
        {
            return new Vector3(NormalizeAngle(eulerAngles.x), NormalizeAngle(eulerAngles.y), NormalizeAngle(eulerAngles.z));
        }

        internal static bool Approximately(Vector3 a, Vector3 b)
        {
            return Mathf.Approximately(a.x, b.x) && Mathf.Approximately(a.y, b.y) && Mathf.Approximately(a.z, b.z);
        }

        private static bool IsNumberedAttachPointAssigned(GrabSystemItemReference targets, string attachPointName)
        {
            if (UsesBoneProxy(targets, attachPointName))
            {
                var boneProxy = FindChildRecursive(targets.GrabSystemRoot, attachPointName)
                    ?.GetComponent<ModularAvatarBoneProxy>();
                return boneProxy != null && boneProxy.boneReference != HumanBodyBones.LastBone;
            }

            return GetAttachPointReference(targets, attachPointName) != null;
        }

        private static void SetAttachPointAvailability(GrabSystemItemReference targets, string attachPointName,
            bool available, bool recordUndo)
        {
            var attachPoint = FindChildRecursive(targets.GrabSystemRoot, attachPointName);
            if (attachPoint == null)
            {
                return;
            }

            var gameObject = attachPoint.gameObject;
            var desiredTag = available ? "Untagged" : "EditorOnly";
            if (gameObject.activeSelf == available && gameObject.CompareTag(desiredTag))
            {
                return;
            }

            if (recordUndo)
            {
                Undo.RecordObject(gameObject, $"Change {attachPointName} Availability");
            }

            gameObject.tag = desiredTag;
            gameObject.SetActive(available);
            PrefabUtility.RecordPrefabInstancePropertyModifications(gameObject);
            EditorUtility.SetDirty(gameObject);
        }

        private static void SyncParameterLocalOnly(GrabSystemItemReference targets, bool recordUndo)
        {
            var parameters = targets.GeneratedGrabSystem != null
                ? targets.GeneratedGrabSystem.GetComponent<ModularAvatarParameters>()
                : null;
            if (parameters == null || parameters.parameters == null)
            {
                return;
            }

            var undoRecorded = false;
            var changed = false;
            for (var attachPointIndex = 0; attachPointIndex < ItemAttachedParameterNames.Length; attachPointIndex++)
            {
                var localOnly = !targets.IsNumberedAttachPointEnabled(attachPointIndex);
                for (var parameterIndex = 0; parameterIndex < parameters.parameters.Count; parameterIndex++)
                {
                    var parameter = parameters.parameters[parameterIndex];
                    if (parameter.isPrefix || parameter.nameOrPrefix != ItemAttachedParameterNames[attachPointIndex]
                        || parameter.localOnly == localOnly)
                    {
                        continue;
                    }

                    if (recordUndo && !undoRecorded)
                    {
                        Undo.RecordObject(parameters, "Update AP Parameter Sync");
                        undoRecorded = true;
                    }

                    parameter.localOnly = localOnly;
                    parameters.parameters[parameterIndex] = parameter;
                    changed = true;
                    break;
                }
            }

            if (changed)
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(parameters);
                EditorUtility.SetDirty(parameters);
            }
        }

        private static void SetComponentEnabled(Component component, bool enabled, bool recordUndo)
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

            if (recordUndo)
            {
                Undo.RecordObject(component, "Change Attach Point Component");
            }

            enabledProperty.boolValue = enabled;
            if (recordUndo)
            {
                serializedComponent.ApplyModifiedProperties();
            }
            else
            {
                serializedComponent.ApplyModifiedPropertiesWithoutUndo();
            }

            PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            EditorUtility.SetDirty(component);
        }

        private static bool IsNumberedAttachPoint(string attachPointName)
        {
            return Array.IndexOf(NumberedAttachPointNames, attachPointName) >= 0;
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

        private static void DestroyEditObjects(GrabSystemItemReference targets, string attachPointName)
        {
            var editRoot = targets.GetAttachPointEditRoot(attachPointName)
                ?? FindDirectChild(targets.GrabSystemRoot, $"__GrabSystem_{attachPointName}_EditRoot");
            if (editRoot != null)
            {
                Undo.DestroyObjectImmediate(editRoot.gameObject);
            }

            targets.SetAttachPointEditObjects(attachPointName, null, null);
        }

        private static bool IsTransformUnder(Transform candidate, Transform parent)
        {
            return candidate != null && parent != null && candidate != parent && candidate.IsChildOf(parent);
        }

        private static bool IsDirectChildOf(Transform candidate, Transform parent)
        {
            return candidate != null && parent != null && candidate.parent == parent;
        }

        private static Transform FindChildRecursiveByNamePattern(Transform parent, string prefix, string suffix)
        {
            if (parent == null)
            {
                return null;
            }

            for (var index = 0; index < parent.childCount; index++)
            {
                var child = parent.GetChild(index);
                if (child.name.StartsWith(prefix, StringComparison.Ordinal)
                    && child.name.EndsWith(suffix, StringComparison.Ordinal))
                {
                    return child;
                }

                var found = FindChildRecursiveByNamePattern(child, prefix, suffix);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static Transform FindDirectChildBySuffix(Transform parent, string suffix)
        {
            if (parent == null)
            {
                return null;
            }

            for (var index = 0; index < parent.childCount; index++)
            {
                var child = parent.GetChild(index);
                if (child.name.EndsWith(suffix, StringComparison.Ordinal))
                {
                    return child;
                }
            }

            return null;
        }

        private static float NormalizeAngle(float angle)
        {
            angle %= 360f;
            if (angle > 180f)
            {
                angle -= 360f;
            }
            else if (angle < -180f)
            {
                angle += 360f;
            }

            return angle;
        }
    }
}
