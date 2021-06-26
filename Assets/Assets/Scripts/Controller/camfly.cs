using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class camfly : MonoBehaviour
{

    private Transform T;
    private Camera C;
    private Vector3 look;
    private Vector3 Cam_Momentum;
    private Vector3 Cam_Momentum_prev;


    void Start()
    {

        look = new Vector3();
        Cam_Momentum = new Vector3();
        C = this.GetComponent<Camera>();
        T = GetComponent<Transform>();


        GetComponent<Rigidbody>().useGravity = false;


    }

    public float sens = 1;
    private Vector3 Momentum;

    public float speed = 2;
    public float drag = 0.6f;



    // Update is called once per frame

    void Update()
    {
        input_fly();
        if(Input.GetKey(KeyCode.Escape))
        {
            Application.Quit();
        }
    }




    private void input_fly()
    {


        if (Input.GetKeyDown(KeyCode.R))
        {
            look = new Vector3();
        }


        Cam_Momentum = Input.mousePosition - Cam_Momentum_prev;
        Cam_Momentum_prev = Input.mousePosition;

        look += Cam_Momentum * sens;

        look.y = Mathf.Clamp(look.y, -45, 45);

        float spb = speed * Time.deltaTime;

        if (Input.GetKey(KeyCode.LeftShift))
        {
            spb *= 2.0f;
        }

        T.rotation = Quaternion.Euler(look.y * -1, look.x, 0f);


        if (Input.GetKey(KeyCode.W))
        {
            Momentum += T.forward * spb;
        }
        if (Input.GetKey(KeyCode.A))
        {
            Momentum -= T.right * spb;
        }
        if (Input.GetKey(KeyCode.S))
        {
            Momentum -= T.forward * spb;
        }
        if (Input.GetKey(KeyCode.D))
        {
            Momentum += T.right * spb;
        }

        Momentum *= drag;

        T.position += Momentum;


    }







}




