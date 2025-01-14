using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using UnityEngine.UI; // 需要引入 UI 命名空间


public class MotionVisualizer2 : MonoBehaviour
{
    public dm2 dataManager; // 数据管理器
    public GameObject voxelPrefab; // 用于显示体素的预制件
    public Transform voxelContainer; // 体素父对象
    public GameObject keypointPrefab; // 用于显示关键点的小球Prefab
    public float playbackSpeed = 1.0f; // 播放速度
    public Vector3 targetCenter = new Vector3(-0.67f, 0.73f, -6.57f); // 主场景目标中心点
    public Vector3 targetScale = new Vector3(5, 5, 5); // 缩放范围

    private Dictionary<string, GameObject> keypointObjects = new Dictionary<string, GameObject>();
    private int currentFrame = 0;
    private bool isPlaying = false;

    public Slider timeSlider; // 时间轴 Slider

    public Slider voxelTimeSlider; // 用于控制体素时间的拖拽条
    private int currentVoxelFrame = 0; // 当前体素帧

    public int voxelSamplingRate = 5;

    public GameObject gridPrefab;

    public float gridSize = 0.1f;

    private bool visualizationEnabled = false;

    private List<GameObject> gridObjects = new List<GameObject>();

    public float cameraMovementSpeed = 5f; // Camera movement speed
    public float cameraVerticalSpeed = 3f; // Vertical movement speed

    public float mouseSensitivity = 2f; // Mouse look sensitivity
    public bool invertMouseY = false; // Option to invert vertical mouse look
    private float cameraPitch = 0f; // Vertical rotation tracking

    private float cameraYaw = 0f; // Horizontal rotation tracking

    private bool mouseControlEnabled = true;
    public struct FrameData
    {
        public float time; // 时间戳
        public Dictionary<string, Vector3> keypoints; // 每帧所有关键点的坐标
    }
    private List<FrameData> filteredFrames = new List<FrameData>(); // 截取后的数据
    private int startFrame = 0; // 起始帧
    private int endFrame = 8000;   // 结束帧

    public List<dm2.FrameData> visframes = new List<dm2.FrameData>();

    public GameObject catheterPrefab;

    public Transform catheterContainer;

    private GameObject currentCatheter;

