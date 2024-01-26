using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Text.RegularExpressions; //for the file handling
using System;
using System.IO;
using System.Linq;
using Random = UnityEngine.Random;

public class GameController : MonoBehaviour
{
    public static GameController gameController;
    private GameHud gameHud;
    public TextAsset playerBotTypesData;
    public int mapSize = 3;
    public int playersCount = 2;
    //1 player is the hidden player, rest will be hunters
    public int movesCount = 3;
    public int maxTurnCount = 15;
    public int roundCount = 0;
    public int maxRoundCount = 10;
    public int maxObjectives = 1;
    public bool hotSeatMode = true;
    public bool logToCSV = true;
    public bool useSmoothMove = false;
    public PlayerPiece playerPrefab;
    public Node nodePrefab;
    public DistanceTextPopup textPopupPrefab;
    public Camera mainCam;
    public int localPlayerID = 0;
    private bool noBotPlayers = true;
    public Dictionary<int, Node> nodesDict = new();
    public List<PlayerPiece> playerPiecesList = new();
    public PlayerPiece hiddenPlayerPiece;
    public int currentPlayerMoves = 0;
    public bool currentPlayerDidSpecialAction = false;
    public int lastInfectedNode;
    public List<int> infectedNodeIDs;
    // the Target node(s) to which the hiding player must go and infect.
    public List<int> targetNodeIDs;
    //The current turn will be, 0 = hidden player, all further players = i+1 in the hunterPlayerLocations array
    public int currentTurnPlayer = 0;
    public String[] playerBotType;
    private BotTemplate[] playerBotControllers;
    public Dictionary<int, BotTemplate> playerBotsDict = new();
    public bool gameEnded = false;
    public int turnCount = 0;
    protected Node hunterSpawn, hiddenSpawn = null;
    protected int hiddenPlayerLocation;
    protected int[] hunterPlayerLocations;
    public List<NodeLink> nodeLinksList = new();
    public List<(int,int[])> scanHistory;
    //Scan History is (node id, distance to hidden player)
    private Dictionary<(int,int), int[]> cachedPaths = new();
    private bool nodeWasInfectedLastTurn = false;
    private void Awake()
    {
        //enforce singleton
        if (gameController == null)
            gameController = this;
        else
            Destroy(gameObject);
    }

