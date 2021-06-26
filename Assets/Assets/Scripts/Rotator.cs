using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotator : MonoBehaviour
{
    // Start is called before the first frame update
    Transform T;
    public float speed;
    public float leng;

    void Start()
    {
        T=GetComponent<Transform>();
    }

    // Update is called once per frame
    void Update()
    {
        T.localEulerAngles+=new Vector3(0,0,speed*Time.deltaTime);
    
        T.position=new Vector3(T.position.x,(float)Math.Sin(Time.time)*leng,T.position.z);
    }
}
