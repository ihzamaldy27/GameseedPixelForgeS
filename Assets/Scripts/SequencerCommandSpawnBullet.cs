using UnityEngine;
using System.Collections;
using PixelCrushers.DialogueSystem;


namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    public class SequencerCommandSpawnBullet : SequencerCommand
    {
        public void Start()
        {
            // Get parameters: prefab, x, y, target, speed
            string _prefab = GetParameter(0);
            GameObject prefab = Resources.Load<GameObject>(_prefab);
            float x = GetParameterAsFloat(1);
            float y = GetParameterAsFloat(2);
            string _target = GetParameter(3);
            Transform target = GameObject.FindWithTag(_target).transform;
            float speed = GetParameterAsFloat(4);

            GameObject bullet = Instantiate(prefab, new Vector3(x, y, 0), Quaternion.identity);
            bullet.GetComponent<BulletChase>().Initialize(target, speed);
            
            Stop(); // Command finishes
        }
    }
}
