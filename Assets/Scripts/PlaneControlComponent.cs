using UnityEngine;

public class PlaneControlComponent : MonoBehaviour
{
    private readonly Transform _transform;
    private readonly float _speed;

    public PlaneControlComponent(Transform transform, float speed)
    {
        _transform = transform;
        _speed = speed;
    }

    public void Move(float horizontal, float vertical)
    {
        Vector3 direction = new Vector3(horizontal, vertical, 0f).normalized;
        _transform.Translate(direction * _speed * Time.deltaTime);

        // Optional: clamp the player within the camera bounds
        // (can be added here or in another dedicated component)
    }
}
