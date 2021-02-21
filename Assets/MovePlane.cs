using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovePlane : MonoBehaviour
{
    public Transform startTarget;

    // The target to travel towards.
    public Transform target;

    // The velocity of the plane. Not sure if this is useful.
    public float velocity = 39f;

    // The amount of time it takes to complete one lerp.
    public float time = 10f;

    private bool targetHit = false;

    private float lerpPct = 0;

    // Start is called before the first frame update
    void Start()
    {
        //startTarget = transform;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        lerpPct += Time.deltaTime / time;
        if (targetHit == false)
        {
            transform.position = Vector3.Lerp(startTarget.position, target.position, lerpPct);
            transform.rotation = Quaternion.Lerp(startTarget.rotation, target.rotation, lerpPct);
        }

        if (lerpPct >= 1.0)
        {
            targetHit = true;
            startTarget = target;
            lerpPct = 0f;
        }
    }

    void setTarget(Transform target)
    {
        this.target = target;
        targetHit = false;
        lerpPct = 0f;
    }
}
