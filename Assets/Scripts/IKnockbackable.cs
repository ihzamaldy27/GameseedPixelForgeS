using UnityEngine;

public interface IKnockbackable
{
    // Interface khusus untuk menerima data posisi penyerang
    void ApplyKnockback(Vector2 sourcePosition);
}