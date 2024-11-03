using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using System.Collections;

public class CuttingAgent3D : Agent
{
    [Header("References")]
    public Transform cuttingTool;
    public Cut cutScript;

    [Header("Training Settings")]
    public bool isTraining = true;
    public int maxStep = 3000;

    [Header("Rewards")]
    public float rewardForMoveTowardsPoint = 0.2f;
    public float rewardForSuccessfulCut = 1.0f;
    public float negativeRewardForFailure = -0.2f;
    public float negativeRewardForIncorrectCut = -0.3f;
    public float maxDistanceForNegativeReward = 1.0f;

    [Header("Movement")]
    public float moveSpeed = 1f;

    private float episodeStartTime;
    private const float MIN_EPISODE_DURATION = 5f;
    private int episodeCount = 0;
    private int stepCount = 0;
    private Vector3 initialCuttingToolPosition;
    private Quaternion initialCuttingToolRotation;
    private bool isInitialized = false;

    public override void Initialize()
    {
        base.Initialize();
        
        // Validate required components
        if (cuttingTool == null)
        {
            Debug.LogError("CuttingAgent3D: Cutting tool reference is missing!");
            return;
        }
        if (cutScript == null) 
        {
            Debug.LogError("CuttingAgent3D: Cut script reference is missing!");
            return;
        }

        // Cache initial transform values
        initialCuttingToolPosition = cuttingTool.position;
        initialCuttingToolRotation = cuttingTool.rotation;

        // Set up event listeners
        cutScript.OnCutCompleted += OnCutCompleted;
        
        isInitialized = true;
        Debug.Log("CuttingAgent3D successfully initialized");
    }

    public override void OnEpisodeBegin()
    {
        if (!isInitialized)
        {
            Debug.LogError("CuttingAgent3D: Attempting to start episode before initialization!");
            return;
        }

        // Reset counters
        stepCount = 0;
        episodeCount++;
        episodeStartTime = Time.time;

        // Reset transforms
        cuttingTool.position = initialCuttingToolPosition;
        cuttingTool.rotation = initialCuttingToolRotation;

        // Reset cut state
        cutScript.ResetCut();

        Debug.Log($"Episode {episodeCount} started at time {Time.time}");
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (!isInitialized) return;

        // Tool state observations
        sensor.AddObservation(cuttingTool.position);
        sensor.AddObservation(cuttingTool.rotation);

        // Target points
        Vector3 point1 = cutScript.GetSelectedPoint1();
        Vector3 point2 = cutScript.GetSelectedPoint2();
        sensor.AddObservation(point1);
        sensor.AddObservation(point2);

        // Directional information
        Vector3 toPoint1 = (point1 - cuttingTool.position).normalized;
        Vector3 toPoint2 = (point2 - cuttingTool.position).normalized;
        sensor.AddObservation(toPoint1);
        sensor.AddObservation(toPoint2);

        // Alignment with cut direction
        Vector3 cutDirection = (point2 - point1).normalized;
        float alignment = Vector3.Dot(cuttingTool.forward, cutDirection);
        sensor.AddObservation(alignment);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (!isInitialized) return;

        stepCount++;

        if (actions.ContinuousActions.Length < 3)
        {
            Debug.LogError($"Invalid action space: expected 3 continuous actions, got {actions.ContinuousActions.Length}");
            return;
        }

        // Process movement
        Vector3 movement = new Vector3(
            actions.ContinuousActions[0],
            actions.ContinuousActions[1],
            actions.ContinuousActions[2]
        ) * moveSpeed * Time.fixedDeltaTime;

        cuttingTool.Translate(movement, Space.World);

        // Calculate rewards
        Vector3 point1 = cutScript.GetSelectedPoint1();
        Vector3 point2 = cutScript.GetSelectedPoint2();
        float distanceToLine = PointLineDistance(cuttingTool.position, point1, point2);

        // Reward for staying close to cutting line
        if (distanceToLine < 0.5f)
        {
            float proximityReward = rewardForMoveTowardsPoint * Time.fixedDeltaTime;
            AddReward(proximityReward);
        }

        // Small negative reward to encourage efficiency
        AddReward(-0.001f * Time.fixedDeltaTime);

        // Check for episode timeout
        if (StepCount >= maxStep)
        {
            AddReward(negativeRewardForFailure);
            EndEpisode();
        }
    }

    private float PointLineDistance(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
    {
        return Vector3.Cross(lineEnd - lineStart, point - lineStart).magnitude / (lineEnd - lineStart).magnitude;
    }

    private void OnCutCompleted()
    {
        if (!isInitialized || !CanEndEpisode()) return;

        Vector3 point1 = cutScript.GetSelectedPoint1();
        Vector3 point2 = cutScript.GetSelectedPoint2();
        float distanceToLine = PointLineDistance(cuttingTool.position, point1, point2);

        if (distanceToLine < 0.5f)
        {
            AddReward(rewardForSuccessfulCut);
            Debug.Log($"Successful cut completed! Reward: {rewardForSuccessfulCut}");
        }
        else
        {
            float normalizedDistance = Mathf.Clamp01(distanceToLine / maxDistanceForNegativeReward);
            float penalty = negativeRewardForIncorrectCut * normalizedDistance;
            AddReward(penalty);
            Debug.Log($"Inaccurate cut. Distance: {distanceToLine:F2}, Penalty: {penalty:F2}");
            OnEpisodeBegin();
        }

        EndEpisode();
    }

    private bool CanEndEpisode()
    {
        float episodeDuration = Time.time - episodeStartTime;
        bool canEnd = episodeDuration >= MIN_EPISODE_DURATION;
        
        if (!canEnd)
        {
            Debug.Log($"Cannot end episode yet. Current duration: {episodeDuration:F2}s");
        }
        
        return canEnd;
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        if (!isInitialized) return;

        var continuousActions = actionsOut.ContinuousActions;

        // Process keyboard input for movement
        float moveX = Input.GetKey(KeyCode.D) ? 1f : Input.GetKey(KeyCode.A) ? -1f : 0f;
        float moveY = Input.GetKey(KeyCode.Q) ? 1f : Input.GetKey(KeyCode.E) ? -1f : 0f;
        float moveZ = Input.GetKey(KeyCode.W) ? 1f : Input.GetKey(KeyCode.S) ? -1f : 0f;

        continuousActions[0] = moveX;
        continuousActions[1] = moveY;
        continuousActions[2] = moveZ;

        // Manual cut trigger
        if (Input.GetKeyDown(KeyCode.Space))
        {
            cutScript.ForceCutBetweenPoints();
            Debug.Log("Manual cut triggered");
        }
    }

    private void OnDestroy()
    {
        if (cutScript != null && isInitialized)
        {
            cutScript.OnCutCompleted -= OnCutCompleted;
        }
    }
}