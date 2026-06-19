using UnityEngine;

public interface IMovementPattern
{
    void UpdateMovement(Transform transform, float deltaTime);
}