    // Start is called before the first frame update
    void Start()
    {   
        gameHud = gameObject.GetComponent<GameHud>();
        ConfigData cfg = LoadData.Load();

        mapSize = cfg.mapSize;
        playersCount = cfg.playersCount;
        movesCount = cfg.movesCount;
        maxTurnCount = cfg.maxTurnCount;
        maxRoundCount = cfg.maxRoundCount;
        maxObjectives = cfg.maxObjectives;
        hotSeatMode = cfg.hotSeatMode;
        logToCSV = cfg.logToCSV;
        useSmoothMove = cfg.useSmoothMove;


        String[] fileLines = Regex.Split ( playerBotTypesData.text, "\n|\r|\r\n" );
        playerBotType = Regex.Split ( fileLines[0], ";" ); //file is split by semicolons

        if (PlayerPrefs.HasKey("RoundsCount"))
        {
            // Handling round count (persistent between round restarts)
            roundCount = PlayerPrefs.GetInt("RoundsCount");
            roundCount ++;
        }
        else
        {
            roundCount = 0;
        }
        PlayerPrefs.SetInt("RoundsCount", roundCount);
        PlayerPrefs.Save();
        StartGame();
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape))
        Quit();
    }
    //Get PlayerPiece object relating to current player
    public PlayerPiece GetCurrentPlayerPiece()
    {
        PlayerPiece curPlayerPiece = currentTurnPlayer==0 ? hiddenPlayerPiece : playerPiecesList[currentTurnPlayer-1];
        return curPlayerPiece;
    }
    //Get PlayerPiece object of Local Player
    public PlayerPiece GetLocalPlayerPiece()
    {
        return localPlayerID==0 ? hiddenPlayerPiece : playerPiecesList[localPlayerID-1];
    }

    //Handling on-start variables, etc. Needed as game can restart.
    public void StartGame(bool restart = false)
    {
        //Reset all variables
        noBotPlayers = true;
        gameEnded = false;
        nodesDict = new Dictionary<int, Node>();
        playerPiecesList = new List<PlayerPiece>();
        hiddenPlayerPiece = null;
        currentPlayerMoves = 0;
        lastInfectedNode = -1;
        currentPlayerDidSpecialAction = false;
        infectedNodeIDs = new List<int>();
        currentTurnPlayer = 0;
        turnCount = 0;
        hunterSpawn = null;
        hiddenSpawn = null;
        hiddenPlayerLocation = -1;
        hunterPlayerLocations = null;
        nodeLinksList = new List<NodeLink>();
        targetNodeIDs = new List<int>();
        cachedPaths = new Dictionary<(int,int), int[]>();
        nodeWasInfectedLastTurn = false;

        if(restart){
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        this.SetupBoard();
        this.SetupBotPlayers();
        // Find marked spawns for each team
        try
        {
            GameObject[] hunterSpawns = GameObject.FindGameObjectsWithTag("HunterSpawn");
            hunterSpawn = hunterSpawns[0].GetComponent<Node> ();
            GameObject[] hiddenSpawns = GameObject.FindGameObjectsWithTag("HiddenSpawn");
            hiddenSpawn = hiddenSpawns[0].GetComponent<Node> ();
        }
        catch (UnityException)
        {
            
        }
        if (hunterSpawn != null && hiddenSpawn != null)
        {
            this.SetupPlayerPositions(hunterSpawn, hiddenSpawn);
        }
    }

    private readonly string trojanWinMessage = "The Trojan successfully evaded detection!";
    private readonly string scannerWinMessage = "The Trojan has been purged!";

    //Ends the game with a message and initiates restart process.
    public void EndGame(bool hiddenwin)
    {
        if (hiddenPlayerPiece == null)
        {
            PlayerPiece hiddenPlayer = Instantiate(playerPrefab, new Vector3(0, 0, 0), Quaternion.identity);
            hiddenPlayer.SetHidden(true);
            hiddenPlayer.setNode(hiddenPlayerLocation,false);
        }
        gameHud.ShowCentreMessage(hiddenwin ? trojanWinMessage : scannerWinMessage);
        gameHud.ResetPlayerActionButton();
        gameEnded = true;

        // Restart the game repeatedly if there are bot players
        if (!noBotPlayers)
        {
            StartCoroutine(WaitToRestart());
        }
    }

    // Wait to restart just to prevent slow loading related errors for restarting.
    IEnumerator WaitToRestart()
    {
        //Wait for 1 second
        yield return new WaitForSeconds(0.25f);

        if (roundCount < maxRoundCount)
        {
            GameController.gameController.StartGame(true);
        }
        else
        {
            PlayerPrefs.SetInt("RoundsCount", 0);
            Quit();
        }
    }

    // Quits the game or stops its playback if in editor
    public static void Quit()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    // Sets up the AI agents depending on the file-specified bot templates.
    void SetupBotPlayers()
    {
        playerBotControllers = new BotTemplate[playersCount];
        for (int i=0; i < playersCount; i++)
        {
            Type botType = Type.GetType(playerBotType[i]);
            //Debug.Log(playerBotType[i]);
            //Debug.Log($"Is defined: {botType != null}");
            if (botType != null)
            {
                noBotPlayers = false;
            }
            playerBotControllers[i] = botType != null ? (BotTemplate) ScriptableObject.CreateInstance(botType) : null;
        }
    }

    //Sets up the board based on the board size specified in the main gamecontroller object.

    /* MAP TRAVERSAL:
    Movement between nodes can be done between one of four directions.
    Up Left: -(mapsize) from current node id
    Down Left: -1 from current node id
    Up Right: +1 from current node id
    Down Right: +(mapsize) from current node id

    The border nodes (nodes on the edge of the map, with less than 4 connections) are all either:
    within range 1 <= x <= mapsize
    within range (mapsize-1) * mapsize <= x <= mapsize * mapsize
    are of value 1+(mapsize * n) where n is an int 0 <= n
    are of value mapsize * n where n is an int 1 <= n
    */
    private readonly float nodeVertDist = 5f;
    private readonly float nodeHorizDist = 4.5f;
    void SetupBoard()
    {   
        // load file for setting the board up. Format: each rank of the board (in number of nodes)
        float totalHorizLength = (mapSize * 2 - 2) * nodeHorizDist;
        int currentNodeID = 1;
        for ( int i=0; i < mapSize ; i++ )
        {
            for ( int j=0; j < mapSize; j++ )
            {
                float zLoc = (totalHorizLength/2) - nodeHorizDist * (i+j);
                float xLoc = nodeVertDist/2*(j-i);
                Node newNode = Instantiate(nodePrefab, new Vector3(xLoc, 0, zLoc), Quaternion.identity);
                newNode.nodeID = currentNodeID;
                newNode.gameObject.name = "Node"+currentNodeID;
                nodesDict.Add(currentNodeID, newNode);
                // Setup Spawn Points
                if(currentNodeID == 1)
                    newNode.gameObject.tag = "HunterSpawn";
                else if(currentNodeID == mapSize * mapSize)
                    newNode.gameObject.tag = "HiddenSpawn";

                // Test for nodes above left and below left, and connect this one to them if within range
                
                if (currentNodeID-mapSize > 0)
                {
                    NodeLink newLink = new(currentNodeID,currentNodeID-mapSize);
                    nodeLinksList.Add(newLink);
                }
                if (currentNodeID-1 > 0 && (currentNodeID-1) % mapSize != 0)
                {
                    NodeLink newLink = new(currentNodeID,currentNodeID-1);
                    nodeLinksList.Add(newLink);
                }
                currentNodeID++;
            }
        }
        double d_mapSize = mapSize;
        for(int i=0;i<maxObjectives;i++)
        {
            bool chosen = false;
            int randomChosenTarget = -1;
            while (!chosen)
            {
                randomChosenTarget = Random.Range(2, (int)Math.Floor(d_mapSize*d_mapSize/2));
                if (!targetNodeIDs.Contains(randomChosenTarget) && !IsAdjacentToTargetNodes(randomChosenTarget))
                {
                    chosen = true;
                }
            }
            
            AddTargetNodeID(randomChosenTarget);

            int oppositeChosenTarget = mapSize*mapSize-(randomChosenTarget-1);
            AddTargetNodeID(oppositeChosenTarget);
        }
        mainCam.transform.position = new Vector3(0,12.5f+2.5f*(mapSize-3),0);
    }

    private void AddTargetNodeID(int nodeID)
    {
        targetNodeIDs.Add(nodeID);
        Node toTarget = Node.GetNode(nodeID);
        toTarget.ForceMakeTarget();
    }

    private bool IsAdjacentToTargetNodes(int nodeID)
    {
        foreach (int otherNodeID in targetNodeIDs)
        {
            foreach(NodeLink link in nodeLinksList)
            {
                if (link.IsLinkBetween(nodeID,otherNodeID))
                {
                    return true;
                }
            } 
        }
        return false;
    }

    [Obsolete("SetupBoard_legacy uses an older numbering scheme. Use the non-legacy version")]
    void SetupBoard_legacy()
    {   
        // load file for setting the board up. Format: each rank of the board (in number of nodes)
        float totalHorizLength = (mapSize * 2 - 2) * nodeHorizDist;
        int currentNodeID = 1;
        int nodeCountAtRank = 1;
        int nodeCountPrevRank = 0;
        List<int> middleRankNodes = new();
        for ( int i=0; i < (mapSize * 2 - 1) ; i++ )
        {
            for ( int j=0; j < nodeCountAtRank; j++ )
            {
                float zLoc = (totalHorizLength/2)-((mapSize * 2 - 1)-(i+1))*nodeHorizDist;
                float xLoc = (nodeVertDist*(nodeCountAtRank-1)/2)-nodeVertDist*j;
                Node newNode = Instantiate(nodePrefab, new Vector3(xLoc, 0, zLoc), Quaternion.identity);
                newNode.nodeID = currentNodeID;
                newNode.gameObject.name = "Node"+currentNodeID;
                nodesDict.Add(currentNodeID, newNode);
                // Setup Spawn Points
                if(i== (mapSize * 2 - 2) && j==nodeCountAtRank-1)
                    newNode.gameObject.tag = "HunterSpawn";
                else if(currentNodeID == 1)
                    newNode.gameObject.tag = "HiddenSpawn";
                //add middle rank nodes
                if (i == (mapSize-1))
                {
                    middleRankNodes.Add(currentNodeID);
                }
                if(i>0)
                {
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
                        NodeLink newLink = new(currentNodeID,otherNode);
                        nodeLinksList.Add(newLink);
                        if(flag)
                            break;
                    }
                }
                currentNodeID++;
            }
            nodeCountPrevRank = nodeCountAtRank;
            if (currentNodeID*2 <= mapSize*mapSize) 
                nodeCountAtRank++;
            else 
                nodeCountAtRank-=1;
        }
        middleRankNodes = ShuffleList(middleRankNodes);
        for(int i=0;i<maxObjectives;i++)
        {
            int nodeID = middleRankNodes[i];
            targetNodeIDs.Add(nodeID);
            Node toTarget = Node.GetNode(nodeID);
            toTarget.isTarget = true;
            toTarget.ForceMakeTarget();
        }
        mainCam.transform.position = new Vector3(0,12.5f+2.5f*(mapSize-3),0);
    }

    // Sets up the players' positions upon first starting, and generates visual representations for each (if applicable).
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

    // Move a player to a node, expending moves as required. Automatically logs movement & prunes movement path based on remaining 
    public void TryMoveToNode(int toMoveTo)
    {
        PlayerPiece toMovePlayerPiece = GetCurrentPlayerPiece();
        int[] movePath = GetCappedPath(toMovePlayerPiece.currentNodeID, toMoveTo, currentPlayerMoves);
        if(movePath.Length <= 0)
        {
            return;
        }
        currentPlayerMoves -= (movePath.Length-1);
        toMoveTo = movePath.Last();
        if(toMoveTo==toMovePlayerPiece.currentNodeID)
        {
            return;
        }
        if(infectedNodeIDs.Contains(toMoveTo))
        {
            if (localPlayerID != 0 || currentPlayerMoves <= 1)
            return;    
        }        
        //Debug.Log(currentPlayerMoves);
        if(logToCSV)
        {
            FileLogger.mainInstance.WriteLineToLog($"{roundCount},{GetTurnNumber()},{currentTurnPlayer},0,{toMovePlayerPiece.currentNodeID},{toMoveTo}");
        }
        if(useSmoothMove)
        {
            toMovePlayerPiece.TrySmoothMove(toMoveTo,movePath);
        }
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
                Node lineDestNode = Node.GetNode(node2);
                GameObject lineDrawer = GameObject.Find("nodeLine" + node1 + "-" + node2) ?? GameObject.Find("nodeLine" + node2 + "-" + node1);
                if (lineDrawer==null)
                    break;
                lineDrawer.GetComponent<LineRenderer> ().material = toHighlight ? lineDestNode.lineActiveMat : lineDestNode.lineMat;
            }
        }
    }
    public void TrySpecialAction(Node thisNode = null)
    {
        if(currentPlayerMoves < 1)
            return;
        if (currentTurnPlayer == 0 && currentPlayerDidSpecialAction)
            return;
        PlayerPiece toMovePlayerPiece = GetCurrentPlayerPiece();
        Node playerNode = Node.GetNode(toMovePlayerPiece.currentNodeID);
        if(thisNode == null)
        {
            thisNode = playerNode
        }
        if (currentTurnPlayer == 0)
        {
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
                if(logToCSV)
                {
                    FileLogger.mainInstance.WriteLineToLog($"{roundCount},{GetTurnNumber()},{currentTurnPlayer},1,{toMovePlayerPiece.currentNodeID},{thisNode.nodeID}");
                }        
                currentPlayerDidSpecialAction = true;
                gameHud.playerActionButtonDown = false;
                TryNodeInfect(thisNode);
            }
        }
        else
        {
            currentPlayerDidSpecialAction = true;
            gameHud.playerActionButtonDown = false;
            currentPlayerMoves--;
            // Currently testing the "track" action. This will be handled immediately from the GameHud
            //TryNodeScan(thisNode);
            TryNodeTrack(playerNode);
        }
    }

    //Infect the specified node. (Trojan Player only)
    public void TryNodeInfect(Node toInfect)
    {
        nodeWasInfectedLastTurn = true;
        lastInfectedNode = toInfect.nodeID;
        infectedNodeIDs.Add(toInfect.nodeID);
        toInfect.Infect();
        if (targetNodeIDs.Contains(toInfect.nodeID) && targetNodeIDs.All(node=> infectedNodeIDs.Contains(node)))
        {
            EndGame(true);
        }

    }

    //Scan the specified node and receive the distance (Scanner Players)
    /*public void TryNodeScan(Node toScan)
    {
        Vector3 offset = new(0,0.55f,0);

        Node hiddenPlayerNode = Node.GetNode(hiddenPlayerLocation);
        int dist = GetPathLength(toScan.nodeID, hiddenPlayerNode.nodeID);
        scanHistory.Add((toScan.nodeID, dist));
        Debug.Log($"The Distance to the Trojan from node {toScan.nodeID} is {dist} Nodes");
        DistanceTextPopup textPopup = Instantiate(textPopupPrefab, new Vector3(0, 0, 0), Quaternion.identity);
        textPopup.transform.position = transform.position = toScan.transform.position + offset;
        textPopup.SetText(dist.ToString()+" Away",mainCam);
        if(toScan.nodeID == hiddenPlayerLocation)
        {
            EndGame(false);
            return;
        }       
    }*/

    private readonly string[] arrowDirs = {"<-","<-","->","->"};
    public void TryNodeTrack(Node toTrack)
    {
        Vector3 offset = new(0,0.55f,0);
        List<int> closestNodes = new();
        int[] adjs = GetAdjacentNodes(toTrack.nodeID);
        if(toTrack.nodeID == hiddenPlayerLocation)
        {
            EndGame(false);
            return;
        }      
        int[] adjsDist = adjs.Select(id=> GetPathLength(id, hiddenPlayerLocation)).ToArray();
        int minDist = adjsDist.Min();
        
        // use the arrow dirs predefined strings.. IF they even are valid
        string[] arrowDirsFiltered = arrowDirs.Where((str, index) => GetAdjacentNodesExist(toTrack.nodeID)[index]).ToArray();
        for(int i = 0; i<adjs.Length; i++){
            if(adjsDist[i] == minDist)
            {
                closestNodes.Add(adjs[i]);
                DistanceTextPopup textPopup = Instantiate(textPopupPrefab, new Vector3(0, 0, 0), Quaternion.identity);
                textPopup.transform.position = transform.position = Node.GetNode(adjs[i]).transform.position + offset;
                textPopup.SetText(arrowDirsFiltered[i], mainCam);
            }
        }
        scanHistory.Add((toTrack.nodeID, closestNodes.ToArray());
        Debug.Log($"Trojan player is in direction of node(s) {string.Join(" and ", closestNodes)}");
    }
    //
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
        currentPlayerPiece.setNode(destID,useSmoothMove);
    }
    public int GetActivePlayerPosition()
    {
        if(currentTurnPlayer==0)
        {
            return hiddenPlayerLocation;
        }
        else 
        {
            return hunterPlayerLocations[currentTurnPlayer-1];
        }
    }
    public void ProgressTurn(bool increment = true)
    {
        if(gameEnded)
            return;
        if(infectedNodeIDs.Contains(GetActivePlayerPosition()))
        // Don't let them end the turn if they're on an infected node: stop stalling
        {
            return;    
        }
        if(increment)
        {
            turnCount++;
            currentTurnPlayer++;
            currentTurnPlayer %= this.playersCount;
            if(this.hotSeatMode)
            {
                localPlayerID = currentTurnPlayer;
            }
            if(GetTurnNumber()>maxTurnCount)
            {
                EndGame(false);
            }
        }
        gameHud.ResetPlayerActionButton();
        currentPlayerMoves = movesCount;
        currentPlayerDidSpecialAction = false;
        
        // If flag is marked then re-cache the paths, they'll be inaccurate now.
        if(nodeWasInfectedLastTurn)
        {
            cachedPaths = new Dictionary<(int,int), int[]>();
            nodeWasInfectedLastTurn = false;
        }

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
        if(playerBotControllers[currentTurnPlayer] != null)
        {
            Debug.Log("handle automatic turn");
            //Handle automatic turn
            int currentPlayerNode = GetActivePlayerPosition();
            playerBotControllers[currentTurnPlayer].ProcessTurn(currentTurnPlayer, currentPlayerNode, currentPlayerMoves);
        }
    }

    void StartHiddenTurn()
    {
        lastInfectedNode = -1;
        scanHistory = new List<(int,int[])>();
        foreach(KeyValuePair<int,Node> nodePair in nodesDict)
        {
            // Reset node infected state when the hidden player's turn activates
            // However objectives stay infected.
            if(nodePair.Value.IsInfected() && !nodePair.Value.isTarget)
            {
                nodePair.Value.DeInfect();
                infectedNodeIDs.Remove(nodePair.Value.nodeID);
            }
        }
        if(this.localPlayerID==0 && hiddenPlayerPiece == null)
        {
            //Debug.Log("Making new hidden player piece");
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
                int[] highlightedPath = GetCappedPath(localPlayerPiece.currentNodeID, thisNode.nodeID, currentPlayerMoves);
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
    public int GetTurnNumber()
    {
        return (1+Mathf.FloorToInt(GameController.gameController.turnCount / GameController.gameController.playersCount));
    }
    // SEARCHING and CACHING

    /*Movement between nodes can be done between one of four directions.
    Up Left: -(mapsize) from current node id
    Down Left: -1 from current node id
    Up Right: +1 from current node id
    Down Right: +(mapsize) from current node id

    The border nodes (nodes on the edge of the map, with less than 4 connections) are all either:
    within range 1 <= x <= mapsize
    within range (mapsize-1) * mapsize <= x <= mapsize * mapsize
    are of value 1+(mapsize * n) where n is an int 0 <= n
    are of value mapsize * n where n is an int 1 <= n
    */
    public bool[] GetAdjacentNodesExist (int nodeID)
    {
        bool[] tests = {
            (nodeID - mapSize > 0), 
            (nodeID - 1 > 0), 
            (nodeID + mapSize <= mapSize*mapSize), 
            (nodeID + 1 <= mapSize*mapSize)
        };
        return tests;
    }
    public int[] GetAdjacentNodes (int nodeID)
    {
        List<int> connectedNodes = new();
        int[] adjNodesRaw = {nodeID - mapSize, nodeID - 1, nodeID + mapSize, nodeID + 1}
        int[] adjNodes = adjNodesRaw.Where((str, index) => GetAdjacentNodesExist(nodeID)[index]).ToArray();

        /*foreach(NodeLink link in nodeLinksList)
        {
            if(link.ConnectsToNode(nodeID))
            {
                int? otherNode = link.getOtherNode(nodeID);
                if (otherNode != null)
                {
                    
                }
            }
        }*/
        Debug.Log(string.Join(", ", adjNodes));
        return adjNodes;
    }

    public void HighlightAllPaths(bool highlighted = true)
    {
        foreach(GameObject lineDrawer in GameObject.FindGameObjectsWithTag("lineHandler"))
        {
            lineDrawer.GetComponent<LineHandler> ().HighlightPath(highlighted);
        }
    }
    
    public int[] GetCappedPath(int startPointID, int endPointID, int maxhops = -1)
    {
        if (!cachedPaths.TryGetValue((startPointID, endPointID), out int[] foundPathRaw))
        {
            foundPathRaw = this.SearchPath(startPointID, endPointID);
            cachedPaths.Add((startPointID, endPointID), foundPathRaw);
        }
        if (maxhops == -1 || foundPathRaw.Length <= maxhops+1)
            return foundPathRaw;
        else   
            return foundPathRaw.Take(maxhops+1).ToArray();
    }

    public int[] SearchPath(int startPointID, int endPointID)
    //Tried implementing BFS, unsure if the most robust or fast
    //Adapted from python implementation https://stackoverflow.com/questions/8922060/how-to-trace-the-path-in-a-breadth-first-search/50575971#50575971
    {
        int maxDepth = 10;
        //Edge case: same node for start & end
        if (startPointID == endPointID)
            return new int[] {endPointID};

        List<int> checkedIDs = new();
        Queue<(int,int[])> nodePaths = new();
        nodePaths.Enqueue((startPointID,new[] {startPointID}));
        while(nodePaths.Count != 0)
        {
            (int,int[]) vpath = nodePaths.Dequeue();
            if(vpath.Item2.Length>maxDepth)
                return new int[0];
            checkedIDs.Add(vpath.Item1);
            foreach(NodeLink link in nodeLinksList)
            {
                int? otherNode = link.getOtherNode(vpath.Item1);
                if(otherNode.HasValue && infectedNodeIDs.Contains(otherNode.Value) && localPlayerID != 0)
                    continue;
                if(otherNode == endPointID)
                    return vpath.Item2.Concat(new int[] {endPointID}).ToArray();
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
    public int GetPathLength(int startPointID, int endPointID)
    {
        int[] pathTo = GetCappedPath(startPointID,endPointID);
        //Debug.Log($"{string.Join(",", pathTo)}");
        int dist = pathTo.Length-1;
        return dist;
    }
    public float GetDistFromHunters(int nodeID)
    {
        int sum = 0;
        if (playersCount<=1)
            return 0;
        for (int i=0; i<playersCount-1; i++)
        {
            sum += GetPathLength(nodeID,hunterPlayerLocations[i]);
        }
        return sum/(playersCount-1);
    }

    // implementation of in-place shuffle algorithm
    public List<int> ShuffleList(List<int> list)
    {
        Debug.Log(string.Join(",",list));
        for(int i=0; i<list.Count-1; ++i)
        {
            int randIndex = Random.Range(i,list.Count);
            int temp = list[i];
            list[i] = list[randIndex];
            list[randIndex] = temp;
        }
        Debug.Log(string.Join(",",list));
        return list;
    }
}
