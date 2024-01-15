using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField]
    private Transform _cameraTransform;

    private float _maxSpeed = 9.0f;
    private float _walkSpeed = 9.0f;
    private float _sprintSpeed = 16.0f;
    private float _maxAcceleration = 7.0f;
    private Vector2 _input;
    private Vector3 _velocity;

    [SerializeField]
    private Rigidbody rb;
    private bool _isGrounded = true;

    [SerializeField]
    private GameObject pauseMenuGameObject;

    [SerializeField]
    private LayerMask layerMask;

    StatsMechanism statsMechanism;

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            _isGrounded = true;
        }
        if (collision.gameObject.CompareTag("Item")) {
            GetComponent<InventoryMechanism>().AddItemInInventory(collision.gameObject);
        }
    }

    public void Jump(InputAction.CallbackContext context)
    {
        if (context.started && _isGrounded)
        {
            rb.AddForce(Vector3.up * 500, ForceMode.Impulse);
            _isGrounded = false;
        }
    }

    public void Attack(InputAction.CallbackContext context)
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit, 5f, layerMask))
        {
            ResourceBehavior resourceBehavior = hit.collider.gameObject.GetComponent<ResourceBehavior>();
            if (resourceBehavior != null)
            {
                resourceBehavior.TakeDamage(30);
            }
        }
    }

    public void Move(InputAction.CallbackContext context) =>
        _input = context.ReadValue<Vector2>();

    public void Sprint(InputAction.CallbackContext context)
    {
        if (context.started) {
            _maxSpeed = _sprintSpeed;
            SetIsSprinting(true);
        } else if (context.canceled) {
            _maxSpeed = _walkSpeed;
            SetIsSprinting(false);
        }
    }

    private void Awake() {
        LoadPersistance();
    }

    void Start() 
    {
        Cursor.lockState = CursorLockMode.Locked;
        statsMechanism = GetComponent<StatsMechanism>();
        Debug.Assert(statsMechanism != null, "Stats mechanism is missing!");
    }

    private void LoadPersistance() {
        string path = Application.persistentDataPath + "/PlayerPosition.json";
        if (File.Exists(path)) {
            string json = File.ReadAllText(path);
            PlayerPosition playerPosition = JsonUtility.FromJson<PlayerPosition>(json);

            transform.position = playerPosition.position;
            transform.rotation = playerPosition.rotation;
        }
    }

    void Update()
    {
        SavePositionRotation();

        if (statsMechanism.GetIsSprinting() && statsMechanism.GetEnergy() <= 0.05f) {
            _maxSpeed = _walkSpeed;
            SetIsSprinting(false);
        }

        if (statsMechanism.GetHealth() <= 0.3f) {
            string path = Application.persistentDataPath + "/PlayerPosition.json";
            if (File.Exists(path)) {
                File.Delete(path);
            }

            Cursor.lockState = CursorLockMode.None;
            SceneManager.LoadScene(0);
        }

        var cameraRotation = Quaternion.Euler(0.0f, _cameraTransform.rotation.eulerAngles.y, 0.0f);
        var desiredVelocity = new Vector3(_input.x, 0.0f, _input.y) * _maxSpeed;
        desiredVelocity = cameraRotation * desiredVelocity;
        var maxSpeedChange = _maxAcceleration * Time.deltaTime;
        if(_input.x == 0.0f && _input.y == 0.0f)
        {
            maxSpeedChange *= 3.0f;
        }

        _velocity.x = Mathf.MoveTowards(_velocity.x, desiredVelocity.x, maxSpeedChange);
        _velocity.z = Mathf.MoveTowards(_velocity.z, desiredVelocity.z, maxSpeedChange);

        var newPosition = transform.position + _velocity * Time.deltaTime;

        transform.position = newPosition;
    }

    private void SetIsSprinting(bool value) {
        StatsMechanism statsMechanism = GetComponent<StatsMechanism>();
        Debug.Assert(statsMechanism != null, "Stats mechanism is missing!");

        statsMechanism.SetIsSprinting(value);
    }

    private void SavePositionRotation() {
        string pathPlayer = Application.persistentDataPath + "/PlayerPosition.json";

        PlayerPosition playerPosition = new PlayerPosition();
        playerPosition.position = transform.position + new Vector3(0, 1, 0);
        playerPosition.rotation = transform.rotation;
        playerPosition.seed = FindObjectOfType<MapGenerator>().GetSeed();

        string json = JsonUtility.ToJson(playerPosition);
        File.WriteAllText(pathPlayer, json);
    }

    public class PlayerPosition {
        public int seed;
        public Vector3 position;
        public Quaternion rotation;
    }
}