    public float catheterScaleFactor = 6.0f;








    
    public List<dm2.FrameData> GetFramesInRange(int startFrame, int endFrame)
{
    // 验证范围
    if (startFrame < 0 || endFrame >= visframes.Count || startFrame > endFrame)
    {
        Debug.LogError($"Invalid range: startFrame={startFrame}, endFrame={endFrame}, totalFrames={visframes.Count}");
        return new List<dm2.FrameData>(); // 返回空列表
    }

    // 使用 GetRange 提取范围内的帧数据
    return visframes.GetRange(startFrame, endFrame - startFrame + 1);
}

private void AddCatheterCenterToVisframes()
{
    Debug.Log("Adding cathetercenter to visframes...");
    
    // 遍历每一帧的数据
    foreach (var frame in visframes)
    {
        // 检查是否包含所有必要的关键点
        if (frame.keypoints.TryGetValue("StickTopLeft", out Vector3 topLeft) &&
            frame.keypoints.TryGetValue("StickTopRight", out Vector3 topRight) &&
            frame.keypoints.TryGetValue("StickBottomLeft", out Vector3 bottomLeft) &&
            frame.keypoints.TryGetValue("StickBottomRight", out Vector3 bottomRight))
        {
            // 计算中心点
            Vector3 topCenter = (topLeft + topRight) / 2;
            Vector3 bottomCenter = (bottomLeft + bottomRight) / 2;
            Vector3 catheterCenter = (topCenter + bottomCenter) / 2;

            // 添加到 keypoints 列表中
            if (!frame.keypoints.ContainsKey("cathetercenter"))
            {
                frame.keypoints["cathetercenter"] = catheterCenter;
            }
        }
        else
        {
            Debug.LogWarning("Missing keypoints for cathetercenter calculation in one or more frames.");
        }
    }

    Debug.Log("cathetercenter column added to visframes.");
}

private void AddSpeedAndAccelerationToVisframes()
{
    Debug.Log("Adding speed and acceleration columns to visframes...");

    // 验证数据完整性
    if (visframes.Count < 2)
    {
        Debug.LogError("Not enough frames to calculate speed and acceleration!");
        return;
    }

    // 遍历帧，计算速度和加速度
    for (int i = 0; i < visframes.Count; i++)
    {
        var frame = visframes[i];

        // 确保 cathetercenter 存在
        if (!frame.keypoints.TryGetValue("cathetercenter", out Vector3 currentCenter))
        {
            Debug.LogWarning($"Frame {i} missing cathetercenter, skipping speed and acceleration calculation.");
            continue;
        }

        // calculation speed
        Vector3 velocity = Vector3.zero;
        if (i > 0) 
        {
            var previousFrame = visframes[i - 1];
            if (previousFrame.keypoints.TryGetValue("cathetercenter", out Vector3 previousCenter))
            {
                float deltaTime = frame.time - previousFrame.time;
                if (deltaTime > 0)
                {
                    velocity = (currentCenter - previousCenter) / deltaTime;
                }
            }
        }

        // Calculated acceleration
        Vector3 acceleration = Vector3.zero;
        if (i > 1) 
        {
            var twoFramesAgo = visframes[i - 2];
            if (twoFramesAgo.keypoints.TryGetValue("cathetercenter", out Vector3 twoFramesAgoCenter))
            {
                float deltaTime1 = frame.time - visframes[i - 1].time;
                float deltaTime2 = visframes[i - 1].time - twoFramesAgo.time;
                if (deltaTime1 > 0 && deltaTime2 > 0)
                {
                    Vector3 velocityPrevious = (currentCenter - visframes[i - 1].keypoints["cathetercenter"]) / deltaTime1;
                    Vector3 velocityTwoFramesAgo = (visframes[i - 1].keypoints["cathetercenter"] - twoFramesAgoCenter) / deltaTime2;
                    acceleration = (velocityPrevious - velocityTwoFramesAgo) / ((deltaTime1 + deltaTime2) / 2);
                }
            }
        }

        // 添加速度和加速度到 keypoints
        frame.keypoints["catheterspeed"] = velocity;
        frame.keypoints["catheteracceleration"] = acceleration;
    }

    Debug.Log("Speed and acceleration columns added to visframes.");
}

public void RemoveUnwantedColumns(List<string> columnsToRemove)
{
    // 遍历每一帧的数据
    foreach (var frame in visframes)
    {
        // 遍历要删除的列名列表
        foreach (var column in columnsToRemove)
        {
            // 如果当前帧的 keypoints 中存在该列，移除它
            if (frame.keypoints.ContainsKey(column))
            {
                frame.keypoints.Remove(column);
            }
        }
    }

    Debug.Log($"Removed {columnsToRemove.Count} columns from visframes.");
}
    float maxSpeed = float.MinValue;
float minSpeed = float.MaxValue;
float maxAcceleration = float.MinValue;
float minAcceleration = float.MaxValue;
void Start()
{   
    // 计算 maxSpeed 和 maxAcceleration
    
    if (dataManager == null || keypointPrefab == null)
    {
        Debug.LogError("DataManager or KeypointPrefab is not assigned!");
        return;
    }
    visframes = dataManager.frames;
    visframes = GetFramesInRange(startFrame, endFrame);
    List<string> unwantedColumns = new List<string> { "TopLeft", 
    "TopRight", 
    "BottomLeft", 
    "BottomRight",
    "Brow",
    "Tip",
    "StickTopLeft",
    "StickTopRight",
    "StickBottomLeft",
    "StickBottomRight",
    // "catheterspeed",
    // "catheteracceleration"
     };
    AddCatheterCenterToVisframes();
    AddSpeedAndAccelerationToVisframes();
    foreach (var frame in visframes)
    {
        if (frame.keypoints.TryGetValue("catheterspeed", out Vector3 speed))
        {
            float speedMagnitude = speed.magnitude;

            if (speedMagnitude > 0) // 过滤掉值为 0 的数据
            {
                maxSpeed = Mathf.Max(maxSpeed, speedMagnitude);
                minSpeed = Mathf.Min(minSpeed, speedMagnitude);
            }
        }

        if (frame.keypoints.TryGetValue("catheteracceleration", out Vector3 acceleration))
        {
            float accelerationMagnitude = acceleration.magnitude;

            if (accelerationMagnitude > 0) // 过滤掉值为 0 的数据
            {
                maxAcceleration = Mathf.Max(maxAcceleration, accelerationMagnitude);
                minAcceleration = Mathf.Min(minAcceleration, accelerationMagnitude);
            }
        }
    }
    RemoveUnwantedColumns(unwantedColumns);
    

    InitializeKeypoints();

    // 初始化时间轴
    if (timeSlider != null)
    {
        timeSlider.minValue = 0;
        timeSlider.maxValue = visframes.Count - 1;
        timeSlider.onValueChanged.AddListener(OnSliderValueChanged);
    }

    // 初始化体素时间轴
    if (voxelTimeSlider != null)
    {
        voxelTimeSlider.minValue = 0;
        voxelTimeSlider.maxValue = visframes.Count - 1;
        voxelTimeSlider.onValueChanged.AddListener(OnVoxelSliderValueChanged);
    }

    // 计算时间密度并显示初始体素
    dataManager.CalculateTemporalDensity();
    VisualizeDensity(0);

    CalculateAndVisualizeDensity();

    InitializeCatheter();
}

private void InitializeCatheter()
{
    Debug.Log("Initializing Catheter...");

    if (!visframes[0].keypoints.TryGetValue("cathetercenter", out Vector3 catheterCenter))
    {
        Debug.LogError("cathetercenter is missing in the first frame!");
        return;
    }

    // 映射 cathetercenter 到场景坐标系
    Vector3 mappedCenter = MapToScene(catheterCenter);

    // 初始化 Catheter
    currentCatheter = Instantiate(catheterPrefab, mappedCenter, Quaternion.identity, catheterContainer);
    if (currentCatheter == null)
    {
        Debug.LogError("Failed to instantiate Catheter prefab!");
        return;
    }

    // 设置初始缩放
    currentCatheter.transform.localScale = new Vector3(
        currentCatheter.transform.localScale.x * catheterScaleFactor,
        currentCatheter.transform.localScale.y * catheterScaleFactor,
        currentCatheter.transform.localScale.z * catheterScaleFactor
    );

    Debug.Log($"Catheter initialized at {mappedCenter}.");
}


public void OnVoxelSliderValueChanged(float value)
{
    isPlaying = false; // 暂停播放
    currentVoxelFrame = Mathf.RoundToInt(value); // 获取滚动条值
    VisualizeDensity(currentVoxelFrame); // 更新体素显示
}


public void ToggleVisualization()
{
    visualizationEnabled = !visualizationEnabled;

    if (visualizationEnabled)
    {
        Debug.Log("Visualization Enabled");
        CalculateAndVisualizeDensity();
    }
    else
    {
        Debug.Log("Visualization Disabled");
        ClearVisualization();
    }
}

private void ClearVisualization()
{
    foreach (var grid in gridObjects)
    {
        Destroy(grid);
    }
    gridObjects.Clear();
}


public enum VisualizationMode
{
    Density,
    Speed,
    Acceleration
}

public VisualizationMode visualizationMode = VisualizationMode.Density;

public void CalculateAndVisualizeDensity()


{

    foreach (Transform child in voxelContainer)
    {
        Destroy(child.gameObject);
    }
    gridObjects.Clear();
    
    if (gridPrefab == null)
    {
        Debug.LogError("GridPrefab is not assigned!");
        return;
    }

    if (gridSize <= 0)
    {
        Debug.LogError("Grid size must be greater than zero!");
        return;
    }

    // 获取映射后的坐标范围
    Vector3 mappedMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
    Vector3 mappedMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);

