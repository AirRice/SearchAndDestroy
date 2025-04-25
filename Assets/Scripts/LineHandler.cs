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
    public Material lineMovedMat;
    private Material lineScanMatReversed;
    private bool showingScanState = false;
    private bool showingMovedState = false;
        
    public void SetupLine(Vector3[] positions, Material lineMat, Material lineActiveMat, Material lineScanMat, Material lineMovedMat, Node node1, Node node2)
    {
        this.lineMat = lineMat;
        this.lineActiveMat = lineActiveMat;
        this.lineScanMat = lineScanMat;
        lineScanMatReversed = new Material(lineScanMat)
        {
            mainTextureScale = new Vector2(-1, 1)
        };
        this.lineMovedMat = lineMovedMat;
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
        if(showingScanState || showingMovedState)
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
    public void SetMovedState (bool movedState)
    {
        showingMovedState = movedState;
        LineRenderer line = gameObject.GetComponent<LineRenderer>();
        line.material = movedState ? lineMovedMat : lineMat;
    }
    public static implicit operator LineHandler(GameObject v)
    {
        throw new NotImplementedException();
    }
}
