using System.Collections.Generic;
using UnityEngine;  // 

[System.Serializable]
public class BVHJoint
{
    public string name;
    public Vector3 offset;
    public List<string> channels; // {"Xrotation","Yrotation"...}
    public List<BVHJoint> children;
    public bool isEndSite;
    // 该关节在帧数据里占用多少个数值
    public int channelCount => channels.Count;

    public BVHJoint()
    {
        children = new List<BVHJoint>();
        channels = new List<string>();
    }
}

[System.Serializable]
public class BVHData
{
    public BVHJoint rootJoint;
    public int frameCount;
    public float frameTime;
    // 所有帧：每一帧是一个 float 数组，按通道顺序排列
    public List<float[]> frames;

    public BVHData()
    {
        frames = new List<float[]>();
    }
}
