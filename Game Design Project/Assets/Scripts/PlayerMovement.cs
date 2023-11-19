using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField]
    private Transform _cameraTransform;

    private float _maxSpeed = 5.0f;
    private float _maxAcceleration = 7.0f;
    private Rect _floorRect = new(-24.5f, -24.5f, 49.0f, 49.0f);
    private Vector2 _input;
    private Vector3 _velocity;

    public void Move(InputAction.CallbackContext context) =>
        _input = context.ReadValue<Vector2>();

    public void Sprint(InputAction.CallbackContext context)
    {
        if (context.started)
            _maxSpeed *= 2.0f;
        else if (context.canceled)
            _maxSpeed /= 2.0f;
    }

    void Update()
    {
        var cameraRotation = Quaternion.Euler(0.0f, _cameraTransform.rotation.eulerAngles.y, 0.0f);
        var desiredVelocity = new Vector3(_input.x, 0.0f, _input.y) * _maxSpeed;
        desiredVelocity = cameraRotation * desiredVelocity;
        var maxSpeedChange = _maxAcceleration * Time.deltaTime;

        _velocity.x = Mathf.MoveTowards(_velocity.x, desiredVelocity.x, maxSpeedChange);
        _velocity.z = Mathf.MoveTowards(_velocity.z, desiredVelocity.z, maxSpeedChange);

        var newPosition = transform.position + _velocity * Time.deltaTime;

        if (newPosition.x > _floorRect.xMax)
            newPosition.x = _floorRect.xMax;

        if (newPosition.x < _floorRect.xMin)
            newPosition.x = _floorRect.xMin;

        if (newPosition.z > _floorRect.yMax)
            newPosition.z = _floorRect.yMax;

        if (newPosition.z < _floorRect.yMin)
            newPosition.z = _floorRect.yMin;

        transform.position = newPosition;
    }
}