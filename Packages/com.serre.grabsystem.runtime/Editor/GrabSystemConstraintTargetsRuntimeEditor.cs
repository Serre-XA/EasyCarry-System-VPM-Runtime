using System.Collections.Generic;
using nadena.dev.modular_avatar.core;
using UnityEditor;
using UnityEngine;

namespace Serre.GrabSystem.Editor
{
    [CustomEditor(typeof(GrabSystemItemReference), true, isFallback = true)]
    internal sealed class GrabSystemItemReferenceRuntimeEditor : UnityEditor.Editor
    {
        private static int activeEditorCount;
        private static bool suppressSelectionChange;

        private readonly Dictionary<string, bool> foldouts = new Dictionary<string, bool>();

        private void OnEnable()
        {
            activeEditorCount++;
            if (activeEditorCount == 1)
            {
                EditorApplication.update += SyncEditHandles;
                Selection.selectionChanged += EndEditWhenSelectionLeaves;
            }

            if (target is GrabSystemItemReference targets)
            {
                GrabSystemEditorSharedUtility.EnsureNumberedAttachPointListInitialized(targets);
                GrabSystemEditorSharedUtility.SyncNumberedAttachPointAvailability(targets, false);
            }
        }

        private void OnDisable()
        {
            activeEditorCount = Mathf.Max(0, activeEditorCount - 1);
            if (activeEditorCount == 0)
            {
                EditorApplication.update -= SyncEditHandles;
                Selection.selectionChanged -= EndEditWhenSelectionLeaves;
            }
        }

        public override void OnInspectorGUI()
        {
            var targets = (GrabSystemItemReference)target;
            serializedObject.Update();
            GrabSystemEditorSharedUtility.DrawRuntimeLogoHeader();
            GrabSystemEditorSharedUtility.DrawGrabSystemReference(targets);
            if (targets.GrabSystemRoot == null)
            {
                EditorGUILayout.HelpBox("生成されたGrabSystemがありません。GrabSystem Setupを実行してください。", MessageType.Warning);
                return;
            }

            EditorGUI.BeginChangeCheck();
            GrabSystemSlotEditorUtility.DrawSlotSelector(targets);
            DrawItemCollisionSection(targets);
            DrawHandSection(targets);
            DrawNumberedAttachPointSection(targets);

            serializedObject.ApplyModifiedProperties();
            if (EditorGUI.EndChangeCheck() && !Application.isPlaying)
            {
                GrabSystemSlotEditorUtility.CaptureAndStoreSettings(targets);
            }
        }

        private void DrawItemCollisionSection(GrabSystemItemReference targets)
        {
            EditorGUILayout.Space(8f);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                GrabSystemEditorSharedUtility.DrawSectionHeader("アイテムの当たり判定");
                GrabSystemEditorSharedUtility.DrawSectionDescription(
                    "アイテムを掴める範囲と位置を調整します。");

                DrawReadOnlyReference("対象", targets.GIItemSize);
                var editing = targets.GIItemSizeContactEditing;
                if (DrawEditButton(
                        "当たり判定調整",
                        editing,
                        "アイテムの当たり判定位置、形状、サイズを調整します。"))
                {
                    ToggleItemCollisionEdit(targets);
                    GUIUtility.ExitGUI();
                }

                if (!editing || targets.GIItemSize == null)
                {
                    return;
                }

                EditorGUILayout.Space(3f);
                DrawTransformFields(targets.GIItemSize);
                DrawContactShapeFields(
                    GrabSystemEditorSharedUtility.FindContactComponent(targets.GIItemSize, "ContactSender"));
            }
        }

