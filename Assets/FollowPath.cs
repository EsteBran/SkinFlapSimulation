using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class FollowPath : MonoBehaviour
{
    // Start is called before the first frame update
    public Transform trans;

    Vector3 position;
    public bool drawLine = true;

    public int res = 10;
    [SerializeField] float t = 0;
    public float speed = 1f;
    List<Vector3> bezierPoints;
    
    LineRenderer curve;

    float val = Mathf.PI/2;

    public GameObject spherePoint;

    List<Vector3> readPoints(string filePath) {
        char[] delimiterChars = { ' ', ',', ':', '(', ')' };
        List<Vector3> points = new List<Vector3>();
        StreamReader sr = new StreamReader(filePath);
        string line = "";
        while ((line = sr.ReadLine()) != null) {
            
            string[] words = line.Split(delimiterChars);
            List<float> dims = new List<float>();
            foreach (var word in words)
            {
                
                if (word.Length != 0) dims.Add(float.Parse(word));
                //points.Add(new Vector3(float.Parse(words[0]), float.Parse(words[1]), float.Parse(words[2])));
            }
            for (int i = 0; i < dims.Count; i+=3) {
                points.Add(new Vector3(dims[i], dims[i+1], dims[i+2]));
            }
            
            //points.Add(new Vector3(float.Parse(words[0]), float.Parse(words[1]), float.Parse(words[2])));
        }
        
        return points;
    }


    Vector3 nBezierPath(List<Vector3> points, float t) {
        
        if (points.Count == 2) return Vector3.Lerp(points[0], points[1], t);
        List<Vector3> first = new List<Vector3>();
        for (int p = 0; p < points.Count-1; p++) {
            first.Add(Vector3.Lerp(points[p], points[p+1], t));
        }
        return nBezierPath(first, t);
    }
    //For cubic beziers only, will generalzie to n control point beziers later
    Vector3 bezierPath(List<Vector3> points, float t) {
        Vector3 result = new Vector3(0f,0f,0f);
        
        // result += points[0]*-1*(Mathf.Pow(t, 3) + 3*Mathf.Pow(t,2) - 3*t+1);
        // result += points[1]*3*(Mathf.Pow(t,3) -6*Mathf.Pow(t,2) + 3*t);
        // result += points[2]*-3*(Mathf.Pow(t, 3) + 3*Mathf.Pow(t, 2));
        // result += points[3] * Mathf.Pow(t, 3);
        result = points[0]*Mathf.Pow(1-t,3) + points[1]*3*Mathf.Pow(1-t, 2)*t + points[2]*3*(1-t)*t*t + points[3]*t*t*t;
        
        return result;
    }


    List<Vector3> split(int n) {
    List<float> acc = new List<float>();               // n + 1 accumulated lengths
    float len = 0;                // overall length
    Vector3 p = bezierPoints[0];              
    
    List<Vector3> res = new List<Vector3>();               // array of n + 1 result points
    
    // find segemnt and overall lengths
    
    for (int i = 0; i < bezierPoints.Count; i++) {
        Vector3 q = bezierPoints[i];
        len += Mathf.Sqrt(Mathf.Pow(q.x-p.x,2) + Mathf.Pow(q.y-p.y,2) + Mathf.Pow(q.z-p.z,2));
        acc.Add(len);

        p = q;
    }
    
    acc.Add(2 * len);          // sentinel

    int curr = 0;
    int next = 1;
    
    // create equidistant result points
    
    for (int i = 0; i < n; i++) {
        float z = len * i / n;        // running length of point i
        
        // advance to current segment
        
        while (z > acc[next]) {
            curr++;
            next++;
        }
        
        // interpolate in segment
                
        Vector3 a = bezierPoints[curr];
        Vector3 q = bezierPoints[next];
        
        float t = (z - acc[curr]) / (acc[next] - acc[curr]);
        
        res.Add(new Vector3(a.x * (1 - t) + q.x * t,
                           a.y * (1 - t) + q.y * t,
                           a.z * (1 - t) + q.z * t));
    }
    
    // push end point (leave out when joining consecutive segments.)
    
    res.Add(bezierPoints[bezierPoints.Count - 1]);
    
    return res;
}
    void drawCurve(int res) {
        
        
        
       if (res%2 == 0) res += 1;
        float inc = 1.0f/res;
        
        List<Vector3> pointsToDraw = new List<Vector3>();
        float t = 0;
        
        while(t <= 1.0) {
            pointsToDraw.Add(nBezierPath(bezierPoints, t));
            t += inc;
            
        }

        for (int p = 0; p < bezierPoints.Count; p++) {
            Instantiate(spherePoint, bezierPoints[p], Quaternion.identity);
        }
        
        
        //List<Vector3> pointsToDraw = split(points, res);
        curve.positionCount = pointsToDraw.Count;
        curve.material = new Material(Shader.Find("Sprites/Default"));
        curve.startColor = new Color(0, 255, 0);
        curve.endColor = curve.startColor; 
        curve.widthMultiplier = 0.1f;
        for (int p = 0; p < pointsToDraw.Count; p++) {
            
            curve.SetPosition(p, pointsToDraw[p]);
            
        }
    }

    void Start()
    {   
        readPoints("Assets/controlPoints.txt");
        curve = GameObject.Find("Line").AddComponent<LineRenderer>();
        position = trans.position;
        
        t = 0f;
        // bezierPoints = new List<Vector3>{GameObject.Find("P1").transform.position, 
        // GameObject.Find("P2").transform.position, 
        // GameObject.Find("P3").transform.position,
        //  GameObject.Find("P4").transform.position,
        //  GameObject.Find("P5").transform.position,
        //  GameObject.Find("P6").transform.position};

        bezierPoints = readPoints("Assets/controlPoints.txt");
        trans.position = bezierPoints[0];
        if (drawLine) drawCurve(res);
    }

    // Update is called once per frame
    void Update()
    {
        trans.position = nBezierPath(bezierPoints, t);
        val+=speed*Time.deltaTime;
        t = 0.5f*(Mathf.Sin(val)+1.0f);
        t = Mathf.Clamp(t, 0f, 1f);
    }
}
