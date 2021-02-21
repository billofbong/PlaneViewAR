using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovePlane : MonoBehaviour
{
    // The target to travel towards.
    public Transform target;

    // The velocity of the plane.
    public float velocity = 39f;

    // The rate of how much the plane will turn in any direction per second in degrees.
    public float rateOfTurn = 2f;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        float yRotation = transform.rotation.eulerAngles.y;
        float angleTarget;
        if (target == null)
            angleTarget = StaticPlaneInfo.staticAircraft.true_track;
        else
            angleTarget = target.rotation.eulerAngles.y;

        if (Mathf.Abs(yRotation - angleTarget) < Time.deltaTime)
        {
            transform.rotation = Quaternion.Euler(0, angleTarget, 0);
        }
        else if (yRotation > angleTarget)
        {
            transform.Rotate(0, -rateOfTurn * Time.deltaTime, 0);
        }
        else
        {
            transform.Rotate(0, rateOfTurn * Time.deltaTime, 0);
        }
    }
}
