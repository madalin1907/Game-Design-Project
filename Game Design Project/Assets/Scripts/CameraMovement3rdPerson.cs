using UnityEngine;

public class CameraMovement3rdPerson : MonoBehaviour
{
    private float _height = 4.0f;
    private float _distance = -7.0f;
    private float _angleRotation = 15.0f;

    [SerializeField]
    private GameObject player;

    void Update()
    {
        var playerPosition = player.transform.position;
        transform.position = new Vector3(playerPosition.x, playerPosition.y + _height, playerPosition.z + _distance);
        transform.rotation = Quaternion.Euler(_angleRotation, 0, 0);
    }
}
