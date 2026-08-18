using nadena.dev.modular_avatar.core;
using Serre.GrabSystem;
using UnityEditor;
using UnityEngine;
#if VRC_SDK_VRCSDK3
using VRC.SDK3.Avatars.Components;
#endif

namespace Serre.GrabSystem.Editor
{
    internal static class GrabSystemGestureCheckerEditorUtility
    {
        private const string GestureCheckerPrefabRelativePath =
            "Prefab/Base_Prefab/GestureChecker.prefab";
        private const string GestureCheckerObjectName = "GestureChecker";
        private static readonly GrabSystemGestureMask[] GestureValues =
        {
            GrabSystemGestureMask.Neutral,
            GrabSystemGestureMask.Fist,
            GrabSystemGestureMask.HandOpen,
            GrabSystemGestureMask.FingerPoint,
            GrabSystemGestureMask.Victory,
            GrabSystemGestureMask.RockNRoll,
            GrabSystemGestureMask.HandGun,
            GrabSystemGestureMask.ThumbsUp,
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
        internal static GrabSystemGestureSettings FindFor(GrabSystemItemReference targets)
        {
            var avatarRoot = ResolveAvatarRoot(targets != null ? targets.transform : null);
            return FindDirectChildSettings(avatarRoot);
        }

        internal static GrabSystemGestureSettings EnsureFor(GrabSystemItemReference targets)
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

            var settings = FindDirectChildSettings(avatarRoot);
            if (settings != null)
            {
                return settings;
            }

            var existingChecker = FindDirectChildChecker(avatarRoot);
            if (existingChecker != null)
            {
                settings = Undo.AddComponent<GrabSystemGestureSettings>(existingChecker);
                ApplyParameterDefaults(settings);
                EditorUtility.SetDirty(existingChecker);
                return settings;
            }

            var gestureCheckerPrefabPath =
                GrabSystemAssetLocator.GetAssetPath(GestureCheckerPrefabRelativePath);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(gestureCheckerPrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"GestureChecker prefab was not found: {gestureCheckerPrefabPath}", targets);
                return null;
            }

            var instance = PrefabUtility.InstantiatePrefab(prefab, avatarRoot) as GameObject;
            if (instance == null)
            {
                Debug.LogError("Failed to instantiate the shared GestureChecker prefab.", targets);
                return null;
            }

            Undo.RegisterCreatedObjectUndo(instance, "Create Shared GestureChecker");
            instance.name = GestureCheckerObjectName;
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            settings = instance.GetComponent<GrabSystemGestureSettings>();
            if (settings == null)
            {
                settings = Undo.AddComponent<GrabSystemGestureSettings>(instance);
            }

            ApplyParameterDefaults(settings);
            EditorUtility.SetDirty(instance);
            return settings;
        }

