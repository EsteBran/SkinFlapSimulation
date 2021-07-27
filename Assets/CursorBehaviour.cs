using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CursorBehaviour : MonoBehaviour
{

    private TrailRenderer trail;
    private Color cursorColor;
    SpriteRenderer spriteRenderer;

    //Cutting
    Color cut = new Color(255, 0, 0);

    //Not cutting
    Color nCut = new Color(0, 255, 0);

    
    void Start () {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Confined;
        Vector2 pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        transform.position = pos;
        trail = GetComponent<TrailRenderer>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.color = nCut;
    }


    // Update is called once per frame
    void Update()
    {
        Vector2 pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        transform.position = pos;
        if (Input.GetMouseButton(0)) {
            
            trail.enabled = true;
            spriteRenderer.color = cut;
        } else {
            trail.enabled = false;
            spriteRenderer.color = nCut;
        }
        
    }
}
