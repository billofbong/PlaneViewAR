using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using static OpenSky;
using Mapbox.Map;
using Mapbox.Unity.Map;
using Mapbox.Utils;

public class TrackPlane : MonoBehaviour
{
    public int queryFrequency; // how many seconds between each query
    public int queryDistance; // distance to search in miles
    public GameObject aircraftObject;
    public GameObject mapObject;

    private float queryTimer; // time since last query
    private const float MILES_TO_LAT = 0.0144927536232f; // 1/69, One degree of latitude = ~69mi
    private const float MILES_TO_METERS = 1609.34f;
    private const float METERS_TO_UNITS = 0.39005540974f;
    private Aircraft ours;
    private Vector2 last_latlong;
    private AbstractMap map;
    private bool queryDone = false;

    void Start()
    {
        map = mapObject.GetComponent<AbstractMap>();
        ours = StaticPlaneInfo.staticAircraft;
        map.ResetMap();
        map.Initialize(new Vector2d(ours.position.x, ours.position.y), 15);
        aircraftObject.transform.Rotate(Vector3.up, ours.true_track);
        Vector3 newPos = new Vector3(aircraftObject.transform.position.x, ours.altitude* METERS_TO_UNITS, aircraftObject.transform.position.z);
        aircraftObject.transform.position = newPos;
    }

    void Update()
    {
        aircraftObject.transform.position += transform.forward * METERS_TO_UNITS * ours.velocity * Time.deltaTime;
        aircraftObject.transform.position += transform.up * METERS_TO_UNITS * ours.vertical_rate * Time.deltaTime;
        queryTimer += Time.deltaTime;
        if (queryTimer > queryFrequency)
        {
            queryTimer = 0;
            StartCoroutine(Query(queryDistance, ours.position));
        }
        /*
        if (queryDone)
        {
            aircraftObject.transform.eulerAngles = new Vector3(
            aircraftObject.transform.eulerAngles.x,
            ours.true_track,
            aircraftObject.transform.eulerAngles.z
            );
            map.UpdateMap(new Vector2d(ours.position.x, ours.position.y));
            Vector3 newPos = new Vector3(aircraftObject.transform.position.x, ours.altitude * METERS_TO_UNITS, aircraftObject.transform.position.z);
            aircraftObject.transform.position = newPos;
            queryDone = false;
        }*/
    }

    /**
     * Query()
     * Coroutine to query the OpenSky REST API to get aircraft within a certain distance. Updates allAircraft list. Should not be run except from Update().
     * Returns: IEnumerator
     * Parameters:
     *     int queryDistance: Great circle distance to one edge of a bounding box to search for aircraft in
     */
    private IEnumerator Query(int queryDistance, Vector2 aircraftCoordinates)
    {
        LatLongBBox bbox = LatLongBoundingBox(aircraftCoordinates, queryDistance);
        string jsonResponse = "";
        using (UnityWebRequest request = UnityWebRequest.Get(string.Format("https://opensky-network.org/api/states/all?lamin={0}&lamax={1}&lomin={2}&lomax={3}",
            bbox.minLat, bbox.maxLat, bbox.minLong, bbox.maxLong)))
        {
            yield return request.SendWebRequest();
            jsonResponse = request.downloadHandler.text;
        }
        string states;
        try
        {
            states = jsonResponse.Split(new[] { "states\":[" }, System.StringSplitOptions.None)[1];
        }
        catch (System.IndexOutOfRangeException)
        {
            yield break;
        }
        string[] aircraft = states.Split(new[] { "],[" }, System.StringSplitOptions.None);
        float timeNow = Time.time;
        foreach (string x in aircraft)
        {
            string[] attributes = x.Replace("[", "").Replace("\"", "").Replace("]]}", "").Trim().Split(','); // remove [ " and ]]} from the strings then split into an array at ,
            if (attributes[1] == ours.callsign)
            {
                ours.callsign = attributes[1];
                last_latlong = ours.position;
                ours.position = new Vector2(float.Parse(attributes[6]), float.Parse(attributes[5]));
                ours.altitude = string.Equals(attributes[7], "null") ? 0 : float.Parse(attributes[7]);
                ours.velocity = string.Equals(attributes[9], "null") ? 0 : float.Parse(attributes[9]);
                ours.true_track = string.Equals(attributes[11], "null") ? 0 : float.Parse(attributes[11]);
                ours.lastSeen = timeNow;
            }
        }
        queryDone = true;
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

    /**
     * LengthOfLonDegreeAt()
     * Returns: Great circle distance of one degree of longitude at a given latitude
     * Parameters:
     *     float lat: Latitude at which to find the distance of one degree of longitude
     */
    private float LengthOfLonDegreeAt(float lat)
    {
        return Mathf.Abs(Mathf.Cos(lat)) * 69.172f; // Length of 1 degree of Longitude = cosine(latitude in decimal degrees) * length of degree (miles) at equator.
    }

    public float GetAngleBetween(Vector2 current, Vector2 dest)
    {
        float lat1 = current.x * Mathf.Deg2Rad;
        float lat2 = dest.x * Mathf.Deg2Rad;
        float long1 = current.y * Mathf.Deg2Rad;
        float long2 = dest.y * Mathf.Deg2Rad;

        float dLon = (long2 - long1);
        float x = Mathf.Cos(lat2) * Mathf.Sin(dLon);
        float y = Mathf.Cos(lat1) * Mathf.Sin(lat2) - Mathf.Sin(lat1) * Mathf.Cos(lat2) * Mathf.Cos(dLon);
        float brng = Mathf.Atan2(x, y);
        brng = Mathf.Rad2Deg * brng;
        brng = (brng + 360) % 360;
        //brng = 360 - brng;
        return brng;
    }

    public float GetDistanceBetween(Vector2 current, Vector2 dest) // from https://stackoverflow.com/questions/27928/calculate-distance-between-two-latitude-longitude-points-haversine-formula
    {
        float p = 0.017453292519943295f;    // Mathf.PI / 180
        float a = 0.5f - Mathf.Cos((dest.x - current.x) * p) / 2.0f +
                Mathf.Cos(current.x * p) * Mathf.Cos(dest.x * p) *
                (1 - Mathf.Cos((dest.y - current.y) * p)) / 2.0f;

        return 7917.6f * Mathf.Asin(Mathf.Sqrt(a)); // 2 * R; R = 3,958.8 mi
    }

    private struct LatLongBBox
    {
        public float minLat, minLong, maxLat, maxLong;
    }
}
