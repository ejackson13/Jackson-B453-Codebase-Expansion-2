using UnityEngine;

namespace TG.Core
{
    public class ShadowScaler : MonoBehaviour
    {
        [SerializeField] float shadowMaxSize = 2f;
        [SerializeField] float maxDistance = 9f;
        [SerializeField] float wallDistance = 5f;
        [SerializeField] Transform shadow3D = null;
        [SerializeField] Transform shadow2D = null;
        [SerializeField] string terrainTag = "Terrain";
        [SerializeField] string playerTag = "Player";

        private bool scalingDisabled = false; // used to prevent this script from updating scaling when a light is shining on the object (we want the light script to handle this)

        bool isPlayer;
        float stuckZPosition;
        ShadowTerrainCreator terrainCreator;

        public float ShadowXScale { get => shadow2D.localScale.x; }

        private void Awake()
        {
            terrainCreator = GameObject.FindGameObjectWithTag(terrainTag).GetComponent<ShadowTerrainCreator>();
            isPlayer = transform.CompareTag(playerTag);

            SetInitialZPosition();
        }

        private void SetInitialZPosition()
        {
            Vector3 shadowColliderStartPosition = transform.position;
            shadowColliderStartPosition.z = terrainCreator.GetDepth;
            shadow2D.position = shadowColliderStartPosition;
            stuckZPosition = shadow2D.position.z;
        }

        private void FixedUpdate()
        {
            StuckColliderZPosition();
            ScaleShadowByDistance();
        }

        private void StuckColliderZPosition()
        {
            Vector3 zClampedPosition = shadow2D.position;
            zClampedPosition.z = stuckZPosition;
            shadow2D.position = zClampedPosition;
        }

        private void ScaleShadowByDistance()
        {
            float lerpT = (wallDistance - transform.position.z - transform.localScale.z / 2) / maxDistance;
            float scaleByDistance = Mathf.Lerp(1, shadowMaxSize, lerpT);

            Vector3 shadowScale = Vector3.one * scaleByDistance;
            shadowScale.z = shadow3D.localScale.z;

            // check if scaling should be handled by movablelight script
            if (!scalingDisabled)
            {
                shadow2D.localScale = shadowScale;
            }

            if (isPlayer) { shadowScale.z = scaleByDistance; }

            shadow3D.localScale = shadowScale;
        }


        public void DisableScaling()
        {
            scalingDisabled = true;
        }


        public void EnableScaling()
        {
            scalingDisabled = false;
        }


        public bool isScalingEnabled()
        {
            return scalingDisabled;
        }
    }
}