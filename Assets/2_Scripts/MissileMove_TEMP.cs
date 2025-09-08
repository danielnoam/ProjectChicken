using UnityEngine;

public class MissileMove_TEMP : MonoBehaviour
{
    public float speed = 5f; // Choose your speed in the Inspector

    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        rb.MovePosition(transform.position + transform.forward * speed * Time.fixedDeltaTime);
    }
}
