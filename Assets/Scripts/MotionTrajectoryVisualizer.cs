using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class MotionTrajectoryVisualizer : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public LineRenderer lineRendererPrefab; // 用于绘制轨迹的 LineRenderer 预制件
    private LineRenderer trajectoryLine; // 当前轨迹的 LineRenderer 对象

    private Dictionary<Vector3Int, int> trajectoryDensity = new Dictionary<Vector3Int, int>(); // 轨迹点密度
    private float voxelSize = 0.1f; // 体素大小
    private Vector3 minBounds; // 最小边界
    private Vector3 maxBounds; // 最大边界

    public void Initialize(Vector3 minBounds, Vector3 maxBounds, float voxelSize)
    {
        this.minBounds = minBounds;
        this.maxBounds = maxBounds;
        this.voxelSize = voxelSize;

        if (lineRendererPrefab == null)
        {
            Debug.LogError("LineRendererPrefab is not assigned!");
            return;
        }

        // 初始化 LineRenderer
        trajectoryLine = Instantiate(lineRendererPrefab, transform);
        trajectoryLine.positionCount = 0;
    }

    public void AddTrajectoryPoint(Vector3 position)
    {
        // 将点映射到体素索引
        Vector3Int voxelIndex = GetVoxelIndex(position);

        // 更新密度
        if (!trajectoryDensity.ContainsKey(voxelIndex))
        {
            trajectoryDensity[voxelIndex] = 0;
        }
        trajectoryDensity[voxelIndex]++;

        // 更新 LineRenderer 的点
        trajectoryLine.positionCount++;
        trajectoryLine.SetPosition(trajectoryLine.positionCount - 1, position);

        // 更新 LineRenderer 的颜色
        UpdateLineRendererColor();
    }

    private Vector3Int GetVoxelIndex(Vector3 position)
    {
        return new Vector3Int(
            Mathf.FloorToInt((position.x - minBounds.x) / voxelSize),
            Mathf.FloorToInt((position.y - minBounds.y) / voxelSize),
            Mathf.FloorToInt((position.z - minBounds.z) / voxelSize)
        );
    }

    private void UpdateLineRendererColor()
    {
        Gradient gradient = new Gradient();
        GradientColorKey[] colorKeys = new GradientColorKey[trajectoryLine.positionCount];
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[trajectoryLine.positionCount];
        float maxDensity = Mathf.Max(1, trajectoryDensity.Count > 0 ? trajectoryDensity.Values.Max() : 0);

        for (int i = 0; i < trajectoryLine.positionCount; i++)
        {
            Vector3 position = trajectoryLine.GetPosition(i);
            Vector3Int voxelIndex = GetVoxelIndex(position);

            int density = trajectoryDensity.ContainsKey(voxelIndex) ? trajectoryDensity[voxelIndex] : 0;
            float normalizedDensity = Mathf.Clamp01((float)density / maxDensity);

            // 颜色映射：低密度为浅蓝色，高密度为深红色
            Color color = Color.Lerp(Color.blue, Color.red, normalizedDensity);
            colorKeys[i] = new GradientColorKey(color, (float)i / trajectoryLine.positionCount);
            alphaKeys[i] = new GradientAlphaKey(1.0f, (float)i / trajectoryLine.positionCount);
        }

        gradient.SetKeys(colorKeys, alphaKeys);
        trajectoryLine.colorGradient = gradient;
    }
}
