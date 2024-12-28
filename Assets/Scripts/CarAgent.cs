using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using System;

public class CarAgent : Agent
{
    public Rigidbody rb;
    public WaypointManager waypointManager; // Менеджер точек пути
    public Transform currentWaypoint; // Текущая цель
    public Transform nowWaypoint; // Текущая цель
    public int index_current;
    public Transform startPosition; // Стартовая позиция
    public float speed = 10f; // Скорость движения
    public float rotationSpeed = 200f; // Скорость вращения

    public Transform raycastOrigin; // Точка, откуда выпускаются лучи
    public float raycastLength = 40f; // Длина лучей

    // Углы для 5 лучей: вперед, влево, вправо, под углом влево, под углом вправо
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
        // Сбрасываем состояние машинки
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.position = nowWaypoint.position; // Начальная позиция машинки
        /*transform.rotation = Quaternion.identity;*/
        transform.rotation = Quaternion.LookRotation(currentWaypoint.position - nowWaypoint.position);

        // Устанавливаем начальную точку пути
        /*currentWaypoint = waypointManager.waypoints[0];*/

        waypointManager.StartEpisode(index_current);
    }

    private float CastRay(float angle)
    {
        // Вычисляем направление для луча с учетом угла
        Vector3 direction = Quaternion.Euler(0, angle, 0) * raycastOrigin.forward;

        // Выпускаем луч
        if (Physics.Raycast(raycastOrigin.position, direction, out RaycastHit hit, raycastLength))
        {
            /*Debug.DrawRay(raycastOrigin.position, direction * hit.distance, Color.red); // Визуализация*/
            return hit.distance / raycastLength; // Нормализованное расстояние (0.0 - 1.0)
        }
        else
        {
            /*Debug.DrawRay(raycastOrigin.position, direction * raycastLength, Color.green); // Визуализация*/
            return 1.0f; // Если ничего не найдено, расстояние равно максимальной длине луча
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Добавляем положение цели относительно машинки
        sensor.AddObservation(currentWaypoint.position - transform.position);

        // Добавляем направление машинки
        sensor.AddObservation(transform.eulerAngles.y / 360.0f);

        // Добавляем расстояние до ближайших препятствий
        // Для каждого угла выпускаем луч и добавляем его длину в нейросеть
        foreach (float angle in rayAngles)
        {
            float normalizedDistance = CastRay(angle);
            sensor.AddObservation(normalizedDistance);
        }

        // Добавляем направление к цели
        Vector3 directionToGoal = (currentWaypoint.position - transform.position).normalized;
        sensor.AddObservation(directionToGoal);

        // Добавляем угол до цели
        float angleToGoal = Vector3.SignedAngle(transform.forward, directionToGoal, Vector3.up);
        sensor.AddObservation(angleToGoal / 180.0f); // Нормализуем угол (-1, 1)
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // Действия от нейросети
        float moveForward = actions.ContinuousActions[0]; // Вперёд-назад
        float rotate = actions.ContinuousActions[1]; // Вращение

        /*// Применяем движения
        Vector3 forward = transform.forward * moveForward * speed * Time.deltaTime;
        rb.MovePosition(rb.position + forward);
        rb.MoveRotation(rb.rotation * Quaternion.Euler(Vector3.up * rotate * rotationSpeed * Time.deltaTime));*/


        // Управление вперед/назад
        float move = moveForward * speed * Time.deltaTime;
        transform.Translate(Vector3.forward * move);

        // Поворот
        float turn = rotate * rotationSpeed * Time.deltaTime;
        if (move < 0)
        {
            turn *= -1;
        }
        transform.Rotate(Vector3.up, turn);



        // Рассчитываем награду
        float distanceToWaypoint = Vector3.Distance(transform.position, currentWaypoint.position);
        
        // Награда за приближение к точке
        if (distanceToWaypoint < 2.0f)
        {
            AddReward(1.0f);
            nowWaypoint = currentWaypoint;
            index_current = (index_current + 1) % waypointManager.waypoints.Length;
            currentWaypoint = waypointManager.GetNextWaypoint(currentWaypoint); // Переход к следующей точке
        }
        else
        {
            AddReward(-0.01f * distanceToWaypoint); // Штраф за нахождение далеко от точки
        }

        // Штраф за выход за пределы трассы
        /*if (transform.localPosition.y < 0 || transform.localPosition.y > 5)
        {
            AddReward(-1.0f);
            EndEpisode();
        }*/
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = Input.GetAxis("Vertical"); // Вперёд-назад через клавиши W/S
        continuousActions[1] = Input.GetAxis("Horizontal"); // Повороты через клавиши A/D
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Штраф за столкновение
        if (collision.gameObject.CompareTag("Obstacle"))
        {
            AddReward(-1.0f);
            EndEpisode();
        }
    }
}
