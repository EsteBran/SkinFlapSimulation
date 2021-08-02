using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cutter : MonoBehaviour
{   

    private Vector3 mousePos;
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
        Debug.Log("x:" + mousePos.x.ToString() + " y:" + mousePos.y.ToString() + " z:" + depth);
        transform.position = mousePos;
        }
    }
}
