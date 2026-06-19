using UnityEngine;

public class SinePattern : IMovementPattern
{
    private readonly float _speed;
    private readonly float _amplitude;
    private readonly float _frequency;
    private float _time;

    public SinePattern(float speed, float amplitude, float frequency)
    {
        _speed = speed;
        _amplitude = amplitude;
        _frequency = frequency;
        _time = 0f;
    }

    public void UpdateMovement(Transform transform, float deltaTime)
    {
        _time += deltaTime;
        float x = -_speed * deltaTime;
        float y = _amplitude * Mathf.Sin(_frequency * _time) * deltaTime * 2; // smoothed movement
        transform.Translate(new Vector3(x, y, 0));
    }
}
