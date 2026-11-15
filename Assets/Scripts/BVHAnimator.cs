using UnityEngine;
using System.Collections.Generic;

public class BVHAnimator : MonoBehaviour
{
    public TextAsset bvhFile;
    public bool playOnStart = true;

    [Tooltip("锁定根Hips位移，人物原地做动作，推荐打开")]
    public bool lockRootPosition = true;

    [Tooltip("骨架整体偏移，方便放到相机视野")]
    public Vector3 globalOffset = new Vector3(0, 1f, 5f);

    [Tooltip("关节小球大小")]
    public float boneSphereRadius = 0.12f;

    [Tooltip("Scene视图Gizmos连线颜色")]
    public Color gizmosLineColor = Color.yellow;

    [HideInInspector] public bool isPlaying;
    private float time;
    private BVHParser.BVHData bvhData;
    private float totalDuration;

    private GameObject rootBone;
    private List<BoneRef> boneRefs;

    [System.Serializable]
    public struct BoneRef
    {
        public Transform trans;
        public List<string> channels;
        public int frameIndex;
        public Transform parentTrans;
    }

    void Start()
    {
        if (bvhFile != null)
        {
            LoadBVH(bvhFile.text);
            if (playOnStart)
                isPlaying = true;
        }
    }

    public void LoadBVH(string text)
    {
        if (rootBone != null)
            Destroy(rootBone);

        time = 0;
        bvhData = BVHParser.ParseFromText(text);
        totalDuration = bvhData.frameCount * bvhData.frameTime;

        boneRefs = new List<BoneRef>();
        int ptr = 0;
        rootBone = CreateBoneRecursive(bvhData.rootJoint, transform, ref ptr);
        rootBone.transform.localPosition = globalOffset;
    }

    void Update()
    {
        if (!isPlaying || bvhData == null) return;

        time += Time.deltaTime;
        if (time > totalDuration)
            time = 0;

        ApplyPose(time);
    }

    // ==========【核心修复】子骨骼只改旋转，不改localPosition，只初始化读取offset ==========
    public void ApplyPose(float t)
    {
        if (bvhData == null || bvhData.frameCount == 0) return;

        float frameF = t / bvhData.frameTime;
        int frameA = Mathf.FloorToInt(frameF);
        int frameB = frameA + 1;
        float blend = frameF - frameA;

        frameA = Mathf.Clamp(frameA, 0, bvhData.frameCount - 1);
        frameB = Mathf.Clamp(frameB, 0, bvhData.frameCount - 1);

        float[] fA = bvhData.frames[frameA];
        float[] fB = bvhData.frames[frameB];

        foreach (var br in boneRefs)
        {
            Vector3 pos = Vector3.zero;
            Vector3 rot = Vector3.zero;
            if (br.channels == null) continue;

            foreach (var ch in br.channels)
            {
                int idx = br.frameIndex + br.channels.IndexOf(ch);
                float val = Mathf.Lerp(fA[idx], fB[idx], blend);
                switch (ch)
                {
                    case "Xposition": pos.x = val; break;
                    case "Yposition": pos.y = val; break;
                    case "Zposition": pos.z = val; break;
                    case "Xrotation": rot.x = val; break;
                    case "Yrotation": rot.y = val; break;
                    case "Zrotation": rot.z = val; break;
                }
            }

            // 只有直接挂在BVHPlayer下的根Hips骨骼，才处理position
            if (br.trans.parent == transform)
            {
                if (lockRootPosition)
                {
                    br.trans.localEulerAngles = rot;
                    // 锁定根：丢弃根位移，只保留旋转
                }
                else
                {
                    br.trans.localPosition = pos;
                    br.trans.localEulerAngles = rot;
                }
            }
            else
            {
                // ！！所有子骨骼：只赋值旋转！localPosition保持初始化joint.offset，绝不覆盖！！
                br.trans.localEulerAngles = rot;
            }
        }
    }

    GameObject CreateBoneRecursive(BVHParser.BVHJoint joint, Transform parent, ref int ptr)
    {
        GameObject boneObj = new GameObject(joint.name);
        boneObj.transform.SetParent(parent, false);

        // 创建关节小球
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.SetParent(boneObj.transform, false);
        sphere.transform.localScale = Vector3.one * boneSphereRadius;
        Destroy(sphere.GetComponent<Collider>());

        // ✅ 初始化骨架绑定姿态，只在这里执行一次！动画运行时不许修改子骨骼localPosition
        boneObj.transform.localPosition = joint.offset;

        BoneRef br = new BoneRef
        {
            trans = boneObj.transform,
            parentTrans = parent,
            channels = joint.channels,
            frameIndex = ptr
        };
        boneRefs.Add(br);

        if (joint.channels != null)
            ptr += joint.channels.Count;

        if (joint.children != null)
        {
            foreach (var child in joint.children)
            {
                CreateBoneRecursive(child, boneObj.transform, ref ptr);
            }
        }
        return boneObj;
    }

    // Gizmos 黄色骨架连线，Scene窗口可见；Game窗口点右上角Gizmos按钮也可以显示
    void OnDrawGizmos()
    {
        if (boneRefs == null) return;
        Gizmos.color = gizmosLineColor;
        foreach (var br in boneRefs)
        {
            if (br.parentTrans != null && br.parentTrans != transform)
            {
                Gizmos.DrawLine(br.parentTrans.position, br.trans.position);
            }
        }
    }

    public void TogglePlay() => isPlaying = !isPlaying;
    public void ResetAnim()
    {
        time = 0;
        ApplyPose(0);
    }
    public void SetTime(float t)
    {
        time = Mathf.Clamp(t, 0, totalDuration);
        ApplyPose(time);
    }
    public float GetTotalTime() => totalDuration;
    public float GetTime() => time;
    public bool HasData() => bvhData != null;
}
