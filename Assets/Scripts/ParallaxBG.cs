using UnityEngine;

public class ParallaxBG : MonoBehaviour
{
    [Header("Parallax Settings")]
    [Tooltip("Speed multiplier relative to the camera. 0 = static, 1 = moves with camera, <1 = slower, >1 = faster.")]
    public float parallaxSpeed = 0.5f;
    public float spriteWidth = 0f;


    private float wrapCounter = 0f;

    void LateUpdate()
    {
        float _speed = -parallaxSpeed * Time.deltaTime;

        transform.Translate(_speed, 0f, 0f, Space.World);

        if (transform.position.x < (-spriteWidth) * (wrapCounter + 1))
        {
            float _leftBGPos = 0.1f;
            GameObject _leftobj = null;
            for(int i = 0; i < transform.childCount; i++)
            {
                GameObject _obj = transform.GetChild(i).gameObject;
                if (_obj.transform.position.x  < _leftBGPos)
                {
                    _leftBGPos = _obj.transform.position.x;
                    _leftobj = _obj;
                }
            }

            if (_leftobj != null)
                _leftobj.transform.localPosition = new Vector2(spriteWidth * (transform.childCount + wrapCounter), _leftobj.transform.localPosition.y);            

            wrapCounter += 1;
        }
    }
}
