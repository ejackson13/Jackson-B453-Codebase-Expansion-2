using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms.GameCenter;

public class MovableLight : MonoBehaviour
{

    public Light spotlight; // the light component attached to the gameobject
    [SerializeField] float wallDistance = 5f; // z position of wall
    [SerializeField] float maxDistance = 9f; // max possible distance from wall
    [SerializeField] float radiusScalingFactor = .8f; // the factor by which the radius of the spotlight is scaled by (since the way light works the spot light doesn't fill the whole expected radius at greater distances)

    // Start is called before the first frame update
    void Start()
    {
        if (spotlight == null)
        {
            spotlight = GetComponent<Light>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
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
        Bounds shadowBounds = shadow.GetComponent<Collider>().bounds;
        Vector3 shadowTopLeft = new Vector2(shadowBounds.min.x, shadowBounds.max.y);
        Vector3 shadowTopRight = new Vector2(shadowBounds.max.x, shadowBounds.max.y);

        // get top left and right bounds of spotlight on wall
        float spotRadius = SpotRadius(wallDistance);
        Vector3 circleTopLeft = new Vector2(transform.position.x, transform.position.y) + new Vector2(-1, 1).normalized * spotRadius;
        Vector3 circleTopRight = new Vector2(transform.position.x, transform.position.y) + new Vector2(1, 1).normalized * spotRadius;

        // check if the shadow is contained in the spotlight's radius using the top left and right values
        // don't check bottom bounds because at all relevant distances the light should touch the ground
        if (circleTopLeft.x <= shadowTopLeft.x && circleTopRight.x >= shadowTopLeft.x && circleTopLeft.y >= shadowTopLeft.y && circleTopRight.y >= shadowTopRight.y)
        {
            return true;
        }


        return false;
    }


    // checks if the spotlight encapsulates a given object and casts a new shadow of it on the wall
    // movableObject should be the object that the spotlight hits
    public bool SpotCastsShadow(GameObject movableObject)
    {
        return false;
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
