using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class dm2 : MonoBehaviour
{
    public string filePath = "Assets/MoCapData/catheter002.txt"; // 动捕文件路径

    public List<string> filePaths = new List<string>(); // 文件路径列表
    public List<FrameData> frames = new List<FrameData>(); // 存储每帧数据

    public List<FrameData> filteredFrames = new List<FrameData>();
    public Vector3 minBounds; // 数据最小值
    public Vector3 maxBounds; // 数据最大值

    public Dictionary<Vector3Int, float> voxelDensity = new Dictionary<Vector3Int, float>();
    public float voxelSize = 0.1f;

    public dm2 dataManager; // 数据管理器
    public MotionTrajectoryVisualizer trajectoryVisualizer;

    public Dictionary<Vector3Int, int> voxelOverlapDensity = new Dictionary<Vector3Int, int>();

    public int voxelSamplingRate = 2;

    public GridDensityVisualizer gridVisualizer;

    
    

    [System.Serializable]
    public struct FrameData
    {
        public float time; // 时间戳
        public Dictionary<string, Vector3> keypoints; // 每帧所有关键点的坐标
    }

    public void CalculateTemporalDensity()
    {
        voxelDensity.Clear();
        voxelOverlapDensity.Clear(); // 清空重叠密度

        for (int i = 1; i < frames.Count; i++) // 从第2帧开始
        {
            var prevFrame = frames[i - 1];
            var currentFrame = frames[i];
            float deltaTime = currentFrame.time - prevFrame.time;

            foreach (var key in currentFrame.keypoints.Keys)
            {
                if (prevFrame.keypoints.TryGetValue(key, out var prevPos) &&
                    currentFrame.keypoints.TryGetValue(key, out var currentPos))
                {
                    float distance = Vector3.Distance(prevPos, currentPos);
                    float speed = distance / deltaTime;
                    Vector3 midpoint = (prevPos + currentPos) / 2;

                    Vector3Int voxelIndex = GetVoxelIndex(midpoint);

                    // 体素采样：仅处理采样率满足条件的体素
                    if ((voxelIndex.x % voxelSamplingRate == 0) &&
                        (voxelIndex.y % voxelSamplingRate == 0) &&
                        (voxelIndex.z % voxelSamplingRate == 0))
                    {
                        // 更新时间密度
                        if (!voxelDensity.ContainsKey(voxelIndex))
                        {
                            voxelDensity[voxelIndex] = 0;
                        }
                        voxelDensity[voxelIndex] += distance / speed;

                        // 更新重叠密度
                        if (!voxelOverlapDensity.ContainsKey(voxelIndex))
                        {
                            voxelOverlapDensity[voxelIndex] = 0;
                        }
                        voxelOverlapDensity[voxelIndex]++;
                    }
                }
            }
        }

        Debug.Log("Temporal and overlap densities calculated with sampling rate.");
    }

    private Vector3Int GetVoxelIndex(Vector3 position)
    {
        return new Vector3Int(
            Mathf.FloorToInt((position.x - minBounds.x) / voxelSize),
            Mathf.FloorToInt((position.y - minBounds.y) / voxelSize),
            Mathf.FloorToInt((position.z - minBounds.z) / voxelSize)
        );
    }
public void VisualizeDensity()
    {
        if (gridVisualizer == null)
        {
            Debug.LogError("GridVisualizer is not assigned!");
            return;
        }

        // 提取所有点
        List<Vector3> points = new List<Vector3>();
        foreach (var frame in frames)
        {
            points.AddRange(frame.keypoints.Values);
        }

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
