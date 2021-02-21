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

    private Transform startTarget;

    private bool targetHit = false;

    private float lerpPct = 0;

    // Start is called before the first frame update
    void Start()
    {
        startTarget = transform;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        lerpPct += Time.deltaTime / 10f;
        if (targetHit == false)
        {
            transform.position = Vector3.Lerp(startTarget.position, target.position, lerpPct);
            transform.rotation = Quaternion.Lerp(startTarget.rotation, target.rotation, lerpPct);
        }

        if (lerpPct >= 1.0)
        {
            targetHit = true;
            startTarget = target;
            lerpPct = 0;
        }
    }
}
