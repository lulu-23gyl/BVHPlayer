using System;
using System.Collections.Generic;
using UnityEngine;

public class BVHParser
{
    public class BVHJoint
    {
        public string name;
        public Vector3 offset;
        public List<string> channels;
        public List<BVHJoint> children;
    }

    public class BVHData
    {
        public BVHJoint rootJoint;
        public int frameCount;
        public float frameTime;
        public List<float[]> frames = new List<float[]>();
    }

    private static string[] tokens;
    private static int tokenIndex;

    public static BVHData ParseFromText(string text)
    {
        tokens = text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        tokenIndex = 0;

        BVHData data = new BVHData();
        Expect("HIERARCHY");
        Expect("ROOT");
        data.rootJoint = ReadJointContent();

        Expect("MOTION");
        Expect("Frames:");
        int frameCnt = int.Parse(NextToken());
        Expect("Frame");
        Expect("Time:");
        float fTime = float.Parse(NextToken());

        int totalCh = CountAllChannels(data.rootJoint);
        List<float[]> frameList = new List<float[]>();
        for (int i = 0; i < frameCnt; i++)
        {
            float[] arr = new float[totalCh];
            for (int k = 0; k < totalCh; k++)
            {
                arr[k] = float.Parse(NextToken());
            }
            frameList.Add(arr);
        }

        return new BVHData
        {
            rootJoint = data.rootJoint,
            frameCount = frameCnt,
            frameTime = fTime,
            frames = frameList
        };
    }

    private static BVHJoint ReadJointContent()
    {
        BVHJoint j = new BVHJoint();
        j.name = NextToken();
        Expect("{");

        while (PeekToken() != "}")
        {
            string t = PeekToken();
            if (t == "OFFSET")
            {
                NextToken();
                j.offset.x = float.Parse(NextToken());
                j.offset.y = float.Parse(NextToken());
                j.offset.z = float.Parse(NextToken());
            }
            else if (t == "CHANNELS")
            {
                NextToken();
                int n = int.Parse(NextToken());
                j.channels = new List<string>();
                for (int i = 0; i < n; i++)
                {
                    j.channels.Add(NextToken());
                }
            }
            else if (t == "JOINT" || t == "End")
            {
                NextToken();
                if (j.children == null) j.children = new List<BVHJoint>();
                j.children.Add(ReadJointContent());
            }
            else
            {
                throw new Exception("Unexpected token inside joint: " + t);
            }
        }
        Expect("}");
        return j;
    }

    private static int CountAllChannels(BVHJoint j)
    {
        int cnt = j.channels?.Count ?? 0;
        if (j.children != null)
        {
            foreach (var c in j.children)
                cnt += CountAllChannels(c);
        }
        return cnt;
    }

    private static string NextToken()
    {
        if (tokenIndex >= tokens.Length)
            throw new Exception("BVH token out of range");
        return tokens[tokenIndex++];
    }

    private static string PeekToken()
    {
        if (tokenIndex >= tokens.Length) return "";
        return tokens[tokenIndex];
    }

    private static void Expect(string s)
    {
        string got = NextToken();
        if (got != s)
            throw new Exception($"Expected '{s}', got '{got}'");
    }
}