    foreach (var frame in visframes)
    {
        if (frame.keypoints.TryGetValue("cathetercenter", out Vector3 catheterCenter))
        {
            Vector3 mappedPosition = MapToScene(catheterCenter);
            mappedMin = Vector3.Min(mappedMin, mappedPosition);
            mappedMax = Vector3.Max(mappedMax, mappedPosition);
        }
    }

    Debug.Log($"Mapped Min: {mappedMin}, Mapped Max: {mappedMax}");

    Vector3Int gridDimensions = new Vector3Int(
        Mathf.CeilToInt((mappedMax.x - mappedMin.x) / gridSize),
        Mathf.CeilToInt((mappedMax.y - mappedMin.y) / gridSize),
        Mathf.CeilToInt((mappedMax.z - mappedMin.z) / gridSize)
    );

    Dictionary<Vector3Int, int> gridDensity = new Dictionary<Vector3Int, int>();

    foreach (var frame in visframes)
    {
        if (frame.keypoints.TryGetValue("cathetercenter", out Vector3 catheterCenter))
        {
            Vector3 mappedPosition = MapToScene(catheterCenter);
            Vector3Int gridIndex = new Vector3Int(
                Mathf.FloorToInt((mappedPosition.x - mappedMin.x) / gridSize),
                Mathf.FloorToInt((mappedPosition.y - mappedMin.y) / gridSize),
                Mathf.FloorToInt((mappedPosition.z - mappedMin.z) / gridSize)
            );

            if (!gridDensity.ContainsKey(gridIndex))
            {
                gridDensity[gridIndex] = 0;
            }

            gridDensity[gridIndex]++;
        }
    }

