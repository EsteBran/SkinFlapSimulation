using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class ControlCam : MonoBehaviour
{
   
    public float rotationSpeed = 100.0f;
    float rotationX = 0;
    float rotationY = 0;
    public float speed = 5f;
    public Transform parent;
    // Start is called before the first frame update
    void Start()
    {
        Vector3 position = this.transform.position;
    }

    // Update is called once per frame
    void Update()
    {   
        if (Cutter.CamControl) {
        float mouseX = Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * rotationSpeed * Time.deltaTime;
        
        rotationX -= mouseY;
        rotationX = Mathf.Clamp(rotationX, -90f, 90f);

        
        parent.Rotate(Vector3.up * mouseX);// = Quaternion.Euler(0, rotationY, 0);
        transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
        // Vector3 rotateValue = new Vector3(mouseY, 0, 0);
        // transform.eulerAngles = transform.eulerAngles - rotateValue;
       // transform.Rotate(0, 90, 0);
        if (Input.GetKey(KeyCode.W)) {
            parent.position += transform.forward * speed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.S)) {
            parent.position -= transform.forward * speed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.D)) {
            parent.position += transform.right * speed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.A)) {
            parent.position -= transform.right * speed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.Space)) {
            parent.position += parent.up * speed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.LeftShift)) {
            parent.position -= parent.up * speed * Time.deltaTime;
        }
    }
    }
}
