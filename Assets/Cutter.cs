using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class Cutter : MonoBehaviour
{   

    public Vector3 mousePos;
    float depth = 5f;
    public static bool CamControl = false;



    // Start is called before the first frame update
    void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Confined;  
    }

    // Update is called once per frame
    void Update()
    {   
        if (Input.GetKeyDown(KeyCode.Q)) {
            CamControl = !CamControl;
        }
        if (!CamControl) {
        mousePos = Input.mousePosition;
        
        depth += Input.mouseScrollDelta.y * 0.5f;
        depth = Mathf.Clamp(depth, 0.75f, 5f);
        mousePos.z = depth;
        mousePos = Camera.main.ScreenToWorldPoint(mousePos);
        //Debug.Log("x:" + mousePos.x.ToString() + " y:" + mousePos.y.ToString() + " z:" + depth);
        transform.position = mousePos;
        //mousePos -= new Vector3(32.0f, 32.0f, 32.0f);
        }
    }
}
