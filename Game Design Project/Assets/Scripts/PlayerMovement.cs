using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField]
    private Transform _cameraTransform;

    private float _maxSpeed = 10.0f;
    private float _maxAcceleration = 7.0f;
    private Vector2 _input;
    private Vector3 _velocity;

    [SerializeField]
    private Rigidbody rb;
    private bool _isGrounded = true;

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            _isGrounded = true;
        }
    }

    public void Jump(InputAction.CallbackContext context)
    {
        if (context.started && _isGrounded)
        { 
            rb.AddForce(Vector3.up * 6, ForceMode.Impulse);
            _isGrounded = false;
        }
    }

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

        transform.position = newPosition;
    }
}
