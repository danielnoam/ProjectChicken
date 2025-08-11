using UnityEngine;

public class StreamLineMovement : MonoBehaviour
{
    [SerializeField] float Jitter = 0.5f;
    [SerializeField] private Vector3 StreamLineMovementDistance = new Vector3(0,0,5);
    //[SerializeField] public Vector3 StreamLineStartLocation;
    [SerializeField] private Transform StreamLineStartLocation;
    [SerializeField] private Transform SpaceshipTransform;
    private Vector3 StreamLineEndLocation;
    
    
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    { 
        //StreamLineStartLocation  = this.transform.position;
        Vector3 localPos = StreamLineStartLocation.localPosition; 
        
        StreamLineEndLocation = localPos + StreamLineMovementDistance;
    }

    // Update is called once per frame
    void Update()
    { 
        Vector3 SpaceshipLocation = SpaceshipTransform.localPosition; 
        float t = Mathf.PingPong(Time.time * Jitter, 1f);
       transform.position = Vector3.Lerp( StreamLineStartLocation.position, StreamLineEndLocation, t)
                            + SpaceshipLocation ;
    }
}
