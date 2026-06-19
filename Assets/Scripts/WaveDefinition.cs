using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "WaveData", menuName = "Shooter/Wave Definition")]
public class WaveDefinition : ScriptableObject
{
    public List<SpawnEvent> spawnEvents = new List<SpawnEvent>();
}

[System.Serializable]
public class SpawnEvent
{
    public float time; // time in seconds from wave start
    public GameObject enemyPrefab;
    public Vector2 spawnPosition; // local to spawner or world?
    public MovementPatternType patternType;

    // Parameters for the pattern
    public float speed = 2f;
    public float amplitude = 1f;
    public float frequency = 2f;
}

public enum MovementPatternType
{
    Straight,
    Sine
}