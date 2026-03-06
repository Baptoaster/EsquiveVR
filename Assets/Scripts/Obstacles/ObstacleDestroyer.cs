using UnityEngine;

public class ObstacleDestroyer : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if(other.tag == "Obstacle")
        {
            Destroy(other.gameObject);
        }
    }
}
