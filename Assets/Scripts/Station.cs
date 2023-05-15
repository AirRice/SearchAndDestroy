using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;
using System;

public class Station : MonoBehaviour
{
    public int stationID = 0;
    public LineHandler lineHandlerPrefab;
    public Material lineActiveMat;
    public Material lineMat;
    public Material unselectedMat;
    public Material selectedMat;
    public static Station getStation(int id)
    {
        GameObject stationObject = Station.getStationObject(id);
        if(stationObject!=null)
            return stationObject.GetComponent<Station> ();
        else
            return null;
    }
    public static GameObject getStationObject(int id)
    {
        return GameObject.Find("Station" + id);
    }
    void Awake()
    {
        // TODO: this is a patchwork solution for station ID enforcement, fix this later when generating stations automatically.
        /*
        string inferredIDString = Regex.Match(gameObject.name, @"Station(\d+)").Groups[1].Value;
        if (!string.IsNullOrWhiteSpace(inferredIDString))
        {
            Debug.Log(inferredIDString);
            stationID = Int32.Parse(inferredIDString);
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
        GameController.gameController.TryMoveToStation(this.stationID);
    }

    public void AddOtherStation(Station st)
    {
        //Set lines to draw
        Vector3[] positions = {gameObject.transform.position,st.gameObject.transform.position};
        LineHandler lineHandler = Instantiate(lineHandlerPrefab,gameObject.transform.position,gameObject.transform.rotation);
        lineHandler.gameObject.transform.parent = this.gameObject.transform;
        lineHandler.SetupLine(positions,lineMat,lineActiveMat,this,st);
        
        //line.enabled = true;
    }
}
