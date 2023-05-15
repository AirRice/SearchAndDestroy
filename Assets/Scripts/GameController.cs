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
    public TextAsset boardSetupData;
    public int playersCount = 2;
    //1 player is the hidden player, rest will be hunters
    public PlayerPiece playerPrefab;
    public Node nodePrefab;
    public int localPlayerID = 0;
    public bool hotSeatMode = true;
    protected List<NodeLink> nodeLinksList = new List<NodeLink>();
    public List<PlayerPiece> playerPiecesList = new List<PlayerPiece>();
    public PlayerPiece hiddenPlayerPiece;
    public int currentPlayerMoves = 0;
    protected Node hunterSpawn, hiddenSpawn = null;
    
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

        /*if (boardSetupData != null)
        {
            // load file for setting the board up. Format: each rank of the board (in number of nodes)
            string[] fileLines = Regex.Split ( boardSetupData.text, "\n|\r|\r\n" );
            string[] values = Regex.Split ( fileLines[0], ";" ); //file is split by semicolons
            for ( int i=0; i < values.Length; i++ ) 
            {
                Node newNode = Instantiate(nodePrefab, new Vector3(0, 0, 0), Quaternion.identity);
            }
        }*/
        if (linksData != null)
        {
            // load file for setting links between nodes
            string linksFile = linksData.text;
            string[] fileLines = Regex.Split ( linksFile, "\n|\r|\r\n" );
            
            for ( int i=0; i < fileLines.Length; i++ ) 
            {
                string[] values = Regex.Split ( fileLines[i], ";" ); //file is split by semicolons
                if (values.Length == 2)
                {
                    NodeLink newLink = new NodeLink(Int32.Parse(values[0]),Int32.Parse(values[1]));
                    nodeLinksList.Add(newLink);
                    //Debug.Log(newLink.ToString());
                }
            }
        }

        // Find marked spawns for each team
        try{
            GameObject[] hunterSpawns = GameObject.FindGameObjectsWithTag("HunterSpawn");
            hunterSpawn = hunterSpawns[0].GetComponent<Node> ();
            GameObject[] hiddenSpawns = GameObject.FindGameObjectsWithTag("HiddenSpawn");
            hiddenSpawn = hiddenSpawns[0].GetComponent<Node> ();
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
    void SetupPlayerPositions(Node hunterSpawn, Node hiddenSpawn)
    {
        if(this.playersCount < 2) 
            return;
        //Init all player locations
        hiddenPlayerLocation = hiddenSpawn.nodeID;
        hunterPlayerLocations = new int[this.playersCount-1];
        Array.Fill(hunterPlayerLocations,hunterSpawn.nodeID);
        for(int i = 1; i<playersCount; i++)
        {
            PlayerPiece newPlayer = Instantiate(playerPrefab, new Vector3(0, 0, 0), Quaternion.identity);
            newPlayer.SetHidden(false);
            newPlayer.gameObject.name = $"Player{i}";
            newPlayer.playerID = i;
            newPlayer.setNode(hunterPlayerLocations[i-1],false);
            playerPiecesList.Add(newPlayer);
        }
        ProgressTurn(false);
    }
    public void TryMoveToNode(int toMoveTo)
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
        currentPlayerPiece.setNode(destID,true);
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
            hiddenPlayer.setNode(hiddenPlayerLocation,false);
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
    public int[] searchPath(Node startPoint, Node endPoint)
    //Tried implementing BFS, unsure if the most robust or fast
    //Adapted from python implementation https://stackoverflow.com/questions/8922060/how-to-trace-the-path-in-a-breadth-first-search/50575971#50575971
    {
        int maxDepth = 5;
        //Edge case: same node for start & end
        if (startPoint.nodeID == endPoint.nodeID)
            return new int[] {endPoint.nodeID};

        List<int> checkedIDs = new List<int>();
        Queue<(int,int[])> nodePaths = new Queue<(int,int[])>();
        nodePaths.Enqueue((startPoint.nodeID,new[] {startPoint.nodeID}));
        while(nodePaths.Count != 0)
        {
            (int,int[]) vpath = nodePaths.Dequeue();
            if(vpath.Item2.Length>maxDepth)
                return new int[0];
            checkedIDs.Add(vpath.Item1);
            foreach(NodeLink link in nodeLinksList)
            {
                int? otherNode = link.getOtherNode(vpath.Item1);
                if(otherNode == endPoint.nodeID)
                    return vpath.Item2.Concat(new int[] {endPoint.nodeID}).ToArray();
                else
                    if(otherNode!= null)
                    {
                        int otherNodeInt = otherNode.GetValueOrDefault();
                        if(!checkedIDs.Contains(otherNodeInt))
                        {
                            checkedIDs.Add(otherNodeInt);
                            nodePaths.Enqueue((otherNodeInt, vpath.Item2.Concat(new int[] {otherNodeInt}).ToArray()));
                        }
                    }
                        
            }
        }
        return new int[0];
    }
}
