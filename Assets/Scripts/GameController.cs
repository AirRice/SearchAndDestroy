using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Text.RegularExpressions; //for the file handling
using System;
using System.Linq;

public class GameController : MonoBehaviour
{
    public static GameController gameController;
    private GameHud gameHud;
    public TextAsset linksData;
    public TextAsset boardSetupData;
    public int playersCount = 2;
    //1 player is the hidden player, rest will be hunters
    public int movesCount = 3;
    public int maxTurnCount = 15;
    public PlayerPiece playerPrefab;
    public Node nodePrefab;
    public int localPlayerID = 0;
    public bool hotSeatMode = true;
    public Dictionary<int, Node> nodesDict = new Dictionary<int, Node>();
    public List<PlayerPiece> playerPiecesList = new List<PlayerPiece>();
    public PlayerPiece hiddenPlayerPiece;
    public int currentPlayerMoves = 0;
    public bool currentPlayerDidSpecialAction = false;
    public int infectedNodeID = -1;
    //The current turn will be, 0 = hidden player, all further players = i+1 in the hunterPlayerLocations array
    public int currentTurnPlayer = 0;
    public bool gameEnded = false;
    public int turnCount = 0;
    protected Node hunterSpawn, hiddenSpawn = null;
    protected int hiddenPlayerLocation;
    protected int[] hunterPlayerLocations;
    protected List<NodeLink> nodeLinksList = new List<NodeLink>();
    private Dictionary<(int,int), int[]> cachedPaths = new Dictionary<(int,int), int[]>();
    private float nodeVertDist = 5f;
    private float nodeHorizDist = 4.5f;
    private void Awake()
    {
        //enforce singleton
        if (gameController == null)
            gameController = this;
        else
            Destroy(gameObject);

        // 
        gameHud = gameObject.GetComponent<GameHud>();
        StartGame();
    }
    // Start is called before the first frame update
    void Start()
    {   
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public PlayerPiece GetCurrentPlayerPiece()
    {
        PlayerPiece curPlayerPiece = currentTurnPlayer==0 ? hiddenPlayerPiece : playerPiecesList[currentTurnPlayer-1];
        return curPlayerPiece;
    }

    public PlayerPiece GetLocalPlayerPiece()
    {
        return localPlayerID==0 ? hiddenPlayerPiece : playerPiecesList[localPlayerID-1];
    }
    public void StartGame(bool restart = false)
    {
        //Reset all variables
        gameEnded = false;
        nodesDict = new Dictionary<int, Node>();
        playerPiecesList = new List<PlayerPiece>();
        hiddenPlayerPiece = null;
        currentPlayerMoves = 0;
        currentPlayerDidSpecialAction = false;
        infectedNodeID = -1;
        currentTurnPlayer = 0;
        turnCount = 0;
        hunterSpawn = null;
        hiddenSpawn = null;
        hiddenPlayerLocation = -1;
        hunterPlayerLocations = null;
        nodeLinksList = new List<NodeLink>();
        cachedPaths = new Dictionary<(int,int), int[]>();

        if(restart){
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        this.SetupBoard();
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
    private string trojanWinMessage = "The Trojan successfully evaded detection!";
    private string scannerWinMessage = "The Trojan has been purged!";
    public void EndGame(bool hiddenwin)
    {
        gameHud.ShowCentreMessage(hiddenwin ? trojanWinMessage : scannerWinMessage);
        gameHud.ResetPlayerActionButton();
        gameEnded = true;
    }
    void SetupBoard(bool useFullSetup=true)
    {   
        if (useFullSetup)
            if (boardSetupData != null)
            {
                // load file for setting the board up. Format: each rank of the board (in number of nodes)
                string[] fileLines = Regex.Split ( boardSetupData.text, "\n|\r|\r\n" );
                string[] values = Regex.Split ( fileLines[0], ";" ); //file is split by semicolons
                float totalHorizLength = (values.Length-1) * nodeHorizDist;
                int currentNodeID = 1;
                for ( int i=0; i <values.Length; i++ ) 
                {
                    int nodeCountAtRank = Int32.Parse(values[i]);
                    for ( int j=0; j < nodeCountAtRank; j++ ) 
                    {
                        float zLoc = (totalHorizLength/2)-(values.Length-(i+1))*nodeHorizDist;
                        float xLoc = (nodeVertDist*(nodeCountAtRank-1)/2)-nodeVertDist*j;
                        Node newNode = Instantiate(nodePrefab, new Vector3(xLoc, 0, zLoc), Quaternion.identity);
                        newNode.nodeID = currentNodeID;
                        newNode.gameObject.name = "Node"+currentNodeID;
                        nodesDict.Add(currentNodeID, newNode);
                        // Setup Spawn Points
                        if(i==values.Length-1 && j==nodeCountAtRank-1)
                            newNode.gameObject.tag = "HunterSpawn";
                        else if(currentNodeID == 1)
                            newNode.gameObject.tag = "HiddenSpawn";

                        if(i>0)
                        {
                            int nodeCountPrevRank = Int32.Parse(values[i-1]);
                            for (int k = 0; k<2; k++)
                            {
                                int otherNode;
                                bool flag = false;
                                if(nodeCountPrevRank==nodeCountAtRank)
                                {
                                    otherNode = currentNodeID-nodeCountAtRank;
                                    flag = true;
                                }    
                                else
                                {
                                    otherNode = currentNodeID-(nodeCountAtRank>nodeCountPrevRank ? nodeCountAtRank : nodeCountPrevRank) + k;
                                }
                                if (otherNode < currentNodeID-(nodeCountPrevRank+j) || otherNode > currentNodeID-(j+1))
                                    continue;
                                NodeLink newLink = new NodeLink(currentNodeID,otherNode);
                                nodeLinksList.Add(newLink);
                                if(flag)
                                    break;
                            }
                        }
                        currentNodeID++;
                    }
                }
            }
        else if (linksData != null)
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
        if(currentPlayerMoves < 1)
            return;
        if(toMoveTo == infectedNodeID && localPlayerID != 0)
        {
            return;    
        }
        PlayerPiece toMovePlayerPiece = GetCurrentPlayerPiece();
        int[] movePath = getCappedPath(Node.getNode(toMovePlayerPiece.currentNodeID), Node.getNode(toMoveTo), currentPlayerMoves);
        currentPlayerMoves -= (movePath.Length-1);
        toMoveTo = movePath.Last();
        //Debug.Log(currentPlayerMoves);
        
        toMovePlayerPiece.TrySmoothMove(toMoveTo,movePath);
        UpdateActivePlayerPosition(toMoveTo);
    }
    // Try handling path checks
    public void TryPathHighlight(int[] pathToHovered, bool toHighlight=true)
    {   
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
                lineDrawer.GetComponent<LineRenderer> ().material = toHighlight ? lineDestNode.lineActiveMat : lineDestNode.lineMat;
            }
        }
    }
    public void TrySpecialAction(Node thisNode)
    {
        if(currentPlayerMoves < 1)
            return;
        if (localPlayerID == 0 && currentPlayerDidSpecialAction)
            return;
        PlayerPiece toMovePlayerPiece = GetCurrentPlayerPiece();
        Node playerNode = Node.getNode(toMovePlayerPiece.currentNodeID);
        bool isAdjacent = false;
        foreach(NodeLink link in nodeLinksList)
        {
            if(link.IsLinkBetween(playerNode.nodeID,thisNode.nodeID))
            {
                isAdjacent = true;
                break;
            }
        }
        if (isAdjacent)
        {
            currentPlayerMoves--;
            if (localPlayerID == 0)
            {               
                currentPlayerDidSpecialAction = true;
                gameHud.playerActionButtonDown = false;
                TryNodeInfect(thisNode);
            }
            else
            {
                TryNodeScan(thisNode);
            }
        }
    }
    public void TryNodeInfect(Node toInfect)
    {
        infectedNodeID = toInfect.nodeID;
        toInfect.Infect();
    }
    public void TryNodeScan(Node toScan)
    {
        if(toScan.nodeID == hiddenPlayerLocation)
        {
            EndGame(false);
            return;
        }       
        Node hiddenPlayerNode = Node.getNode(hiddenPlayerLocation);
        int[] pathTo = getCappedPath(toScan,hiddenPlayerNode);
        int dist = pathTo.Length-1;
        Debug.Log($"The Distance to the Trojan is {dist} Nodes");
    }
    public void UpdateActivePlayerPosition(int destID)
    {
        PlayerPiece currentPlayerPiece = GetCurrentPlayerPiece();;
        if(currentTurnPlayer==0)
        {
            hiddenPlayerLocation = destID;
        }
        else 
        {
            hunterPlayerLocations[currentTurnPlayer-1] = destID;
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
        gameHud.ResetPlayerActionButton();
        currentPlayerMoves = movesCount;
        currentPlayerDidSpecialAction = false;
        if(infectedNodeID!=-1)
            cachedPaths = new Dictionary<(int,int), int[]>();
        // Only handle hidden player if one *IS* the hidden player
        if(currentTurnPlayer==0)
        {
            this.StartHiddenTurn();
        }
        else if(localPlayerID != 0)
        {
            if(hiddenPlayerPiece != null)
                Destroy(hiddenPlayerPiece.gameObject);
        }
    }

    void StartHiddenTurn()
    {
        infectedNodeID = -1;
        foreach(KeyValuePair<int,Node> nodePair in nodesDict)
        {
            // Reset node infected state when the hidden player's turn activates
            if(nodePair.Value.IsInfected())
            {
                nodePair.Value.DeInfect();
            }
        }
        if(this.localPlayerID==0 && hiddenPlayerPiece == null)
        {
            PlayerPiece hiddenPlayer = Instantiate(playerPrefab, new Vector3(0, 0, 0), Quaternion.identity);
            hiddenPlayer.SetHidden(true);
            hiddenPlayer.gameObject.name = "Player0";
            hiddenPlayer.playerID = 0;
            hiddenPlayer.setNode(hiddenPlayerLocation,false);
            hiddenPlayerPiece = hiddenPlayer;
        }
    }


    // CLIENTSIDE EVENT HANDLING
    public void OnNodeHovered(Node thisNode)
    {
        if(gameEnded)
            return;
        if(localPlayerID == currentTurnPlayer)
        {
            PlayerPiece localPlayerPiece = GetLocalPlayerPiece();
            if(localPlayerPiece!=null)
            {
                if(gameHud.playerActionButtonDown)
                    return;
                int[] highlightedPath = getCappedPath(Node.getNode(localPlayerPiece.currentNodeID), thisNode, currentPlayerMoves);
                int moveSpend = (movesCount-currentPlayerMoves)+highlightedPath.Length - 1;
                TryPathHighlight(highlightedPath, true);
                if(currentPlayerMoves>0)
                    gameHud.ShowMoveSpend(moveSpend);
            }
        }
    }
    public void OnNodeDehovered(Node thisNode)
    {
        if(localPlayerID == currentTurnPlayer)
        {
            HighlightAllPaths(false);
            gameHud.ShowMoveSpend(0);
        }
    }
    public void OnNodeClicked(Node thisNode)
    {
        if(gameEnded)
            return;
        if(!gameHud.playerActionButtonDown)
            TryMoveToNode(thisNode.nodeID);
        else
        {
            TrySpecialAction(thisNode);
        }
    }
    // SEARCHING and CACHING


    public void HighlightAllPaths(bool highlighted = true)
    {
        foreach(GameObject lineDrawer in GameObject.FindGameObjectsWithTag("lineHandler"))
        {
            lineDrawer.GetComponent<LineHandler> ().HighlightPath(highlighted);
        }
    }
    
    public int[] getCappedPath(Node startPoint, Node endPoint, int maxhops = -1)
    {
        int[] foundPathRaw;
        if(!cachedPaths.TryGetValue((startPoint.nodeID, endPoint.nodeID), out foundPathRaw))
        {
            foundPathRaw = this.searchPath(startPoint,endPoint);
            cachedPaths.Add((startPoint.nodeID,endPoint.nodeID),foundPathRaw);
        }
        if(maxhops == -1 || foundPathRaw.Length <= maxhops+1)
            return foundPathRaw;
        else   
            return foundPathRaw.Take(maxhops+1).ToArray();
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
                if(otherNode.HasValue && otherNode.Value == infectedNodeID && localPlayerID != 0)
                    continue;
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
