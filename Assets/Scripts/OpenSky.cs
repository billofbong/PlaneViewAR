using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using UnityEngine;

public class OpenSky : MonoBehaviour
{
    public int queryFrequency; // how many seconds between each query
    public int queryDistance; // distance to search in miles 

    private float timer = 10; // time since last query
    private const float MILES_TO_LAT = 0.0144927536232f; // 1/69, One degree of latitude = ~69mi
    private bool found = false;

    void Start()
    {

    }

    void Update()
    {
        timer += Time.deltaTime;
        if(!found)
            gameObject.GetComponent<UnityEngine.UI.Text>().text = timer.ToString();

        if (timer > queryFrequency)
        {
            found = true;
            Query(queryDistance);
        }
    }

    private void Query(int queryRadius)
    {
        LatLongBBox bbox = LatLongBoundingBox(new Vector2(37.287659f, -121.942429f), queryDistance);
        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(string.Format("https://opensky-network.org/api/states/all?lamin={0}&lamax={1}&lomin={2}&lomax={3}",
            bbox.minLat, bbox.maxLat, bbox.minLong, bbox.maxLong));
        HttpWebResponse response = (HttpWebResponse)request.GetResponse();
        StreamReader reader = new StreamReader(response.GetResponseStream());
        string jsonResponse = reader.ReadToEnd();
        gameObject.GetComponent<UnityEngine.UI.Text>().text = jsonResponse;
    }

    /**
     * LatLongBoundingBox()
     * Parameters:
     *     Vector2 Center: Lat/Long of center of desired bounding box
     *     float distance: Shortest distance to one of the sides of the bounding box
     * Returns: LatLongBBox
     */
    private LatLongBBox LatLongBoundingBox(Vector2 center, float distance)
    {
        LatLongBBox bbox = new LatLongBBox();
        bbox.minLat = center.x - distance * MILES_TO_LAT;
        bbox.maxLat = center.x + distance * MILES_TO_LAT;
        bbox.minLong = center.y - distance / LengthOfLonDegreeAt(center.x);
        bbox.maxLong = center.y + distance / LengthOfLonDegreeAt(center.x);
        return bbox;
    }

    float LengthOfLonDegreeAt(float lat)
    {
        return Mathf.Abs(Mathf.Cos(lat)) * 69.172f; // Length of 1 degree of Longitude = cosine(latitude in decimal degrees) * length of degree (miles) at equator.
    }

    struct Aircraft
    {
        float longitude, last_longitude, latitude, last_latitude;
        int altitude, last_altitude;
    }

    struct LatLongBBox
    {
        public float minLat, minLong, maxLat, maxLong;
    }
}
