using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerLocomotion : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _rotationSpeed = 720f;
    [SerializeField] private float _gravity = -20f;
    [SerializeField] private Transform _cameraTransform;
    private CharacterController _controller;
    private float _verticalVelocity;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        if (_cameraTransform == null)
        {
            _cameraTransform = Camera.main.transform;
        }
    }

    public Vector3 GetCameraRelativeDirection(Vector2 input)
    {
        Vector3 cameraForward = _cameraTransform.forward;
        cameraForward.y = 0f;
        cameraForward = cameraForward.normalized;

        Vector3 cameraRight = _cameraTransform.right;
        cameraRight.y = 0f;
        cameraRight = cameraRight.normalized;

        Vector3 dir = cameraForward * input.y + cameraRight * input.x;

        return Vector3.ClampMagnitude(dir, 1f);
    }

    private void RotateTowards(Vector3 direction)
    {
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
    }

    public void Move(Vector2 input, float deltaTime)
    {
        Vector3 moveDirection = GetCameraRelativeDirection(input);

        if (moveDirection.sqrMagnitude > 0.0001f)
        {
            RotateTowards(moveDirection);
        }

        if (_controller.isGrounded && _verticalVelocity < 0)
        {
            _verticalVelocity = -2f;
        }
        else
        {
            _verticalVelocity += _gravity * deltaTime;
        }

        Vector3 horizontalVelocity = moveDirection * _moveSpeed;
        Vector3 verticalVelocity = Vector3.up * _verticalVelocity;
        _controller.Move((horizontalVelocity + verticalVelocity) * deltaTime);
    }
}
