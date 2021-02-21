using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrigTransform : MonoBehaviour
{
    // Start is called before the first frame update
    public float strength = 1;

    void Start()
    {
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        float Z_POS = transform.position.z;
        transform.Translate(0, 0, strength * Mathf.Cos(Time.time));
    }
}
