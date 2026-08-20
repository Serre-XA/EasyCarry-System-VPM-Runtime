using UnityEditor;
using UnityEngine;

namespace Serre.EasyCarrySystem.Editor
{
    internal static class EasyCarrySystemSetupMenu
    {
        private const string MenuPath = "GameObject/EasyCarry System/Setup";

        [MenuItem(MenuPath, false, 10)]
        private static void SetupSelectedObject(MenuCommand command)
        {
            var targetObject = command.context as GameObject;
            if (targetObject == null)
            {
                targetObject = Selection.activeGameObject;
            }

            EasyCarrySystemSetupEditorUtility.Setup(targetObject);
        }

        [MenuItem(MenuPath, true)]
        private static bool ValidateSetupSelectedObject()
        {
            var selected = Selection.activeGameObject;
            return !Application.isPlaying && selected != null
                && !EditorUtility.IsPersistent(selected) && selected.scene.IsValid();
        }
    }
}
