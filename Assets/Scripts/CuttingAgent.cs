using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using System.Collections;
using System.Collections.Generic;

public class CuttingAgent : Agent
{
    private float episodeStartTime;
    private const float MIN_EPISODE_DURATION = 5f; // Adjust as needed

    private const float MAX_EPISODE_DURATION = 60f; // 1 minute, adjust as needed

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

    private int episodeCount = 0;

    private List<Vector3> meshVertices = new List<Vector3>();

    private Vector3 initialXRRigPosition;
    private Quaternion initialXRRigRotation;
    private Vector3 initialSwordPosition;
    private Quaternion initialSwordRotation;

    void Start()
    {
        initialXRRigPosition = XR_Rig.position;
        initialXRRigRotation = XR_Rig.rotation;
        initialSwordPosition = sword.position;
        initialSwordRotation = sword.rotation;
    }


    public override void Initialize()
    {
        Debug.Log("CuttingAgent Initialize called");
        StartCoroutine(WaitForMeshAndInitialize());
    }

    private IEnumerator WaitForMeshAndInitialize()
    {
        Debug.Log("WaitForMeshAndInitialize started");
        while (remTriScript == null || remTriScript.vertices == null || remTriScript.vertices.Count == 0)
        {
            Debug.Log($"Waiting for mesh. remTriScript: {remTriScript != null}, vertices: {remTriScript?.vertices != null}, vertex count: {remTriScript?.vertices?.Count ?? 0}");
            yield return null;
        }
        Debug.Log("Mesh initialized, updating observations");
        UpdateObservations();
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        Debug.Log($"OnActionReceived - Continuous actions: {actions.ContinuousActions[0]}, {actions.ContinuousActions[1]}");

        if (episodeEnding) return;

        if (actions.ContinuousActions.Length >= 2)
        {
            float moveX = actions.ContinuousActions[0];
            float moveY = actions.ContinuousActions[1];
            Vector3 movement = new Vector3(moveX, moveY, 0) * moveSpeed;

            sword.Translate(movement * Time.deltaTime, Space.World);
            Debug.DrawRay(sword.position, movement, Color.red, 0.1f);

            if (IsInCuttingRange(sword.position))
            {
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

                if (remTriScript.HasCutBeenMadeBetweenPoints() && CanEndEpisode())
                {
                    AddReward(rewardForSuccessfulCut);
                    EndEpisode();
                }
            }
        }
        else
        {
            Debug.LogError($"Not enough continuous actions received. Expected at least 2, but received {actions.ContinuousActions.Length}.");
        }
    }





    private bool CanEndEpisode()
    {
        return Time.time - episodeStartTime >= MIN_EPISODE_DURATION;
    }

    private void Update()
    {
        Debug.Log($"Episode {episodeCount}, Step: {StepCount}, Time: {Time.time}, Sword Position: {sword.position}");

        if (!episodeEnding && remTriScript.HasCutBeenMadeBetweenPoints())
        {
            episodeEnding = true;
            AddReward(rewardForSuccessfulCut);
            Debug.Log("Cut made between points. Attempting to end episode.");
            Academy.Instance.StatsRecorder.Add("EpisodeComplete", 1, StatAggregationMethod.Sum);
            EndEpisode();
        }

        if (Time.time - episodeStartTime > MAX_EPISODE_DURATION)
        {
            Debug.Log("Maximum episode duration reached. Ending episode.");
            EndEpisode();
        }
    }



    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(XR_Rig.localPosition);  // 3 floats
        sensor.AddObservation(XR_Rig.localRotation);  // 4 floats
        sensor.AddObservation(sword.localPosition);   // 3 floats
        sensor.AddObservation(sword.localRotation);   // 4 floats
        sensor.AddObservation(point1);                // 3 floats
        sensor.AddObservation(point2);                // 3 floats

        // Total so far: 20 floats

        // Be careful with this part, it could add a lot of observations
        int vertexStep = Mathf.Max(1, meshVertices.Count / maxObservedVertices);
        for (int i = 0; i < Mathf.Min(maxObservedVertices, meshVertices.Count); i += vertexStep)
        {
            sensor.AddObservation(meshVertices[i]);  // 3 floats per vertex
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
        float cuttingRangeThreshold = 0.5f; // Adjust as needed
        Debug.DrawLine(point1, point2, Color.blue);
        Debug.DrawLine(swordPosition, closestPoint, Color.yellow);
        return distance <= cuttingRangeThreshold;
    }


    public override void OnEpisodeBegin()
    {
        Debug.Log("OnEpisodeBegin called.");
        StartCoroutine(InitializeEpisode());
        episodeStartTime = Time.time;

        Debug.Log("OnEpisodeBegin called. Resetting scene...");

        // Reset the XR Rig position and rotation
        XR_Rig.position = new Vector3(0, 1, -5); // Adjust as needed
        XR_Rig.rotation = Quaternion.identity;

        // Reset the sword position and rotation relative to the XR Rig
        sword.position = XR_Rig.position + XR_Rig.forward + Vector3.up * 0.5f;
        sword.rotation = Quaternion.identity;

        // Reset the mesh and re-select the points
        remTriScript.ResetMesh();

        episodeEnding = false;
        UpdateObservations();

        Debug.Log($"Scene reset. XR_Rig: {XR_Rig.position}, Sword: {sword.position}");

        episodeCount++;
        Debug.Log($"Episode {episodeCount} started. Time: {Time.time}");
    }






    private IEnumerator InitializeEpisode()
    {
        while (remTriScript == null || remTriScript.vertices == null || remTriScript.vertices.Count == 0)
        {
            Debug.Log("Waiting for mesh to initialize...");
            yield return null;
        }

        Debug.Log($"Initializing episode - Before: XR_Rig: {XR_Rig.position}, Sword: {sword.position}, Mesh: {remTriScript.transform.position}");

        episodeEnding = false;

        // Instead of setting to zero, move slightly in front of the XR_Rig
        XR_Rig.localPosition = new Vector3(0, 0, -5);  // Adjust as needed
        XR_Rig.localRotation = Quaternion.identity;
        sword.localPosition = XR_Rig.localPosition + XR_Rig.forward + Vector3.up * 0.5f;
        sword.localRotation = Quaternion.identity;

        meshVertices = new List<Vector3>(remTriScript.vertices);
        point1 = remTriScript.GetSelectedPoint1();
        point2 = remTriScript.GetSelectedPoint2();

        Debug.Log($"Episode initialized - After: XR_Rig: {XR_Rig.position}, Sword: {sword.position}, Mesh: {remTriScript.transform.position}");
        Debug.Log($"Points: Point1: {point1}, Point2: {point2}");
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