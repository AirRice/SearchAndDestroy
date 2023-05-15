using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerPiece : MonoBehaviour
{
    public int playerID = 0;
    public Material hunterMat;
    public Material hiddenMat;
    public int currentNodeID = 0;
    private int lastNodeID = 0;
    private Vector3 offset = new Vector3(0,0.75f,0);
    private Dictionary<int, int[]> cachedPaths = new Dictionary<int, int[]>();

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(lastNodeID!=currentNodeID)
        {
            lastNodeID = currentNodeID;
            //Wipe the cached Paths from the previous location as it has now changed
            cachedPaths = new Dictionary<int, int[]>();
            //Debug.Log($"Wiped path caches for player{playerID}, new lastnode = {lastNodeID}, current = {currentNodeID}");
        }
    }
    // Try handling path checks
    public void TryPathHighlight(Node destNode, bool toHighlight=true)
    {   
        int[] pathToHovered;
        int remainingMoves = GameController.gameController.currentPlayerMoves;
        Debug.Log(cachedPaths.ContainsKey(destNode.nodeID));
        if(!cachedPaths.TryGetValue(destNode.nodeID, out pathToHovered))
        {
            pathToHovered = GameController.gameController.searchPath(Node.getNode(this.currentNodeID), destNode);
            Debug.Log($"Added cached path to node {destNode.nodeID}, path:"+string.Join(" ",pathToHovered));
            cachedPaths.Add(destNode.nodeID,pathToHovered);
        }
        if(pathToHovered.Length > 1)
        {
            for(int i = 0; i < pathToHovered.Length-1; i++)
            {   
                int node1 = pathToHovered[i];
                int node2 = pathToHovered[i+1];
                //Debug.Log("nodeLine"+node2+"-"+node1);
                Node lineDestNode = Node.getNode(node2);
                GameObject lineDrawer = GameObject.Find("nodeLine"+node1+"-"+node2);
                if(lineDrawer==null)
                    lineDrawer = GameObject.Find("nodeLine"+node2+"-"+node1);
                    if(lineDrawer==null)
                        break;
                lineDrawer.GetComponent<LineRenderer> ().material = (toHighlight && remainingMoves > 0) ? lineDestNode.lineActiveMat : lineDestNode.lineMat;
                remainingMoves--;
            }
        }
    }
    public void SetHidden(bool hidden)
    {
        if(!hidden)
        {
            gameObject.GetComponent<MeshRenderer> ().material = hunterMat;
        }
        else
        {
            gameObject.GetComponent<MeshRenderer> ().material = hiddenMat;
        }
    }
    public void setNode(int nodeid, bool nomove = false)
    {
        if (nodeid != currentNodeID)
        {
            currentNodeID = nodeid;
            GameObject nodeObject = Node.getNodeObject(nodeid);
            if (nodeObject != null)
            {
                if (!nomove)
                    transform.position = nodeObject.transform.position + offset;
            }
        
        }
    }
    public void TrySmoothMove(int toMoveTo)
    {
        if(toMoveTo == currentNodeID)
            return;
        int[] pathToHovered;
        if(!cachedPaths.TryGetValue(toMoveTo, out pathToHovered))
            pathToHovered = GameController.gameController.searchPath(Node.getNode(this.currentNodeID), Node.getNode(toMoveTo));
        int remainingMoves = GameController.gameController.currentPlayerMoves;
        StartCoroutine(MoveToPosition(this.transform.position, pathToHovered, this.currentNodeID, toMoveTo, 1.5f));
        //Debug.Log("Moving");
    }
    //Coroutine for smooth movement
    //TODO: Find way to make piece move over each node on path (may be solved?)
    public IEnumerator MoveToPosition(Vector3 lastPos, int[] dests, int startNodeID, int finalDestID, float timeToMove)
    {
        Vector3 startPos = lastPos;
        timeToMove = timeToMove/Math.Max(dests.Length,1);
        foreach(int destID in dests)
        {

            Vector3 destPos = lastPos;
            if (destID == startNodeID)
                continue;
            else
            {
                GameObject nodeObject = Node.getNodeObject(destID);
                destPos = nodeObject.transform.position + this.offset;

                float t = 0f;
                while(t < 1)
                {
                    t += Time.deltaTime / timeToMove;
                    transform.position = Vector3.Lerp(startPos, destPos, t);
                    yield return null;
                }
            }
            startPos = destPos;
        }
        
    }
}
