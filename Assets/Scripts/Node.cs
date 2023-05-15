using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;
using System;

public class Node : MonoBehaviour
{
    public int nodeID = 0;
    public LineHandler lineHandlerPrefab;
    public Material lineActiveMat;
    public Material lineMat;
    public Material unselectedMat;
    public Material selectedMat;
    public static Node getNode(int id)
    {
        Node nodeObject;
        if (GameController.gameController.nodesDict.TryGetValue(id, out nodeObject))
            return nodeObject;
        else
            return null;
    }
    public static GameObject getNodeObject(int id)
    {
        Node nodeObject = Node.getNode(id);
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
        
    }
    private bool lastMouseOverState = false;
    void OnMouseOver()
    {
        if(!lastMouseOverState)
        {
            gameObject.GetComponent<MeshRenderer> ().material = selectedMat;
            lastMouseOverState = true;
            int gameCurrentTurnPlayer = GameController.gameController.currentTurnPlayer;
            int localPlayer = GameController.gameController.localPlayerID;
            if(localPlayer == gameCurrentTurnPlayer)
            {
                PlayerPiece localPlayerPiece = localPlayer==0 ? GameController.gameController.hiddenPlayerPiece : GameController.gameController.playerPiecesList[localPlayer-1];
                if(localPlayerPiece!=null)
                    localPlayerPiece.TryPathHighlight(this);
            }
        }
    }
    void OnMouseExit()
    {
        if(lastMouseOverState)
        {
            gameObject.GetComponent<MeshRenderer> ().material = unselectedMat;
            lastMouseOverState = false;
            GameController.gameController.HighlightAllPaths(false);
        }
    }
    void OnMouseDown(){
        GameController.gameController.TryMoveToNode(this.nodeID);
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
}
