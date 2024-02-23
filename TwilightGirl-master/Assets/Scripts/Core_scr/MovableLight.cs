using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms.GameCenter;

public class MovableLight : MonoBehaviour
{

    public Light spotlight; // the light component attached to the gameobject
    [SerializeField] CapsuleCollider lightCylinder; // the cylinder used to check if an object intersects the light
    [SerializeField] float colliderLength = 10f; // the length of the cylinder (always the length of full area)
    [SerializeField] float wallDistance = 5f; // z position of wall
    [SerializeField] float maxDistance = 9f; // max possible distance from wall
    [SerializeField] float radiusScalingFactor = .8f; // the factor by which the radius of the spotlight is scaled by (since the way light works the spot light doesn't fill the whole expected radius at greater distances)

    [SerializeField] string movableObjectParentName = "Shadow Enhanced Objects"; // the name of the parent that all movable objects have (used to determine what we hit on raycasts)
    [SerializeField] string stationaryObjectParentName = "Diorama"; // the name of the parent that all unmovable objects have (used to determine what we hit on raycasts)
    [SerializeField] string stationaryObjectShadowColliderParentName = "Shadow Colliders"; // the name of the parent that the shadow colliders for unmovable objects are stored in (used to disable colliders of unmovable objects)

    [SerializeField] List<GameObject> intersectingObjects = new List<GameObject>();

    // Start is called before the first frame update
    void Start()
    {
        if (spotlight == null)
        {
            spotlight = GetComponent<Light>();
        }

        if (lightCylinder == null)
        {
            //lightCylinder = transform.GetChild(0).gameObject;
            lightCylinder = GetComponent<CapsuleCollider>();
        }
        colliderLength /= transform.parent.localScale.z;
        lightCylinder.height = colliderLength;
        Vector3 newCenter = lightCylinder.center;
        newCenter.z = -5;
        lightCylinder.center = newCenter;
    }

    // Update is called once per frame
    void Update()
    {
        // adjust size of collider to match radius of spotlight
        lightCylinder.radius = SpotRadius(wallDistance) / transform.parent.localScale.x;

        // adjust center of collider so that it always covers the whole stage (in the z direction)
        Vector3 newCenter = lightCylinder.center;
        newCenter.z = Mathf.Lerp(colliderLength/-2, colliderLength/2, (wallDistance - transform.position.z) / (colliderLength * transform.parent.localScale.z)) + .5f;
        lightCylinder.center = newCenter;


        // go through each intersecting item and see if its collider needs adjusted
        foreach (GameObject obj in intersectingObjects)
        {
            if (obj.transform.position.z <= transform.position.z)
            {
                RemoveCollider(obj);
            } else
            {
                AddCollider(obj);
            }
        }
    }



    private void RemoveCollider(GameObject obj)
    {
        // check if we hit the player
        if (obj.name == "Player")
        {
            // do nothing for now, but want to leave the door open for adding interactions between player and light in the future
            return;
        }
        // check if we hit a movable object
        else if (obj.transform.parent.name == movableObjectParentName)
        {
            // get the shadow colliders
            //GameObject shadow3D = obj.transform.GetChild(0).gameObject;
            GameObject shadow2D = obj.transform.GetChild(1).gameObject;

            // check if the spot light is big enough to encapsulate the object's shadow, if so, disable the collider, otherwise enable it (makes sure the collider is appropriately reactivated)
            shadow2D.GetComponent<Collider>().enabled = !SpotContainsShadowWholly(shadow2D);
        } 
        // check if we hit a stationary object 
        else if (obj.transform.parent.name == stationaryObjectParentName)
        {
            // get gameobject that holds shadow collider for the current object
            GameObject shadow2D = obj.transform.parent.parent.Find(stationaryObjectShadowColliderParentName).Find(obj.name).gameObject;

            // check if the spot light is big enough to encapsulate the object's shadow, if so, disable the collider, otherwise enable it (makes sure the collider is appropriately reactivated)
            shadow2D.GetComponent<Collider>().enabled = !SpotContainsShadowWholly(shadow2D);
        }
    }




