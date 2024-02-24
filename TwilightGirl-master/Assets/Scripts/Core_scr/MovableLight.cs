using System.Collections;
using System.Collections.Generic;
using TG.Core;
using UnityEngine;
using UnityEngine.SocialPlatforms.GameCenter;
using UnityEngine.UI;

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
        // set collider correctly
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
                // try to remove colliders of objects behind light
                RemoveCollider(obj);
            } else
            {
                // try to alter scaling and position of objects in front of light
                AlterCollider(obj);
            }
        }
    }



    private void RemoveCollider(GameObject obj)
    {
        // check if we hit the player
        if (obj.name == "Player" || obj.transform.parent == null)
        {
            // do nothing for now, but want to leave the door open for adding interactions between player and light in the future
            return;
        }
        // check if we hit a movable object
        else if (obj.transform.parent.name == movableObjectParentName)
        {
            // enable scaling
            obj.GetComponent<ShadowScaler>().EnableScaling();

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




    private void AlterCollider(GameObject obj)
    {
        // check if we hit the player
        if (obj.name == "Player" || obj.transform.parent == null)
        {
            // do nothing for now, but want to leave the door open for adding interactions between player and light in the future
            return;
        }
        // check if we hit a movable object
        else if (obj.transform.parent.name == movableObjectParentName)
        {
            // get the objects that represent the shadow
            GameObject shadow3D = obj.transform.GetChild(0).gameObject;
            GameObject shadow2D = obj.transform.GetChild(1).gameObject;

            // make sure the collider is reactivated as soon as it's not covered anymore
            shadow2D.GetComponent<Collider>().enabled = true;

            // check if the object is fully covered by the light
            if (SpotCastsShadow(obj))
            {
                // get dist from light to object
                float distToObj = obj.GetComponent<MeshRenderer>().bounds.min.z - transform.position.z;

                // disable scaling so that this script controls the scaling while the light hits the obj (don't want the ShadowScaler script to overwrite)
                obj.GetComponent<ShadowScaler>().DisableScaling();

                // update scale of shadow collider
                // we don't change the scaling of the mesh that casts the shadow because unmovable objects don't have that scalable mesh and we don't want the shadow sizes to be really inconsistent
                Vector2 scaleByDistance = CalculateScale(shadow3D.GetComponent<MeshRenderer>().bounds);

                // copy code from ShadowScaler script to scale the collider
                Vector3 shadowScale = shadow3D.transform.localScale * scaleByDistance;
                shadowScale.z = shadow3D.transform.localScale.z;
                Debug.Log("Original Scale: " + shadow3D.transform.localScale + " | Scaling factors: " + scaleByDistance + " | New Scale: " + shadowScale);
                shadow2D.transform.localScale = shadowScale;

                // update the position of the shadow collider to match the angle the light hits it at
                float hitAngle = Mathf.Atan((obj.transform.position.x - transform.position.x) / distToObj); // use trig to get angle from light to center of object
                //float hitAngle = Mathf.Atan((obj.transform.position.x - transform.position.x) / (obj.transform.position.z - transform.position.z)); // use trig to get angle from light to center of object
                float deltaX = (wallDistance - transform.position.z) * hitAngle; // use trig to get the new change in x (relative to light position)

                Vector3 newPos = shadow2D.transform.position;
                newPos.x = transform.position.x + deltaX;
                shadow2D.transform.position = newPos;

            }
            else
            {
                // enable scaling
                obj.GetComponent<ShadowScaler>().EnableScaling(); // this won't necessarily reset the scale immediately, but it should be reset by the ShadowScaler script by the next frame

                // reset position
                Vector3 newPos = shadow2D.transform.position;
                newPos.x = shadow3D.transform.position.x;
                shadow2D.transform.position = newPos;

                // check if light still covers shadow
                shadow2D.GetComponent<Collider>().enabled = !SpotContainsShadowWholly(shadow2D);
            }


     
        }
        // check if we hit a stationary object 
        else if (obj.transform.parent.name == stationaryObjectParentName)
        {
            // get gameobject that holds shadow collider for the current object
            GameObject shadow2D = obj.transform.parent.parent.Find(stationaryObjectShadowColliderParentName).Find(obj.name).gameObject;

            // make sure the collider is reactivated as soon as it's not covered anymore
            shadow2D.GetComponent<Collider>().enabled = true;

            // check if the object is fully covered by the light
            if (SpotCastsShadow(obj))
            {
                // get dist from light to object
                float distToObj = obj.GetComponent<MeshRenderer>().bounds.min.z - transform.position.z;

                // update scale of shadow collider
                Vector2 scaleByDistance = CalculateScale(obj.GetComponent<MeshRenderer>().bounds);


                // copy code from ShadowScaler script to scale the collider
                Vector3 shadowScale = (obj.transform.localScale * scaleByDistance);
                shadowScale.z = shadow2D.transform.localScale.z; // hold z constant (I believe this keeps the collider from poking through the wall)
                shadow2D.transform.localScale = shadowScale;

                // update the position of the shadow collider to match the angle the light hits it at
                float hitAngle = Mathf.Atan((obj.transform.position.x - transform.position.x) / distToObj); // use trig to get angle from light to center of object
                //float hitAngle = Mathf.Atan((obj.transform.position.x - transform.position.x) / (obj.transform.position.z - transform.position.z)); // use trig to get angle from light to center of object
                float deltaX = (wallDistance - transform.position.z) * hitAngle; // use trig to get the new change in x (relative to light position)

                Vector3 newPos = shadow2D.transform.position;
                newPos.x = transform.position.x + deltaX;
                newPos.y = obj.transform.position.y + (shadow2D.GetComponent<Collider>().bounds.extents.y-obj.GetComponent<Collider>().bounds.extents.y); // move y value up so that the collider is close to grounded
                shadow2D.transform.position = newPos;
            }
            else
            {
                // reset scale
                shadow2D.transform.localScale = new Vector3(obj.transform.localScale.x, obj.transform.localScale.y, obj.transform.localScale.z);

                // reset position
                Vector3 newPos = shadow2D.transform.position;
                newPos.x = obj.transform.position.x;
                newPos.y = obj.transform.position.y;
                shadow2D.transform.position = newPos;

                // check if light still covers shadow
                shadow2D.GetComponent<Collider>().enabled = !SpotContainsShadowWholly(shadow2D);
            }
        }
    }



    // returns radius of circle that appears when the spotlight hits an object (based on distance to object and spotlight angle)
    // argument is the z value of the point the light is hitting
    public float SpotRadius(float hitZ)
    {
        float distToHit = hitZ - (transform.position.z); // get distance to wall
        float spotRadius = distToHit * Mathf.Tan(Mathf.Deg2Rad * (spotlight.spotAngle / 2)); // use trigonometry to calculate radius of circle of light on wall
        spotRadius *= Mathf.Lerp(1, radiusScalingFactor, (wallDistance - (transform.position.z)) / maxDistance); // scale radius so that bounds more closely match the 
        return spotRadius;
    }



    // takes the gameObject that determines a shadow's size and compares it to the spot radius of the light to see if the spotlight completely contains the shadow
    // shadow should be the object that contains the mesh renderer that actually casts the shadow (typically called Shadow in the hierarchy)
    public bool SpotContainsShadowWholly(GameObject shadow)
    {
        // get bounds of shadow collider
        shadow.GetComponent<Collider>().enabled = true; // quickly reenable and disable collider to check bounds
        Bounds shadowBounds = shadow.GetComponent<Collider>().bounds;
        shadow.GetComponent<Collider>().enabled = false;

        // get min and max bounds
        Vector3 adjustedMin = shadowBounds.min;
        adjustedMin.z = transform.position.z; // make sure we are only checking based on x and y value
        Vector3 adjustedMax = shadowBounds.max;
        adjustedMax.z = transform.position.z; // make sure we are only checking based on x and y value

        // check if both the min bound and the max bound are in the collider
        return lightCylinder.ClosestPoint(adjustedMax) == adjustedMax && lightCylinder.ClosestPoint(adjustedMin) == adjustedMin;
    }


    // checks if the spotlight encapsulates a given object and casts a new shadow of it on the wall
    // obj should be the object the light is directly behind
    public bool SpotCastsShadow(GameObject obj)
    {
        // get bounds of object
        Bounds objBounds = obj.GetComponent<MeshRenderer>().bounds;

        // get min and max bounds
        Vector3 adjustedMin = objBounds.min;
        Vector3 adjustedMax = objBounds.max;
        adjustedMax.z = objBounds.min.z; // make sure we are only checking based on x and y value

        // calculate size of circle that would hit this object based on dist to obj (make sure to use min z so that we aren't calculating based on center of object)
        float hitRad = SpotRadius(objBounds.min.z);

        // determine if the size of the circle covers the bounds of the obj
        // method to do this from this thread https://forum.unity.com/threads/how-can-i-check-if-there-exists-something-in-a-circle.688237/
        Vector3 adjustedHitPos = transform.position;
        adjustedHitPos.z = objBounds.min.z;
        return Vector3.Distance(adjustedHitPos, adjustedMin) <= hitRad && Vector3.Distance(adjustedHitPos, adjustedMax) <= hitRad; // check that the min and max points are within radius of the center
    }


    // calculates the bounds should be scaled up to match the shadow on the wall
    // returns a vector2 where x is the scaling factor for the x axis and y is the scaling factor for the y axis
    private Vector2 CalculateScale(Bounds bounds)
    {
        float zDist = bounds.center.z - transform.position.z; // will use center of bounding box for distance, should be relatively minor difference compared to using the min z
        float distToWall = wallDistance - transform.position.z;

        // calculate new center
        Vector2 newCenter = bounds.center;
        float cXAngle = Mathf.Atan((bounds.center.x - transform.position.x) / zDist);
        float newXCent = distToWall * cXAngle;
        newCenter.x = newXCent;

        float cYAngle = Mathf.Atan((bounds.center.y - transform.position.y) / zDist);
        float newYCent = distToWall * cYAngle;
        newCenter.y = newYCent;

        // calculate new x extents
        float xAngle = Mathf.Atan((bounds.max.x - transform.position.x) / zDist); 
        float newXMax = distToWall * xAngle;
        float newXExtents = newXMax - newCenter.x;

        // calculate new y extents
        float yAngle = Mathf.Atan((bounds.max.y - transform.position.y) / zDist);
        float newYMax = distToWall * yAngle; 
        float newYExtents = newYMax - newCenter.y;

        // calculate degree to which x and y have been scaled up
        return new Vector2 (newXExtents/bounds.extents.x, newYExtents/bounds.extents.y);
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

        Gizmos.color = Color.magenta;
        //DrawGizmosForObjectsInFront();
    }



    private void DrawGizmosForObjectsInFront()
    {
        foreach (GameObject obj in intersectingObjects)
        {
            if (obj.name == "Player")
            {
                continue;
            }
            if (obj.transform.position.z > transform.position.z)
            {
                Vector3 sphereCenter = transform.position;
                float zVal = obj.GetComponent<MeshRenderer>().bounds.min.z;
                sphereCenter.z = zVal;
                Gizmos.DrawWireSphere(sphereCenter, SpotRadius(zVal));
            }
        }
    }
}
