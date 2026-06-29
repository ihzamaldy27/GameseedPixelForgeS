using UnityEngine;

public class BossMovement : MonoBehaviour
{
    [Header("Entry Phase")]
    [SerializeField] private float entrySpeed = 3f;
    [SerializeField] private Vector2 stopPosition; // world position where it stops

    [Header("Patrol Phase")]
    [SerializeField] private float patrolTop = 4f;   // relative Y from stopPosition? We'll use world Y.
    [SerializeField] private float patrolBottom = -4f;
    [SerializeField] private float patrolSpeed = 1f;

    private enum State { Entry, Patrolling }
    private State _state = State.Entry;
    private Vector3 _entryDirection; // leftwards
    private float _patrolTime;

    private void Start()
    {
        // Entry direction: move from right to stopPosition
        _entryDirection = (stopPosition - (Vector2)transform.position).normalized;
        // If already at stop position, skip entry
        if (Vector2.Distance(transform.position, stopPosition) < 0.1f)
            _state = State.Patrolling;
    }

    private void Update()
    {
        switch (_state)
        {
            case State.Entry:
                // Move towards stop position
                Vector2 currentPos = transform.position;
                Vector2 newPos = Vector2.MoveTowards(currentPos, stopPosition, entrySpeed * Time.deltaTime);
                transform.position = newPos;
                if (Vector2.Distance(newPos, stopPosition) < 0.01f)
                {
                    transform.position = stopPosition;
                    _state = State.Patrolling;
                    _patrolTime = 0f;
                }
                break;

            case State.Patrolling:
                // Patrol vertically between patrolTop and patrolBottom
                _patrolTime += Time.deltaTime * patrolSpeed;
                float y = Mathf.Lerp(patrolBottom, patrolTop, (Mathf.Sin(_patrolTime) + 1f) * 0.5f);
                Vector3 pos = transform.position;
                pos.y = y;
                transform.position = pos;
                break;
        }
    }

    // Optional: method to reset state when reused (if pooling later)
    public void ResetMovement(Vector2 startPos)
    {
        transform.position = startPos;
        _state = State.Entry;
        _entryDirection = (stopPosition - (Vector2)startPos).normalized;
        _patrolTime = 0f;
    }
}