    int maxDensity = 0;
    int minDensity = int.MaxValue;

    foreach (int density in gridDensity.Values)
    {
        maxDensity = Mathf.Max(maxDensity, density);
        minDensity = Mathf.Min(minDensity, density);
    }

    Debug.Log($"Min Density: {minDensity}, Max Density: {maxDensity}");

    float logMaxDensity = Mathf.Log10(maxDensity + 1);
    float logMinDensity = Mathf.Log10(minDensity + 1);

    

    foreach (var kvp in gridDensity)

{



    Vector3Int index = kvp.Key;
    int density = kvp.Value;

    float valueToVisualize = 0f; // 用于存储当前可视化值
    float normalizedValue = 0f; // 用于归一化可视化值

    Debug.Log($"Mode: {visualizationMode}, Value: {valueToVisualize}, Normalized: {normalizedValue}");


    switch (visualizationMode)
{
    case VisualizationMode.Density:
        float logDensity = Mathf.Log10(density + 1); // 对数变换
        normalizedValue = (logDensity - logMinDensity) / (logMaxDensity - logMinDensity);
        Debug.Log($"Density: {density}, Normalized: {normalizedValue}");
        break;

    case VisualizationMode.Speed:
        if (visframes[index.z].keypoints.TryGetValue("catheterspeed", out Vector3 speed))
        {
            valueToVisualize = speed.magnitude;

            // 对速度进行对数变换
            float logSpeed = Mathf.Log10(valueToVisualize + 1); // 避免对 0 取对数
            float logSpeedMin = Mathf.Log10(minSpeed + 1);
            float logSpeedMax = Mathf.Log10(maxSpeed + 1);

            // 使用对数范围进行归一化
            normalizedValue = (logSpeed - logSpeedMin) / (logSpeedMax - logSpeedMin);
            normalizedValue = Mathf.Clamp01(normalizedValue);
            Debug.Log($"Speed: {valueToVisualize}, LogSpeed: {logSpeed}, Normalized: {normalizedValue}");
        }
        break;

    case VisualizationMode.Acceleration:
        if (visframes[index.z].keypoints.TryGetValue("catheteracceleration", out Vector3 acceleration))
        {
            valueToVisualize = acceleration.magnitude;

            // 对加速度进行对数变换
            float logAccel = Mathf.Log10(valueToVisualize + 1); // 避免对 0 取对数
            float logAccelMin = Mathf.Log10(minAcceleration + 1);
            float logAccelMax = Mathf.Log10(maxAcceleration + 1);

            // 使用对数范围进行归一化
            normalizedValue = (logAccel - logAccelMin) / (logAccelMax - logAccelMin);
            normalizedValue = Mathf.Clamp01(normalizedValue);
            Debug.Log($"Acceleration: {valueToVisualize}, LogAccel: {logAccel}, Normalized: {normalizedValue}");
        }
        break;
}


    normalizedValue = Mathf.Pow(normalizedValue, 0.7f);


    Vector3 gridCenter = new Vector3(
        mappedMin.x + index.x * gridSize + gridSize / 2,
        mappedMin.y + index.y * gridSize + gridSize / 2,
        mappedMin.z + index.z * gridSize + gridSize / 2
    );

    GameObject grid = Instantiate(gridPrefab, gridCenter, Quaternion.identity, voxelContainer);
    grid.transform.localScale = Vector3.one * gridSize;

    Renderer renderer = grid.GetComponent<Renderer>();
    if (renderer != null)
    {
        Color color = Color.Lerp(Color.blue, Color.red, normalizedValue);
        float enhancedValue = Mathf.Pow(normalizedValue, 0.7f); // 增强颜色分布

        if (enhancedValue < 0.33f)
        {
            // Cool colors: deep blue to vibrant green
            color = Color.Lerp(Color.blue, Color.green, enhancedValue * 3f);
            color = Color.Lerp(color, Color.green * 1.2f, 0.3f);
        }
        else if (enhancedValue < 0.66f)
        {
            // Warm colors: green to saturated orange
            Color saturatedOrange = new Color(1f, 0.6f, 0f, 1f);
            color = Color.Lerp(Color.green, saturatedOrange, (enhancedValue - 0.33f) * 3f);
            color = Color.Lerp(color, saturatedOrange * 1.3f, 0.4f);
        }
        else
        {
            // Hot colors: orange to deep red
            Color saturatedOrange = new Color(1f, 0.6f, 0f, 1f);
            color = Color.Lerp(saturatedOrange, Color.red, (enhancedValue - 0.66f) * 3f);
            color = Color.Lerp(color, Color.red * 1.2f, 0.3f);
        }

        color.a = Mathf.Lerp(0.2f, 1f, enhancedValue);

        renderer.material.color = color;

        Debug.Log($"Grid Index: {index}, Color: {color}");


        var material = renderer.material;
        material.SetFloat("_Mode", 3);
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = 3000;
    }

    gridObjects.Add(grid); // 将网格对象添加到列表中
}
}


    private void VisualizeDensity(int frameIndex = -1)
{
    // 清理旧的体素
    foreach (Transform child in voxelContainer)
    {
        Destroy(child.gameObject);
    }

    // 获取最大密度值和最大重叠密度值
    float maxDensity = 0;
    int maxOverlapDensity = 0;

    foreach (var density in dataManager.voxelDensity.Values)
    {
        maxDensity = Mathf.Max(maxDensity, density);
    }

    foreach (var overlap in dataManager.voxelOverlapDensity.Values)
    {
        maxOverlapDensity = Mathf.Max(maxOverlapDensity, overlap);
    }

    // 遍历体素密度，生成体素
    foreach (var kvp in dataManager.voxelOverlapDensity)
    {
        Vector3Int index = kvp.Key;
        int overlapDensity = kvp.Value;

        // 时间过滤：仅显示当前帧及之前的体素
        if (frameIndex >= 0 && index.z > frameIndex)
        {
            continue;
        }

        // 计算体素中心位置
        Vector3 voxelCenter = new Vector3(
            index.x * dataManager.voxelSize + dataManager.minBounds.x + dataManager.voxelSize / 2,
            index.y * dataManager.voxelSize + dataManager.minBounds.y + dataManager.voxelSize / 2,
            index.z * dataManager.voxelSize + dataManager.minBounds.z + dataManager.voxelSize / 2
        );

        // 映射到场景位置
        Vector3 mappedPosition = MapToScene(voxelCenter);

        GameObject voxel = Instantiate(voxelPrefab, mappedPosition, Quaternion.identity, voxelContainer);
        voxel.transform.localScale = Vector3.one * dataManager.voxelSize;

        Renderer renderer = voxel.GetComponent<Renderer>();
        if (renderer != null)
        {
            float normalizedDensity = Mathf.Clamp01(dataManager.voxelDensity.ContainsKey(index) 
                ? dataManager.voxelDensity[index] / maxDensity 
                : 0);
            float normalizedOverlap = Mathf.Clamp01((float)overlapDensity / maxOverlapDensity);

            // 非线性映射增强颜色
            Color color = GetEnhancedColor(normalizedDensity);

            // 设置透明度
            color.a = Mathf.Lerp(0.2f, 1f, 1f - normalizedOverlap); // 透明度由重叠密度控制
            renderer.material.color = color;
        }
    }
}


