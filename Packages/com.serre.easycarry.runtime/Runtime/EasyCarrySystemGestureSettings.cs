using System;
using UnityEngine;
#if VRC_SDK_VRCSDK3
using VRC.SDKBase;
#endif

namespace Serre.EasyCarrySystem
{
    [Flags]
    public enum EasyCarrySystemGestureMask
    {
        None = 0,
        Neutral = 1 << 0,
        Fist = 1 << 1,
        HandOpen = 1 << 2,
        FingerPoint = 1 << 3,
        Victory = 1 << 4,
        RockNRoll = 1 << 5,
        HandGun = 1 << 6,
        ThumbsUp = 1 << 7,
    }

    [DisallowMultipleComponent]
    public sealed class EasyCarrySystemGestureSettings : MonoBehaviour
#if VRC_SDK_VRCSDK3
        , IEditorOnly
#endif
    {
        public const EasyCarrySystemGestureMask DefaultGrabGestures =
            EasyCarrySystemGestureMask.Fist
            | EasyCarrySystemGestureMask.FingerPoint
            | EasyCarrySystemGestureMask.HandGun
            | EasyCarrySystemGestureMask.ThumbsUp;
        public const EasyCarrySystemGestureMask DefaultTriggerPullGestures =
            EasyCarrySystemGestureMask.Fist
            | EasyCarrySystemGestureMask.ThumbsUp;

        [SerializeField, HideInInspector]
        private EasyCarrySystemGestureMask leftHandGrabGestures = DefaultGrabGestures;

        [SerializeField, HideInInspector]
        private EasyCarrySystemGestureMask rightHandGrabGestures = DefaultGrabGestures;

        [SerializeField, HideInInspector]
        private EasyCarrySystemGestureMask leftHandTriggerPullGestures = DefaultTriggerPullGestures;

        [SerializeField, HideInInspector]
        private EasyCarrySystemGestureMask rightHandTriggerPullGestures = DefaultTriggerPullGestures;

        public EasyCarrySystemGestureMask LeftHandGrabGestures => leftHandGrabGestures;
        public EasyCarrySystemGestureMask RightHandGrabGestures => rightHandGrabGestures;
        public EasyCarrySystemGestureMask LeftHandTriggerPullGestures => leftHandTriggerPullGestures;
        public EasyCarrySystemGestureMask RightHandTriggerPullGestures => rightHandTriggerPullGestures;
    }
}
