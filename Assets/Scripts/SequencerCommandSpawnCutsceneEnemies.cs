using UnityEngine;
using PixelCrushers.DialogueSystem;


namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    public class SequencerCommandSpawnCutsceneEnemies : SequencerCommand
    {
        public void Start()
        {
            // Fetch variable boss X, enemy positions(later)
            float _bossX = GetParameterAsFloat(0);
            float _enemyChaseX = GetParameterAsFloat(1);

            Transform _bossIdle = GameObject.Find("BossIdle").transform;
            _bossIdle.position = new Vector2(_bossX, _bossIdle.position.y);

            Transform _enemyChase = GameObject.Find("EnemyChase").transform;
            _enemyChase.position = new Vector2(_enemyChaseX, _enemyChase.position.y);

            Stop();
        }
    }
}