private Color GetOverlapColor(float normalizedDensity)
{
    // 颜色过渡：蓝 -> 绿 -> 红
    return Color.Lerp(Color.blue, Color.red, normalizedDensity);
}



private Color GetEnhancedColor(float normalizedDensity)
{
    // 提高高密度区域的红色显著性
    return Color.Lerp(Color.blue, Color.red, Mathf.Pow(normalizedDensity, 1.5f));
}





    public void OnSliderValueChanged(float value)
{
    currentFrame = Mathf.RoundToInt(value); // 更新当前帧
    UpdateKeypointsImmediate(); // 立即更新关键点位置
    UpdateCatheter();
}
private void UpdateCatheter()
{
    Debug.Log("Updating Catheter...");

    // 验证当前帧索引
    if (currentFrame < 0 || currentFrame >= visframes.Count)
    {
        Debug.LogError($"Invalid frame index: {currentFrame}. Total frames: {visframes.Count}");
        return;
    }

    var frameData = visframes[currentFrame];

    // 确保 cathetercenter 存在
    if (!frameData.keypoints.TryGetValue("cathetercenter", out Vector3 catheterCenter))
    {
        Debug.LogWarning("cathetercenter is missing in the current frame!");
        return;
    }

    // 映射 cathetercenter 到场景坐标系
    Vector3 mappedCenter = MapToScene(catheterCenter);

    // 更新 Catheter 的位置
    currentCatheter.transform.position = mappedCenter;

    Debug.Log($"Catheter updated at {mappedCenter}.");
}





