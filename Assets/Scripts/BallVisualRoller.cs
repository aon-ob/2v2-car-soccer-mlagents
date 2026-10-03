using UnityEngine;

public class BallVisualRoller : MonoBehaviour
{
    [Tooltip("Drag the parent Ball Rigidbody2D here")]
    public Rigidbody2D ballRb;

    [Tooltip("The visual radius of the ball in world units")]
    public float ballRadius = 0.5f;

    void Start()
    {
        if (ballRb == null && transform.parent != null)
        {
            ballRb = transform.parent.GetComponent<Rigidbody2D>();
        }
    }

    void Update()
    {
        if (ballRb == null) return;

        Vector2 vel = ballRb.linearVelocity;
        float speed = vel.magnitude;

        if (speed > 0.05f)
        {
            float angleDelta = (speed * Time.deltaTime / ballRadius) * Mathf.Rad2Deg;
            Vector3 rollAxis = new Vector3(vel.y, -vel.x, 0f).normalized;
            transform.Rotate(rollAxis, angleDelta, Space.World);
        }
    }
}