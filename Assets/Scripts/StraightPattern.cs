using UnityEngine;

public class StraightPattern : IMovementPattern
{
    private readonly float _speed;

    public StraightPattern(float speed)
    {
        _speed = speed;
    }

    public void UpdateMovement(Transform transform, float deltaTime)
    {
        transform.Translate(Vector3.left * _speed * deltaTime);
    }
}