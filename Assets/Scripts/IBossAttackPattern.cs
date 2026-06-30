using System.Collections;

public interface IBossAttackPattern
{
    IEnumerator ExecuteAttack(); // coroutine to perform the attack
}