using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using System.Collections;
using System.Collections.Generic;

public class CuttingAgent : Agent
{
    public Transform XR_Rig;
    public Transform sword;
    public RemTri remTriScript;

    public Vector3 point1;
    public Vector3 point2;

    public float rewardForPickup = 0.5f;
    public float rewardForMoveTowardsPoint = 0.2f;
    public float rewardForSuccessfulCut = 1.0f;
    public float negativeRewardForFailure = -0.5f;
    public float moveSpeed = 1f;
    public int maxObservedVertices = 100;

    private bool episodeEnding = false;

    private List<Vector3> meshVertices = new List<Vector3>();

    public override void Initialize()
    {
        StartCoroutine(WaitForMeshAndInitialize());
    }

    private IEnumerator WaitForMeshAndInitialize()
    {
        while (remTriScript == null || remTriScript.vertices == null || remTriScript.vertices.Count == 0)
        {
            yield return null;
        }
        UpdateObservations();
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (episodeEnding) return;

        if (actions.ContinuousActions.Length >= 2)
        {
            float moveX = actions.ContinuousActions[0];
            float moveY = actions.ContinuousActions[1];
            Vector3 movement = new Vector3(moveX, moveY, 0) * moveSpeed;

            sword.Translate(movement * Time.deltaTime, Space.World);
            Debug.DrawRay(sword.position, movement, Color.red, 0.1f);
            float distanceToPoint1 = Vector3.Distance(sword.position, point1);
            float distanceToPoint2 = Vector3.Distance(sword.position, point2);

            if (distanceToPoint1 < 0.5f || distanceToPoint2 < 0.5f)
            {
                AddReward(rewardForMoveTowardsPoint);
            }
            else
            {
                AddReward(-rewardForMoveTowardsPoint);
            }

            if (IsInCuttingRange(sword.position))
            {
                AddReward(rewardForSuccessfulCut);
                EndEpisode();
            }
            else
            {
                AddReward(negativeRewardForFailure);
            }
        }
        else
        {
            Debug.LogError($"Not enough continuous actions received. Expected at least 2, but received {actions.ContinuousActions.Length}.");
        }

        bool cutMade = remTriScript.HasCutBeenMadeBetweenPoints();

        Debug.Log($"Cut made between points: {cutMade}");
        if (cutMade)
        {
            episodeEnding = true;
            AddReward(rewardForSuccessfulCut);
            Debug.Log("Cut made between points. Attempting to end episode.");
            Academy.Instance.StatsRecorder.Add("EpisodeComplete", 1, StatAggregationMethod.Sum);
            EpisodeInterrupted();
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(XR_Rig.localPosition);
        sensor.AddObservation(XR_Rig.localRotation);
        sensor.AddObservation(sword.localPosition);
        sensor.AddObservation(sword.localRotation);

        sensor.AddObservation(point1);
        sensor.AddObservation(point2);

        int vertexStep = Mathf.Max(1, meshVertices.Count / maxObservedVertices);
        for (int i = 0; i < meshVertices.Count; i += vertexStep)
        {
            sensor.AddObservation(meshVertices[i]);
        }
    }

    private bool IsInCuttingRange(Vector3 swordPosition)
    {
        Vector3 lineDirection = point2 - point1;
        Vector3 toSword = swordPosition - point1;
        float projection = Vector3.Dot(toSword, lineDirection.normalized);
        projection = Mathf.Clamp(projection, 0f, lineDirection.magnitude);
        Vector3 closestPoint = point1 + lineDirection.normalized * projection;
        float distance = Vector3.Distance(swordPosition, closestPoint);
        float cuttingRangeThreshold = 0.5f;
        Debug.DrawLine(point1, point2, Color.blue);
        Debug.DrawLine(swordPosition, closestPoint, Color.yellow);
        return distance <= cuttingRangeThreshold;
    }

    public override void OnEpisodeBegin()
    {
        episodeEnding = false;

        XR_Rig.localPosition = Vector3.zero;
        XR_Rig.localRotation = Quaternion.identity;
        sword.localPosition = Vector3.zero;
        sword.localRotation = Quaternion.identity;

        meshVertices.Clear();

        // Update these lines
        point1 = remTriScript.GetSelectedPoint1();
        point2 = remTriScript.GetSelectedPoint2();

        UpdateObservations();
    }

    private void UpdateObservations()
    {
        if (remTriScript != null)
        {
            meshVertices = remTriScript.vertices;
        }
        else
        {
            Debug.LogWarning("RemTri script reference not set.");
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = Input.GetAxis("Horizontal");
        continuousActions[1] = Input.GetAxis("Vertical");

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Manual cut triggered");
            remTriScript.ForceCutBetweenPoints(); // Add this method to RemTri
        }
    }
}