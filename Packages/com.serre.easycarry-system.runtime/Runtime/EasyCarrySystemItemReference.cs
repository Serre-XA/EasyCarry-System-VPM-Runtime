using System;
using System.Collections.Generic;
using UnityEngine;
#if VRC_SDK_VRCSDK3
using VRC.SDKBase;
#endif

namespace Serre.EasyCarrySystem
{
    [Serializable]
    public sealed class EasyCarrySystemAttachPointSettings
    {
        public Transform SourceTransform;
        public Vector3 PositionOffset;
        public Vector3 RotationOffset;
        public EasyCarrySystemAttachPointMethod AttachmentMethod;
        public bool HasLocalTransform;
        public Vector3 LocalPosition;
        public Quaternion LocalRotation = Quaternion.identity;
        public Vector3 LocalScale = Vector3.one;
        public bool HasBoneProxy;
        public int BoneReference;
        public string BoneSubPath;
        public int BoneAttachmentMode;
    }

    [Serializable]
    public sealed class EasyCarrySystemContactSettings
    {
        public bool Initialized;
        public Vector3 LocalPosition;
        public Quaternion LocalRotation = Quaternion.identity;
        public Vector3 LocalScale = Vector3.one;
        public bool HasShape;
        public int ShapeType;
        public float Radius;
        public float Height;
        public Vector3 Size = Vector3.one;
    }

    [Serializable]
    public sealed class EasyCarrySystemItemSettings
    {
        public const int AttachPointCount = 9;
        public const int ContactCount = 15;
        public const int MainWeightCount = 16;
        public const int NumberedAttachPointCount = 7;

        public bool Initialized;
        public int SourceSlot;
        public int[] NumberedAttachPointOrder = Array.Empty<int>();
        public string MenuSettingsName;
        public string MenuResetName;
        public string MenuSwitchHandsName;
        public string MenuFreezeName;
        public EasyCarrySystemAttachPointSettings[] AttachPoints = new EasyCarrySystemAttachPointSettings[AttachPointCount];
        public EasyCarrySystemContactSettings[] Contacts = new EasyCarrySystemContactSettings[ContactCount];
        public float[] MainWeights = new float[MainWeightCount];
        public bool[] HideWhenAttachedDefaults = new bool[NumberedAttachPointCount];
        public bool WorldFixedDefault;

        public void EnsureInitialized()
        {
            AttachPoints = Resize(AttachPoints, AttachPointCount, () => new EasyCarrySystemAttachPointSettings());
            Contacts = Resize(Contacts, ContactCount, () => new EasyCarrySystemContactSettings());
            MainWeights = Resize(MainWeights, MainWeightCount);
            HideWhenAttachedDefaults = Resize(HideWhenAttachedDefaults, NumberedAttachPointCount);
            NumberedAttachPointOrder ??= Array.Empty<int>();
        }

        private static T[] Resize<T>(T[] source, int length, Func<T> createItem = null)
        {
            var result = new T[length];
            if (source != null)
            {
                Array.Copy(source, result, Mathf.Min(source.Length, length));
            }

            if (createItem != null)
            {
                for (var index = 0; index < result.Length; index++)
                {
                    result[index] ??= createItem();
                }
            }

            return result;
        }
    }
    public enum EasyCarrySystemAttachPointMethod
    {
        BoneProxy,
        ParentConstraint,
    }

