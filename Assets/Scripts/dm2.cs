using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class dm2 : MonoBehaviour
{
    public string filePath = "Assets/MoCapData/catheter002.txt"; // 动捕文件路径

    public List<string> filePaths = new List<string>(); // 文件路径列表
    public List<FrameData> frames = new List<FrameData>(); // 存储每帧数据
    public Vector3 minBounds; // 数据最小值
    public Vector3 maxBounds; // 数据最大值

    [System.Serializable]
    public struct FrameData
    {
        public float time; // 时间戳
        public Dictionary<string, Vector3> keypoints; // 每帧所有关键点的坐标
    }

    void Start()
    {
        
    }

    void Awake() {
        LoadData();
        CalculateBounds();
    }

    public void LoadData()
{
    frames.Clear();

    foreach (var filePath in filePaths)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError($"File not found: {filePath}");
            continue;
        }

        string[] lines = File.ReadAllLines(filePath);
        string[] headers = null;

        foreach (string line in lines)
        {
            if (line.StartsWith("Field")) // 获取表头
            {
                headers = line.Split('\t');
                continue;
            }

            if (string.IsNullOrWhiteSpace(line) || headers == null)
                continue;

            string[] parts = line.Split('\t');
            if (parts.Length != headers.Length)
            {
                Debug.LogError($"Data line does not match header length in file {filePath}");
                continue;
            }

            FrameData frame = new FrameData
            {
                time = float.Parse(parts[1]),
                keypoints = new Dictionary<string, Vector3>()
            };

            for (int i = 2; i < headers.Length; i += 3)
            {
                if (i + 2 < headers.Length)
                {
                    string key = headers[i].Split(':')[0];
                    float x = float.Parse(parts[i]);
                    float y = float.Parse(parts[i + 1]);
                    float z = float.Parse(parts[i + 2]);
                    frame.keypoints[key] = new Vector3(x, y, z);
                }
            }

            frames.Add(frame);
        }

        Debug.Log($"Loaded {frames.Count} frames from file {filePath}");
    }

    Debug.Log($"Total frames loaded: {frames.Count}");
}

    private void CalculateBounds()
{
    minBounds = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
    maxBounds = new Vector3(float.MinValue, float.MinValue, float.MinValue);

    foreach (var frame in frames)
    {
        foreach (var keypoint in frame.keypoints.Values)
        {
            minBounds = Vector3.Min(minBounds, keypoint);
            maxBounds = Vector3.Max(maxBounds, keypoint);
        }
    }

    Debug.Log($"Global Data Bounds: Min {minBounds}, Max {maxBounds}");
}

}
