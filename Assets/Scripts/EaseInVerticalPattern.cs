using UnityEngine;

public class EaseInVerticalPattern : IMovementPattern
{
    private readonly float _horizontalSpeed;
    private readonly float _verticalSpeed;   // final vertical velocity (positive = up)
    private readonly float _transitionDuration;
    private float _elapsed;

    public EaseInVerticalPattern(float horizontalSpeed, float verticalSpeed, float transitionDuration)
    {
        _horizontalSpeed = horizontalSpeed;
        _verticalSpeed = verticalSpeed;
        _transitionDuration = transitionDuration;
        _elapsed = 0f;
    }

    public void UpdateMovement(Transform transform, float deltaTime)
    {
        // Horizontal movement (always left)
        transform.Translate(Vector3.left * _horizontalSpeed * deltaTime);

        // Vertical movement with quadratic ease‑in
        _elapsed += deltaTime;
        float t = Mathf.Clamp01(_elapsed / _transitionDuration);
        float eased = t * t;              // quadratic ease-in; you can use t*t*t for stronger easing
        float vertical = _verticalSpeed * eased;
        transform.Translate(Vector3.up * vertical * deltaTime);
    }
}
