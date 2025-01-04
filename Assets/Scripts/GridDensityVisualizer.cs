using System.Collections.Generic;
using UnityEngine;

public class GridDensityVisualizer : MonoBehaviour
{
    public dm2 dataManager; // 数据管理器

    public Vector3 targetCenter = new Vector3(-0.67f, 0.73f, -6.57f); // 主场景目标中心点
    public Vector3 targetScale = new Vector3(5, 5, 5); // 缩放范围
    public GameObject keypointPrefab;
    private Dictionary<string, GameObject> keypointObjects = new Dictionary<string, GameObject>();

    void Start(){
        if (dataManager == null || keypointPrefab == null)
    {
        Debug.LogError("DataManager or KeypointPrefab is not assigned!");
        return;
    }

    InitializeKeypoints();
    
    }
    private void InitializeKeypoints()
{
    if (dataManager.frames.Count == 0)
    {
        Debug.LogError("No data loaded in DataManager!");
        return;
    }

    var firstFrame = dataManager.frames[0];
    foreach (var keypoint in firstFrame.keypoints)
    {
        GameObject keypointObj = Instantiate(keypointPrefab, MapToScene(keypoint.Value), Quaternion.identity);
        keypointObj.name = $"Keypoint_{keypoint.Key}";
        keypointObjects[keypoint.Key] = keypointObj;
        Debug.Log($"Keypoint {keypoint.Key} initialized at {keypointObj.transform.position}");
    }
}
private Vector3 MapToScene(Vector3 original)
{
    // 计算范围
    Vector3 range = new Vector3(
        Mathf.Max(dataManager.maxBounds.x - dataManager.minBounds.x, 0.0001f),
        Mathf.Max(dataManager.maxBounds.y - dataManager.minBounds.y, 0.0001f),
        Mathf.Max(dataManager.maxBounds.z - dataManager.minBounds.z, 0.0001f)
    );

    // 归一化到 [0, 1]
    Vector3 normalized = new Vector3(
        (original.x - dataManager.minBounds.x) / range.x,
        (original.y - dataManager.minBounds.y) / range.y,
        (original.z - dataManager.minBounds.z) / range.z
    );

    // 映射到目标范围
    return targetCenter + Vector3.Scale(normalized, targetScale);
}
}