    [DisallowMultipleComponent]
    public sealed class EasyCarrySystemItemReference : MonoBehaviour
#if VRC_SDK_VRCSDK3
        , IEditorOnly
#endif
    {

        [SerializeField, HideInInspector]
        private GameObject generatedEasyCarrySystem;

        [SerializeField, HideInInspector]
        private EasyCarrySystemItemSettings itemSettings = new EasyCarrySystemItemSettings();

        public GameObject GeneratedEasyCarrySystem => generatedEasyCarrySystem;
        public Transform EasyCarrySystemRoot => generatedEasyCarrySystem != null ? generatedEasyCarrySystem.transform : null;
        public EasyCarrySystemItemSettings ItemSettings
        {
            get
            {
                itemSettings ??= new EasyCarrySystemItemSettings();
                itemSettings.EnsureInitialized();
                return itemSettings;
            }
        }

        public void SetGeneratedEasyCarrySystem(GameObject value)
        {
            generatedEasyCarrySystem = value;
        }

        public void SetItemSettings(EasyCarrySystemItemSettings value)
        {
            itemSettings = value ?? new EasyCarrySystemItemSettings();
            itemSettings.EnsureInitialized();
        }

        [SerializeField, HideInInspector]
        private int ciSlot = -1;

        [SerializeField, HideInInspector]
        private int numberedAttachPointCount;

        [SerializeField, HideInInspector]
        private bool numberedAttachPointListInitialized;

        [SerializeField, HideInInspector]
        private List<int> numberedAttachPointOrder = new List<int>();

        [SerializeField, HideInInspector]
        private Transform menuSettingsRoot;

        [SerializeField, HideInInspector]
        private Transform menuResetItem;

        [SerializeField, HideInInspector]
        private Transform menuSwitchHandsItem;

        [SerializeField, HideInInspector]
        private Transform menuFreezeItem;

        [SerializeField]
        private Transform apHandL;

        [SerializeField]
        private bool apHandLEditing;

        [SerializeField]
        private Vector3 apHandLPositionOffset;

        [SerializeField]
        private Vector3 apHandLRotationOffset;

        [SerializeField]
        private Transform apHandLEditRoot;

        [SerializeField]
        private Transform apHandLEditHandle;

        [SerializeField]
        private Transform apHandR;

        [SerializeField]
        private bool apHandREditing;

        [SerializeField]
        private Vector3 apHandRPositionOffset;

        [SerializeField]
        private Vector3 apHandRRotationOffset;

        [SerializeField]
        private Transform apHandREditRoot;

        [SerializeField]
        private Transform apHandREditHandle;

        [SerializeField]
        private Transform ap00;

        [SerializeField]
        private bool ap00Editing;

        [SerializeField]
        private Vector3 ap00PositionOffset;

        [SerializeField]
        private Vector3 ap00RotationOffset;

        [SerializeField]
        private Transform ap00EditRoot;

        [SerializeField]
        private Transform ap00EditHandle;

        [SerializeField]
        private Transform ap01;

        [SerializeField]
        private bool ap01Editing;

        [SerializeField]
        private Vector3 ap01PositionOffset;

        [SerializeField]
        private Vector3 ap01RotationOffset;

        [SerializeField]
        private Transform ap01EditRoot;

        [SerializeField]
        private Transform ap01EditHandle;

        [SerializeField]
        private Transform ap02;

        [SerializeField]
        private bool ap02Editing;

        [SerializeField]
        private Vector3 ap02PositionOffset;

        [SerializeField]
        private Vector3 ap02RotationOffset;

        [SerializeField]
        private Transform ap02EditRoot;

        [SerializeField]
        private Transform ap02EditHandle;

        [SerializeField]
        private Transform ap03;

        [SerializeField]
        private bool ap03Editing;

        [SerializeField]
        private Vector3 ap03PositionOffset;

        [SerializeField]
        private Vector3 ap03RotationOffset;

        [SerializeField]
        private Transform ap03EditRoot;

        [SerializeField]
        private Transform ap03EditHandle;

        [SerializeField]
        private Transform ap04;

        [SerializeField]
        private bool ap04Editing;

        [SerializeField]
        private Vector3 ap04PositionOffset;

        [SerializeField]
        private Vector3 ap04RotationOffset;

        [SerializeField]
        private Transform ap04EditRoot;

        [SerializeField]
        private Transform ap04EditHandle;

        [SerializeField]
        private Transform ap05;

        [SerializeField]
        private bool ap05Editing;

        [SerializeField]
        private Vector3 ap05PositionOffset;

        [SerializeField]
        private Vector3 ap05RotationOffset;

        [SerializeField]
        private Transform ap05EditRoot;

        [SerializeField]
        private Transform ap05EditHandle;

        [SerializeField]
        private Transform ap06;

        [SerializeField]
        private bool ap06Editing;

        [SerializeField]
        private Vector3 ap06PositionOffset;

        [SerializeField]
        private Vector3 ap06RotationOffset;

        [SerializeField]
        private Transform ap06EditRoot;

        [SerializeField]
        private Transform ap06EditHandle;

        [SerializeField]
        private EasyCarrySystemAttachPointMethod ap00AttachmentMethod;

        [SerializeField]
        private EasyCarrySystemAttachPointMethod ap01AttachmentMethod;

        [SerializeField]
        private EasyCarrySystemAttachPointMethod ap02AttachmentMethod;

        [SerializeField]
        private EasyCarrySystemAttachPointMethod ap03AttachmentMethod;

        [SerializeField]
        private EasyCarrySystemAttachPointMethod ap04AttachmentMethod;

        [SerializeField]
        private EasyCarrySystemAttachPointMethod ap05AttachmentMethod;

        [SerializeField]
        private EasyCarrySystemAttachPointMethod ap06AttachmentMethod;

        [SerializeField]
        private Transform ciItemSize;

        [SerializeField]
        private bool ciItemSizeEditing;

        [SerializeField]
        private bool ciItemSizeContactEditing;

        [SerializeField]
        private bool ciInputContactEditing;

        [SerializeField]
        private bool ciOutputContactEditing;

        [SerializeField]
        private Transform apContactHandL;

        [SerializeField]
        private bool apContactHandLEditing;

        [SerializeField]
        private Transform apContactHandR;

        [SerializeField]
        private bool apContactHandREditing;

        [SerializeField]
        private Transform apContact00;

        [SerializeField]
        private bool apContact00Editing;

        [SerializeField]
        private Transform apContact01;

        [SerializeField]
        private bool apContact01Editing;

        [SerializeField]
        private Transform apContact02;

        [SerializeField]
        private bool apContact02Editing;

        [SerializeField]
        private Transform apContact03;

        [SerializeField]
        private bool apContact03Editing;

        [SerializeField]
        private Transform apContact04;

        [SerializeField]
        private bool apContact04Editing;

        [SerializeField]
        private Transform apContact05;

        [SerializeField]
        private bool apContact05Editing;

        [SerializeField]
        private Transform apContact06;

        [SerializeField]
        private bool apContact06Editing;

        public int CISlot => ciSlot;
        public Transform MenuSettingsRoot => menuSettingsRoot;
        public Transform MenuResetItem => menuResetItem;
        public Transform MenuSwitchHandsItem => menuSwitchHandsItem;
        public Transform MenuFreezeItem => menuFreezeItem;
        public int NumberedAttachPointCount => HasValidNumberedAttachPointOrder()
            ? numberedAttachPointOrder.Count
            : Mathf.Clamp(numberedAttachPointCount, 0, 7);
        public bool NumberedAttachPointListInitialized => numberedAttachPointListInitialized
            && HasValidNumberedAttachPointOrder()
            && numberedAttachPointOrder.Contains(0);
        public Transform APHandL => apHandL;
        public bool APHandLEditing => apHandLEditing;
        public Vector3 APHandLPositionOffset => apHandLPositionOffset;
        public Vector3 APHandLRotationOffset => apHandLRotationOffset;
        public Transform APHandLEditRoot => apHandLEditRoot;
        public Transform APHandLEditHandle => apHandLEditHandle;
        public Transform APHandR => apHandR;
        public bool APHandREditing => apHandREditing;
        public Vector3 APHandRPositionOffset => apHandRPositionOffset;
        public Vector3 APHandRRotationOffset => apHandRRotationOffset;
        public Transform APHandREditRoot => apHandREditRoot;
        public Transform APHandREditHandle => apHandREditHandle;
        public Transform AP00 => ap00;
        public bool AP00Editing => ap00Editing;
        public Vector3 AP00PositionOffset => ap00PositionOffset;
        public Vector3 AP00RotationOffset => ap00RotationOffset;
        public Transform AP00EditRoot => ap00EditRoot;
        public Transform AP00EditHandle => ap00EditHandle;
        public Transform AP01 => ap01;
        public bool AP01Editing => ap01Editing;
        public Vector3 AP01PositionOffset => ap01PositionOffset;
        public Vector3 AP01RotationOffset => ap01RotationOffset;
        public Transform AP01EditRoot => ap01EditRoot;
        public Transform AP01EditHandle => ap01EditHandle;
        public Transform AP02 => ap02;
        public bool AP02Editing => ap02Editing;
        public Vector3 AP02PositionOffset => ap02PositionOffset;
        public Vector3 AP02RotationOffset => ap02RotationOffset;
        public Transform AP02EditRoot => ap02EditRoot;
        public Transform AP02EditHandle => ap02EditHandle;
        public Transform AP03 => ap03;
        public bool AP03Editing => ap03Editing;
        public Vector3 AP03PositionOffset => ap03PositionOffset;
        public Vector3 AP03RotationOffset => ap03RotationOffset;
        public Transform AP03EditRoot => ap03EditRoot;
        public Transform AP03EditHandle => ap03EditHandle;
        public Transform AP04 => ap04;
        public bool AP04Editing => ap04Editing;
        public Vector3 AP04PositionOffset => ap04PositionOffset;
        public Vector3 AP04RotationOffset => ap04RotationOffset;
        public Transform AP04EditRoot => ap04EditRoot;
        public Transform AP04EditHandle => ap04EditHandle;
        public Transform AP05 => ap05;
        public bool AP05Editing => ap05Editing;
        public Vector3 AP05PositionOffset => ap05PositionOffset;
        public Vector3 AP05RotationOffset => ap05RotationOffset;
        public Transform AP05EditRoot => ap05EditRoot;
        public Transform AP05EditHandle => ap05EditHandle;
        public Transform AP06 => ap06;
        public bool AP06Editing => ap06Editing;
        public Vector3 AP06PositionOffset => ap06PositionOffset;
        public Vector3 AP06RotationOffset => ap06RotationOffset;
        public Transform AP06EditRoot => ap06EditRoot;
        public Transform AP06EditHandle => ap06EditHandle;
        public Transform CIItemSize => ciItemSize;
        public bool CIItemSizeEditing => ciItemSizeEditing;
        public bool CIItemSizeContactEditing => ciItemSizeContactEditing;
        public bool CIInputContactEditing => ciInputContactEditing;
        public bool CIOutputContactEditing => ciOutputContactEditing;
        public Transform APContactHandL => apContactHandL;
        public bool APContactHandLEditing => apContactHandLEditing;
        public Transform APContactHandR => apContactHandR;
        public bool APContactHandREditing => apContactHandREditing;
        public Transform APContact00 => apContact00;
        public bool APContact00Editing => apContact00Editing;
        public Transform APContact01 => apContact01;
        public bool APContact01Editing => apContact01Editing;
        public Transform APContact02 => apContact02;
        public bool APContact02Editing => apContact02Editing;
        public Transform APContact03 => apContact03;
        public bool APContact03Editing => apContact03Editing;
        public Transform APContact04 => apContact04;
        public bool APContact04Editing => apContact04Editing;
        public Transform APContact05 => apContact05;
        public bool APContact05Editing => apContact05Editing;
        public Transform APContact06 => apContact06;
        public bool APContact06Editing => apContact06Editing;

        public void SetCISlot(int value)
        {
            ciSlot = Mathf.Clamp(value, 0, 15);
        }

        public void SetCIItemSize(Transform value)
        {
            ciItemSize = value;
        }

        public void SetMenuObjects(
            Transform settingsRoot,
            Transform resetItem,
            Transform switchHandsItem,
            Transform freezeItem)
        {
            menuSettingsRoot = settingsRoot;
            menuResetItem = resetItem;
            menuSwitchHandsItem = switchHandsItem;
            menuFreezeItem = freezeItem;
        }

        public void InitializeNumberedAttachPointList(int count)
        {
            var clampedCount = Mathf.Clamp(count, 1, 7);
            var order = new int[clampedCount];
            for (var index = 0; index < clampedCount; index++)
            {
                order[index] = index;
            }

            SetNumberedAttachPointOrder(order);
        }

        public void SetNumberedAttachPointCount(int count)
        {
            var clampedCount = Mathf.Clamp(count, 1, 7);
            var resizedOrder = new List<int>(GetNumberedAttachPointOrder());
            if (!resizedOrder.Contains(0))
            {
                resizedOrder.Insert(0, 0);
            }

            while (resizedOrder.Count > clampedCount)
            {
                var removeIndex = resizedOrder.FindLastIndex(attachPointIndex => attachPointIndex != 0);
                if (removeIndex < 0)
                {
                    break;
                }

                resizedOrder.RemoveAt(removeIndex);
            }

            for (var candidate = 0; resizedOrder.Count < clampedCount && candidate < 7; candidate++)
            {
                if (!resizedOrder.Contains(candidate))
                {
                    resizedOrder.Add(candidate);
                }
            }

            SetNumberedAttachPointOrder(resizedOrder);
        }

        public int GetNumberedAttachPointAt(int orderIndex)
        {
            if (HasValidNumberedAttachPointOrder())
            {
                return orderIndex >= 0 && orderIndex < numberedAttachPointOrder.Count
                    ? numberedAttachPointOrder[orderIndex]
                    : -1;
            }

            var legacyCount = Mathf.Clamp(numberedAttachPointCount, 0, 7);
            return orderIndex >= 0 && orderIndex < legacyCount ? orderIndex : -1;
        }

        public int[] GetNumberedAttachPointOrder()
        {
            if (HasValidNumberedAttachPointOrder())
            {
                return numberedAttachPointOrder.ToArray();
            }

            var legacyCount = Mathf.Clamp(numberedAttachPointCount, 0, 7);
            var order = new int[legacyCount];
            for (var index = 0; index < legacyCount; index++)
            {
                order[index] = index;
            }

            return order;
        }

        public bool IsNumberedAttachPointEnabled(int attachPointIndex)
        {
            if (attachPointIndex < 0 || attachPointIndex >= 7)
            {
                return false;
            }

            if (attachPointIndex == 0)
            {
                return true;
            }

            if (HasValidNumberedAttachPointOrder())
            {
                return numberedAttachPointOrder.Contains(attachPointIndex);
            }

            return attachPointIndex < Mathf.Clamp(numberedAttachPointCount, 0, 7);
        }

        public void SetNumberedAttachPointOrder(IEnumerable<int> order)
        {
            var sanitizedOrder = new List<int>();
            if (order != null)
            {
                foreach (var attachPointIndex in order)
                {
                    if (attachPointIndex < 0 || attachPointIndex >= 7
                        || sanitizedOrder.Contains(attachPointIndex))
                    {
                        continue;
                    }

                    sanitizedOrder.Add(attachPointIndex);
                }
            }

            if (!sanitizedOrder.Contains(0))
            {
                sanitizedOrder.Insert(0, 0);
            }

            numberedAttachPointOrder = sanitizedOrder;
            numberedAttachPointCount = numberedAttachPointOrder.Count;
            numberedAttachPointListInitialized = true;
        }

        private bool HasValidNumberedAttachPointOrder()
        {
            if (numberedAttachPointOrder == null
                || numberedAttachPointOrder.Count != Mathf.Clamp(numberedAttachPointCount, 0, 7))
            {
                return false;
            }

            var seen = new bool[7];
            foreach (var attachPointIndex in numberedAttachPointOrder)
            {
                if (attachPointIndex < 0 || attachPointIndex >= seen.Length || seen[attachPointIndex])
                {
                    return false;
                }

                seen[attachPointIndex] = true;
            }

            return true;
        }

        public void SetAPHandLEditing(bool value)
        {
            apHandLEditing = value;
        }

        public void SetAPHandLOffsets(Vector3 positionOffset, Vector3 rotationOffset)
        {
            apHandLPositionOffset = positionOffset;
            apHandLRotationOffset = rotationOffset;
        }

        public void SetAPHandLEditObjects(Transform editRoot, Transform editHandle)
        {
            apHandLEditRoot = editRoot;
            apHandLEditHandle = editHandle;
        }

        public void SetCIItemSizeEditing(bool value)
        {
            ciItemSizeEditing = value;
        }

        public void SetCIItemSizeContactEditing(bool value)
        {
            ciItemSizeContactEditing = value;
        }

        public void SetCIInputContactEditing(bool value)
        {
            ciInputContactEditing = value;
        }

        public void SetCIOutputContactEditing(bool value)
        {
            ciOutputContactEditing = value;
        }

        public void SetAPContactHandLEditing(bool value)
        {
            apContactHandLEditing = value;
        }

        public Transform GetContactTransform(string contactName)
        {
            switch (contactName)
            {
                case "AP_Contact_Hand_L": return apContactHandL;
                case "AP_Contact_Hand_R": return apContactHandR;
                case "AP_Contact_00": return apContact00;
                case "AP_Contact_01": return apContact01;
                case "AP_Contact_02": return apContact02;
                case "AP_Contact_03": return apContact03;
                case "AP_Contact_04": return apContact04;
                case "AP_Contact_05": return apContact05;
                case "AP_Contact_06": return apContact06;
                default: return null;
            }
        }

        public bool GetContactEditing(string contactName)
        {
            switch (contactName)
            {
                case "AP_Contact_Hand_L": return apContactHandLEditing;
                case "AP_Contact_Hand_R": return apContactHandREditing;
                case "AP_Contact_00": return apContact00Editing;
                case "AP_Contact_01": return apContact01Editing;
                case "AP_Contact_02": return apContact02Editing;
                case "AP_Contact_03": return apContact03Editing;
                case "AP_Contact_04": return apContact04Editing;
                case "AP_Contact_05": return apContact05Editing;
                case "AP_Contact_06": return apContact06Editing;
                default: return false;
            }
        }

        public void SetContactEditing(string contactName, bool value)
        {
            switch (contactName)
            {
                case "AP_Contact_Hand_L": apContactHandLEditing = value; break;
                case "AP_Contact_Hand_R": apContactHandREditing = value; break;
                case "AP_Contact_00": apContact00Editing = value; break;
                case "AP_Contact_01": apContact01Editing = value; break;
                case "AP_Contact_02": apContact02Editing = value; break;
                case "AP_Contact_03": apContact03Editing = value; break;
                case "AP_Contact_04": apContact04Editing = value; break;
                case "AP_Contact_05": apContact05Editing = value; break;
                case "AP_Contact_06": apContact06Editing = value; break;
            }
        }

        public bool AnyContactEditing()
        {
            return apContactHandLEditing || apContactHandREditing || apContact00Editing || apContact01Editing
                || apContact02Editing || apContact03Editing || apContact04Editing || apContact05Editing || apContact06Editing;
        }

        public bool GetAttachPointEditing(string attachPointName)
        {
            switch (attachPointName)
            {
                case "AP_Hand_L": return apHandLEditing;
                case "AP_Hand_R": return apHandREditing;
                case "AP_00": return ap00Editing;
                case "AP_01": return ap01Editing;
                case "AP_02": return ap02Editing;
                case "AP_03": return ap03Editing;
                case "AP_04": return ap04Editing;
                case "AP_05": return ap05Editing;
                case "AP_06": return ap06Editing;
                default: return false;
            }
        }

        public Vector3 GetAttachPointPositionOffset(string attachPointName)
        {
            switch (attachPointName)
            {
                case "AP_Hand_L": return apHandLPositionOffset;
                case "AP_Hand_R": return apHandRPositionOffset;
                case "AP_00": return ap00PositionOffset;
                case "AP_01": return ap01PositionOffset;
                case "AP_02": return ap02PositionOffset;
                case "AP_03": return ap03PositionOffset;
                case "AP_04": return ap04PositionOffset;
                case "AP_05": return ap05PositionOffset;
                case "AP_06": return ap06PositionOffset;
                default: return Vector3.zero;
            }
        }

        public Vector3 GetAttachPointRotationOffset(string attachPointName)
        {
            switch (attachPointName)
            {
                case "AP_Hand_L": return apHandLRotationOffset;
                case "AP_Hand_R": return apHandRRotationOffset;
                case "AP_00": return ap00RotationOffset;
                case "AP_01": return ap01RotationOffset;
                case "AP_02": return ap02RotationOffset;
                case "AP_03": return ap03RotationOffset;
                case "AP_04": return ap04RotationOffset;
                case "AP_05": return ap05RotationOffset;
                case "AP_06": return ap06RotationOffset;
                default: return Vector3.zero;
            }
        }

        public Transform GetAttachPointEditRoot(string attachPointName)
        {
            switch (attachPointName)
            {
                case "AP_Hand_L": return apHandLEditRoot;
                case "AP_Hand_R": return apHandREditRoot;
                case "AP_00": return ap00EditRoot;
                case "AP_01": return ap01EditRoot;
                case "AP_02": return ap02EditRoot;
                case "AP_03": return ap03EditRoot;
                case "AP_04": return ap04EditRoot;
                case "AP_05": return ap05EditRoot;
                case "AP_06": return ap06EditRoot;
                default: return null;
            }
        }

        public Transform GetAttachPointEditHandle(string attachPointName)
        {
            switch (attachPointName)
            {
                case "AP_Hand_L": return apHandLEditHandle;
                case "AP_Hand_R": return apHandREditHandle;
                case "AP_00": return ap00EditHandle;
                case "AP_01": return ap01EditHandle;
                case "AP_02": return ap02EditHandle;
                case "AP_03": return ap03EditHandle;
                case "AP_04": return ap04EditHandle;
                case "AP_05": return ap05EditHandle;
                case "AP_06": return ap06EditHandle;
                default: return null;
            }
        }

        public void SetAttachPointEditing(string attachPointName, bool value)
        {
            switch (attachPointName)
            {
                case "AP_Hand_L": apHandLEditing = value; break;
                case "AP_Hand_R": apHandREditing = value; break;
                case "AP_00": ap00Editing = value; break;
                case "AP_01": ap01Editing = value; break;
                case "AP_02": ap02Editing = value; break;
                case "AP_03": ap03Editing = value; break;
                case "AP_04": ap04Editing = value; break;
                case "AP_05": ap05Editing = value; break;
                case "AP_06": ap06Editing = value; break;
            }
        }

        public EasyCarrySystemAttachPointMethod GetAttachPointMethod(string attachPointName)
        {
            switch (attachPointName)
            {
                case "AP_00": return ap00AttachmentMethod;
                case "AP_01": return ap01AttachmentMethod;
                case "AP_02": return ap02AttachmentMethod;
                case "AP_03": return ap03AttachmentMethod;
                case "AP_04": return ap04AttachmentMethod;
                case "AP_05": return ap05AttachmentMethod;
                case "AP_06": return ap06AttachmentMethod;
                default: return EasyCarrySystemAttachPointMethod.BoneProxy;
            }
        }

        public void SetAttachPointMethod(string attachPointName, EasyCarrySystemAttachPointMethod value)
        {
            switch (attachPointName)
            {
                case "AP_00": ap00AttachmentMethod = value; break;
                case "AP_01": ap01AttachmentMethod = value; break;
                case "AP_02": ap02AttachmentMethod = value; break;
                case "AP_03": ap03AttachmentMethod = value; break;
                case "AP_04": ap04AttachmentMethod = value; break;
                case "AP_05": ap05AttachmentMethod = value; break;
                case "AP_06": ap06AttachmentMethod = value; break;
            }
        }

        public void SetAttachPointOffsets(string attachPointName, Vector3 positionOffset, Vector3 rotationOffset)
        {
            switch (attachPointName)
            {
                case "AP_Hand_L": SetAPHandLOffsets(positionOffset, rotationOffset); break;
                case "AP_Hand_R": apHandRPositionOffset = positionOffset; apHandRRotationOffset = rotationOffset; break;
                case "AP_00": ap00PositionOffset = positionOffset; ap00RotationOffset = rotationOffset; break;
                case "AP_01": ap01PositionOffset = positionOffset; ap01RotationOffset = rotationOffset; break;
                case "AP_02": ap02PositionOffset = positionOffset; ap02RotationOffset = rotationOffset; break;
                case "AP_03": ap03PositionOffset = positionOffset; ap03RotationOffset = rotationOffset; break;
                case "AP_04": ap04PositionOffset = positionOffset; ap04RotationOffset = rotationOffset; break;
                case "AP_05": ap05PositionOffset = positionOffset; ap05RotationOffset = rotationOffset; break;
                case "AP_06": ap06PositionOffset = positionOffset; ap06RotationOffset = rotationOffset; break;
            }
        }

        public void SetAttachPointEditObjects(string attachPointName, Transform editRoot, Transform editHandle)
        {
            switch (attachPointName)
            {
                case "AP_Hand_L": SetAPHandLEditObjects(editRoot, editHandle); break;
                case "AP_Hand_R": apHandREditRoot = editRoot; apHandREditHandle = editHandle; break;
                case "AP_00": ap00EditRoot = editRoot; ap00EditHandle = editHandle; break;
                case "AP_01": ap01EditRoot = editRoot; ap01EditHandle = editHandle; break;
                case "AP_02": ap02EditRoot = editRoot; ap02EditHandle = editHandle; break;
                case "AP_03": ap03EditRoot = editRoot; ap03EditHandle = editHandle; break;
                case "AP_04": ap04EditRoot = editRoot; ap04EditHandle = editHandle; break;
                case "AP_05": ap05EditRoot = editRoot; ap05EditHandle = editHandle; break;
                case "AP_06": ap06EditRoot = editRoot; ap06EditHandle = editHandle; break;
            }
        }

        public bool AnyAttachPointEditing()
        {
            return apHandLEditing || apHandREditing || ap00Editing || ap01Editing || ap02Editing || ap03Editing || ap04Editing || ap05Editing || ap06Editing;
        }
    }
}
