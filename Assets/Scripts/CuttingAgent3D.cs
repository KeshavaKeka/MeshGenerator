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
    public float maxDistanceForNegativeReward = 1.0f; // Maximum distance for scaling negative reward

    [Header("Movement")]
    public float moveSpeed = 1f;

    private float episodeStartTime;
    private const float MIN_EPISODE_DURATION = 5f;
    private int episodeCount = 0;
    private int stepCount = 0;
    private Vector3 initialCuttingToolPosition;
    private Quaternion initialCuttingToolRotation;

    public override void Initialize()
    {
        base.Initialize();
        Debug.Log("CuttingAgent3D Initialized");

        if (cuttingTool == null || cutScript == null)
        {
            Debug.LogError("Cutting tool or Cut script reference is missing!");
        }

        initialCuttingToolPosition = cuttingTool.position;
        initialCuttingToolRotation = cuttingTool.rotation;

        cutScript.OnCutCompleted += OnCutCompleted;
    }

    public override void OnEpisodeBegin()
    {
        stepCount = 0;
        episodeCount++;
        episodeStartTime = Time.time;
        Debug.Log($"Episode {episodeCount} Begin - Time: {Time.time}");

        cuttingTool.position = initialCuttingToolPosition;
        cuttingTool.rotation = initialCuttingToolRotation;

        cutScript.ResetCut();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        Debug.Log($"Collecting Observations - Step: {stepCount}, Time: {Time.time}");

        sensor.AddObservation(cuttingTool.position);
        sensor.AddObservation(cuttingTool.rotation);
        sensor.AddObservation(cutScript.GetSelectedPoint1());
        sensor.AddObservation(cutScript.GetSelectedPoint2());

        Vector3 toPoint1 = (cutScript.GetSelectedPoint1() - cuttingTool.position).normalized;
        Vector3 toPoint2 = (cutScript.GetSelectedPoint2() - cuttingTool.position).normalized;
        sensor.AddObservation(toPoint1);
        sensor.AddObservation(toPoint2);

        Vector3 cutDirection = (cutScript.GetSelectedPoint2() - cutScript.GetSelectedPoint1()).normalized;
        sensor.AddObservation(Vector3.Dot(cuttingTool.forward, cutDirection));
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        stepCount++;
        Debug.Log($"Action Received - Step: {stepCount}, Time: {Time.time}");

        if (actions.ContinuousActions.Length >= 3)
        {
            float moveX = actions.ContinuousActions[0];
            float moveY = actions.ContinuousActions[1];
            float moveZ = actions.ContinuousActions[2];
            Vector3 movement = new Vector3(moveX, moveY, moveZ) * moveSpeed * Time.fixedDeltaTime;

            cuttingTool.Translate(movement, Space.World);

            Vector3 point1 = cutScript.GetSelectedPoint1();
            Vector3 point2 = cutScript.GetSelectedPoint2();

            float distanceToLine = PointLineDistance(cuttingTool.position, point1, point2);
            if (distanceToLine < 0.5f)
            {
                AddReward(rewardForMoveTowardsPoint * Time.fixedDeltaTime);
                Debug.Log($"Reward added for moving towards line: {rewardForMoveTowardsPoint * Time.fixedDeltaTime}");
            }

            AddReward(-0.001f * Time.fixedDeltaTime);
        }
        else
        {
            Debug.LogError($"Not enough continuous actions received. Expected at least 3, but received {actions.ContinuousActions.Length}.");
        }

        if (StepCount >= maxStep)
        {
            AddReward(negativeRewardForFailure);
            Debug.Log($"Episode ended due to max steps reached. Negative reward added: {negativeRewardForFailure}");
            EndEpisode();
        }
    }

    private float PointLineDistance(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
    {
        return Vector3.Cross(lineEnd - lineStart, point - lineStart).magnitude / (lineEnd - lineStart).magnitude;
    }

    private void OnCutCompleted()
    {
        Debug.Log($"Cut Completed - Step: {stepCount}, Time: {Time.time}");
        if (CanEndEpisode())
        {
            Vector3 point1 = cutScript.GetSelectedPoint1();
            Vector3 point2 = cutScript.GetSelectedPoint2();
            float distanceToLine = PointLineDistance(cuttingTool.position, point1, point2);

            if (distanceToLine < 0.5f)
            {
                Debug.Log("Ending Episode due to successful cut");
                AddReward(rewardForSuccessfulCut);
            }
            else
            {
                float normalizedDistance = Mathf.Clamp01(distanceToLine / maxDistanceForNegativeReward);
                float scaledNegativeReward = negativeRewardForIncorrectCut * normalizedDistance;
                Debug.Log($"Ending Episode due to incorrect cut. Distance: {distanceToLine}, Negative Reward: {scaledNegativeReward}");
                AddReward(scaledNegativeReward);
            }
            EndEpisode();
        }
    }

    private bool CanEndEpisode()
    {
        bool canEnd = Time.time - episodeStartTime >= MIN_EPISODE_DURATION;
        Debug.Log($"CanEndEpisode called. Result: {canEnd}, Time since start: {Time.time - episodeStartTime}, MIN_DURATION: {MIN_EPISODE_DURATION}");
        return canEnd;
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActions = actionsOut.ContinuousActions;

        float moveZ = 0f;
        if (Input.GetKey(KeyCode.W)) moveZ += 1f;
        if (Input.GetKey(KeyCode.S)) moveZ -= 1f;

        float moveX = 0f;
        if (Input.GetKey(KeyCode.D)) moveX += 1f;
        if (Input.GetKey(KeyCode.A)) moveX -= 1f;

        float moveY = 0f;
        if (Input.GetKey(KeyCode.Q)) moveY += 1f;
        if (Input.GetKey(KeyCode.E)) moveY -= 1f;

        continuousActions[0] = moveX;
        continuousActions[1] = moveY;
        continuousActions[2] = moveZ;

        Debug.Log($"Heuristic Input: X: {moveX}, Y: {moveY}, Z: {moveZ}");

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Manual cut triggered");
            cutScript.ForceCutBetweenPoints();
        }
    }

    private void OnDestroy()
    {
        if (cutScript != null)
        {
            cutScript.OnCutCompleted -= OnCutCompleted;
        }
    }
}