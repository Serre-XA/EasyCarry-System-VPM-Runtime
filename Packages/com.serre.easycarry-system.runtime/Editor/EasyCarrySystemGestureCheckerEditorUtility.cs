using System.Collections.Generic;
using nadena.dev.modular_avatar.core;
using Serre.EasyCarrySystem;
using UnityEditor;
using UnityEngine;
#if VRC_SDK_VRCSDK3
using VRC.SDK3.Avatars.Components;
#endif

namespace Serre.EasyCarrySystem.Editor
{
    internal static class EasyCarrySystemGestureCheckerEditorUtility
    {
        private const string MenuRootPrefabRelativePath =
            "Prefabs/Base/EasyCarrySystem_Menu_Root.prefab";
        private const string MenuRootObjectName = "EasyCarrySystem_Menu_Root";
        private const string MenuEntryObjectName = "ECS設定";

        private static readonly EasyCarrySystemGestureMask[] GestureValues =
        {
            EasyCarrySystemGestureMask.Neutral,
            EasyCarrySystemGestureMask.Fist,
            EasyCarrySystemGestureMask.HandOpen,
            EasyCarrySystemGestureMask.FingerPoint,
            EasyCarrySystemGestureMask.Victory,
            EasyCarrySystemGestureMask.RockNRoll,
            EasyCarrySystemGestureMask.HandGun,
            EasyCarrySystemGestureMask.ThumbsUp,
        };

        private static readonly string[] GestureNames =
        {
            "Neutral",
            "Fist",
            "HandOpen",
            "FingerPoint",
            "Victory",
            "RockNRoll",
            "HandGun",
            "ThumbsUp",
        };

        internal static EasyCarrySystemGestureSettings FindSettingsFor(
            EasyCarrySystemItemReference targets)
        {
            return targets != null && targets.GeneratedEasyCarrySystem != null
                ? targets.GeneratedEasyCarrySystem
                    .GetComponentInChildren<EasyCarrySystemGestureSettings>(true)
                : null;
        }

        internal static GameObject FindMenuRootFor(EasyCarrySystemItemReference targets)
        {
            var avatarRoot = ResolveAvatarRoot(targets != null ? targets.transform : null);
            return FindDirectMenuRoot(avatarRoot);
        }

        internal static List<EasyCarrySystemItemReference> FindMissingMenuRootsForLoadedAvatars()
        {
            var results = new List<EasyCarrySystemItemReference>();
            var avatarRootIds = new HashSet<int>();
            foreach (var targets in Resources.FindObjectsOfTypeAll<EasyCarrySystemItemReference>())
            {
                if (targets == null || EditorUtility.IsPersistent(targets)
                    || !targets.gameObject.scene.IsValid())
                {
                    continue;
                }

                if (EasyCarrySystemEditorSharedUtility.GetEditorOnlyState(targets)
                    == EasyCarrySystemEditorOnlyState.Both)
                {
                    continue;
                }

                var avatarRoot = ResolveAvatarRoot(targets.transform);
                if (avatarRoot == null || !avatarRootIds.Add(avatarRoot.GetInstanceID())
                    || FindDirectMenuRoot(avatarRoot) != null)
                {
                    continue;
                }

                results.Add(targets);
            }

            return results;
        }

        internal static string GetAvatarName(EasyCarrySystemItemReference targets)
        {
            var avatarRoot = ResolveAvatarRoot(targets != null ? targets.transform : null);
            return avatarRoot != null ? avatarRoot.name : "アバター";
        }

        internal static GameObject EnsureMenuRootFor(EasyCarrySystemItemReference targets)
        {
            if (targets == null || Application.isPlaying || EditorUtility.IsPersistent(targets)
                || !targets.gameObject.scene.IsValid())
            {
                return null;
            }

            var avatarRoot = ResolveAvatarRoot(targets.transform);
            if (avatarRoot == null)
            {
                return null;
            }

            var existingMenuRoot = FindDirectMenuRoot(avatarRoot);
            if (existingMenuRoot != null)
            {
                return existingMenuRoot;
            }

            var prefabPath = EasyCarrySystemAssetLocator.GetAssetPath(MenuRootPrefabRelativePath);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError($"EasyCarry System menu root prefab was not found: {prefabPath}", targets);
                return null;
            }

            var instance = PrefabUtility.InstantiatePrefab(prefab, avatarRoot) as GameObject;
            if (instance == null)
            {
                Debug.LogError("Failed to instantiate the shared EasyCarry System menu root.", targets);
                return null;
            }

            Undo.RegisterCreatedObjectUndo(instance, "Create EasyCarry System Menu Root");
            instance.name = MenuRootObjectName;
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            EditorUtility.SetDirty(instance);
            return instance;
        }

