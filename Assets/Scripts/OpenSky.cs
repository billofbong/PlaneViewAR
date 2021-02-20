using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using UnityEngine;
using UnityEngine.Networking;

public class OpenSky : MonoBehaviour
{
    public int queryFrequency; // how many seconds between each query
    public int queryDistance; // distance to search in miles 

    private float timer; // time since last query
    private const float MILES_TO_LAT = 0.0144927536232f; // 1/69, One degree of latitude = ~69mi
    private bool found = false;
    private List<Aircraft> allAircraft;

    void Start()
    {
        allAircraft = new List<Aircraft>();
        StartCoroutine(Location.Start());
    }

    void Update()
    {
        timer += Time.deltaTime;
        gameObject.GetComponent<UnityEngine.UI.Text>().text = Location.GetUserCoords().ToString("F5");

        if (timer > queryFrequency)
        {
            found = true;
            StartCoroutine(Query(queryDistance));
            timer = 0;
        }
    }

    /**
     * Query()
     * Coroutine to query the OpenSky REST API to get aircraft within a certain distance. Updates allAircraft list. Should not be run except from Update().
     * Returns: IEnumerator
     * Parameters: 
     *     int queryDistance: Great circle distance to one edge of a bounding box to search for aircraft in
     */
    private IEnumerator Query(int queryDistance)
    {
        LatLongBBox bbox = LatLongBoundingBox(Location.GetUserCoords(), queryDistance);
        string jsonResponse = "";
        using (UnityWebRequest request = UnityWebRequest.Get(string.Format("https://opensky-network.org/api/states/all?lamin={0}&lamax={1}&lomin={2}&lomax={3}",
            bbox.minLat, bbox.maxLat, bbox.minLong, bbox.maxLong)))
        {
            yield return request.SendWebRequest();
            jsonResponse = request.downloadHandler.text;
        }
        string states = jsonResponse.Split(new[] { "states\":[" }, System.StringSplitOptions.None)[1];
        string[] aircraft = states.Split(new[] { "],[" }, System.StringSplitOptions.None);
        float timeNow = Time.time;
        foreach(string x in aircraft)
        {
            string[] attributes = x.Replace("[", "").Replace("\"", "").Replace("]]}", "").Trim().Split(','); // remove [ " and ]]} from the strings then split into an array at ,
            try
            {
                Aircraft a = allAircraft[allAircraft.FindIndex(y => y.callsign == attributes[1])];
                a.latitude = float.Parse(attributes[6]); // https://opensky-network.org/apidoc/rest.html
                a.longitude = float.Parse(attributes[5]);
                a.altitude = string.Equals(attributes[7], "null") ? float.NaN : float.Parse(attributes[7]); // sometimes these values are null (eg. if aircraft on ground)
                a.velocity = string.Equals(attributes[9], "null") ? float.NaN : float.Parse(attributes[9]);
                a.true_track = string.Equals(attributes[11], "null") ? float.NaN : float.Parse(attributes[11]);
                a.lastSeen = timeNow;
            }
            catch(System.ArgumentOutOfRangeException)
            {
                Aircraft a = new Aircraft
                {
                    callsign = attributes[1],
                    latitude = float.Parse(attributes[6]),
                    longitude = float.Parse(attributes[5]),
                    altitude = string.Equals(attributes[7], "null") ? float.NaN : float.Parse(attributes[7]),
                    velocity = string.Equals(attributes[9], "null") ? float.NaN : float.Parse(attributes[9]),
                    true_track = string.Equals(attributes[11], "null") ? float.NaN : float.Parse(attributes[11]),
                    lastSeen = timeNow
                };
                allAircraft.Add(a);
            }
        }
        for (int i = 0; i < allAircraft.Count; i++)
        {
            yield return null;
            Debug.Log(allAircraft[i].callsign);
            if (allAircraft[i].lastSeen < timeNow)
            {
                allAircraft.RemoveAt(i);
                i--; // have to check the same index again now everything has shifted left
            }
        }
        yield return null;
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
     * GetAircraftList()
     * Returns: List<Aircraft>: All aircraft found by the latest query.
     */
    public List<Aircraft> GetAircraftList()
    {
        return allAircraft;
    }

    /**
     * GetAircraftByCallsign()
     * Returns: Aircraft: First aircraft in the list with a callsign that matches. Works with partial matches
     * Parameters:
     *     string callsign: The callsign to search for
     */
    public Aircraft GetAircraftByCallsign(string callsign)
    {
        return allAircraft.Find(x => x.callsign == callsign);
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

    public class Aircraft
    {
        public string callsign;
        public float longitude, latitude, altitude, last_altitude, velocity, true_track, vertical_rate, lastSeen;
    }

    private struct LatLongBBox
    {
        public float minLat, minLong, maxLat, maxLong;
    }
}