    private void AddCollider(GameObject obj)
    {
        // check if we hit the player
        if (obj.name == "Player")
        {
            // do nothing for now, but want to leave the door open for adding interactions between player and light in the future
            return;
        }
    }



    // returns radius of circle that appears when the spotlight hits an object (based on distance to object and spotlight angle)
    public float SpotRadius(float dist)
    {
        float distToWall = dist - (transform.position.z); // get distance to wall
        float spotRadius = distToWall * Mathf.Tan(Mathf.Deg2Rad * (spotlight.spotAngle / 2)); // use trigonometry to calculate radius of circle of light on wall
        spotRadius *= Mathf.Lerp(1, radiusScalingFactor, (wallDistance - (transform.position.z)) / maxDistance); // scale radius so that bounds more closely match the 
        return spotRadius;
    }



    // takes the gameObject that determines a shadow's size and compares it to the spot radius of the light to see if the spotlight completely contains the shadow
    // shadow should be the object that contains the mesh renderer that actually casts the shadow (typically called Shadow in the hierarchy)
    public bool SpotContainsShadowWholly(GameObject shadow)
    {
        // get top left and right bounds of shadow collider
        shadow.GetComponent<Collider>().enabled = true; // quickly reenable and disable collider to check bounds
        Bounds shadowBounds = shadow.GetComponent<Collider>().bounds;
        shadow.GetComponent<Collider>().enabled = false;

        
        Vector3 adjustedMin = shadowBounds.min;
        adjustedMin.z = transform.position.z; // make sure we are only checking based on x and y value
        Vector3 adjustedMax = shadowBounds.max;
        adjustedMax.z = transform.position.z; // make sure we are only checking based on x and y value

        /*
        Debug.Log("Extents: " + shadowBounds.extents);
        Debug.Log("Adjusted min: " + adjustedMin + " | Closest point: " + lightCylinder.ClosestPoint(adjustedMin));
        Debug.Log("Adjusted max: " + adjustedMax + " | Closest point: " + lightCylinder.ClosestPoint(adjustedMax));
        */
        return lightCylinder.ClosestPoint(adjustedMax) == adjustedMax && lightCylinder.ClosestPoint(adjustedMin) == adjustedMin;
    }


    // checks if the spotlight encapsulates a given object and casts a new shadow of it on the wall
    // movableObject should be the object that the spotlight hits
    public bool SpotCastsShadow(GameObject movableObject)
    {
        return false;
    }


    private void OnTriggerEnter(Collider other)
    {
        // add to list of intersecting objects if it is not in it. Also avoid adding the wall and the grass floor to the list because we never want to mess with their colliders
        if (!intersectingObjects.Contains(other.gameObject) && other.gameObject.name != "Grassy Terrain" && other.gameObject.name != "Wall")
        {
            intersectingObjects.Add(other.gameObject);
        }
    }


    private void OnTriggerExit(Collider other)
    {
        // remove the object from the list as soon as it is no longer intersecting the collider
        if (intersectingObjects.Contains(other.gameObject))
        {
            intersectingObjects.Remove(other.gameObject);
        }
    }


    private void OnDrawGizmos()
    {
        // draw radius that we calculate for the spotlight to check if it is accurate
        Vector3 wallPos = new Vector3(transform.position.x, transform.position.y, wallDistance);
        float spotRadius = SpotRadius(wallDistance);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(wallPos, spotRadius);

        // draw top left and right values that we calculate for SpotContainsShadowWholly to make sure they are correct
        Gizmos.color = Color.red;
        Vector3 circleTopLeft = new Vector2(transform.position.x, transform.position.y) + new Vector2(-1, 1).normalized * spotRadius;
        circleTopLeft.z = wallDistance;
        Vector3 circleTopRight = new Vector2(transform.position.x, transform.position.y) + new Vector2(1, 1).normalized * spotRadius;
        circleTopRight.z = wallDistance;
        Gizmos.DrawLine(wallPos, circleTopLeft);
        Gizmos.DrawLine(wallPos, circleTopRight);
    }
}
