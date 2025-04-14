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
    private Vector3 offset = new Vector3(0,0.75f,0);

    private IEnumerator previousMove = null;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void SetPlayerMarker(bool set)
    {
        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        sr.enabled = set;
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
            GameObject nodeObject = Node.GetNodeObject(nodeid);
            if (nodeObject != null)
            {
                if (!nomove)
                    transform.position = nodeObject.transform.position + offset;
            }
        
        }
    }
    public void TrySmoothMove(int toMoveTo, int[] pathToHovered)
    {
        if(toMoveTo == currentNodeID)
            return; 
        int remainingMoves = GameController.gameController.currentPlayerMoves;
        IEnumerator _previousMove = this.previousMove;
        previousMove = MoveToPosition(_previousMove, Node.GetNodeObject(currentNodeID).transform.position + this.offset, pathToHovered, this.currentNodeID, toMoveTo, 0.65f);
        StartCoroutine(previousMove);
        //Debug.Log("Moving");
    }
    //Coroutine for smooth movement
    //TODO: Find way to make piece move over each node on path (may be solved?)
    public IEnumerator MoveToPosition(IEnumerator previousMove, Vector3 lastPos, int[] dests, int startNodeID, int finalDestID, float timeToMove)
    {   
        
        Vector3 startPos = lastPos;
        timeToMove = timeToMove*(float)Math.Pow(0.85f,Math.Max(dests.Length-1,1));
        foreach(int destID in dests)
        {

            Vector3 destPos = lastPos;
            if (destID == startNodeID)
                continue;
            else
            {
                GameObject nodeObject = Node.GetNodeObject(destID);
                destPos = nodeObject.transform.position + this.offset;

                float t = 0f;
                while(t < 1)
                {
                    t += Time.deltaTime / timeToMove;
                    transform.position = Vector3.Lerp(startPos, destPos, t);
                    yield return previousMove;
                }
            }
            startPos = destPos;
        }
    }
}