void OnGUI()
{
    GUI.Label(new Rect(10, 10, 200, 20), $"Playback Speed: {playbackSpeed:F1}");
    GUI.Label(new Rect(10, 30, 200, 20), isPlaying ? "Status: Playing" : "Status: Paused");
}

public void UpdateKeypointsImmediate()
{
    if (currentFrame < 0 || currentFrame >= visframes.Count) return;

    var frameData = visframes[currentFrame];
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

    if (Input.GetKeyDown(KeyCode.B))
    {
        visualizationMode = VisualizationMode.Density;
        Debug.Log("Switched to Density visualization.");
        CalculateAndVisualizeDensity();
    }
    else if (Input.GetKeyDown(KeyCode.N))
    {
        visualizationMode = VisualizationMode.Speed;
        Debug.Log("Switched to Speed visualization.");
        CalculateAndVisualizeDensity();
    }
    else if (Input.GetKeyDown(KeyCode.M))
    {
        visualizationMode = VisualizationMode.Acceleration;
        Debug.Log("Switched to Acceleration visualization.");
        CalculateAndVisualizeDensity();
    }
    // 按下 Q 键切换鼠标控制状态
    if (Input.GetKeyDown(KeyCode.Q))
    {
        mouseControlEnabled = !mouseControlEnabled;

        if (!mouseControlEnabled)
        {
            Debug.Log("Mouse control disabled. Camera view locked.");
        }
        else
        {
            Debug.Log("Mouse control enabled.");
        }
    }

    // 鼠标控制逻辑
    if (mouseControlEnabled)
    {
        HandleMouseControls(); // 只有启用鼠标控制时才更新视角
    }

    // 始终处理拖拽条的逻辑
    if (timeSlider != null)
    {
        if (Mathf.RoundToInt(timeSlider.value) != currentFrame)
        {
            currentFrame = Mathf.RoundToInt(timeSlider.value); // 获取滑块值
            UpdateKeypointsImmediate(); // 更新关键点位置
        }
    }

    // 其他播放逻辑
    if (Input.GetKey(KeyCode.R))
    {
        elapsedTime += Time.deltaTime * playbackSpeed;

        if (elapsedTime >= 1f / playbackSpeed)
        {
            elapsedTime -= 1f / playbackSpeed;

            currentVoxelFrame++;
            if (currentVoxelFrame >= visframes.Count)
            {
                currentVoxelFrame = 0;
            }

            VisualizeDensity(currentVoxelFrame);
            UpdateKeypointsImmediate();

            if (voxelTimeSlider != null)
            {
                voxelTimeSlider.value = currentVoxelFrame;
            }
        }
    }

    if (Input.GetKeyDown(KeyCode.UpArrow))
    {
        playbackSpeed = Mathf.Min(playbackSpeed + 0.5f, 10f);
        Debug.Log($"Playback Speed Increased: {playbackSpeed}");
    }

    if (Input.GetKeyDown(KeyCode.DownArrow))
    {
        playbackSpeed = Mathf.Max(playbackSpeed - 0.5f, 0.5f);
        Debug.Log($"Playback Speed Decreased: {playbackSpeed}");
    }

    if (Input.GetKeyDown(KeyCode.V)) ToggleVisualization();
    if (Input.GetKeyDown(KeyCode.LeftArrow)) StepBackward();
    if (Input.GetKeyDown(KeyCode.RightArrow)) StepForward();

    // 相机移动逻辑始终启用
    HandleCameraMovement();
}

