using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using UnityEngine.UI; // 需要引入 UI 命名空间


public class MotionVisualizer2 : MonoBehaviour
{
    public dm2 dataManager; // 数据管理器
    public GameObject keypointPrefab; // 用于显示关键点的小球Prefab
    public float playbackSpeed = 1.0f; // 播放速度
    public Vector3 targetCenter = new Vector3(-0.67f, 0.73f, -6.57f); // 主场景目标中心点
    public Vector3 targetScale = new Vector3(5, 5, 5); // 缩放范围

    private Dictionary<string, GameObject> keypointObjects = new Dictionary<string, GameObject>();
    private int currentFrame = 0;
    private bool isPlaying = false;

    public Slider timeSlider; // 时间轴 Slider

    

    void Start()
    {
        if (dataManager == null || keypointPrefab == null)
        {
            Debug.LogError("DataManager or KeypointPrefab is not assigned!");
            return;
        }

        InitializeKeypoints();
        

        if (timeSlider != null)
{
    timeSlider.minValue = 0;
    timeSlider.maxValue = dataManager.frames.Count - 1;
    timeSlider.onValueChanged.AddListener(OnSliderValueChanged);
}

    }

    public void OnSliderValueChanged(float value)
{
    currentFrame = Mathf.RoundToInt(value); // 设置当前帧为滑块值
    UpdateKeypointsImmediate(); // 立即更新关键点位置
}

public void PlayPause()
{
    isPlaying = !isPlaying;
    Debug.Log(isPlaying ? "Playback started." : "Playback paused.");
}

public void UpdateKeypointsImmediate()
{
    if (currentFrame < 0 || currentFrame >= dataManager.frames.Count) return;

    var frameData = dataManager.frames[currentFrame];
    foreach (var keypoint in frameData.keypoints)
    {
        if (keypointObjects.ContainsKey(keypoint.Key))
        {
            keypointObjects[keypoint.Key].transform.position = MapToScene(keypoint.Value);
        }
    }
}
    

    void Update()
    {
        if (isPlaying)
        {
            UpdateKeypoints();
        }
    }

    public void Play()
    {
        isPlaying = true;
        currentFrame = 0;
    }

    public void Pause()
    {
        isPlaying = false;
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
    }
}


    private float elapsedTime = 0f; // 累计时间

private void UpdateKeypoints()
{
    elapsedTime += Time.deltaTime * playbackSpeed;

    if (elapsedTime >= 1f / playbackSpeed)
    {
        elapsedTime = 0f;

        if (currentFrame >= dataManager.frames.Count)
        {
            isPlaying = false;
            Debug.Log("Playback finished.");
            return;
        }

        var frameData = dataManager.frames[currentFrame];
        foreach (var keypoint in frameData.keypoints)
        {
            if (keypointObjects.ContainsKey(keypoint.Key))
            {
                keypointObjects[keypoint.Key].transform.position = MapToScene(keypoint.Value);
            }
        }

        currentFrame++;
        if (timeSlider != null)
        {
            timeSlider.value = currentFrame; // 更新时间轴
        }
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
