﻿using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class TrackPlane : MonoBehaviour
{
    public int queryFrequency; // how many seconds between each query
    public int queryDistance; // distance to search in miles

    private float queryTimer; // time since last query
    private const float MILES_TO_LAT = 0.0144927536232f; // 1/69, One degree of latitude = ~69mi
    private const float MILES_TO_METERS = 1609.34f;
    private List<Aircraft> allAircraft;

    void Start()
    {
        allAircraft = new List<Aircraft>();
    }

    void Update()
    {
        queryTimer += Time.deltaTime;
        if (queryTimer > queryFrequency)
        {
            queryTimer = 0;
            StartCoroutine(Query(queryDistance, new Vector2(0, 0))); // TODO: PUT AIRCRAFT COORDINATES HERE
        }
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
        catch (System.IndexOutOfRangeException e)
        {
            yield break;
        }
        string[] aircraft = states.Split(new[] { "],[" }, System.StringSplitOptions.None);
        float timeNow = Time.time;
        foreach (string x in aircraft)
        {
            string[] attributes = x.Replace("[", "").Replace("\"", "").Replace("]]}", "").Trim().Split(','); // remove [ " and ]]} from the strings then split into an array at ,
            try
            {
                Aircraft a = allAircraft[allAircraft.FindIndex(y => y.callsign == attributes[1])];
                a.position = new Vector2(float.Parse(attributes[6]), float.Parse(attributes[5])); // https://opensky-network.org/apidoc/rest.html
                a.altitude = string.Equals(attributes[7], "null") || float.Parse(attributes[7]) < 7 ? 0 : float.Parse(attributes[7]); // sometimes these values are null (eg. if aircraft on ground)
                a.velocity = string.Equals(attributes[9], "null") ? 0 : float.Parse(attributes[9]);
                a.true_track = string.Equals(attributes[11], "null") ? 0 : float.Parse(attributes[11]);
                a.lastSeen = timeNow;
            }
            catch (System.ArgumentOutOfRangeException)
            {
                Aircraft a = new Aircraft
                {
                    callsign = attributes[1],
                    position = new Vector2(float.Parse(attributes[6]), float.Parse(attributes[5])),
                    altitude = string.Equals(attributes[7], "null") ? 0 : float.Parse(attributes[7]),
                    velocity = string.Equals(attributes[9], "null") ? 0 : float.Parse(attributes[9]),
                    true_track = string.Equals(attributes[11], "null") ? 0 : float.Parse(attributes[11]),
                    lastSeen = timeNow
                };
                allAircraft.Add(a);
            }
        }
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

    public class Aircraft
    {
        public string callsign;
        public float altitude, velocity, true_track, vertical_rate, lastSeen;
        public Vector2 position;
        public Vector3 normalizedPosition;
    }

    private struct LatLongBBox
    {
        public float minLat, minLong, maxLat, maxLong;
    }
}