using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AvatarFace : MonoBehaviour
{
    public enum SyncEyesBlinkMode 
    { 
        None, // Basically unmodified mediapipe data (true to the detection), but not aesthetically pleasing.
        Average, 
        PreferOpen,
        PreferClosed // Used in the video, basically when the detection is unsure it will prefer to keep its eyes closed (good effects in terms of a viewer).
    }

    [Header("General")]
    [SerializeField] 
    private PipeServer server;
    [SerializeField] 
    private SkinnedMeshRenderer meshRenderer;
    [SerializeField]
    private Animator animator; // Used to look up eye transform references.
    [SerializeField]
    private Transform headIKTarget;
    [SerializeField]
    private Vector3 headIKTargetOffset;
    [Header("Smoothing")]
    [SerializeField]
    private float headSmoothSpeed = 10f;
    [SerializeField]
    private float eyeSmoothSpeed = 30f;
    [SerializeField]
    private float expressionSmoothSpeed = 15f;
    [Header("Movement")]
    [SerializeField]
    private float movementScaler = 0.01f;
    [Header("Blinking")]
    // Customize these parameters if you're having blinking problems.
    [SerializeField]
    private AnimationCurve blinkCurve;
    [SerializeField]
    private float[] blinkMinMax = new float[] { .1f, .3f };
    [SerializeField]
    private SyncEyesBlinkMode syncEyeBlinks = SyncEyesBlinkMode.Average; // Particularly these...
    [Header("Blendshape Management")]
    [SerializeField]
    private BlendshapeMapper mapper;

    /// <summary>
    /// Latest values from detection. [0f, 1f] range.
    /// </summary>
    private Dictionary<BlendshapeKey, float> latestValues;
    private Vector3 faceRotation;
    private Vector3 zeroFaceRotation;
    private Quaternion smoothLookRot;

    private Vector3 i;
    private Vector3 translation;
    private Vector3 zeroTranslation;

    private Mesh sharedMesh;
    private Transform eyeLeft, eyeRight, head;

    private Quaternion lastRotation;
    private Vector3 lastPosition;

    public Vector3 initialTrackPosition => i;
    public Vector3 TPosition => headIKTarget.transform.position;

    private void Start()
    {
        mapper.Initialize();

        i = headIKTarget.position;
        latestValues = new Dictionary<BlendshapeKey, float>();

        sharedMesh = meshRenderer.sharedMesh;
        eyeLeft = animator.GetBoneTransform(HumanBodyBones.LeftEye).transform;
        eyeRight = animator.GetBoneTransform(HumanBodyBones.RightEye).transform;
        head = animator.GetBoneTransform(HumanBodyBones.Head).transform;

        server.onDetection += Server_onDetection;
    }
    private void LateUpdate()
    {
        Vector3 angles = (faceRotation - zeroFaceRotation) * Mathf.Rad2Deg;
        Quaternion target = Quaternion.Euler(angles);
        smoothLookRot = Quaternion.Slerp(smoothLookRot, target, Time.deltaTime * headSmoothSpeed);
        headIKTarget.transform.localRotation = smoothLookRot;

        Vector3 tt = i + (translation - zeroTranslation) * movementScaler;
        headIKTarget.transform.position = Vector3.Lerp(headIKTarget.transform.position, tt, Time.deltaTime * 5f);

        // some hardcoded values to achieve behaviour.
        float s = (((headIKTarget.transform.position - lastPosition) / Time.deltaTime).magnitude);
        float m2 = Mathf.InverseLerp(.2f, .1f, s);

        float d = Mathf.Abs(Quaternion.Angle(target, headIKTarget.transform.localRotation));
        float m = Mathf.InverseLerp(10, 3, d);

        // Use data to apply to model (readonly operation).
        foreach (BlendshapeKey k in AllKeys)
        {
            if (!latestValues.ContainsKey(k)) continue;

            float confidence = Mathf.Min(m, m2);
            confidence = Mathf.Clamp(confidence, .1f, 1f);

            float v = GetBlendshapeNormalized(k);
            switch (k)
            {
                case BlendshapeKey.EYE_BLINK_LEFT:
                case BlendshapeKey.EYE_BLINK_RIGHT:
                    if(confidence > .5f)
                        v = Mathf.Lerp(GetBlendshapeNormalized(k),GetBlinkValue(latestValues[k]), Time.deltaTime * eyeSmoothSpeed*confidence);
                    break;
                case BlendshapeKey.EYE_SQUINT_LEFT:
                case BlendshapeKey.EYE_SQUINT_RIGHT:
                    if (confidence > .5f)
                        v = Mathf.Lerp(GetBlendshapeNormalized(k), latestValues[k], Time.deltaTime * eyeSmoothSpeed * confidence);
                    break;
                case BlendshapeKey.BROW_DOWN_LEFT:
                case BlendshapeKey.BROW_DOWN_RIGHT:
                case BlendshapeKey.BROW_INNER_UP:
                case BlendshapeKey.BROW_OUTER_UP_LEFT:
                case BlendshapeKey.BROW_OUTER_UP_RIGHT:
                    v = Mathf.Lerp(GetBlendshapeNormalized(k), latestValues[k], Time.deltaTime * eyeSmoothSpeed * confidence);
                    break;
                default:
                    v = Mathf.Lerp(GetBlendshapeNormalized(k), latestValues[k], Time.deltaTime * expressionSmoothSpeed*confidence);
                    break;
            }

            SetBlendshape(k, v);
        }

        if (syncEyeBlinks != SyncEyesBlinkMode.None)
        {
            // Average eye blinks out to same value.
            float v1 = GetBlendshapeNormalized(BlendshapeKey.EYE_BLINK_LEFT), v2 = GetBlendshapeNormalized(BlendshapeKey.EYE_BLINK_RIGHT);
            float a = (v1 + v2) / 2f;
            switch (syncEyeBlinks)
            {
                case SyncEyesBlinkMode.Average:
                    a =(v1 + v2) / 2f;
                    break;

                case SyncEyesBlinkMode.PreferOpen:
                    a = Mathf.Min(v1,v2);
                    break;

                case SyncEyesBlinkMode.PreferClosed:
                    a = Mathf.Max(v1,v2);
                    break;
            }
            SetBlendshape(BlendshapeKey.EYE_BLINK_LEFT, a);
            SetBlendshape(BlendshapeKey.EYE_BLINK_RIGHT, a);
        }

        Vector3 leftEyeRot = Vector3.zero;
        leftEyeRot.x += Mathf.Lerp(0, -5, GetBlendshapeNormalized(BlendshapeKey.EYE_LOOK_UP_LEFT));
        leftEyeRot.x += Mathf.Lerp(0, 5, GetBlendshapeNormalized(BlendshapeKey.EYE_LOOK_DOWN_LEFT));
        leftEyeRot.y += Mathf.Lerp(0, 6, GetBlendshapeNormalized(BlendshapeKey.EYE_LOOK_IN_LEFT));
        leftEyeRot.y += Mathf.Lerp(0, -10, GetBlendshapeNormalized(BlendshapeKey.EYE_LOOK_OUT_LEFT));

        eyeLeft.localRotation = Quaternion.Euler(leftEyeRot);
        eyeRight.localRotation = Quaternion.Euler(leftEyeRot);

        lastRotation = headIKTarget.transform.localRotation;
        lastPosition = headIKTarget.transform.position;
    }

    private float GetBlinkValue(float r)
    {
        r = Mathf.InverseLerp(blinkMinMax[0], blinkMinMax[1], r);
        return blinkCurve.Evaluate(r);

    }

    private void Server_onDetection(string[] data)
    {
        for(int i = 0;i<data.Length;++i)
        {
            string l = data[i];
            if (string.IsNullOrWhiteSpace(l))
                continue;
            string[] s = l.Split('|');
            if (i == 0)
            {
                float x, y, z;
                if (!float.TryParse(s[0], out x)) continue;
                if (!float.TryParse(s[1], out y)) continue;
                if (!float.TryParse(s[2], out z)) continue;
                faceRotation = new Vector3(z, -y, -x);
                if (zeroFaceRotation.sqrMagnitude == 0f)
                    zeroFaceRotation = faceRotation;
                continue;
            }
            if (i == 1)
            {
                float x, y, z;
                if (!float.TryParse(s[0], out x)) continue;
                if (!float.TryParse(s[1], out y)) continue;
                if (!float.TryParse(s[2], out z)) continue;
                translation = new Vector3(x,-y,-z);
                if (zeroTranslation.sqrMagnitude == 0f)
                    zeroTranslation = translation;
                continue;
            }
            if (s.Length < 2) continue;
            int k;
            if (!int.TryParse(s[0], out k)) continue;
            float v;
            if (!float.TryParse(s[1], out v)) continue;

            latestValues[(BlendshapeKey)k] = v;
        }
    }

    private float GetBlendshapeNormalized(BlendshapeKey key)
    {
        int i = sharedMesh.GetBlendShapeIndex(mapper.GetBlendshapeName(key));
        return meshRenderer.GetBlendShapeWeight(i) / 100f;
    }
    private void SetBlendshape(BlendshapeKey i, float value)
    {
        SetBlendshape(mapper.GetBlendshapeName(i), value);
    }
    private void SetBlendshape(string name, float value)
    {
        int i = sharedMesh.GetBlendShapeIndex(name);
        if (i == -1) return;
        meshRenderer.SetBlendShapeWeight(i, value * 100);
    }


    // Static data (used to map from blendshape enum to actual blendshape names).
    /// <summary>
    /// Identical to pythons internal representation.
    /// </summary>
    public enum BlendshapeKey
    {
        NEUTRAL = 0,
        BROW_DOWN_LEFT = 1,
        BROW_DOWN_RIGHT = 2,
        BROW_INNER_UP = 3,
        BROW_OUTER_UP_LEFT = 4,
        BROW_OUTER_UP_RIGHT = 5,
        CHEEK_PUFF = 6,
        CHEEK_SQUINT_LEFT = 7,
        CHEEK_SQUINT_RIGHT = 8,
        EYE_BLINK_LEFT = 9,
        EYE_BLINK_RIGHT = 10,
        EYE_LOOK_DOWN_LEFT = 11,
        EYE_LOOK_DOWN_RIGHT = 12,
        EYE_LOOK_IN_LEFT = 13,
        EYE_LOOK_IN_RIGHT = 14,
        EYE_LOOK_OUT_LEFT = 15,
        EYE_LOOK_OUT_RIGHT = 16,
        EYE_LOOK_UP_LEFT = 17,
        EYE_LOOK_UP_RIGHT = 18,
        EYE_SQUINT_LEFT = 19,
        EYE_SQUINT_RIGHT = 20,
        EYE_WIDE_LEFT = 21,
        EYE_WIDE_RIGHT = 22,
        JAW_FORWARD = 23,
        JAW_LEFT = 24,
        JAW_OPEN = 25,
        JAW_RIGHT = 26,
        MOUTH_CLOSE = 27,
        MOUTH_DIMPLE_LEFT = 28,
        MOUTH_DIMPLE_RIGHT = 29,
        MOUTH_FROWN_LEFT = 30,
        MOUTH_FROWN_RIGHT = 31,
        MOUTH_FUNNEL = 32,
        MOUTH_LEFT = 33,
        MOUTH_LOWER_DOWN_LEFT = 34,
        MOUTH_LOWER_DOWN_RIGHT = 35,
        MOUTH_PRESS_LEFT = 36,
        MOUTH_PRESS_RIGHT = 37,
        MOUTH_PUCKER = 38,
        MOUTH_RIGHT = 39,
        MOUTH_ROLL_LOWER = 40,
        MOUTH_ROLL_UPPER = 41,
        MOUTH_SHRUG_LOWER = 42,
        MOUTH_SHRUG_UPPER = 43,
        MOUTH_SMILE_LEFT = 44,
        MOUTH_SMILE_RIGHT = 45,
        MOUTH_STRETCH_LEFT = 46,
        MOUTH_STRETCH_RIGHT = 47,
        MOUTH_UPPER_UP_LEFT = 48,
        MOUTH_UPPER_UP_RIGHT = 49,
        NOSE_SNEER_LEFT = 50,
        NOSE_SNEER_RIGHT = 51
    }
    public static readonly BlendshapeKey[] AllKeys = (BlendshapeKey[])System.Enum.GetValues(typeof(BlendshapeKey));

    [System.Serializable]
    public class BlendshapeMapper
    {
        [SerializeField]
        private bool capitalizeFirstWord = false;

        public void Initialize()
        {
            // Modify each value according to the options (high performance after this).
            foreach(BlendshapeKey k in AllKeys)
            {
                string s = Shape2Name[k];
                if (capitalizeFirstWord&&s.Length>1)
                {
                    s = s[0].ToString().ToUpper() + s[1..];
                }
                Shape2Name[k] = s;
            }
        }

        public string GetBlendshapeName(BlendshapeKey key)
        {
            return Shape2Name[key];
        }

        /// <summary>
        /// Map from blendshape key to actual names of the blendshapes on the face...
        /// <br>IF you need to modify this, create a custom class overriding Initialize instead of directly modifying this (generally speaking).</br>
        /// </summary>
        private Dictionary<BlendshapeKey, string> Shape2Name = new Dictionary<BlendshapeKey, string>()
    {
        { BlendshapeKey.NEUTRAL, "" },
        { BlendshapeKey.BROW_DOWN_LEFT, "browDownLeft" },
        { BlendshapeKey.BROW_DOWN_RIGHT, "browDownRight" },
        { BlendshapeKey.BROW_INNER_UP, "browInnerUp" },
        { BlendshapeKey.BROW_OUTER_UP_LEFT, "browOuterUpLeft" },
        { BlendshapeKey.BROW_OUTER_UP_RIGHT, "browOuterUpRight" },
        { BlendshapeKey.CHEEK_PUFF, "cheekPuff" },
        { BlendshapeKey.CHEEK_SQUINT_LEFT, "cheekSquintLeft" },
        { BlendshapeKey.CHEEK_SQUINT_RIGHT, "cheekSquintRight" },
        { BlendshapeKey.EYE_BLINK_LEFT, "eyeBlinkLeft" },
        { BlendshapeKey.EYE_BLINK_RIGHT, "eyeBlinkRight" },
        { BlendshapeKey.EYE_LOOK_DOWN_LEFT, "eyeLookDownLeft" },
        { BlendshapeKey.EYE_LOOK_DOWN_RIGHT, "eyeLookDownRight" },
        { BlendshapeKey.EYE_LOOK_IN_LEFT, "eyeLookInLeft" },
        { BlendshapeKey.EYE_LOOK_IN_RIGHT, "eyeLookInRight" },
        { BlendshapeKey.EYE_LOOK_OUT_LEFT, "eyeLookOutLeft" },
        { BlendshapeKey.EYE_LOOK_OUT_RIGHT, "eyeLookOutRight" },
        { BlendshapeKey.EYE_LOOK_UP_LEFT, "eyeLookUpLeft" },
        { BlendshapeKey.EYE_LOOK_UP_RIGHT, "eyeLookUpRight" },
        { BlendshapeKey.EYE_SQUINT_LEFT, "eyeSquintLeft" },
        { BlendshapeKey.EYE_SQUINT_RIGHT, "eyeSquintRight" },
        { BlendshapeKey.EYE_WIDE_LEFT, "eyeWideLeft" },
        { BlendshapeKey.EYE_WIDE_RIGHT, "eyeWideRight" },
        { BlendshapeKey.JAW_FORWARD, "jawForward" },
        { BlendshapeKey.JAW_LEFT, "jawLeft" },
        { BlendshapeKey.JAW_OPEN, "jawOpen" },
        { BlendshapeKey.JAW_RIGHT, "jawRight" },
        { BlendshapeKey.MOUTH_CLOSE, "mouthClose" },
        { BlendshapeKey.MOUTH_DIMPLE_LEFT, "mouthDimpleLeft" },
        { BlendshapeKey.MOUTH_DIMPLE_RIGHT, "mouthDimpleRight" },
        { BlendshapeKey.MOUTH_FROWN_LEFT, "mouthFrownLeft" },
        { BlendshapeKey.MOUTH_FROWN_RIGHT, "mouthFrownRight" },
        { BlendshapeKey.MOUTH_FUNNEL, "mouthFunnel" },
        { BlendshapeKey.MOUTH_LEFT, "mouthLeft" },
        { BlendshapeKey.MOUTH_LOWER_DOWN_LEFT, "mouthLowerDownLeft" },
        { BlendshapeKey.MOUTH_LOWER_DOWN_RIGHT, "mouthLowerDownRight" },
        { BlendshapeKey.MOUTH_PRESS_LEFT, "mouthPressLeft" },
        { BlendshapeKey.MOUTH_PRESS_RIGHT, "mouthPressRight" },
        { BlendshapeKey.MOUTH_PUCKER, "mouthPucker" },
        { BlendshapeKey.MOUTH_RIGHT, "mouthRight" },
        { BlendshapeKey.MOUTH_ROLL_LOWER, "mouthRollLower" },
        { BlendshapeKey.MOUTH_ROLL_UPPER, "mouthRollUpper" },
        { BlendshapeKey.MOUTH_SHRUG_LOWER, "mouthShrugLower" },
        { BlendshapeKey.MOUTH_SHRUG_UPPER, "mouthShrugUpper" },
        { BlendshapeKey.MOUTH_SMILE_LEFT, "mouthSmileLeft" },
        { BlendshapeKey.MOUTH_SMILE_RIGHT, "mouthSmileRight" },
        { BlendshapeKey.MOUTH_STRETCH_LEFT, "mouthStretchLeft" },
        { BlendshapeKey.MOUTH_STRETCH_RIGHT, "mouthStretchRight" },
        { BlendshapeKey.MOUTH_UPPER_UP_LEFT, "mouthUpperUpLeft" },
        { BlendshapeKey.MOUTH_UPPER_UP_RIGHT, "mouthUpperUpRight" },
        { BlendshapeKey.NOSE_SNEER_LEFT, "noseSneerLeft" },
        { BlendshapeKey.NOSE_SNEER_RIGHT, "noseSneerRight" },
    };
    }
}
