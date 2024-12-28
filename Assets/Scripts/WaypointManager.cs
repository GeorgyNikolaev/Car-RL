using UnityEngine;

public class WaypointManager : MonoBehaviour
{
    public Transform[] waypoints; // ������ ����������� �����
    public Material next_material;
    public Material prev_material;

    public Transform GetNextWaypoint(Transform currentWaypoint)
    {
        int index = System.Array.IndexOf(waypoints, currentWaypoint);
        if (index == -1 || waypoints.Length == 0) return null;

        // ��������� � ��������� ����� (��������� ����)
        waypoints[(index + 1) % waypoints.Length].GetComponent<MeshRenderer>().material = next_material;
        currentWaypoint.GetComponent<MeshRenderer>().material = prev_material;
        return waypoints[(index + 1) % waypoints.Length];
    }

    public void StartEpisode(int index)
    {
        foreach (Transform point in waypoints)
        {
            point.GetComponent<MeshRenderer>().material = prev_material;
        }
        waypoints[index % waypoints.Length].GetComponent<MeshRenderer>().material = next_material;
    }
}
