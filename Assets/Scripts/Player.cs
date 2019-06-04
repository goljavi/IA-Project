using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public float speedH = 2.0f;
    public float speedV = 2.0f;
    public float pitchClamp = 60;
    public float speed = 10;

    private float yaw = 0.0f;
    private float pitch = 0.0f;

    void Update()
    {
        //Mouse
        yaw += speedH * Input.GetAxis("Mouse X");
        pitch -= speedV * Input.GetAxis("Mouse Y");
        pitch = Mathf.Clamp(pitch, -pitchClamp, pitchClamp);
        transform.eulerAngles = new Vector3(pitch, yaw, 0.0f);


        transform.position += transform.forward * Input.GetAxis("Vertical") * speed * Time.deltaTime;
        transform.position += transform.right * Input.GetAxis("Horizontal") * speed * Time.deltaTime;
    }
}