        private void DrawHandSection(GrabSystemItemReference targets)
        {
            EditorGUILayout.Space(8f);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                GrabSystemEditorSharedUtility.DrawSectionHeader("手持ち位置");
                GrabSystemEditorSharedUtility.DrawSectionDescription(
                    "左右の手で持ったときの位置を調整します。");

                DrawHandCard(targets, "AP_Hand_L", "左手", "右手へ反転コピー");
                DrawHandCard(targets, "AP_Hand_R", "右手", "左手へ反転コピー");
            }
        }

        private void DrawHandCard(GrabSystemItemReference targets, string attachPointName, string label,
            string mirrorLabel)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
                    var mirrorButtonStyle = GrabSystemEditorSharedUtility.CreateReadableStyle(
                        EditorStyles.miniButton);
                    if (GUILayout.Button(new GUIContent(mirrorLabel,
                            "位置と回転をアバターRoot基準で反転し、反対側の手へコピーします。"),
                            mirrorButtonStyle, GUILayout.Width(128f)))
                    {
                        MirrorHandPosition(targets, attachPointName);
                    }
                }

                var editing = targets.GetAttachPointEditing(attachPointName);
                if (DrawEditButton("位置調整", editing, "Scene上のギズモで手持ち位置を調整します。"))
                {
                    ToggleAttachPointEdit(targets, attachPointName);
                    GUIUtility.ExitGUI();
                }

                if (editing)
                {
                    DrawActiveAttachPointValues(targets, attachPointName);
                }
            }
        }

        private void DrawNumberedAttachPointSection(GrabSystemItemReference targets)
        {
            EditorGUILayout.Space(8f);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                GrabSystemEditorSharedUtility.DrawSectionHeader(
                    $"装備位置リスト ({targets.NumberedAttachPointCount}/7)");
                GrabSystemEditorSharedUtility.DrawSectionDescription(
                    "Authoring Toolsで登録された装備位置の位置調整ができます。");

                var order = targets.GetNumberedAttachPointOrder();
                if (order.Length == 0)
                {
                    EditorGUILayout.HelpBox("登録されている装備位置はありません。", MessageType.Info);
                    return;
                }

                foreach (var index in order)
                {
                    if (index < 0 || index >= GrabSystemEditorSharedUtility.NumberedAttachPointNames.Length)
                    {
                        continue;
                    }

                    DrawNumberedAttachPointCard(targets,
                        GrabSystemEditorSharedUtility.NumberedAttachPointNames[index]);
                }
            }
        }

        private void DrawNumberedAttachPointCard(GrabSystemItemReference targets, string attachPointName)
        {
            var expanded = !foldouts.TryGetValue(attachPointName, out var value) || value;
            var attachPointIndex = System.Array.IndexOf(
                GrabSystemEditorSharedUtility.NumberedAttachPointNames, attachPointName);
            var label = attachPointIndex >= 0
                ? GrabSystemEditorSharedUtility.GetNumberedAttachPointDisplayName(attachPointIndex)
                : attachPointName;

            using (new GrabSystemEditorSharedUtility.HorizontalMarginScope())
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.ExpandWidth(true)))
                {
                    var rect = EditorGUILayout.GetControlRect(false, 24f);
                    var foldoutStyle = new GUIStyle(EditorStyles.foldout)
                    {
                        alignment = TextAnchor.MiddleLeft,
                        fontStyle = FontStyle.Bold,
                        fontSize = 13,
                        fixedHeight = 24f,
                        padding = new RectOffset(14, 4, 0, 0),
                    };
                    expanded = EditorGUI.Foldout(rect, expanded, label, true, foldoutStyle);
                    GrabSystemEditorSharedUtility.HandleAttachPointTransformClipboardContext(
                        rect,
                        targets,
                        attachPointName);
                    foldouts[attachPointName] = expanded;
                    if (!expanded)
                    {
                        return;
                    }

                    using (new EditorGUI.IndentLevelScope())
                    {
                        DrawReadOnlyReference("参照先オブジェクト",
                            ResolveAttachPointReference(targets, attachPointName));
                        if (!GrabSystemEditorSharedUtility.IsAttachPointAssigned(targets, attachPointName))
                        {
                            EditorGUILayout.HelpBox("この装備位置には接続先が設定されていません。", MessageType.Info);
                            return;
                        }

                        var editing = targets.GetAttachPointEditing(attachPointName);
                        if (DrawEditButton("位置調整", editing, "Scene上のギズモで装備位置を調整します。"))
                        {
                            ToggleAttachPointEdit(targets, attachPointName);
                            GUIUtility.ExitGUI();
                        }

                        if (editing)
                        {
                            DrawActiveAttachPointValues(targets, attachPointName);
                        }
                    }
                }
            }
        }

        private static bool DrawEditButton(string label, bool editing, string tooltip)
        {
            var previousColor = GUI.color;
            if (editing)
            {
                GUI.color = new Color(0.55f, 1f, 0.55f);
            }

            var clicked = GUILayout.Button(new GUIContent(label, tooltip), GUILayout.Height(22f));
            GUI.color = previousColor;
            return clicked;
        }

        private static void DrawReadOnlyReference(string label, Transform value)
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField(label, value, typeof(Transform), true);
            }
        }

        private static Transform ResolveAttachPointReference(
            GrabSystemItemReference targets,
            string attachPointName)
        {
            var attachPoint = GrabSystemEditorSharedUtility.FindChildRecursive(
                targets.GrabSystemRoot, attachPointName);
            if (GrabSystemEditorSharedUtility.UsesBoneProxy(targets, attachPointName))
            {
                var boneProxy = attachPoint != null
                    ? attachPoint.GetComponent<ModularAvatarBoneProxy>()
                    : null;
                if (boneProxy == null || boneProxy.boneReference == HumanBodyBones.LastBone)
                {
                    return null;
                }

                for (var current = attachPoint; current != null; current = current.parent)
                {
                    var animator = current.GetComponent<Animator>();
                    if (animator == null || !animator.isHuman)
                    {
                        continue;
                    }

                    var reference = animator.GetBoneTransform(boneProxy.boneReference);
                    if (reference != null && !string.IsNullOrEmpty(boneProxy.subPath))
                    {
                        reference = reference.Find(boneProxy.subPath);
                    }

                    return reference;
                }

                return null;
            }

            var referenceTransform = GrabSystemEditorSharedUtility.GetAttachPointReference(
                targets, attachPointName);
            if (referenceTransform != null)
            {
                return referenceTransform;
            }

            var constraint = GrabSystemEditorSharedUtility.FindVrcParentConstraint(attachPoint);
            var serializedConstraint = constraint != null ? new SerializedObject(constraint) : null;
            return serializedConstraint?.FindProperty(GrabSystemEditorSharedUtility.SourceTransformPath)
                ?.objectReferenceValue as Transform;
        }

        private static void DrawTransformFields(Transform value)
        {
            if (value == null)
            {
                return;
            }

            EditorGUI.BeginChangeCheck();
            var localPosition = EditorGUILayout.Vector3Field("Position", value.localPosition);
            var localRotation = EditorGUILayout.Vector3Field("Rotation", value.localEulerAngles);
            var localScale = EditorGUILayout.Vector3Field("Scale", value.localScale);
            if (!EditorGUI.EndChangeCheck())
            {
                return;
            }

            Undo.RecordObject(value, "Edit GrabSystem Transform");
            value.localPosition = localPosition;
            value.localEulerAngles = localRotation;
            value.localScale = localScale;
            PrefabUtility.RecordPrefabInstancePropertyModifications(value);
            EditorUtility.SetDirty(value);
        }

        private static void DrawContactShapeFields(Component contact)
        {
            if (contact == null)
            {
                EditorGUILayout.HelpBox("VRC Contact Sender が見つかりません。", MessageType.Warning);
                return;
            }

            var serializedContact = new SerializedObject(contact);
            serializedContact.Update();
            var shapeType = serializedContact.FindProperty(GrabSystemEditorSharedUtility.ContactShapeTypePath);
            var radius = serializedContact.FindProperty(GrabSystemEditorSharedUtility.ContactRadiusPath);
            var height = serializedContact.FindProperty(GrabSystemEditorSharedUtility.ContactHeightPath);
            var size = serializedContact.FindProperty(GrabSystemEditorSharedUtility.ContactSizePath);
            if (shapeType == null || radius == null)
            {
                EditorGUILayout.HelpBox("Contactの形状プロパティが見つかりません。", MessageType.Warning);
                return;
            }

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(shapeType, new GUIContent("Shape Type"));
            var shapeName = string.Empty;
            if (shapeType.propertyType == SerializedPropertyType.Enum)
            {
                var displayNames = shapeType.enumDisplayNames;
                shapeName = displayNames != null && shapeType.enumValueIndex >= 0
                    && shapeType.enumValueIndex < displayNames.Length
                    ? displayNames[shapeType.enumValueIndex] : string.Empty;
            }
            var isBox = shapeName.Contains("Box") || shapeType.intValue == 2;
            var isCapsule = shapeName.Contains("Capsule") || shapeType.intValue == 1;
            if (isBox && size != null)
            {
                EditorGUILayout.PropertyField(size, new GUIContent("Size"));
            }
            else
            {
                EditorGUILayout.PropertyField(radius, new GUIContent("Radius"));
                if (isCapsule && height != null)
                {
                    EditorGUILayout.PropertyField(height, new GUIContent("Height"));
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                serializedContact.ApplyModifiedProperties();
                PrefabUtility.RecordPrefabInstancePropertyModifications(contact);
                EditorUtility.SetDirty(contact);
            }
        }

        private static void DrawActiveAttachPointValues(GrabSystemItemReference targets, string attachPointName)
        {
            EditorGUILayout.Space(3f);
            if (GrabSystemEditorSharedUtility.UsesBoneProxy(targets, attachPointName))
            {
                DrawTransformFields(GrabSystemEditorSharedUtility.FindChildRecursive(targets.GrabSystemRoot, attachPointName));
                return;
            }

            var position = targets.GetAttachPointPositionOffset(attachPointName);
            var rotation = targets.GetAttachPointRotationOffset(attachPointName);
            EditorGUI.BeginChangeCheck();
            position = EditorGUILayout.Vector3Field("Position Offset", position);
            rotation = EditorGUILayout.Vector3Field("Rotation Offset", rotation);
            if (!EditorGUI.EndChangeCheck())
            {
                return;
            }

            Undo.RecordObject(targets, $"Edit {attachPointName} Offset");
            targets.SetAttachPointOffsets(attachPointName, position, rotation);
            ApplyConstraintOffset(targets, attachPointName, position, rotation);
            EditorUtility.SetDirty(targets);
        }

        private static void ToggleItemCollisionEdit(GrabSystemItemReference targets)
        {
            var next = !targets.GIItemSizeContactEditing;
            suppressSelectionChange = true;
            try
            {
                Undo.RecordObject(targets, "Toggle Item Collision Edit");
                EndAllEditModes(targets, false);
                targets.SetGIItemSizeContactEditing(next);
                targets.SetGIItemSizeEditing(next);
                EditorUtility.SetDirty(targets);

                if (next && targets.GIItemSize != null)
                {
                    GrabSystemEditorSharedUtility.SetMainConstraintSourceWeights(targets.GrabSystemRoot, null);
                    LockInspector();
                    Selection.activeTransform = targets.GIItemSize;
                }
                else
                {
                    UnlockInspectorIfIdle(targets);
                    Selection.activeObject = targets.gameObject;
                    GrabSystemEditorSharedUtility.CollapseGrabSystemHierarchy(targets);
                }
            }
            finally
            {
                suppressSelectionChange = false;
            }
        }

        private static void ToggleAttachPointEdit(GrabSystemItemReference targets, string attachPointName)
        {
            var next = !targets.GetAttachPointEditing(attachPointName);
            suppressSelectionChange = true;
            try
            {
                Undo.RecordObject(targets, $"Toggle {attachPointName} Edit");
                EndAllEditModes(targets, false);
                targets.SetAttachPointEditing(attachPointName, next);
                EditorUtility.SetDirty(targets);

                if (!next)
                {
                    DeleteEditObjects(targets, attachPointName);
                    UnlockInspectorIfIdle(targets);
                    Selection.activeObject = targets.gameObject;
                    GrabSystemEditorSharedUtility.CollapseGrabSystemHierarchy(targets);
                    return;
                }

                GrabSystemEditorSharedUtility.SetMainConstraintSourceWeights(targets.GrabSystemRoot, attachPointName);
                if (GrabSystemEditorSharedUtility.UsesBoneProxy(targets, attachPointName))
                {
                    var attachPoint = GrabSystemEditorSharedUtility.FindChildRecursive(targets.GrabSystemRoot, attachPointName);
                    if (attachPoint == null)
                    {
                        targets.SetAttachPointEditing(attachPointName, false);
                        return;
                    }

                    InitializeBoneProxyTransform(attachPoint, attachPointName);
                    LockInspector();
                    Selection.activeTransform = attachPoint;
                }
                else
                {
                    CreateEditObjects(targets, attachPointName);
                }
            }
            finally
            {
                suppressSelectionChange = false;
            }
        }

        private static void EndAllEditModes(GrabSystemItemReference targets, bool unlockInspector)
        {
            GrabSystemSlotEditorUtility.CaptureAndStoreSettings(targets);
            foreach (var attachPointName in GrabSystemEditorSharedUtility.AttachPointNames)
            {
                targets.SetAttachPointEditing(attachPointName, false);
                DeleteEditObjects(targets, attachPointName);
            }

            targets.SetGIItemSizeEditing(false);
            targets.SetGIItemSizeContactEditing(false);
            targets.SetGIInputContactEditing(false);
            targets.SetGIOutputContactEditing(false);
            GrabSystemEditorSharedUtility.SetItemOutputContactVisibility(targets, false);
            foreach (var contactName in GrabSystemEditorSharedUtility.ContactNames)
            {
                targets.SetContactEditing(contactName, false);
            }

            EditorUtility.SetDirty(targets);
            if (unlockInspector)
            {
                UnlockInspector();
            }
        }

        private static void CreateEditObjects(GrabSystemItemReference targets, string attachPointName)
        {
            var attachPoint = GrabSystemEditorSharedUtility.FindChildRecursive(targets.GrabSystemRoot, attachPointName);
            if (attachPoint == null)
            {
                return;
            }

            var constraint = GrabSystemEditorSharedUtility.FindVrcParentConstraint(attachPoint);
            var source = GrabSystemEditorSharedUtility.GetAttachPointReference(targets, attachPointName);
            if (constraint != null)
            {
                var serializedConstraint = new SerializedObject(constraint);
                var sourceProperty = serializedConstraint.FindProperty(GrabSystemEditorSharedUtility.SourceTransformPath);
                source ??= sourceProperty?.objectReferenceValue as Transform;
                var positionProperty = serializedConstraint.FindProperty(GrabSystemEditorSharedUtility.SourcePositionOffsetPath);
                var rotationProperty = serializedConstraint.FindProperty(GrabSystemEditorSharedUtility.SourceRotationOffsetPath);
                if (positionProperty != null && rotationProperty != null)
                {
                    targets.SetAttachPointOffsets(attachPointName, positionProperty.vector3Value, rotationProperty.vector3Value);
                }
            }

            DeleteEditObjects(targets, attachPointName);
            var rootObject = new GameObject($"__GrabSystem_{attachPointName}_EditRoot");
            var handleObject = new GameObject($"__GrabSystem_{attachPointName}_EditHandle");
            Undo.RegisterCreatedObjectUndo(rootObject, $"Create {attachPointName} Edit Root");
            Undo.RegisterCreatedObjectUndo(handleObject, $"Create {attachPointName} Edit Handle");

            var editRoot = rootObject.transform;
            var editHandle = handleObject.transform;
            Undo.SetTransformParent(editRoot, targets.GrabSystemRoot, $"Parent {attachPointName} Edit Root");
            editRoot.position = source != null ? source.position : attachPoint.position;
            editRoot.rotation = source != null ? source.rotation : attachPoint.rotation;
            editRoot.localScale = Vector3.one;
            Undo.SetTransformParent(editHandle, editRoot, $"Parent {attachPointName} Edit Handle");
            editHandle.localPosition = targets.GetAttachPointPositionOffset(attachPointName);
            editHandle.localRotation = Quaternion.Euler(targets.GetAttachPointRotationOffset(attachPointName));
            editHandle.localScale = Vector3.one;

            Undo.RecordObject(targets, $"Register {attachPointName} Edit Objects");
            targets.SetAttachPointEditObjects(attachPointName, editRoot, editHandle);
            EditorUtility.SetDirty(targets);
            LockInspector();
            Selection.activeTransform = editHandle;
        }

        private static void DeleteEditObjects(GrabSystemItemReference targets, string attachPointName)
        {
            var editRoot = targets.GetAttachPointEditRoot(attachPointName)
                ?? GrabSystemEditorSharedUtility.FindDirectChild(targets.GrabSystemRoot,
                    $"__GrabSystem_{attachPointName}_EditRoot");
            if (editRoot != null)
            {
                Undo.DestroyObjectImmediate(editRoot.gameObject);
            }

            targets.SetAttachPointEditObjects(attachPointName, null, null);
        }

        private static void SyncEditHandles()
        {
            if (Application.isPlaying)
            {
                return;
            }

            foreach (var targets in Resources.FindObjectsOfTypeAll<GrabSystemItemReference>())
            {
                if (targets == null || EditorUtility.IsPersistent(targets))
                {
                    continue;
                }

                GrabSystemEditorSharedUtility.SyncItemContactGroupTransforms(targets);

                foreach (var attachPointName in GrabSystemEditorSharedUtility.AttachPointNames)
                {
                    var handle = targets.GetAttachPointEditHandle(attachPointName);
                    if (!targets.GetAttachPointEditing(attachPointName) || handle == null)
                    {
                        continue;
                    }

                    var position = handle.localPosition;
                    var rotation = GrabSystemEditorSharedUtility.NormalizeEulerAngles(handle.localEulerAngles);
                    if (GrabSystemEditorSharedUtility.Approximately(
                            targets.GetAttachPointPositionOffset(attachPointName), position)
                        && GrabSystemEditorSharedUtility.Approximately(
                            targets.GetAttachPointRotationOffset(attachPointName), rotation))
                    {
                        continue;
                    }

                    Undo.RecordObject(targets, $"Update {attachPointName} Offset");
                    targets.SetAttachPointOffsets(attachPointName, position, rotation);
                    ApplyConstraintOffset(targets, attachPointName, position, rotation);
                    EditorUtility.SetDirty(targets);
                }
            }
        }

        private static void ApplyConstraintOffset(GrabSystemItemReference targets, string attachPointName,
            Vector3 position, Vector3 rotation)
        {
            var attachPoint = GrabSystemEditorSharedUtility.FindChildRecursive(targets.GrabSystemRoot, attachPointName);
            var constraint = GrabSystemEditorSharedUtility.FindVrcParentConstraint(attachPoint);
            if (constraint == null)
            {
                return;
            }

            var serializedConstraint = new SerializedObject(constraint);
            var positionProperty = serializedConstraint.FindProperty(GrabSystemEditorSharedUtility.SourcePositionOffsetPath);
            var rotationProperty = serializedConstraint.FindProperty(GrabSystemEditorSharedUtility.SourceRotationOffsetPath);
            if (positionProperty == null || rotationProperty == null)
            {
                return;
            }

            Undo.RecordObject(constraint, $"Set {attachPointName} Offset");
            positionProperty.vector3Value = position;
            rotationProperty.vector3Value = rotation;
            serializedConstraint.ApplyModifiedProperties();
            PrefabUtility.RecordPrefabInstancePropertyModifications(constraint);
            EditorUtility.SetDirty(constraint);
        }

        private static void InitializeBoneProxyTransform(Transform attachPoint, string attachPointName)
        {
            if (attachPoint.localPosition.sqrMagnitude > 0.00000001f
                || Quaternion.Angle(attachPoint.localRotation, Quaternion.identity) > 0.1f)
            {
                return;
            }

            var proxy = attachPoint.GetComponent<ModularAvatarBoneProxy>();
            if (proxy == null || proxy.boneReference == HumanBodyBones.LastBone)
            {
                return;
            }

            Transform reference = null;
            for (var current = attachPoint; current != null; current = current.parent)
            {
                var animator = current.GetComponent<Animator>();
                if (animator == null || !animator.isHuman)
                {
                    continue;
                }

                reference = animator.GetBoneTransform(proxy.boneReference);
                if (reference != null && !string.IsNullOrEmpty(proxy.subPath))
                {
                    reference = reference.Find(proxy.subPath);
                }
                break;
            }

            if (reference == null)
            {
                Debug.LogWarning($"MA Bone Proxy target was not found: {attachPointName}", attachPoint);
                return;
            }

            Undo.RecordObject(attachPoint, $"Initialize {attachPointName} Transform");
            attachPoint.position = reference.position;
            attachPoint.rotation = reference.rotation;
            PrefabUtility.RecordPrefabInstancePropertyModifications(attachPoint);
            EditorUtility.SetDirty(attachPoint);
        }

        private static void MirrorHandPosition(GrabSystemItemReference targets, string sourceName)
        {
            var destinationName = sourceName == "AP_Hand_L" ? "AP_Hand_R" : "AP_Hand_L";
            var source = GrabSystemEditorSharedUtility.FindChildRecursive(targets.GrabSystemRoot, sourceName);
            var destination = GrabSystemEditorSharedUtility.FindChildRecursive(targets.GrabSystemRoot, destinationName);
            var avatarRoot = FindAvatarRoot(targets.GrabSystemRoot);
            if (source == null || destination == null || avatarRoot == null)
            {
                Debug.LogError("反転コピーに必要な手持ち位置またはアバターRootが見つかりません。", targets);
                return;
            }

            var localPosition = avatarRoot.InverseTransformPoint(source.position);
            localPosition.x = -localPosition.x;
            var localRotation = Quaternion.Inverse(avatarRoot.rotation) * source.rotation;
            var mirroredRotation = new Quaternion(
                localRotation.x, -localRotation.y, -localRotation.z, localRotation.w).normalized;

            Undo.RecordObject(destination, "Mirror Copy Hand Position");
            destination.position = avatarRoot.TransformPoint(localPosition);
            destination.rotation = avatarRoot.rotation * mirroredRotation;
            PrefabUtility.RecordPrefabInstancePropertyModifications(destination);
            EditorUtility.SetDirty(destination);
            SceneView.RepaintAll();
        }

        private static Transform FindAvatarRoot(Transform source)
        {
            for (var current = source; current != null; current = current.parent)
            {
                var animator = current.GetComponent<Animator>();
                if (animator != null && animator.isHuman)
                {
                    return current;
                }
            }

            return source != null ? source.root : null;
        }

        private static void EndEditWhenSelectionLeaves()
        {
            if (suppressSelectionChange || Application.isPlaying)
            {
                return;
            }

            foreach (var targets in Resources.FindObjectsOfTypeAll<GrabSystemItemReference>())
            {
                if (targets == null || EditorUtility.IsPersistent(targets) || !IsAnyEditActive(targets)
                    || IsSelectionWithinEdit(targets))
                {
                    continue;
                }

                Undo.RecordObject(targets, "Turn Off GrabSystem Edit");
                EndAllEditModes(targets, true);
                GrabSystemEditorSharedUtility.CollapseGrabSystemHierarchy(targets);
            }
        }

        private static bool IsSelectionWithinEdit(GrabSystemItemReference targets)
        {
            var selected = Selection.activeTransform;
            if (selected == null)
            {
                return false;
            }

            if (selected == targets.transform || selected == targets.GrabSystemRoot)
            {
                return true;
            }

            if (targets.GIItemSizeContactEditing && selected == targets.GIItemSize)
            {
                return true;
            }

            if ((targets.GIInputContactEditing
                    && GrabSystemEditorSharedUtility.IsItemContactGroupSelection(targets, true, selected))
                || (targets.GIOutputContactEditing
                    && GrabSystemEditorSharedUtility.IsItemContactGroupSelection(targets, false, selected)))
            {
                return true;
            }

            foreach (var attachPointName in GrabSystemEditorSharedUtility.AttachPointNames)
            {
                if (!targets.GetAttachPointEditing(attachPointName))
                {
                    continue;
                }

                var attachPoint = GrabSystemEditorSharedUtility.FindChildRecursive(targets.GrabSystemRoot, attachPointName);
                if (GrabSystemEditorSharedUtility.UsesBoneProxy(targets, attachPointName) && selected == attachPoint)
                {
                    return true;
                }

                var root = targets.GetAttachPointEditRoot(attachPointName);
                if (root != null && (selected == root || selected.IsChildOf(root)))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsAnyEditActive(GrabSystemItemReference targets)
        {
            return targets.AnyAttachPointEditing() || targets.GIItemSizeEditing
                || targets.GIItemSizeContactEditing || targets.GIInputContactEditing
                || targets.GIOutputContactEditing || targets.AnyContactEditing();
        }

        private static void LockInspector()
        {
            GrabSystemEditorSharedUtility.LockInspector();
        }

        private static void UnlockInspectorIfIdle(GrabSystemItemReference targets)
        {
            if (!IsAnyEditActive(targets))
            {
                UnlockInspector();
            }
        }

        private static void UnlockInspector()
        {
            GrabSystemEditorSharedUtility.UnlockInspector();
        }
    }
}
