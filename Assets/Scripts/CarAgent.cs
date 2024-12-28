using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using System;

public class CarAgent : Agent
{
    public Rigidbody rb;
    public WaypointManager waypointManager; // �������� ����� ����
    public Transform currentWaypoint; // ������� ����
    public Transform nowWaypoint; // ������� ����
    public int index_current;
    public Transform startPosition; // ��������� �������
    public float speed = 10f; // �������� ��������
    public float rotationSpeed = 200f; // �������� ��������

    public Transform raycastOrigin; // �����, ������ ����������� ����
    public float raycastLength = 40f; // ����� �����

    // ���� ��� 5 �����: ������, �����, ������, ��� ����� �����, ��� ����� ������
    private readonly float[] rayAngles = { 0f, -45f, 45f, -90f, 90f, -135f, 135f, -180 };

    private void Start()
    {
        currentWaypoint = waypointManager.waypoints[0];
        index_current = waypointManager.waypoints.Length;
        transform.position = startPosition.position;
        nowWaypoint = startPosition;
    }

    public override void OnEpisodeBegin()
    {
        // ���������� ��������� �������
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.position = nowWaypoint.position; // ��������� ������� �������
        /*transform.rotation = Quaternion.identity;*/
        transform.rotation = Quaternion.LookRotation(currentWaypoint.position - nowWaypoint.position);

        // ������������� ��������� ����� ����
        /*currentWaypoint = waypointManager.waypoints[0];*/

        waypointManager.StartEpisode(index_current);
    }

    private float CastRay(float angle)
    {
        // ��������� ����������� ��� ���� � ������ ����
        Vector3 direction = Quaternion.Euler(0, angle, 0) * raycastOrigin.forward;

        // ��������� ���
        if (Physics.Raycast(raycastOrigin.position, direction, out RaycastHit hit, raycastLength))
        {
            /*Debug.DrawRay(raycastOrigin.position, direction * hit.distance, Color.red); // ������������*/
            return hit.distance / raycastLength; // ��������������� ���������� (0.0 - 1.0)
        }
        else
        {
            /*Debug.DrawRay(raycastOrigin.position, direction * raycastLength, Color.green); // ������������*/
            return 1.0f; // ���� ������ �� �������, ���������� ����� ������������ ����� ����
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // ��������� ��������� ���� ������������ �������
        sensor.AddObservation(currentWaypoint.position - transform.position);

        // ��������� ����������� �������
        sensor.AddObservation(transform.eulerAngles.y / 360.0f);

        // ��������� ���������� �� ��������� �����������
        // ��� ������� ���� ��������� ��� � ��������� ��� ����� � ���������
        foreach (float angle in rayAngles)
        {
            float normalizedDistance = CastRay(angle);
            sensor.AddObservation(normalizedDistance);
        }

        // ��������� ����������� � ����
        Vector3 directionToGoal = (currentWaypoint.position - transform.position).normalized;
        sensor.AddObservation(directionToGoal);

        // ��������� ���� �� ����
        float angleToGoal = Vector3.SignedAngle(transform.forward, directionToGoal, Vector3.up);
        sensor.AddObservation(angleToGoal / 180.0f); // ����������� ���� (-1, 1)
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // �������� �� ���������
        float moveForward = actions.ContinuousActions[0]; // �����-�����
        float rotate = actions.ContinuousActions[1]; // ��������

        /*// ��������� ��������
        Vector3 forward = transform.forward * moveForward * speed * Time.deltaTime;
        rb.MovePosition(rb.position + forward);
        rb.MoveRotation(rb.rotation * Quaternion.Euler(Vector3.up * rotate * rotationSpeed * Time.deltaTime));*/


        // ���������� ������/�����
        float move = moveForward * speed * Time.deltaTime;
        transform.Translate(Vector3.forward * move);

        // �������
        float turn = rotate * rotationSpeed * Time.deltaTime;
        if (move < 0)
        {
            turn *= -1;
        }
        transform.Rotate(Vector3.up, turn);



        // ������������ �������
        float distanceToWaypoint = Vector3.Distance(transform.position, currentWaypoint.position);
        
        // ������� �� ����������� � �����
        if (distanceToWaypoint < 2.0f)
        {
            AddReward(1.0f);
            nowWaypoint = currentWaypoint;
            index_current = (index_current + 1) % waypointManager.waypoints.Length;
            currentWaypoint = waypointManager.GetNextWaypoint(currentWaypoint); // ������� � ��������� �����
        }
        else
        {
            AddReward(-0.01f * distanceToWaypoint); // ����� �� ���������� ������ �� �����
        }

        // ����� �� ����� �� ������� ������
        /*if (transform.localPosition.y < 0 || transform.localPosition.y > 5)
        {
            AddReward(-1.0f);
            EndEpisode();
        }*/
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = Input.GetAxis("Vertical"); // �����-����� ����� ������� W/S
        continuousActions[1] = Input.GetAxis("Horizontal"); // �������� ����� ������� A/D
    }

    private void OnCollisionEnter(Collision collision)
    {
        // ����� �� ������������
        if (collision.gameObject.CompareTag("Obstacle"))
        {
            AddReward(-1.0f);
            EndEpisode();
        }
    }
}