        internal static bool ApplyParameterDefaults(GrabSystemGestureSettings settings)
        {
            if (settings == null)
            {
                return false;
            }

            var parametersComponent = settings.GetComponent<ModularAvatarParameters>();
            if (parametersComponent == null || parametersComponent.parameters == null)
            {
                return false;
            }

            var allParametersFound = true;
            var undoRecorded = false;
            ApplyHandParameterDefaults(
                parametersComponent,
                "L",
                "GrabCheck",
                settings.LeftHandGrabGestures,
                ref allParametersFound,
                ref undoRecorded);
            ApplyHandParameterDefaults(
                parametersComponent,
                "R",
                "GrabCheck",
                settings.RightHandGrabGestures,
                ref allParametersFound,
                ref undoRecorded);
            ApplyHandParameterDefaults(
                parametersComponent,
                "L",
                "TriggerCheck",
                settings.LeftHandTriggerPullGestures,
                ref allParametersFound,
                ref undoRecorded);
            ApplyHandParameterDefaults(
                parametersComponent,
                "R",
                "TriggerCheck",
                settings.RightHandTriggerPullGestures,
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
            var currentValue = (GrabSystemGestureMask)property.intValue;
            var nextValue = (GrabSystemGestureMask)EditorGUILayout.EnumFlagsField(label, currentValue);
            if (EditorGUI.EndChangeCheck())
            {
                property.intValue = (int)nextValue;
            }

            EditorGUI.showMixedValue = previousShowMixedValue;
        }

        private static void DrawCombinedResetButton(
            SerializedProperty grabProperty,
            SerializedProperty triggerProperty,
            SerializedProperty otherGrabProperty = null,
            SerializedProperty otherTriggerProperty = null)
        {
            var grabDefault = (int)GrabSystemGestureSettings.DefaultGrabGestures;
            var triggerDefault = (int)GrabSystemGestureSettings.DefaultTriggerPullGestures;
            var isDefault = IsDefaultValue(grabProperty, grabDefault)
                && IsDefaultValue(triggerProperty, triggerDefault)
                && (otherGrabProperty == null || IsDefaultValue(otherGrabProperty, grabDefault))
                && (otherTriggerProperty == null || IsDefaultValue(otherTriggerProperty, triggerDefault));

            using (new EditorGUI.DisabledScope(isDefault))
            {
                if (!GUILayout.Button(
                        new GUIContent(
                            "\u65e2\u5b9a\u5024\u306b\u30ea\u30bb\u30c3\u30c8",
                            "\u63e1\u308a\u5224\u5b9a\u3092 Fist / FingerPoint / HandGun / ThumbsUp\u3001\u30c8\u30ea\u30ac\u30fc\u30d7\u30eb\u5224\u5b9a\u3092 Fist / ThumbsUp \u306b\u623b\u3057\u307e\u3059\u3002")))
                {
                    return;
                }
            }

            grabProperty.intValue = grabDefault;
            triggerProperty.intValue = triggerDefault;
            if (otherGrabProperty != null)
            {
                otherGrabProperty.intValue = grabDefault;
            }

            if (otherTriggerProperty != null)
            {
                otherTriggerProperty.intValue = triggerDefault;
            }
        }

        private static bool IsDefaultValue(SerializedProperty property, int defaultValue)
        {
            return property != null
                && !property.hasMultipleDifferentValues
                && property.intValue == defaultValue;
        }

        internal static void DrawHandSettingsFields(SerializedObject serializedSettings, bool leftHand)
        {
            var handLabel = leftHand ? "\u5de6\u624b" : "\u53f3\u624b";
            var grabProperty = serializedSettings.FindProperty(
                leftHand ? "leftHandGrabGestures" : "rightHandGrabGestures");
            var triggerProperty = serializedSettings.FindProperty(
                leftHand ? "leftHandTriggerPullGestures" : "rightHandTriggerPullGestures");

            EditorGUILayout.LabelField("\u63e1\u308a\u5224\u5b9a", EditorStyles.boldLabel);
            DrawGestureMaskField(grabProperty, new GUIContent(handLabel));

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("\u30c8\u30ea\u30ac\u30fc\u30d7\u30eb\u5224\u5b9a", EditorStyles.boldLabel);
            DrawGestureMaskField(triggerProperty, new GUIContent(handLabel));

            EditorGUILayout.Space(6f);
            DrawCombinedResetButton(grabProperty, triggerProperty);
        }

        internal static void DrawSettingsFields(SerializedObject serializedSettings)
        {
            var leftGrabProperty = serializedSettings.FindProperty("leftHandGrabGestures");
            var rightGrabProperty = serializedSettings.FindProperty("rightHandGrabGestures");
            var leftTriggerProperty = serializedSettings.FindProperty("leftHandTriggerPullGestures");
            var rightTriggerProperty = serializedSettings.FindProperty("rightHandTriggerPullGestures");

            EditorGUILayout.LabelField("\u63e1\u308a\u5224\u5b9a", EditorStyles.boldLabel);
            DrawGestureMaskField(leftGrabProperty, new GUIContent("\u5de6\u624b"));
            DrawGestureMaskField(rightGrabProperty, new GUIContent("\u53f3\u624b"));

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("\u30c8\u30ea\u30ac\u30fc\u30d7\u30eb\u5224\u5b9a", EditorStyles.boldLabel);
            DrawGestureMaskField(leftTriggerProperty, new GUIContent("\u5de6\u624b"));
            DrawGestureMaskField(rightTriggerProperty, new GUIContent("\u53f3\u624b"));

            EditorGUILayout.Space(6f);
            DrawCombinedResetButton(
                leftGrabProperty,
                leftTriggerProperty,
                rightGrabProperty,
                rightTriggerProperty);
        }

        private static void ApplyHandParameterDefaults(
            ModularAvatarParameters parametersComponent,
            string hand,
            string checkName,
            GrabSystemGestureMask enabledGestures,
            ref bool allParametersFound,
            ref bool undoRecorded)
        {
            for (var gestureIndex = 0; gestureIndex < GestureValues.Length; gestureIndex++)
            {
                var parameterName = $"Hand/{hand}/{checkName}/{GestureNames[gestureIndex]}";
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
                        Undo.RecordObject(parametersComponent, "Set Gesture Check Defaults");
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

        internal static bool ValidateForAvatar(GameObject avatarGameObject, int grabSystemCount)
        {
            if (avatarGameObject == null || grabSystemCount <= 0)
            {
                return true;
            }

            var settingsCount = 0;
            for (var childIndex = 0; childIndex < avatarGameObject.transform.childCount; childIndex++)
            {
                var child = avatarGameObject.transform.GetChild(childIndex);
                if (child.GetComponent<GrabSystemGestureSettings>() != null)
                {
                    settingsCount++;
                }
            }

            if (settingsCount == 1)
            {
                return true;
            }

            Debug.LogError(
                settingsCount == 0
                    ? "GrabSystem is present, but the shared GestureChecker was not found. Select a GrabSystem once to repair it before building."
                    : "Multiple shared GestureChecker objects were found. Keep exactly one GestureChecker directly under the avatar root.",
                avatarGameObject);
            return false;
        }

        private static GrabSystemGestureSettings FindDirectChildSettings(Transform avatarRoot)
        {
            if (avatarRoot == null)
            {
                return null;
            }

            for (var childIndex = 0; childIndex < avatarRoot.childCount; childIndex++)
            {
                var settings = avatarRoot.GetChild(childIndex).GetComponent<GrabSystemGestureSettings>();
                if (settings != null)
                {
                    return settings;
                }
            }

            return null;
        }

        private static GameObject FindDirectChildChecker(Transform avatarRoot)
        {
            if (avatarRoot == null)
            {
                return null;
            }

            var gestureCheckerPrefabPath =
                GrabSystemAssetLocator.GetAssetPath(GestureCheckerPrefabRelativePath);
            for (var childIndex = 0; childIndex < avatarRoot.childCount; childIndex++)
            {
                var child = avatarRoot.GetChild(childIndex);
                if (child.name != GestureCheckerObjectName)
                {
                    continue;
                }

                var prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(child.gameObject);
                if (prefabPath == gestureCheckerPrefabPath)
                {
                    return child.gameObject;
                }
            }

            return null;
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

#if GRABSYSTEM_AUTHORING
    [CustomEditor(typeof(GrabSystemGestureSettings))]
    public sealed class GrabSystemGestureSettingsEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            GrabSystemGestureCheckerEditorUtility.DrawSettingsFields(serializedObject);

            if (serializedObject.ApplyModifiedProperties())
            {
                foreach (var changedTarget in targets)
                {
                    var settings = changedTarget as GrabSystemGestureSettings;
                    if (settings == null)
                    {
                        continue;
                    }

                    if (PrefabUtility.IsPartOfPrefabInstance(settings))
                    {
                        PrefabUtility.RecordPrefabInstancePropertyModifications(settings);
                    }

                    EditorUtility.SetDirty(settings);
                }
            }

            var allParametersFound = true;
            foreach (var currentTarget in targets)
            {
                allParametersFound &= GrabSystemGestureCheckerEditorUtility.ApplyParameterDefaults(
                    currentTarget as GrabSystemGestureSettings);
            }

            if (!allParametersFound)
            {
                EditorGUILayout.HelpBox(
                    "\u30b8\u30a7\u30b9\u30c1\u30e3\u30fc\u5224\u5b9a\u7528\u30d1\u30e9\u30e1\u30fc\u30bf\u30fc\u304c MA Parameters \u306b\u63c3\u3063\u3066\u3044\u307e\u305b\u3093\u3002",
                    MessageType.Warning);
            }
        }
    }
#endif
}