using System.Collections;
using UnityEngine;

public class Location
{
    /**
     * Start()
     * Initialize location services
     */
    public static IEnumerator Start()
    {

        yield return new WaitForSeconds(5);

        if (!Input.location.isEnabledByUser)
            yield break;
        Input.location.Start(1);

        int maxWait = 20; // wait 20 seconds max to start location services
        while(Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1);
            maxWait--;
        }

        if(maxWait < 1)
        {
            Debug.Log("Timed out while initializing location!");
            yield break;
        }

        if(Input.location.status == LocationServiceStatus.Failed)
        {
            Debug.Log("Unable to determine device location!");
            yield break;
        }
        else
        {
            Debug.Log("Location initialized successfully.");
        }
    }

    /**
     * GetUserCoords()
     * Get last known user coordinates as a Vector2
     * Returns: Vector2: User coordinates in lat, long format.
     * Returns: Vector2: 0, 0 if location service is not operating
     */
    public static Vector2 GetUserCoords()
    {
        if (!(Input.location.status == LocationServiceStatus.Running))
            return new Vector2(0, 0);
        else
            return new Vector2(Input.location.lastData.latitude, Input.location.lastData.longitude);
    }

    /**
     * LocationAvailable()
     * Are location services available
     * Returns: bool: True is location services are available
     */
    public static bool LocationAvailable()
    {
        return Input.location.status == LocationServiceStatus.Running;
    }

    public static float GetCompassHeading()
    {
        return Input.compass.trueHeading;
    }
}