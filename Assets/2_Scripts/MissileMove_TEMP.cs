using UnityEngine;

public class MissileMove_TEMP : MonoBehaviour
{
    public float speed = 5f; // Choose your speed in the Inspector

    void Update()
    {
        // Move the object forward every frame
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }
}
