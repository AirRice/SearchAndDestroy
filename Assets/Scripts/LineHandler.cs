using System;
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
    public Material lineScanMat;
    private Material lineScanMatReversed;
    private bool showingScanState = false;
    
        
    public void SetupLine(Vector3[] positions, Material lineMat, Material lineActiveMat, Material lineScanMat, Node node1, Node node2)
    {
        this.lineMat = lineMat;
        this.lineActiveMat = lineActiveMat;
        this.lineScanMat = lineScanMat;
        lineScanMatReversed = new Material(lineScanMat);
        lineScanMatReversed.mainTextureScale = new Vector2(-1,1);
        this.node1 = node1;
        this.node2 = node2;
        gameObject.name = "nodeLine"+node1.nodeID+"-"+node2.nodeID;
        LineRenderer line = gameObject.GetComponent<LineRenderer>();
        line.material = lineMat;
        line.SetPositions(positions);
        showingScanState = false;
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
        if(showingScanState)
        {
            return;
        }
        LineRenderer line = gameObject.GetComponent<LineRenderer>();
        line.material = highlighted ? lineActiveMat : lineMat;
    }
    public void SetScanState(bool scanState, bool reversed = false)
    {
        showingScanState = scanState;
        LineRenderer line = gameObject.GetComponent<LineRenderer>();
        line.material = scanState ? (reversed ? lineScanMatReversed : lineScanMat) : lineMat;
    }
    public static implicit operator LineHandler(GameObject v)
    {
        throw new NotImplementedException();
    }
}
