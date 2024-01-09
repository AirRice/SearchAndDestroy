using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;
using System;

public class Node : MonoBehaviour
{
    public int nodeID = 0;
    private bool isInfected = false;
    public LineHandler lineHandlerPrefab;
    public Material lineActiveMat;
    public Material lineMat;
    public Material unselectedMat;
    public Material infectedMat;
    public Material infectedSelectedMat;
    public Material selectedMat;
    public Material targetMat;
    public Material targetClaimedMat;
    private int lastActivePlayer;
    public bool isTarget;
    private bool currentPlayerIsHidden;
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
        // TODO: this is a patchwork solution for node ID enforcement, fix this later when generating nodes automatically.
        /*
        string inferredIDString = Regex.Match(gameObject.name, @"Node(\d+)").Groups[1].Value;
        if (!string.IsNullOrWhiteSpace(inferredIDString))
        {
            Debug.Log(inferredIDString);
            nodeID = Int32.Parse(inferredIDString);
        }
        */
    }
    // Start is called before the first frame update
    void Start()
    {
        gameObject.GetComponent<MeshRenderer> ().material = unselectedMat;
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
    }

    public void ForceMakeTarget(bool b = true)
    {
        LineRenderer line = gameObject.GetComponent<LineRenderer>();
        isTarget = b;
        line.enabled = (GameController.gameController.localPlayerID == 0);
    }
    private bool lastMouseOverState = false;
    void OnMouseOver()
    {
        if(!lastMouseOverState)
        {
            gameObject.GetComponent<MeshRenderer> ().material = isInfected ? infectedSelectedMat : selectedMat;
            lastMouseOverState = true;
            GameController.gameController.OnNodeHovered(this);
        }
    }
    void OnMouseExit()
    {
        if(lastMouseOverState)
        {
            gameObject.GetComponent<MeshRenderer> ().material = isInfected ? infectedMat : unselectedMat;
            lastMouseOverState = false;
            GameController.gameController.OnNodeDehovered(this);
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
        lineHandler.SetupLine(positions,lineMat,lineActiveMat,this,st);
        
        //line.enabled = true;
    }

    public void DeInfect()
    {
        LineRenderer line = gameObject.GetComponent<LineRenderer>();
        isInfected = false;
        gameObject.GetComponent<MeshRenderer> ().material = lastMouseOverState ? selectedMat : unselectedMat;
        if (isTarget)
        {
            //line.material = targetMat;
        }
    }

    public void Infect()
    {
        LineRenderer line = gameObject.GetComponent<LineRenderer>();
        isInfected = true;
        gameObject.GetComponent<MeshRenderer> ().material = lastMouseOverState ? infectedSelectedMat : infectedMat;
        if (isTarget)
        {
            line.material = targetClaimedMat;
        }
    }

    public bool IsInfected()
    {
        return isInfected;
    }
}
