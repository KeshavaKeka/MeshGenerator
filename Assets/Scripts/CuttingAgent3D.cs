using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using System.Collections;

public class CuttingAgent3D : Agent
{
    [Header("References")]
    public Transform cuttingTool; // Reference to the sword GameObject
    public Cut cutScript; // Reference to the Cut script

    [Header("Training Settings")]
    public bool isTraining = true;
    public float autoResetDelay = 1f;

    [Header("Rewards")]
    public float rewardForMoveTowardsPoint = 0.2f;
    public float rewardForSuccessfulCut = 1.0f;
    public float negativeRewardForFailure = -0.5f;

    [Header("Movement")]
    public float moveSpeed = 1f;

    private float episodeStartTime;
    private const float MIN_EPISODE_DURATION = 5f;
    private const float MAX_EPISODE_DURATION = 60f;

    private bool episodeEnding = false;
    private int episodeCount = 0;

    private Vector3 initialCuttingToolPosition;
    private Quaternion initialCuttingToolRotation;

    private void Start()
    {
        if (cuttingTool == null)
        {
            Debug.LogError("Cutting tool (sword) reference is missing!");
        }
        if (cutScript == null)
        {
            Debug.LogError("Cut script reference is missing!");
        }

        initialCuttingToolPosition = cuttingTool.position;
        initialCuttingToolRotation = cuttingTool.rotation;
    }

    public override void Initialize()
    {
        Debug.Log("CuttingAgent3D Initialize called");
    }

    public override void OnEpisodeBegin()
    {
        Debug.Log("OnEpisodeBegin called. Resetting scene...");

        cuttingTool.position = initialCuttingToolPosition;
        cuttingTool.rotation = initialCuttingToolRotation;

        cutScript.ResetCut();

        episodeStartTime = Time.time;
        episodeEnding = false;

        episodeCount++;
        Debug.Log($"Episode {episodeCount} started. Time: {Time.time}");
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(cuttingTool.position);
        sensor.AddObservation(cuttingTool.rotation);
        sensor.AddObservation(cutScript.GetSelectedPoint1());
        sensor.AddObservation(cutScript.GetSelectedPoint2());

        // Add relative position to both selected points
        sensor.AddObservation(cuttingTool.position - cutScript.GetSelectedPoint1());
        sensor.AddObservation(cuttingTool.position - cutScript.GetSelectedPoint2());
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (episodeEnding) return;

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
            }

            if (cutScript.HasStraightLineCutBeenMade() && CanEndEpisode())
            {
                AddReward(rewardForSuccessfulCut);
                EndEpisode();
                Debug.Log("Episode ended: Successful straight line cut");
            }
        }
        else
        {
            Debug.LogError($"Not enough continuous actions received. Expected at least 3, but received {actions.ContinuousActions.Length}.");
        }
    }

    private bool CanEndEpisode()
    {
        return Time.time - episodeStartTime >= MIN_EPISODE_DURATION;
    }

    private void Update()
    {
        if (!episodeEnding && Time.time - episodeStartTime > MAX_EPISODE_DURATION)
        {
            EndEpisode();
            Debug.Log("Episode ended: Maximum duration reached");
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = Input.GetAxis("Horizontal");
        continuousActions[1] = Input.GetAxis("Vertical");
        continuousActions[2] = Input.GetKey(KeyCode.Q) ? -1f : Input.GetKey(KeyCode.E) ? 1f : 0f;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Manual cut triggered");
            cutScript.ForceCutBetweenPoints();
        }
    }
}