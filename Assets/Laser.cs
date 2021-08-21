 using UnityEngine;
 using System.Collections;
 
 public class Laser : MonoBehaviour
 {
     public LineRenderer laserLineRenderer;
     public float laserWidth = 0.1f;
     public float laserMaxLength = 5f;
     public static  Vector3 laserPos;
     public static Vector3 laserDir;
 
     void Start() {
        Vector3[] initLaserPositions = new Vector3[ 2 ] { Vector3.zero, Vector3.zero };
        laserLineRenderer.SetPositions( initLaserPositions );
        //laserLineRenderer.SetWidth( laserWidth, laserWidth );
        laserPos = transform.position;
        laserDir = transform.right;
     }
 
     void Update() 
     {
         if( true || Input.GetKey( KeyCode.Z ) ) {
             ShootLaserFromTargetPosition( transform.position, transform.right, laserMaxLength );
             Debug.Log(transform.right);
             laserLineRenderer.enabled = true;
         }
         else {
             laserLineRenderer.enabled = false;
         }
         laserDir = transform.right;
         laserPos = transform.position;
     }
 
     void ShootLaserFromTargetPosition( Vector3 targetPosition, Vector3 direction, float length )
     {
         Ray ray = new Ray( targetPosition, direction );
         RaycastHit raycastHit;
         Vector3 endPosition = targetPosition + ( length * direction );
 
         if( Physics.Raycast( ray, out raycastHit, length ) ) {
             endPosition = raycastHit.point;
         }
 
         laserLineRenderer.SetPosition( 0, targetPosition );
         laserLineRenderer.SetPosition( 1, endPosition );
     }
 }