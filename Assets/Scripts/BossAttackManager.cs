using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BossAttackManager : MonoBehaviour
{
    [Header("Attack Patterns")]
    [SerializeField] private List<MonoBehaviour> patternBehaviours; // assign in inspector

    [Header("Timing")]
    [SerializeField] private float timeBetweenAttacks = 2f;
    [SerializeField] private float initialDelay = 1f; // wait before first attack

    private List<IBossAttackPattern> _patterns = new List<IBossAttackPattern>();
    private int _currentPatternIndex = 0;
    private bool _isAttacking = false;

    private void Awake()
    {
        // Collect all attack pattern components from the assigned behaviours
        foreach (var behaviour in patternBehaviours)
        {
            if (behaviour is IBossAttackPattern pattern)
                _patterns.Add(pattern);
        }
        if (_patterns.Count == 0)
            Debug.LogWarning("No attack patterns assigned to BossAttackManager.");
    }

    private void Start()
    {
        StartCoroutine(AttackCycle());
    }

    private IEnumerator AttackCycle()
    {
        yield return new WaitForSeconds(initialDelay);

        while (true)
        {
            if (_patterns.Count == 0) yield break;

            // Get current pattern
            IBossAttackPattern pattern = _patterns[_currentPatternIndex];
            _currentPatternIndex = (_currentPatternIndex + 1) % _patterns.Count;

            // Execute the attack
            _isAttacking = true;
            yield return StartCoroutine(pattern.ExecuteAttack());
            _isAttacking = false;

            // Wait between attacks
            yield return new WaitForSeconds(timeBetweenAttacks);
        }
    }

    // Optionally stop attacks (e.g., when boss dies)
    public void StopAttacks()
    {
        StopAllCoroutines();
    }
}