using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaserPointerVertical : MonoBehaviour
{
    // Can interact with
    public LayerMask layerMask;
    
    // can not interact with
    public LayerMask notAllowedLayerMask;

    private bool doAction = false;

    public GameObject laserPrefab;
    public GameObject reticlePrefab;

    private GameObject laser;
    private GameObject reticle;
    
    //private Valve.VR.SteamVR_TrackedObject source;
    public GameObject source;
    // Start is called before the first frame update
    void Start()
    {
        laser = Instantiate(laserPrefab);
        reticle = Instantiate(reticlePrefab);
    }

    RaycastHit hit;
    public Vector3 vrReticleOffset;

    // Update is called once per frame
    void Update()
    {
        bool buttonpressed = false;

        if (buttonpressed)
        {
            doAction = false;

            // Ray from source.
            if ( Physics.Raycast( source.transform.position, transform.up, out hit, 100, layerMask ) )
            {
                // Are we hitting something on acceptable layer?
                doAction = !LayerMatchTest( notAllowedLayerMask, hit.collider.gameObject );

                if (doAction)
                {
                    PointLaser();
                }
                else
                {
                    DisplayLaser(false);
                }
            }
        }
        else
        {
            // Hide laser 
            DisplayLaser(false);
        }

        if (buttonpressed)
        {
            // TeleportToNewPosition
            //Reset();
        }
    }

    private void PointLaser()
    {
        DisplayLaser(true);

        // Position laser between controller and point where raycast hits. Use Lerp because you can
        // give it two positions and the % it should travel. If you pass it .5f, which is 50%
        // you get the precise middle point.
        laser.transform.position = Vector3.Lerp(source.transform.position, hit.point, .25f);

        // Point the laser at position where raycast hits.
        laser.transform.LookAt( hit.point);

        // Scale the laser so it fits perfectly between the two positions
        laser.transform.localScale = new Vector3(laser.transform.localScale.x, 
            laser.transform.localScale.y, hit.distance);
        
    reticle.transform.position = hit.point;// + vrReticleOffset;

    }

    private static bool LayerMatchTest(LayerMask layerMask, GameObject obj)
    {
        return ( ( 1 << obj.layer ) & layerMask ) != 0;
    }

    private void DisplayLaser(bool show)
    {
        laser.SetActive(show);
        reticle.SetActive(show);
    }
}