        internal static bool ValidateMenuRootForAvatar(
            GameObject avatarGameObject,
            int easyCarrySystemCount)
        {
            if (avatarGameObject == null || easyCarrySystemCount <= 0)
            {
                return true;
            }

            var menuRootCount = CountDirectMenuRoots(avatarGameObject.transform);
            if (menuRootCount == 1)
            {
                return true;
            }

            Debug.LogError(
                menuRootCount == 0
                    ? "EasyCarry System is present, but the shared menu root was not found."
                    : "Multiple shared EasyCarry System menu roots were found. Keep exactly one directly under the avatar root.",
                avatarGameObject);
            return false;
        }

        internal static bool IsMenuRootHierarchy(Transform source)
        {
            var current = source;
            while (current != null)
            {
                if (IsMenuRoot(current.gameObject))
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }


        internal static bool ApplyStoredGestureSettings(
            EasyCarrySystemGestureSettings settings,
            EasyCarrySystemGestureMask leftHandGestures,
            EasyCarrySystemGestureMask rightHandGestures)
        {
            if (settings == null)
            {
                return false;
            }

            var serializedSettings = new SerializedObject(settings);
            serializedSettings.Update();
            serializedSettings.FindProperty("leftHandGrabGestures").intValue = (int)leftHandGestures;
            serializedSettings.FindProperty("rightHandGrabGestures").intValue = (int)rightHandGestures;
            serializedSettings.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.RecordPrefabInstancePropertyModifications(settings);
            EditorUtility.SetDirty(settings);
            return ApplyParameterDefaults(settings);
        }

        internal static bool ApplyParameterDefaults(EasyCarrySystemGestureSettings settings)
        {
            if (settings == null)
            {
                return false;
            }

            var parametersComponent = settings.GetComponentInParent<ModularAvatarParameters>();
            if (parametersComponent == null || parametersComponent.parameters == null)
            {
                return false;
            }

            var allParametersFound = true;
            var undoRecorded = false;
            ApplyHandParameterDefaults(
                parametersComponent,
                "L",
                settings.LeftHandGrabGestures,
                ref allParametersFound,
                ref undoRecorded);
            ApplyHandParameterDefaults(
                parametersComponent,
                "R",
                settings.RightHandGrabGestures,
                ref allParametersFound,
                ref undoRecorded);

            if (undoRecorded)
            {
                if (PrefabUtility.IsPartOfPrefabInstance(parametersComponent))
                {
                    PrefabUtility.RecordPrefabInstancePropertyModifications(parametersComponent);
                }

                EditorUtility.SetDirty(parametersComponent);
            }

            return allParametersFound;
        }

        internal static void DrawGestureMaskField(SerializedProperty property, GUIContent label)
        {
            var previousShowMixedValue = EditorGUI.showMixedValue;
            EditorGUI.showMixedValue = property.hasMultipleDifferentValues;

            EditorGUI.BeginChangeCheck();
            var currentValue = (EasyCarrySystemGestureMask)property.intValue;
            var nextValue =
                (EasyCarrySystemGestureMask)EditorGUILayout.EnumFlagsField(label, currentValue);
            if (EditorGUI.EndChangeCheck())
            {
                property.intValue = (int)nextValue;
            }

            EditorGUI.showMixedValue = previousShowMixedValue;
        }

        internal static void DrawHandSettingsFields(SerializedObject serializedSettings, bool leftHand)
        {
            var handLabel = leftHand ? "左手" : "右手";
            var otherHandLabel = leftHand ? "右手" : "左手";
            var grabProperty = serializedSettings.FindProperty(
                leftHand ? "leftHandGrabGestures" : "rightHandGrabGestures");
            var otherGrabProperty = serializedSettings.FindProperty(
                leftHand ? "rightHandGrabGestures" : "leftHandGrabGestures");

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("握り判定", EditorStyles.boldLabel);
                if (GUILayout.Button(
                        new GUIContent(
                            $"{otherHandLabel}にジェスチャー設定をコピー",
                            $"{handLabel}の握り判定設定を{otherHandLabel}へコピーします。"),
                        GUILayout.ExpandWidth(false)))
                {
                    otherGrabProperty.intValue = grabProperty.intValue;
                }
            }

            DrawGestureMaskField(grabProperty, new GUIContent(handLabel));

            EditorGUILayout.Space(6f);
            DrawResetButton(grabProperty);
        }

        internal static void DrawSettingsFields(SerializedObject serializedSettings)
        {
            var leftGrabProperty = serializedSettings.FindProperty("leftHandGrabGestures");
            var rightGrabProperty = serializedSettings.FindProperty("rightHandGrabGestures");

            EditorGUILayout.LabelField("握り判定", EditorStyles.boldLabel);
            DrawGestureMaskField(leftGrabProperty, new GUIContent("左手"));
            DrawGestureMaskField(rightGrabProperty, new GUIContent("右手"));

            EditorGUILayout.Space(6f);
            DrawResetButton(leftGrabProperty, rightGrabProperty);
        }

