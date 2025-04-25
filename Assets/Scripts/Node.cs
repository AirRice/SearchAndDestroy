using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;
using System;

public class Node : MonoBehaviour
{
    public int nodeID = 0;
    public LineHandler lineHandlerPrefab;
    public Material lineMovedMat;
    public Material lineScanMat;
    public Material lineActiveMat;
    public Material lineMat;
    public Material unselectedMat;
    public Material infectedMat;
    public Material infectedSelectedMat;
    public Material selectedMat;
    public Material traversedMat;
    public Material scannedMat;
    public Material targetMat;
    public Material targetClaimedMat;
    NodeVisualState curState;
    NodeVisualState lastVisualState;
    private int lastActivePlayer;
    public bool isTarget;
    private bool currentPlayerIsHidden;
    public enum NodeVisualState
    {
        Default,
        Traversed,
        Scanned,
        Infected
    }
    public static Node GetNode(int id)
    {
        if (GameController.gameController.nodesDict.TryGetValue(id, out Node nodeObject))
            return nodeObject;
        else
            return null;
    }
    public static GameObject GetNodeObject(int id)
    {
        Node nodeObject = Node.GetNode(id);
        if(nodeObject!=null)
            return nodeObject.gameObject;
        else
            return GameObject.Find("Node" + id);        
    }
    void Awake()
    {
    }
    // Start is called before the first frame update
    void Start()
    {
        curState = NodeVisualState.Default;
    }

    // Update is called once per frame
    void Update()
    {
        int currentTurnPlayer = GameController.gameController.currentTurnPlayer;
        if (lastActivePlayer != currentTurnPlayer)
        {
            lastActivePlayer = currentTurnPlayer;
            currentPlayerIsHidden = (lastActivePlayer == 0);
        }
        if (lastVisualState != curState)
        {
            lastVisualState = curState;
            switch(curState)
            {
                case NodeVisualState.Default:
                    gameObject.GetComponent<MeshRenderer> ().material = unselectedMat;
                    break;
                case NodeVisualState.Traversed:
                    gameObject.GetComponent<MeshRenderer> ().material = traversedMat;
                    break;
                case NodeVisualState.Scanned:
                    gameObject.GetComponent<MeshRenderer> ().material = scannedMat;
                    break;
                case NodeVisualState.Infected:
                    gameObject.GetComponent<MeshRenderer> ().material = infectedMat;
                    break;
            }
        }
    }

    public void ForceMakeTarget(bool b = true)
    {
        LineRenderer line = gameObject.GetComponent<LineRenderer>();
        isTarget = b;
        line.enabled = true;
    }
    private bool lastMouseOverState = false;
    void OnMouseOver()
    {
        if(!lastMouseOverState)
        {
            if(curState == NodeVisualState.Default || curState == NodeVisualState.Infected)
            {
                bool isInfected = curState == NodeVisualState.Infected;
                gameObject.GetComponent<MeshRenderer> ().material = isInfected ? infectedSelectedMat : selectedMat;
                lastMouseOverState = true;
                GameController.gameController.OnNodeHovered(this);
            }
        }
    }
    void OnMouseExit()
    {
        if(lastMouseOverState)
        {
            if(curState == NodeVisualState.Default || curState == NodeVisualState.Infected)
            {
                bool isInfected = curState == NodeVisualState.Infected;
                gameObject.GetComponent<MeshRenderer> ().material = isInfected ? infectedMat : unselectedMat;
                lastMouseOverState = false;
                GameController.gameController.OnNodeDehovered(this);
            } 
        }
    }

    void OnMouseDown(){
        if(GameController.gameController.localPlayerID == GameController.gameController.currentTurnPlayer)
        {
            GameController.gameController.OnNodeClicked(this);
        }
    }

    public void AddOtherNode(Node st)
    {
        //Set lines to draw
        Vector3[] positions = {gameObject.transform.position,st.gameObject.transform.position};
        LineHandler lineHandler = Instantiate(lineHandlerPrefab,gameObject.transform.position,gameObject.transform.rotation);
        lineHandler.gameObject.transform.parent = this.gameObject.transform;
        lineHandler.SetupLine(positions,lineMat,lineActiveMat,lineScanMat,lineMovedMat,this,st);
        
        //line.enabled = true;
    }

    public void DeInfect()
    {
        LineRenderer line = gameObject.GetComponent<LineRenderer>();
        curState = NodeVisualState.Default;
        if (isTarget)
        {
            line.material = targetMat;
        }
    }

    public void Infect()
    {
        LineRenderer line = gameObject.GetComponent<LineRenderer>();
        curState = NodeVisualState.Infected;
        if (isTarget)
        {
            line.material = targetClaimedMat;
        }
    }

    public bool IsInfected()
    {
        return curState == NodeVisualState.Infected;
    }

    public void SetVisualState(NodeVisualState visState)
    {
        curState = visState;
    }
}
