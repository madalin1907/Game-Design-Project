using UnityEngine;
using UnityEngine.InputSystem;

public enum View { FIRST_PERSON, THIRD_PERSON };

public class CameraMovement : MonoBehaviour
{
    private float _mouseX;
    private float _mouseY;

    public void Rotate(InputAction.CallbackContext context)
    {
        var mouseCoords = context.ReadValue<Vector2>();
        _mouseX = mouseCoords.x;
        _mouseY = mouseCoords.y;
    }

    [SerializeField]
    private View view = View.THIRD_PERSON;

    [SerializeField]
    private GameObject player;

    [SerializeField]
    private Transform firstPersonCameraTransform;

    [SerializeField]
    private float sensitivity = 30.0f;

    [SerializeField]
    private float _distance = 7.0f; // Adjust the distance to your preference

    private float _currentRotationY = 0.0f;
    private float _currentRotationX = 0.0f;

    private float _waitingTimeBeforeRotationCamera = 0.2f;

    void Awake()
    {
        _currentRotationX = transform.rotation.eulerAngles.x;
        _currentRotationY = transform.rotation.eulerAngles.y;
    }

    void Update()
    {
        if (_waitingTimeBeforeRotationCamera > 0f)
            _waitingTimeBeforeRotationCamera -= Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.V))
        {
            if (view == View.FIRST_PERSON)
                view = View.THIRD_PERSON;
            else
                view = View.FIRST_PERSON;
        }
    }

    void LateUpdate()
    {
        if (_waitingTimeBeforeRotationCamera > 0f)
            return;

        if (view == View.THIRD_PERSON)
        {
            LateUpdateThirdPerson();
        }
        else
        {
            LateUpdateFirstPerson();
        }
    }

    void LateUpdateThirdPerson()
    {
        _currentRotationY += _mouseX * sensitivity * Time.deltaTime;
        _currentRotationX += _mouseY * sensitivity * Time.deltaTime;

        // Limit the vertical rotation to keep the camera from flipping
        _currentRotationX = Mathf.Clamp(_currentRotationX, 90f, 180f);

        // Calculate the new camera position based on the current rotation
        Vector3 offset = new Vector3(0, 0, -_distance);
        Quaternion rotation = Quaternion.Euler(_currentRotationX, _currentRotationY, 0);
        Vector3 desiredPosition = player.transform.position + rotation * offset;

        // Smoothly move the camera to the desired position
        transform.position = Vector3.Lerp(transform.position, desiredPosition, 0.2f);

        // Make the camera look at the player
        transform.LookAt(player.transform.position);
    }

    void LateUpdateFirstPerson()
    {
        transform.position = firstPersonCameraTransform.position;

        _currentRotationY = _mouseX * sensitivity * Time.deltaTime;
        _currentRotationX = _mouseY * sensitivity * Time.deltaTime;

        var prevRotationX = transform.rotation.eulerAngles.x;
        var nextRotationX = transform.rotation.eulerAngles.x - _currentRotationX;

        if (prevRotationX <= 90f && nextRotationX > 90f)
            nextRotationX = 90f;
        else if (prevRotationX >= 270f && nextRotationX < 270f)
            nextRotationX = 270f;

        Quaternion nextRotation = Quaternion.Euler(
            nextRotationX,
            transform.rotation.eulerAngles.y + _currentRotationY,
            0
        );

        transform.rotation = nextRotation;

    }
}