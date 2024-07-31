using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Text.RegularExpressions; //for the file handling
using System;
using System.IO;
using System.Text;
using System.Linq;
using HuggingFace.API;
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
    public int maxRoundCount = 10;
    public int maxObjectives = 1;
    public bool hotSeatMode = true;
    public bool logToCSV = true;
    public bool useSmoothMove = false;
    public bool autoProgressTurn = false;
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
    public string[] playerBotType;
    private BotTemplate[] playerBotControllers;
    private BotProfile[] playerBotProfiles;
    public Dictionary<int, BotTemplate> playerBotsDict = new();
    public bool gameEnded = false;
    public int turnCount = 0;
    public Node hunterSpawn, hiddenSpawn = null;
    protected int hiddenPlayerLocation;
    protected int[] hunterPlayerLocations;
    public List<NodeLink> nodeLinksList = new();
    public List<(int,int[])> scanHistory;
    //Scan History is (node id, distance to hidden player)
    private Dictionary<(int,int), int[]> cachedPaths = new();
    private bool nodeWasInfectedLastTurn = false;
    public List<string> chatHistory = new();
    public List<int> chattedCurrentTurn;
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
        ConfigDataList cfgList = LoadData.Load();
        ConfigData cfg = new();
        int runNumber = (FileLogger.mainInstance == null) ? 0 : FileLogger.mainInstance.GetCurrentRunCount();
        if (cfgList != null && cfgList.configList.Length > runNumber)
        {
            cfg = cfgList.configList[runNumber];
        }

        mapSize = cfg.mapSize;
        playersCount = cfg.playersCount;
        movesCount = cfg.movesCount;
        maxTurnCount = cfg.maxTurnCount;
        maxRoundCount = cfg.maxRoundCount;
        maxObjectives = cfg.maxObjectives;
        hotSeatMode = cfg.hotSeatMode;
        logToCSV = cfg.logToCSV;
        useSmoothMove = cfg.useSmoothMove;
        autoProgressTurn = cfg.autoProcessTurn;
        playerBotType = cfg.playerBotType;
        
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
        FileLogger.mainInstance.IncrementRound();
        chatHistory.Clear();
        
        if(restart){
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        this.SetupBoard();
        this.SetupBotPlayers();

        if (logToCSV)
        {
            // Log the selected objectives using action 2
            FileLogger.mainInstance.WriteLineToLog($"||2||{string.Join(",", targetNodeIDs)}");
        }
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

        if (FileLogger.mainInstance.GetCurrentRoundCount() < maxRoundCount)
        {
            GameController.gameController.StartGame(true);
        }
        else
        {
            FileLogger.mainInstance.IncrementRunCount();
            FileLogger.mainInstance.Reset();
            if (LoadData.Load().configList.Length <= FileLogger.mainInstance.GetCurrentRunCount())
            {
                Quit();
            }
            else
            {
                GameController.gameController.StartGame(true);
            }
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

    // BOT PROFILES
    public static readonly List<BotProfile> BotProfiles = new()
    {
        new BotProfile("CautiousClosestScanner", "ClosestScan", "Cautious, Professional"),
        new BotProfile("HotHeadedClosestScanner", "ClosestScan", "Hotheaded, Tense, Jumpy"),
        new BotProfile("FriendlyClosestScanner", "ClosestScan", "Friendly, Jovial"),
        new BotProfile("CautiousMiddleSplitScanner", "MiddleSplitScan", "Cautious, Professional"),
        new BotProfile("HotHeadedMiddleSplitScanner", "MiddleSplitScan", "Hotheaded, Tense, Jumpy"),
        new BotProfile("FriendlyMiddleSplitScanner", "MiddleSplitScan", "Friendly, Jovial"),
        new BotProfile("CockyGreedyTrojan", "GreedyTrojan", "Cocky, Witty"),
        new BotProfile("HeatedGreedyTrojan", "GreedyTrojan", "Heated, Hotheaded"),
        new BotProfile("CautiousTrojan", "CautiousTrojan", "Cautious, Cowardly, Nervous")
    };

    // Sets up the AI agents depending on the file-specified bot templates.
    void SetupBotPlayers()
    {
        playerBotControllers = new BotTemplate[playersCount];
        playerBotProfiles = new BotProfile[playersCount];
        for (int i=0; i < playersCount; i++)
        {
            BotProfile botProfile = BotProfiles.Where(v => v.name.Equals(playerBotType[i])).DefaultIfEmpty(null).First();
            Type botType = Type.GetType(botProfile.bottype);
            //Type botType = Type.GetType(playerBotType[i]);
            //Debug.Log(playerBotType[i]);
            //Debug.Log($"Is defined: {botType != null}");
            if (botType != null)
            {
                noBotPlayers = false;
            }
            playerBotProfiles[i] = botProfile;
            playerBotControllers[i] = botType != null ? (BotTemplate) ScriptableObject.CreateInstance(botType) : null;
            if (playerBotControllers[i] != null)
            {
                playerBotControllers[i].SetPlayerID( i );
            }
        }
    }

    public void IncrementMoodMultiple(int origin, bool allies, bool positive)
    {
        for (int i=0; i < playersCount; i++)
        {
            if (i == origin || origin == 0 && allies)
                continue;
            if(origin != 0 && allies && i != 0 || origin != 0 && i == 0 && !allies || origin == 0 && !allies)
            {
                playerBotControllers[i].IncrementMood((positive ? 1 : -1) * (allies ? playerBotControllers[i].friendMoodFactor : playerBotControllers[i].enemyMoodFactor));
            }
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
    private readonly int generateObjsTries = 1000;
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
            for(int j = 0; j < generateObjsTries; j++)
            {
                randomChosenTarget = Random.Range(2, (int)Math.Floor(d_mapSize*d_mapSize/2));
                if (!targetNodeIDs.Contains(randomChosenTarget) && !IsAdjacentToTargetNodes(randomChosenTarget))
                {
                    break;
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
        if (logToCSV)
        {
            // Log starting positions of players: Action 3
            for(int i = 0; i<playersCount; i++)
            {
                FileLogger.mainInstance.WriteLineToLog($"|{i}|3|{(i==0 ? hiddenPlayerLocation : hunterPlayerLocations[i-1])}|");
            }
        }
        ProgressTurn(false);
    }

    // Move a player to a node, expending moves as required. Automatically logs movement & prunes movement path based on remaining 
    // returns the int we ended up at
    public int TryMoveToNode(int toMoveTo)
    {
        int[] movePath = GetCappedPath(GetActivePlayerPosition(), toMoveTo, currentPlayerMoves);
        if(movePath.Length <= 0)
        {
            return -1;
        }
        toMoveTo = movePath.Last();
        if(toMoveTo==GetActivePlayerPosition())
        {
            return -1;
        }
        if(infectedNodeIDs.Contains(toMoveTo))
        {
            if (localPlayerID != 0 || currentPlayerMoves <= 1)
            return -1;    
        }        
        currentPlayerMoves -= (movePath.Length-1);
        //Debug.Log(currentPlayerMoves);
        if(logToCSV)
        {
            FileLogger.mainInstance.WriteLineToLog($"{GetTurnNumber()}|{currentTurnPlayer}|0|{GetActivePlayerPosition()}|{toMoveTo}");
        }
        GenerateStatusString(currentTurnPlayer, 0, GetActivePlayerPosition(), new[] {toMoveTo}, 0.25f, playerBotProfiles[currentTurnPlayer].personality);
        if(useSmoothMove)
        {
            PlayerPiece toMovePlayerPiece = GetCurrentPlayerPiece();
            toMovePlayerPiece.TrySmoothMove(toMoveTo,movePath);
        }
        UpdateActivePlayerPosition(toMoveTo);
        return toMoveTo;
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
    public bool TrySpecialAction(Node thisNode = null)
    {
        if(currentPlayerMoves < 1)
            return false;
        if (currentTurnPlayer == 0 && currentPlayerDidSpecialAction)
            return false;
        Node playerNode = Node.GetNode(GetActivePlayerPosition());
        if(thisNode == null)
        {
            thisNode = playerNode;
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
                currentPlayerDidSpecialAction = true;
                gameHud.playerActionButtonDown = false;
                return TryNodeInfect(thisNode);
            }
        }
        else
        {
            currentPlayerDidSpecialAction = true;
            gameHud.playerActionButtonDown = false;
            currentPlayerMoves--;
            // Currently testing the "track" action. This will be handled immediately from the GameHud
            //TryNodeScan(thisNode);
            return TryNodeTrack(playerNode);
        }
        return false;
    }
    private readonly string[] TrojanPersonality = {"gloating", "arrogant", "playful"};
    private readonly string[] ScannerPersonality = {"professional", "stoic", "determined"};
    //Infect the specified node. (Trojan Player only)
    public bool TryNodeInfect(Node toInfect)
    {
        nodeWasInfectedLastTurn = true;
        lastInfectedNode = toInfect.nodeID;
        infectedNodeIDs.Add(toInfect.nodeID);
        toInfect.Infect();
        if(logToCSV)
        {
            FileLogger.mainInstance.WriteLineToLog($"{GetTurnNumber()}|{currentTurnPlayer}|1|{GetActivePlayerPosition()}|{toInfect.nodeID}");
        }
        GenerateStatusString(currentTurnPlayer, 1, GetActivePlayerPosition(), new[] {toInfect.nodeID}, 1, playerBotProfiles[currentTurnPlayer].personality);
        if (targetNodeIDs.Contains(toInfect.nodeID) && targetNodeIDs.All(node=> infectedNodeIDs.Contains(node)))
        {
            EndGame(true);
            return true;
        }
        return false;
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
    public bool TryNodeTrack(Node toTrack)
    {
        if(toTrack.nodeID == hiddenPlayerLocation)
        {
            if(logToCSV)
            {
                FileLogger.mainInstance.WriteLineToLog($"{GetTurnNumber()}|{currentTurnPlayer}|1|{GetActivePlayerPosition()}|{hiddenPlayerLocation}");
            }    
            EndGame(false);
            return true;
        }
        List<int> closestNodes = GetClosestAdjToDest(toTrack.nodeID, hiddenPlayerLocation);
        Vector3 offset = new(0,0.55f,0);

        foreach(int i in closestNodes){
            DistanceTextPopup textPopup = Instantiate(textPopupPrefab, new Vector3(0, 0, 0), Quaternion.identity);
            textPopup.transform.position = Node.GetNode(i).transform.position + offset;
            textPopup.SetText((i < toTrack.nodeID ? "<-" : "->"), mainCam);
        }
        if(logToCSV)
        {
            FileLogger.mainInstance.WriteLineToLog($"{GetTurnNumber()}|{currentTurnPlayer}|1|{GetActivePlayerPosition()}|{string.Join(',', closestNodes)}");
        }
        GenerateStatusString(currentTurnPlayer, 1, GetActivePlayerPosition(), closestNodes.ToArray(), 1, playerBotProfiles[currentTurnPlayer].personality);
        scanHistory.Add((toTrack.nodeID, closestNodes.ToArray()));
        Debug.Log($"Trojan player is in direction of node(s) {string.Join(" and ", closestNodes)}");
        return false;
    }
    public List<int> GetClosestAdjToDest(int sourceID, int destID)
    {
        List<int> closestNodes = new();
        int[] adjs = GetAdjacentNodes(sourceID);
        int[] adjsDist = adjs.Select(id=> GetPathLength(id, destID)).ToArray();
        int minDist = adjsDist.Min();

        for(int i = 0; i<adjs.Length; i++){
            if(adjsDist[i] == minDist)
            {
                closestNodes.Add(adjs[i]);
            }
        }
        return closestNodes;
    }
    public List<int> GetDestsClosestToAdjs(int sourceID, int[] selectedAdjs)
    {
        int[] adjs = GetAdjacentNodes(sourceID);
        if (selectedAdjs.Length > 2 || selectedAdjs.Length < 0 || adjs.Intersect(selectedAdjs).ToArray().Length <= 0)
        {
            return new List<int>();
        }
        List<int> possibleNodes = new();
        for(int i = 1; i <= mapSize*mapSize; i++)
        {
            if (i == sourceID)
            {
                continue;
            }
            int minDist = adjs.Select(id => GetPathLength(id, i)).Min();
            int[] closestAdjs = adjs.Where(adj => GetPathLength(adj, i) == minDist).ToArray();
            //Debug.Log($"Node {i} closest to adjs {string.Join(", ", closestAdjs)}");
            if(closestAdjs.SequenceEqual(selectedAdjs))
            {
                possibleNodes.Add(i);
            }
        }
        return possibleNodes;
    }

    public bool GetIsNodeDiagonalFromSource(int sourceID, int targetID)
    {
        return (GetClosestAdjToDest(sourceID, targetID).Count == 1);
    }
    public void UpdateActivePlayerPosition(int destID)
    {
        PlayerPiece currentPlayerPiece = GetCurrentPlayerPiece();
        if(currentTurnPlayer==0)
        {
            hiddenPlayerLocation = destID;
        }
        else 
        {
            hunterPlayerLocations[currentTurnPlayer-1] = destID;
        }
        if (currentPlayerPiece != null)
            currentPlayerPiece.setNode(destID,useSmoothMove);
    }
    public int GetActivePlayerPosition()
    {
        return GetPlayerPosition(currentTurnPlayer);
    }
    protected int GetPlayerPosition(int player)
    {
        if(currentTurnPlayer!=0 && player == 0)
        {
            return -1;
        }
        else
        {
            return (player == 0) ? hiddenPlayerLocation : hunterPlayerLocations[player-1];
        }
    }
    public void ProgressTurn(bool increment = true)
    {
        if(gameEnded)
            return;
        if(infectedNodeIDs.Contains(GetActivePlayerPosition()))
        // Don't let them end the turn if they're on an infected node: stop stalling
        // Disabled for now as it was causing errors if a bot lands on the infected node
        {
            //return;    
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
            // Reset the per turn player index for the algorithms
            ClosestScan.algoPlayerInTurn = 0;
            MiddleSplitScan.algoPlayerInTurn = 0;
            MiddleSplitScan.algoPlayerLocs = new();
            chattedCurrentTurn.Clear();
            this.StartHiddenTurn();
        }
        else if(localPlayerID != 0)
        {
            if(hiddenPlayerPiece != null)
                Destroy(hiddenPlayerPiece.gameObject);
        }
        if(playerBotControllers[currentTurnPlayer] != null)
        {
            //Handle automatic turn
            int currentPlayerNode = GetActivePlayerPosition();
            playerBotControllers[currentTurnPlayer].ProcessTurn(currentPlayerNode, currentPlayerMoves);
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
        if(this.localPlayerID==0)
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

    // STRING BUILDING FOR TEXT GENERATION API

    // Prompt tuned for Mistral 7B
    private readonly string tWinCondition = "The objective of the game is to travel next to each of the given objective nodes to successfully infect them. However, the enemy team can capture you by moving to your location. Your \n";
    private readonly string sWinCondition = "The objective of the game is to find and purge the single enemy player before they can infect all the objectives. You do not know the exact location of the enemy player, but you can scan your surroundings to get an estimated heading. You will lose the game if the enemy player successfully infects all the objectives on the map.\n";
    public void GenerateStatusString(int player, int actiontype, int nodeFrom, int[] nodeTargets, float sendChatChance = 1.0f, string personalityParams = null)
    {
        int turn = GetTurnNumber();
        // Only generate this sometimes
        if (Random.value > sendChatChance || chattedCurrentTurn.Contains(player))
        {
            return;
        }

        personalityParams = personalityParams ?? "";
        
        string instruction = $"TASK:\nGenerate a message from player {player} in an in-game discussion at this moment. Also associate one of the given emotions with the generated message. Pick one of these emotions: angry, confused, content, fear, gloating, happy, sad, surprised.\n";

        string gameDefinition = $"CONTEXT:\nPlayer {player}, is participating in a session of \"Search and Destroy\", a multiplayer board game. Player 0 is a Trojan virus infecting a computer system trying to access and infect all {targetNodeIDs.Count} objective nodes on the grid. All other players are Scanners trying to deduce the Trojan's location and purge it. The Trojan's precise location is unknown to the Scanners and they can only find out which direction the trojan is relative to themselves when they scan.";
        
        string gameState = "\nGAME STATE:\nThe players are at nodes:\n";

        for(int i = 0; i < playersCount; i++)
        {
            if (i == 0 && player == 0 || i != 0)
            {
                gameState = gameState + $"Player {i}: Node {GetPlayerPosition(i)}\n";
            }
        }

        string playerCountString = player == 0 ? "the only player" : $"one of {playersCount-1} players";
        string playerInfoString = $"{(player == 0 ? $"Player {player} is the Trojan, being " : $"Player {player} is one of the Scanners, being")} {playerCountString} on their team.";

        playerInfoString = playerInfoString + " Assume the Trojan wants to stay hidden and will not give away information about their location, and Scanner players will try to work together. Assume the messages are directed at other players rather than simply commenting on the game.";
        if (personalityParams.Length > 0 )
        {
            //string personality = string.Join(",", personalityParams);
            playerInfoString = playerInfoString + $"Player {player} has a {personalityParams} personality.";
        }
        if(playerBotControllers[currentTurnPlayer] != null)
        {
            string emotion = playerBotControllers[currentTurnPlayer].GetCurrentEmotion();
            if (emotion.Length > 0)
            {
                playerInfoString = playerInfoString + $" Player {player} is feeling {emotion}.";
            }
        }

        string lastActionString = "";
        switch (actiontype)
        {
            case 0:
                lastActionString = $"moved from node {nodeFrom} to {nodeTargets[0]}";
                break;
            case 1:
                if (player == 0)
                {
                    lastActionString = $"infected node {nodeTargets[0]}";
                }
                else
                {
                    lastActionString = $"scanned for the Trojan, finding that they are in the direction of node(s) {string.Join(",", nodeTargets)}";
                }
                break;
            default:
                break;
        }
        string playerActionString = $" Player {player} has just {lastActionString}.";

        string extraInfoString = $" Out of {targetNodeIDs.Count} objectives, {infectedNodeIDs.Intersect(targetNodeIDs).ToArray().Length} have been infected so far. This is turn number {turn}. ";

        string prevChat = "";
        if (chatHistory.Count > 0)
        {
            prevChat = "\n\nPREVIOUS CHAT LOG:\n";
            foreach(string chat in chatHistory)
            {
                prevChat = prevChat + chat + "\n";
            }
        }

        string responseFormatting = "\nRESPONSE FORMAT:\n {\"player\": <PLAYER NUMBER>, \"emotion\": <EMOTION> (SELECTED BETWEEN angry, confused, content, fear, gloating, happy, sad, surprised),\"message\":<GENERATED MESSAGE>}\n ANSWER:\n";

        string inputText = instruction + gameDefinition + playerInfoString + /* playerActionString + extraInfoString + */prevChat + responseFormatting;
        chattedCurrentTurn.Add(player);
        //Debug.Log(inputText);
        HuggingFaceAPI.TextGeneration(inputText, OnAPIResult, OnAPIError);
        //GroqAPIHandler.TextGeneration(inputText);
    }

    void OnAPIResult(string result)
    {
        Debug.Log(result);
        MatchCollection matches = Regex.Matches(result, @"(\{[^}]+\})");
        TextAPIResponse response = JsonUtility.FromJson<TextAPIResponse>(matches[^1].Value);
        string responseFormatted = ChatFormat(response.message);
        chatHistory.Add($"Player {response.player}: " + responseFormatted);
        gameHud.PlayerDialogue(currentTurnPlayer, response.emotion, responseFormatted);
        
    }
    void OnAPIError(string error) 
    {
        Debug.LogError(error);
    }
    public string ChatFormat(string s)
    {
        StringBuilder sb = new StringBuilder();
        foreach (char c in s)
        {
            sb.Append(c);
            /*if (!char.IsPunctuation(c))
            {
                sb.Append(char.ToLower(c));
            }*/
        }
        return sb.ToString();
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

    public int[] GetAdjacentNodes (int nodeID, int maxdist = 1)
    {
        if (maxdist <= 0)
        {
            return new int[]{nodeID};
        }
        List<int> connectedNodes = new();
        connectedNodes.Add(nodeID);
        connectedNodes = connectedNodes.Union(GetAdjacentNodes(nodeID - mapSize, maxdist-1)).ToList();
        connectedNodes = connectedNodes.Union(GetAdjacentNodes(nodeID + mapSize, maxdist-1)).ToList();
        if(nodeID % mapSize > 1 || nodeID % mapSize == 0)
        {
            connectedNodes = connectedNodes.Union(GetAdjacentNodes(nodeID - 1, maxdist-1)).ToList();
        }
        if(mapSize - (nodeID % mapSize) >= 1 && nodeID % mapSize != 0)
        {
            connectedNodes = connectedNodes.Union(GetAdjacentNodes(nodeID + 1, maxdist-1)).ToList();
        }
        //Debug.Log(string.Join(", ", connectedNodes));
        return connectedNodes.Where(node => NodeIsValid(node)).ToArray();
    }
    
    public int GetCentreNode(int[] nodes)
    {
        //Given some nodes of the graph calculate the centroid point
        //turn the node ids into coordinates
        (int,int,int)[] nodes_coords = nodes.Select(id => (id,(id-1)%mapSize,(id-1)/mapSize)).ToArray();
        int sumX = 0;
        int sumY = 0;
        foreach ((int,int,int) node in nodes_coords)
        {
            sumX += node.Item2;
            sumY += node.Item3;
        }
        // find centre point.
        int centreX = sumX / nodes_coords.Length;
        int centreY = sumY / nodes_coords.Length;
        //Debug.Log((centreY+1) * mapSize + (centreX+1));
        return ((centreY+1) * mapSize + (centreX+1));
    }

    public bool NodeIsValid(int nodeID){
        return (nodeID <= mapSize*mapSize && nodeID > 0);
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
            if (infectedNodeIDs.Intersect(foundPathRaw).ToArray().Count() <= 0)
            {
                cachedPaths.Add((startPointID, endPointID), foundPathRaw);
            }
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
        int maxDepth = 20;
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
    public int[] GetHunterPos()
    {
        return hunterPlayerLocations;
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
    public int GetClosestTargetNodeDist(int nodeID, bool ignoreInfected = true)
    {
        int min = 9999;
        foreach (int targetPos in targetNodeIDs)
        {
            if(ignoreInfected && infectedNodeIDs.Contains(targetPos))
                continue;
            int targetdist = GetPathLength(nodeID,targetPos);
            if (targetdist < min)
            {
                min = targetdist;
            }
        }
        return min;
    }

    public double ScanStandardDeviation(int[] nodesToAssess, int scanPos)
    {
        Dictionary<String, int> eachDirNodes = new();
        foreach (int nodeID in nodesToAssess)
        {
            string identifier = string.Join("|", GetClosestAdjToDest(scanPos,nodeID));
            if (eachDirNodes.ContainsKey(identifier))
            {
                eachDirNodes[identifier]++;
            }
            else
            {
                eachDirNodes.Add(identifier,1);
            }
        }
        if (eachDirNodes.Count > 0)
        {
            int[] values = eachDirNodes.Values.ToArray();
            double avg = values.Average();
            return Math.Sqrt(values.Average(v=>Math.Pow(v-avg,2)));
        }
        return -1;
    }

    // implementation of in-place shuffle algorithm
    public List<int> ShuffleList(List<int> list)
    {
        for(int i=0; i<list.Count-1; ++i)
        {
            int randIndex = Random.Range(i,list.Count);
            int temp = list[i];
            list[i] = list[randIndex];
            list[randIndex] = temp;
        }
        return list;
    }
}

public class BotProfile{
    public string name;
    public string bottype;
    public string personality;
    public BotProfile(string name, string bottype, string personality){
        this.name = name;
        this.bottype = bottype;
        this.personality = personality;
    }
}

[System.Serializable]
public class TextAPIResponse{
    public int player;
    public string emotion;
    public string message;
}