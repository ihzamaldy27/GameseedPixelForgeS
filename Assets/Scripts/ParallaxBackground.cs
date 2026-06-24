using UnityEngine;

/// <summary>
/// Attach this script to each background layer you want to move with parallax.
/// Each layer can have a different speed multiplier.
/// </summary>
public class ParallaxBackground : MonoBehaviour
{
    [Header("Parallax Settings")]
    [Tooltip("Speed multiplier relative to the camera. 0 = static, 1 = moves with camera, <1 = slower, >1 = faster.")]
    public float parallaxSpeed = 0.5f;

    [Header("Tiling (optional)")]
    [Tooltip("Enable to make the sprite wrap horizontally when it moves out of view.")]
    public bool wrapHorizontally = true;

    [Tooltip("The width of one tile (sprite's local width). Only used if wrapHorizontally is true.")]
    public float tileWidth = 10f;

    // Reference to the main camera (can be assigned manually or auto‑found)
    private Transform cameraTransform;

    // Store camera's previous frame position to compute movement delta
    private Vector3 previousCameraPosition;

    // Store our starting position to reset when wrapping
    private float startX;

    void Start()
    {
        // Find the main camera (assumes a Camera tagged "MainCamera")
        cameraTransform = Camera.main.transform;
        if (cameraTransform == null)
        {
            Debug.LogError("ParallaxBackground: No MainCamera found in scene. Please assign a camera.");
            enabled = false;
            return;
        }

        // Store initial camera position and our own starting X
        previousCameraPosition = cameraTransform.position;
        startX = transform.position.x;
    }

    void LateUpdate()
    {
        // Calculate how much the camera moved since last frame
        Vector3 cameraDelta = cameraTransform.position - previousCameraPosition;

        // Move this object by the camera delta multiplied by our parallax speed.
        // Only horizontal movement matters for a side‑scroller.
        float deltaX = cameraDelta.x * parallaxSpeed;
        transform.Translate(deltaX, 0f, 0f, Space.World);

        // Update stored camera position for next frame
        previousCameraPosition = cameraTransform.position;

        // Optional: Wrap the sprite horizontally to create an infinite scroll effect
        if (wrapHorizontally && tileWidth > 0f)
        {
            WrapHorizontally();
        }
    }

    /// <summary>
    /// Wraps the object's horizontal position so it appears to repeat indefinitely.
    /// Assumes the sprite's pivot is at its centre and tiles seamlessly.
    /// </summary>
    private void WrapHorizontally()
    {
        // Calculate how far we have moved from our start position
        float offset = transform.position.x - startX;

        // If we've moved more than one tile width to the right, wrap left
        if (offset > tileWidth)
        {
            startX += tileWidth;
            transform.position = new Vector3(transform.position.x - tileWidth, transform.position.y, transform.position.z);
        }
        // If we've moved more than one tile width to the left, wrap right
        else if (offset < -tileWidth)
        {
            startX -= tileWidth;
            transform.position = new Vector3(transform.position.x + tileWidth, transform.position.y, transform.position.z);
        }
    }
}