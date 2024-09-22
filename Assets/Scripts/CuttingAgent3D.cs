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
    public float negativeRewardForFailure = -0.5f;

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

        sensor.AddObservation(cuttingTool.position - cutScript.GetSelectedPoint1());
        sensor.AddObservation(cuttingTool.position - cutScript.GetSelectedPoint2());
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
            Vector3 movement = new Vector3(moveX, moveY, moveZ) * moveSpeed * Time.deltaTime;

            cuttingTool.Translate(movement, Space.World);

            Vector3 point1 = cutScript.GetSelectedPoint1();
            Vector3 point2 = cutScript.GetSelectedPoint2();

            if (Vector3.Distance(cuttingTool.position, point1) < 0.5f || Vector3.Distance(cuttingTool.position, point2) < 0.5f)
            {
                AddReward(rewardForMoveTowardsPoint);
                Debug.Log($"Reward added for moving towards point: {rewardForMoveTowardsPoint}");
            }
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

    private void OnCutCompleted()
    {
        Debug.Log($"Cut Completed - Step: {stepCount}, Time: {Time.time}");
        if (CanEndEpisode())
        {
            Debug.Log("Ending Episode due to cut completion");
            AddReward(rewardForSuccessfulCut);
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

        // Forward/Backward movement (Z-axis)
        float moveZ = 0f;
        if (Input.GetKey(KeyCode.W)) moveZ += 1f;
        if (Input.GetKey(KeyCode.S)) moveZ -= 1f;

        // Left/Right movement (X-axis)
        float moveX = 0f;
        if (Input.GetKey(KeyCode.D)) moveX += 1f;
        if (Input.GetKey(KeyCode.A)) moveX -= 1f;

        // Up/Down movement (Y-axis)
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