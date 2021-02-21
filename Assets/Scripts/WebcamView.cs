using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WebcamView : MonoBehaviour
{
    private WebCamTexture webCamTexture;
    public Renderer renderer;
    private WebCamDevice[] devices;

    // Start is called before the first frame update
    void Start()
    {
        webCamTexture = new WebCamTexture();
        devices = WebCamTexture.devices;
        for (int i = 0; i < devices.Length; i++)
        {
            print(devices[i].name);
        }

        webCamTexture.deviceName = devices[1].name;
        renderer.material.mainTexture = webCamTexture;
        webCamTexture.Play();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
