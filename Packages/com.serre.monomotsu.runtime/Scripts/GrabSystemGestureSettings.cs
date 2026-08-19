using System;
using UnityEngine;
#if VRC_SDK_VRCSDK3
using VRC.SDKBase;
#endif

namespace Serre.GrabSystem
{
    [Flags]
    public enum GrabSystemGestureMask
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
    public sealed class GrabSystemGestureSettings : MonoBehaviour
#if VRC_SDK_VRCSDK3
        , IEditorOnly
#endif
    {
        public const GrabSystemGestureMask DefaultGrabGestures =
            GrabSystemGestureMask.Fist
            | GrabSystemGestureMask.FingerPoint
            | GrabSystemGestureMask.HandGun
            | GrabSystemGestureMask.ThumbsUp;
        public const GrabSystemGestureMask DefaultTriggerPullGestures =
            GrabSystemGestureMask.Fist
            | GrabSystemGestureMask.ThumbsUp;

        [SerializeField, HideInInspector]
        private GrabSystemGestureMask leftHandGrabGestures = DefaultGrabGestures;

        [SerializeField, HideInInspector]
        private GrabSystemGestureMask rightHandGrabGestures = DefaultGrabGestures;

        [SerializeField, HideInInspector]
        private GrabSystemGestureMask leftHandTriggerPullGestures = DefaultTriggerPullGestures;

        [SerializeField, HideInInspector]
        private GrabSystemGestureMask rightHandTriggerPullGestures = DefaultTriggerPullGestures;

        public GrabSystemGestureMask LeftHandGrabGestures => leftHandGrabGestures;
        public GrabSystemGestureMask RightHandGrabGestures => rightHandGrabGestures;
        public GrabSystemGestureMask LeftHandTriggerPullGestures => leftHandTriggerPullGestures;
        public GrabSystemGestureMask RightHandTriggerPullGestures => rightHandTriggerPullGestures;
    }
}
