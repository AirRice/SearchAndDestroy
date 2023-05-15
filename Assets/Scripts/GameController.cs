using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions; //for the file handling
using System;
using System.Linq;

public class GameController : MonoBehaviour
{
    public static GameController gameController;
    public TextAsset linksData;
    public int playersCount = 2;
    //1 player is the hidden player, rest will be hunters
    public PlayerPiece playerPrefab;
    public int localPlayerID = 0;
    public bool hotSeatMode = true;
    protected List<StationLink> stationLinksList = new List<StationLink>();
    public List<PlayerPiece> playerPiecesList = new List<PlayerPiece>();
    public PlayerPiece hiddenPlayerPiece;
    public int currentPlayerMoves = 0;
    protected Station hunterSpawn, hiddenSpawn = null;
    
    //The current turn will be, 0 = hidden player, all further players = i+1 in the hunterPlayerLocations array
    public int currentTurnPlayer = 0;
    protected int hiddenPlayerLocation;
    protected int[] hunterPlayerLocations;
    private void Awake()
    {
        //enforce singleton
        if (gameController == null)
            gameController = this;
        else
            Destroy(gameObject);

        // 
        if (linksData != null)
        {
            // load file for setting links between stations
            string linksFile = linksData.text;
            string[] fileLines = Regex.Split ( linksFile, "\n|\r|\r\n" );
            
            for ( int i=0; i < fileLines.Length; i++ ) 
            {
                string[] values = Regex.Split ( fileLines[i], ";" ); //file is split by semicolons
                if (values.Length == 2)
                {
                    StationLink newLink = new StationLink(Int32.Parse(values[0]),Int32.Parse(values[1]));
                    stationLinksList.Add(newLink);
                    //Debug.Log(newLink.ToString());
                }
            }
        }
        // Find marked spawns for each team
        try{
            GameObject[] hunterSpawns = GameObject.FindGameObjectsWithTag("HunterSpawn");
            hunterSpawn = hunterSpawns[0].GetComponent<Station> ();
            GameObject[] hiddenSpawns = GameObject.FindGameObjectsWithTag("HiddenSpawn");
            hiddenSpawn = hiddenSpawns[0].GetComponent<Station> ();
        }
        catch(UnityException uex){

        }
        if (hunterSpawn != null && hiddenSpawn != null)
        {
            this.SetupPlayerPositions(hunterSpawn, hiddenSpawn);
        }
    }
    // Start is called before the first frame update
    void Start()
    {   
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    void SetupPlayerPositions(Station hunterSpawn, Station hiddenSpawn)
    {
        if(this.playersCount < 2) 
            return;
        //Init all player locations
        hiddenPlayerLocation = hiddenSpawn.stationID;
        hunterPlayerLocations = new int[this.playersCount-1];
        Array.Fill(hunterPlayerLocations,hunterSpawn.stationID);
        for(int i = 1; i<playersCount; i++)
        {
            PlayerPiece newPlayer = Instantiate(playerPrefab, new Vector3(0, 0, 0), Quaternion.identity);
            newPlayer.SetHidden(false);
            newPlayer.gameObject.name = $"Player{i}";
            newPlayer.playerID = i;
            newPlayer.setStation(hunterPlayerLocations[i-1],false);
            playerPiecesList.Add(newPlayer);
        }
        ProgressTurn(false);
    }
    public void TryMoveToStation(int toMoveTo)
    {
        PlayerPiece toMovePlayerPiece = currentTurnPlayer==0 ? hiddenPlayerPiece : playerPiecesList[currentTurnPlayer-1];
        toMovePlayerPiece.TrySmoothMove(toMoveTo);
        UpdateActivePlayerPosition(toMoveTo);
    }
    public void UpdateActivePlayerPosition(int destID)
    {
        PlayerPiece currentPlayerPiece;
        if(currentTurnPlayer==0)
        {
            hiddenPlayerLocation = destID;
            currentPlayerPiece = hiddenPlayerPiece;
        }
        else 
        {
            hunterPlayerLocations[currentTurnPlayer-1] = destID;
            currentPlayerPiece = playerPiecesList[currentTurnPlayer-1];
        }
        currentPlayerPiece.setStation(destID,true);
    }
    public void ProgressTurn(bool increment = true)
    {
        if(increment)
        {
            currentTurnPlayer++;
            currentTurnPlayer = currentTurnPlayer%this.playersCount;
            if(this.hotSeatMode)
            {
                localPlayerID = currentTurnPlayer;
            }
        }
        currentPlayerMoves = 3;
        // Only handle hidden player if one *IS* the hidden player
        if(currentTurnPlayer==0 && localPlayerID == 0)
        {
            this.StartHiddenTurn();
        }
        else if(localPlayerID != 0)
        {
            Destroy(hiddenPlayerPiece.gameObject);
        }
    }

    void StartHiddenTurn()
    {
        if(this.localPlayerID!=0)
            return;
        if(hiddenPlayerPiece == null)
        {
            PlayerPiece hiddenPlayer = Instantiate(playerPrefab, new Vector3(0, 0, 0), Quaternion.identity);
            hiddenPlayer.SetHidden(true);
            hiddenPlayer.gameObject.name = "Player0";
            hiddenPlayer.playerID = 0;
            hiddenPlayer.setStation(hiddenPlayerLocation,false);
            hiddenPlayerPiece = hiddenPlayer;
        }
    }

    public void EndTurn()
    {}
    // SEARCHING


    public void HighlightAllPaths(bool highlighted = true)
    {
        foreach(GameObject lineDrawer in GameObject.FindGameObjectsWithTag("lineHandler"))
        {
            lineDrawer.GetComponent<LineHandler> ().HighlightPath(highlighted);
        }
    }
    public int[] searchPath(Station startPoint, Station endPoint)
    //Tried implementing BFS, unsure if the most robust or fast
    //Adapted from python implementation https://stackoverflow.com/questions/8922060/how-to-trace-the-path-in-a-breadth-first-search/50575971#50575971
    {
        int maxDepth = 5;
        //Edge case: same station for start & end
        if (startPoint.stationID == endPoint.stationID)
            return new int[] {endPoint.stationID};

        List<int> checkedIDs = new List<int>();
        Queue<(int,int[])> stationNodes = new Queue<(int,int[])>();
        stationNodes.Enqueue((startPoint.stationID,new[] {startPoint.stationID}));
        while(stationNodes.Count != 0)
        {
            (int,int[]) vpath = stationNodes.Dequeue();
            if(vpath.Item2.Length>maxDepth)
                return new int[0];
            checkedIDs.Add(vpath.Item1);
            foreach(StationLink link in stationLinksList)
            {
                int? otherStation = link.getOtherStation(vpath.Item1);
                if(otherStation == endPoint.stationID)
                    return vpath.Item2.Concat(new int[] {endPoint.stationID}).ToArray();
                else
                    if(otherStation!= null)
                    {
                        int otherStationInt = otherStation.GetValueOrDefault();
                        if(!checkedIDs.Contains(otherStationInt))
                        {
                            checkedIDs.Add(otherStationInt);
                            stationNodes.Enqueue((otherStationInt, vpath.Item2.Concat(new int[] {otherStationInt}).ToArray()));
                        }
                    }
                        
            }
        }
        return new int[0];
    }
}
