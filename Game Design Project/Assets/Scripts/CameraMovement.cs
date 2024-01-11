using UnityEngine;
using UnityEngine.InputSystem;


public class CameraMovement : MonoBehaviour
{
    private float _mouseX;
    private float _mouseY;

    [SerializeField]
    private enum View { FIRST_PERSON, THIRD_PERSON };

    [SerializeField]
    private View view = View.THIRD_PERSON;

    [SerializeField]
    private GameObject player;

    [SerializeField]
    private Transform firstPersonCameraTransform;

    [SerializeField]
    private float _sensitivity = 40.0f;

    [SerializeField]
    private float _distance = 7.0f; // Adjust the distance to your preference

    [SerializeField]
    private InventoryMechanism inventoryMechanism;

    private float _currentRotationY = 0.0f;
    private float _currentRotationX = 0.0f;

    private float _waitingTimeBeforeRotationCamera = 0.2f;

    public void Rotate(InputAction.CallbackContext context)
    {
        if (inventoryMechanism.IsInventoryOpen()) {
            _mouseX = 0f;
            _mouseY = 0f;
            return;
        }
        
        var mouseCoords = context.ReadValue<Vector2>();
        _mouseX = mouseCoords.x;
        _mouseY = mouseCoords.y;
    }

    public void Switch(InputAction.CallbackContext context)
    {
        if (inventoryMechanism.IsInventoryOpen())
            return;

        if (context.started)
        {
            if (view == View.FIRST_PERSON)
            {
                view = View.THIRD_PERSON;
                Debug.Log("Switch to third person camera.");
            }
            else
            {
                view = View.FIRST_PERSON;
                Debug.Log("Switch to first person camera.");
            }
        }
    }

    void Awake()
    {
        _currentRotationX = transform.rotation.eulerAngles.x;
        _currentRotationY = transform.rotation.eulerAngles.y;
    }

    void Update()
    {
        if (_waitingTimeBeforeRotationCamera > 0f)
            _waitingTimeBeforeRotationCamera -= Time.deltaTime;
    }

    void LateUpdate()
    {
        if (_waitingTimeBeforeRotationCamera > 0f)
            return;

        if (view == View.THIRD_PERSON)
            LateUpdateThirdPerson();
        else
            LateUpdateFirstPerson();
    }

    void LateUpdateThirdPerson()
    {
        // Calculate the current rotation based on the mouse input
        _currentRotationY += _mouseX * _sensitivity * Time.deltaTime;
        _currentRotationX -= _mouseY * _sensitivity * Time.deltaTime;

        // Limit the vertical rotation to keep the camera from flipping
        _currentRotationX = Mathf.Clamp(_currentRotationX, 180f, 260f);

        // Calculate the new camera position based on the current rotation
        Vector3 offset = new Vector3(0, 0, -_distance);
        Quaternion rotation = Quaternion.Euler(_currentRotationX, _currentRotationY, 0);
        Vector3 desiredPosition = player.transform.position + rotation * offset;

        // Smoothly move the camera to the desired position
        transform.position = Vector3.Lerp(transform.position, desiredPosition, 0.2f);

        // Make the player rotate with the camera
        player.transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);

        // Make the camera look at the player
        transform.LookAt(player.transform.position);
    }

    void LateUpdateFirstPerson()
    {
        // Make the camera follow the first person camera
        transform.position = firstPersonCameraTransform.position;

        // Calculate the current rotation based on the mouse input
        _currentRotationY = _mouseX * _sensitivity * Time.deltaTime;
        _currentRotationX = _mouseY * _sensitivity * Time.deltaTime;

        // Limit the vertical rotation to keep the camera from flipping
        var prevRotationX = transform.rotation.eulerAngles.x;
        var nextRotationX = transform.rotation.eulerAngles.x - _currentRotationX;

        if (prevRotationX <= 90f && nextRotationX > 90f)
            nextRotationX = 90f;
        else if (prevRotationX >= 270f && nextRotationX < 270f)
            nextRotationX = 270f;

        // Set the new rotation and apply it to the camera transform
        Quaternion nextRotation = Quaternion.Euler(
            nextRotationX,
            transform.rotation.eulerAngles.y + _currentRotationY,
            0
        );
        transform.rotation = nextRotation;

        // Make the player rotate with the camera
        player.transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
    }
}
