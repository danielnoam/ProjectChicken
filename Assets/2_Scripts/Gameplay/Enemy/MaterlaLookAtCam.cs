using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class MaterlaLookAtCam : MonoBehaviour
{ 
    Camera _cam;
    void Start()
    {
        _cam = Camera.current;
    }

    void Update()
    {
        transform.forward = _cam.transform.position - transform.position;
    }

}
