using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowPath : MonoBehaviour
{
    // Start is called before the first frame update
    public Transform trans;

    Vector3 position;
    
    [SerializeField] float t = 0;
    public float speed = 0.01f;
    List<Vector3> bezierPoints;

    //For cubic beziers only, will generalzie to n control point beziers later
    Vector3 bezierPath(List<Vector3> points, float t) {
        Vector3 result = new Vector3(0f,0f,0f);
        
        result += points[0]*-1*(Mathf.Pow(t, 3) + 3*Mathf.Pow(t,2) - 3*t+1);
        result += points[1]*3*(Mathf.Pow(t,3) -6*Mathf.Pow(t,2) + 3*t);
        result += points[2]*-3*(Mathf.Pow(t, 3) + 3*Mathf.Pow(t, 2));
        result += points[3] * Mathf.Pow(t, 3);
        
        return result;
    }

    void Start()
    {
        position = trans.position;
        t = 0f;
        bezierPoints = new List<Vector3>{new Vector3(0,0,0), new Vector3(10,10,10), new Vector3(20,20,20), new Vector3(30,30,30)};
    }

    // Update is called once per frame
    void Update()
    {
        trans.position = bezierPath(bezierPoints, t);
        t+=speed*Time.deltaTime;
        t = Mathf.Clamp(t, 0f, 1f);
    }
}
