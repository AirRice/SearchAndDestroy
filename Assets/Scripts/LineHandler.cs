using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//Convenience class for handling line drawing
public class LineHandler : MonoBehaviour
{
    public Station node1;
    public Station node2;
    public Material lineMat;
    public Material lineActiveMat;     
    
    
    
        
    public void SetupLine(Vector3[] positions, Material lineMat, Material lineActiveMat, Station node1, Station node2)
    {
        this.lineMat = lineMat;
        this.lineActiveMat = lineActiveMat;
        this.node1 = node1;
        this.node2 = node2;
        gameObject.name = "stationLine"+node1.stationID+"-"+node2.stationID;
        LineRenderer line = gameObject.GetComponent<LineRenderer>();
        line.material = lineMat;
        line.SetPositions(positions);
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void HighlightPath(bool highlighted = true)
    {
        LineRenderer line = gameObject.GetComponent<LineRenderer>();
        line.material = highlighted ? lineActiveMat : lineMat;
    }
}
