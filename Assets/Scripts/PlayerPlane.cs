using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerPlane : MonoBehaviour, PlaneControl.IPlayerActions
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Shooting Settings")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float fireRate = 0.2f;

    // Composition: the player owns separate handlers for movement and shooting
    private PlaneControlComponent _movement;
    private ShootingComponent _shooting;
    private Vector2 _direction;
    private bool _firePressed;

    public void OnMove(InputAction.CallbackContext context)
    {
        _direction = context.ReadValue<Vector2>();
    }

    public void OnShoot(InputAction.CallbackContext context)
    {
        float _value = context.ReadValue<float>();
        _firePressed = _value > 0 ? true : false;
    }

    private void Awake()
    {
        // Instantiate the composed objects, passing any needed dependencies
        _movement = new PlaneControlComponent(transform, moveSpeed);
        _shooting = new ShootingComponent(firePoint, bulletPrefab, fireRate);
    }

    private void Update()
    {
        // Read input
        float horizontal = _direction.x;
        float vertical = _direction.y;
        bool firePressed = _firePressed;

        // Delegate to the composed handlers
        _movement.Move(horizontal, vertical);
        _shooting.HandleShoot(firePressed);
    }
}
