using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.XR;

public class EnhancedProfilerLogger : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Interval in seconds between data collection points.")]
    public float collectionInterval = 0.5f;

    private float timer = 0f;

    // Data structure to hold profiling logs
    private List<ProfilerData> profilerLogs = new List<ProfilerData>();

    // Store the start time
    private float startTime;

    void Start()
    {
        // Record the start time when the application starts
        startTime = Time.time;
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= collectionInterval)
        {
            CollectProfilerData();
            timer = 0f;
        }
    }

    private void CollectProfilerData()
    {
        ProfilerData data = new ProfilerData
        {
            frame = Time.frameCount,
            fps = 1.0f / Time.deltaTime,
            totalCpuMemory = Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f), // In MB
            reservedCpuMemory = Profiler.GetTotalReservedMemoryLong() / (1024f * 1024f), // In MB
            unusedReservedCpuMemory = Profiler.GetTotalUnusedReservedMemoryLong() / (1024f * 1024f), // In MB
            monoHeapSize = Profiler.GetMonoHeapSizeLong() / (1024f * 1024f), // In MB
            monoUsedSize = Profiler.GetMonoUsedSizeLong() / (1024f * 1024f), // In MB
            graphicsMemory = Profiler.GetAllocatedMemoryForGraphicsDriver() / (1024f * 1024f), // In MB
            gpuTime = XRStats.TryGetGPUTimeLastFrame(out float gpuTimeMs) ? gpuTimeMs : 0f,
            elapsedTime = Time.time - startTime // Calculate elapsed time
        };

        // Add more XR-specific stats (if using VR)
        if (XRStats.TryGetDroppedFrameCount(out int droppedFrames))
            data.droppedFrames = droppedFrames;

        profilerLogs.Add(data);

        Debug.Log($"Elapsed Time={data.elapsedTime}s Frame {data.frame}: FPS={data.fps}, CPU={data.totalCpuMemory}MB, Graphics Memory={data.graphicsMemory}MB, GPU={data.gpuTime}ms");
    }

    public void ExportToCSV(string fileName = "ProfilerLogs.csv")
    {
        string path = Path.Combine(Application.persistentDataPath, fileName);

        using (StreamWriter writer = new StreamWriter(path))
        {
            // Write CSV headers
            writer.WriteLine("Elapsed Time (s),Frame,FPS,Total CPU (MB),Reserved CPU (MB),Unused Reserved CPU (MB),Mono Heap (MB),Mono Used (MB),Graphics Memory (MB),GPU Time (ms),Dropped Frames");

            // Write log data
            foreach (var log in profilerLogs)
            {
                writer.WriteLine($"{log.elapsedTime}, {log.frame},{log.fps},{log.totalCpuMemory},{log.reservedCpuMemory},{log.unusedReservedCpuMemory},{log.monoHeapSize},{log.monoUsedSize},{log.graphicsMemory},{log.gpuTime},{log.droppedFrames}");
            }
        }

        Debug.Log($"Profiler logs exported to: {path}");
    }

    private void OnApplicationQuit()
    {
        // Automatically export logs on exit
        ExportToCSV();
    }

    // Profiler data structure
    private class ProfilerData
    {
        public float elapsedTime; // New field for elapsed time
        public int frame;
        public float fps;
        public float totalCpuMemory;
        public float reservedCpuMemory;
        public float unusedReservedCpuMemory;
        public float monoHeapSize;
        public float monoUsedSize;
        public float graphicsMemory;    
        public float gpuTime;
        public int droppedFrames;
    }
}