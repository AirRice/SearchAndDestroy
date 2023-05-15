using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//Convenience class for handling line drawing
public class LineHandler : MonoBehaviour
{
    public Node node1;
    public Node node2;
    public Material lineMat;
    public Material lineActiveMat;     
    
    
    
        
    public void SetupLine(Vector3[] positions, Material lineMat, Material lineActiveMat, Node node1, Node node2)
    {
        this.lineMat = lineMat;
        this.lineActiveMat = lineActiveMat;
        this.node1 = node1;
        this.node2 = node2;
        gameObject.name = "nodeLine"+node1.nodeID+"-"+node2.nodeID;
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
