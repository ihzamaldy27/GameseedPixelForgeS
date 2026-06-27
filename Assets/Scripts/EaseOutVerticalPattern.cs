using UnityEngine;

public class EaseOutVerticalPattern : IMovementPattern
{
    private readonly float _horizontalSpeed;
    private readonly float _initialVerticalSpeed; // starting vertical speed (+ up, – down)
    private readonly float _transitionDuration;
    private float _elapsed;

    public EaseOutVerticalPattern(float horizontalSpeed, float initialVerticalSpeed, float transitionDuration)
    {
        _horizontalSpeed = horizontalSpeed;
        _initialVerticalSpeed = initialVerticalSpeed;
        _transitionDuration = transitionDuration;
        _elapsed = 0f;
    }

    public void UpdateMovement(Transform transform, float deltaTime)
    {
        // Horizontal movement (always left)
        transform.Translate(Vector3.left * _horizontalSpeed * deltaTime);

        // Vertical movement with quadratic ease‑out
        _elapsed += deltaTime;
        float t = Mathf.Clamp01(_elapsed / _transitionDuration);
        float easeOut = 1f - (1f - t) * (1f - t); // quadratic; can use cubic for stronger effect
        float factor = 1f - easeOut; // goes from 1 to 0
        float verticalSpeed = _initialVerticalSpeed * factor;
        transform.Translate(Vector3.up * verticalSpeed * deltaTime);
    }
}