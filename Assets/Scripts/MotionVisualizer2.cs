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
    private int endFrame = 0;   // 结束帧


    
    void Start()
{
    if (dataManager == null || keypointPrefab == null)
    {
        Debug.LogError("DataManager or KeypointPrefab is not assigned!");
        return;
    }

    InitializeKeypoints();

    // 初始化时间轴
    if (timeSlider != null)
    {
        timeSlider.minValue = 0;
        timeSlider.maxValue = dataManager.frames.Count - 1;
        timeSlider.onValueChanged.AddListener(OnSliderValueChanged);
    }

    // 初始化体素时间轴
    if (voxelTimeSlider != null)
    {
        voxelTimeSlider.minValue = 0;
        voxelTimeSlider.maxValue = dataManager.frames.Count - 1;
        voxelTimeSlider.onValueChanged.AddListener(OnVoxelSliderValueChanged);
    }

    // 计算时间密度并显示初始体素
    dataManager.CalculateTemporalDensity();
    VisualizeDensity(0);

    CalculateAndVisualizeDensity();
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

public void CalculateAndVisualizeDensity()
{
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

    foreach (var frame in dataManager.frames)
    {
        foreach (var keypoint in frame.keypoints.Values)
        {
            Vector3 mappedPosition = MapToScene(keypoint);
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

    foreach (var frame in dataManager.frames)
    {
        foreach (var keypoint in frame.keypoints.Values)
        {
            Vector3 mappedPosition = MapToScene(keypoint);
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

        float logDensity = Mathf.Log10(density + 1);
        float normalizedDensity = (logDensity - logMinDensity) / (logMaxDensity - logMinDensity);

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
            Color color = Color.Lerp(Color.blue, Color.red, normalizedDensity);
        float enhancedDensity = Mathf.Pow(normalizedDensity, 0.7f); // Adjust exponent to control color spread

        if (enhancedDensity < 0.33f)
        {
            // Cool colors: deep blue to vibrant green
            color = Color.Lerp(Color.blue, Color.green, enhancedDensity * 3f);
            // Increase saturation for low-mid densities
            color = Color.Lerp(color, Color.green * 1.2f, 0.3f);
        }
        else if (enhancedDensity < 0.66f)
        {
            // Warm colors: green to saturated orange
            Color saturatedOrange = new Color(1f, 0.6f, 0f, 1f); // More vibrant orange
            color = Color.Lerp(Color.green, saturatedOrange, (enhancedDensity - 0.33f) * 3f);
            // Further increase saturation and brightness for mid densities
            color = Color.Lerp(color, saturatedOrange * 1.3f, 0.4f);
        }
        else
        {
            // Hot colors: orange to deep red
            Color saturatedOrange = new Color(1f, 0.6f, 0f, 1f);
            color = Color.Lerp(saturatedOrange, Color.red, (enhancedDensity - 0.66f) * 3f);
            // Increase intensity for high densities
            color = Color.Lerp(color, Color.red * 1.2f, 0.3f);
        }
        
        // Adjust transparency based on the enhanced density
        color.a = Mathf.Lerp(0.2f, 1f, enhancedDensity);


            color.a = Mathf.Lerp(0.2f, 1f, normalizedDensity);

            renderer.material.color = color;

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
}





void OnGUI()
{
    GUI.Label(new Rect(10, 10, 200, 20), $"Playback Speed: {playbackSpeed:F1}");
    GUI.Label(new Rect(10, 30, 200, 20), isPlaying ? "Status: Playing" : "Status: Paused");
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
            if (currentVoxelFrame >= dataManager.frames.Count)
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
    if (currentFrame < dataManager.frames.Count - 1)
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
