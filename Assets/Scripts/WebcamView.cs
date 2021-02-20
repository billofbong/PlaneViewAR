using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WebcamView : MonoBehaviour
{
    public WebCamTexture webCamTexture;
    public RawImage rawImage;

    // Start is called before the first frame update
    void Start()
    {
        webCamTexture = new WebCamTexture();
        //rawImage.texture = webCamTexture;
        //rawImage.material.mainTexture = webCamTexture;
        WebCamDevice[] devices = WebCamTexture.devices;
        for (int i = 0; i < devices.Length; i++)
        {
            print(devices[i].name);
        }

        webCamTexture.deviceName = devices[1].name;
        webCamTexture.Play();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