        private static void DrawResetButton(
            SerializedProperty grabProperty,
            SerializedProperty otherGrabProperty = null)
        {
            var defaultValue = (int)EasyCarrySystemGestureSettings.DefaultGrabGestures;
            var isDefault = IsDefaultValue(grabProperty, defaultValue)
                && (otherGrabProperty == null || IsDefaultValue(otherGrabProperty, defaultValue));

            using (new EditorGUI.DisabledScope(isDefault))
            {
                if (!GUILayout.Button(new GUIContent(
                        "既定値にリセット",
                        "握り判定を Fist / FingerPoint / HandGun / ThumbsUp に戻します。")))
                {
                    return;
                }
            }

            grabProperty.intValue = defaultValue;
            if (otherGrabProperty != null)
            {
                otherGrabProperty.intValue = defaultValue;
            }
        }

        private static bool IsDefaultValue(SerializedProperty property, int defaultValue)
        {
            return property != null
                && !property.hasMultipleDifferentValues
                && property.intValue == defaultValue;
        }

        private static void ApplyHandParameterDefaults(
            ModularAvatarParameters parametersComponent,
            string hand,
            EasyCarrySystemGestureMask enabledGestures,
            ref bool allParametersFound,
            ref bool undoRecorded)
        {
            for (var gestureIndex = 0; gestureIndex < GestureValues.Length; gestureIndex++)
            {
                var parameterName = $"Hand/{hand}/GrabCheck/{GestureNames[gestureIndex]}";
                var parameterFound = false;
                for (var parameterIndex = 0;
                     parameterIndex < parametersComponent.parameters.Count;
                     parameterIndex++)
                {
                    var parameter = parametersComponent.parameters[parameterIndex];
                    if (parameter.isPrefix || parameter.nameOrPrefix != parameterName)
                    {
                        continue;
                    }

                    parameterFound = true;
                    var enabled = (enabledGestures & GestureValues[gestureIndex]) != 0;
                    var defaultValue = enabled ? 1f : 0f;
                    if (Mathf.Approximately(parameter.defaultValue, defaultValue)
                        && parameter.hasExplicitDefaultValue)
                    {
                        break;
                    }

                    if (!undoRecorded)
                    {
                        Undo.RecordObject(parametersComponent, "Set Grab Check Defaults");
                        undoRecorded = true;
                    }

                    parameter.defaultValue = defaultValue;
                    parameter.hasExplicitDefaultValue = true;
                    parametersComponent.parameters[parameterIndex] = parameter;
                    break;
                }

                if (!parameterFound)
                {
                    allParametersFound = false;
                }
            }
        }

        private static GameObject FindDirectMenuRoot(Transform avatarRoot)
        {
            if (avatarRoot == null)
            {
                return null;
            }

            for (var childIndex = 0; childIndex < avatarRoot.childCount; childIndex++)
            {
                var childObject = avatarRoot.GetChild(childIndex).gameObject;
                if (IsMenuRoot(childObject))
                {
                    return childObject;
                }
            }

            return null;
        }

        private static int CountDirectMenuRoots(Transform avatarRoot)
        {
            if (avatarRoot == null)
            {
                return 0;
            }

            var count = 0;
            for (var childIndex = 0; childIndex < avatarRoot.childCount; childIndex++)
            {
                if (IsMenuRoot(avatarRoot.GetChild(childIndex).gameObject))
                {
                    count++;
                }
            }

            return count;
        }

        private static bool IsMenuRoot(GameObject candidate)
        {
            if (candidate == null)
            {
                return false;
            }

            var prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(candidate);
            var expectedPath = EasyCarrySystemAssetLocator.GetAssetPath(MenuRootPrefabRelativePath);
            var menuEntry = candidate.transform.Find(MenuEntryObjectName);
            return (!string.IsNullOrEmpty(prefabPath) && prefabPath == expectedPath)
                || (candidate.name == MenuRootObjectName
                    && menuEntry != null
                    && menuEntry.GetComponent<ModularAvatarMenuInstaller>() != null);
        }

        internal static Transform ResolveAvatarRoot(Transform source)
        {
            if (source == null)
            {
                return null;
            }

#if VRC_SDK_VRCSDK3
            var current = source;
            while (current != null)
            {
                if (current.GetComponent<VRCAvatarDescriptor>() != null)
                {
                    return current;
                }

                current = current.parent;
            }
#endif

            return source.root;
        }
    }
}