// 重置相机方向为默认朝向
private void ResetCameraDirection()
{
    if (Camera.main != null)
    {
        cameraPitch = 0f; // 重置俯仰角
        cameraYaw = 0f;   // 重置偏航角
        Camera.main.transform.rotation = Quaternion.Euler(cameraPitch, cameraYaw, 0f);
    }
}

// 鼠标控制逻辑
private void HandleMouseControls()
{
    if (Camera.main != null)
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * (invertMouseY ? -1 : 1);

        cameraYaw += mouseX;
        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, -90f, 90f);

        Quaternion rotation = Quaternion.Euler(cameraPitch, cameraYaw, 0f);
        Camera.main.transform.rotation = rotation;
    }
}
// 相机移动逻辑（始终启用）
private void HandleCameraMovement()
{
    if (Camera.main != null)
    {
        Vector3 moveDirection = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) moveDirection += Camera.main.transform.forward;
        if (Input.GetKey(KeyCode.S)) moveDirection -= Camera.main.transform.forward;
        if (Input.GetKey(KeyCode.D)) moveDirection += Camera.main.transform.right;
        if (Input.GetKey(KeyCode.A)) moveDirection -= Camera.main.transform.right;
        if (Input.GetKey(KeyCode.Space)) moveDirection += Vector3.up;
        if (Input.GetKey(KeyCode.LeftShift)) moveDirection -= Vector3.up;

        Camera.main.transform.position += moveDirection.normalized * cameraMovementSpeed * Time.deltaTime;
    }
}


public void PlayPause()
{
    isPlaying = !isPlaying; // 切换播放状态
    Debug.Log(isPlaying ? "Playback started." : "Playback paused.");
}





private void HandleCameraControls()
{
    if (Camera.main != null)
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * (invertMouseY ? -1 : 1);

        cameraYaw += mouseX;
        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, -90f, 90f);

        Quaternion rotation = Quaternion.Euler(cameraPitch, cameraYaw, 0f);
        Camera.main.transform.rotation = rotation;

        Vector3 moveDirection = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) moveDirection += Camera.main.transform.forward;
        if (Input.GetKey(KeyCode.S)) moveDirection -= Camera.main.transform.forward;
        if (Input.GetKey(KeyCode.D)) moveDirection += Camera.main.transform.right;
        if (Input.GetKey(KeyCode.A)) moveDirection -= Camera.main.transform.right;
        if (Input.GetKey(KeyCode.Space)) moveDirection += Vector3.up;
        if (Input.GetKey(KeyCode.LeftShift)) moveDirection -= Vector3.up;

        Camera.main.transform.position += moveDirection.normalized * cameraMovementSpeed * Time.deltaTime;
    }
}

public void StepForward()
{
    if (currentFrame < visframes.Count - 1)
    {
        currentFrame++;
        UpdateKeypointsImmediate();
        if (timeSlider != null)
        {
            timeSlider.value = currentFrame;
        }
    }
}

public void StepBackward()
{
    if (currentFrame > 0)
    {
        currentFrame--;
        UpdateKeypointsImmediate();
        if (timeSlider != null)
        {
            timeSlider.value = currentFrame;
        }
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
    if (visframes.Count == 0)
    {
        Debug.LogError("No data loaded in DataManager!");
        return;
    }

    var firstFrame = visframes[0];
    foreach (var keypoint in firstFrame.keypoints)
    {   
        Debug.Log($"Column: {keypoint.Key}");
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

        if (currentFrame >= visframes.Count)
        {
            isPlaying = false;
            Debug.Log("Playback finished.");
            return;
        }

        var frameData = visframes[currentFrame];
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
