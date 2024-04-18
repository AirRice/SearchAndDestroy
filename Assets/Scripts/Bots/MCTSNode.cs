using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Random = UnityEngine.Random;
using Math = System.Math;
public class MCTSNode
{

    //Breadth-first MCTS algorithm that uses a simple reward function (wins/losses count as +1/-1 respectively)
    //Uses upper confidence bound formula to determine best child node. (reward value/# of simulations + constant * sqrt (2*ln(total simulations for parent/total simulations for current child of parent)))
    //Referenced ai-boson's page on MCTS algorithm (https://ai-boson.github.io/mcts/)
    
    //Variables to describe current game state
    //Positions of players
    protected int[] playerPos;
    //currently infected nodes
    protected List<int> infectedNodes;
    //Winner of current state if any
    protected (int, int) lastActionTaken;
    protected int curStateWinner = -1;
    protected int actionsAsPlayer = 0;
    protected int actingPlayer = 0;
    protected int currentTurn = 0;
    protected MCTSNode parentNode;
    protected List<MCTSNode> childNodes;
    protected int occurences = 0;
    protected Dictionary<int, int> resultDict = new Dictionary<int, int>();
    protected Queue<MCTSNode> untraversedNodes = new();
    bool uninited = true;
    private readonly bool debugLogging = true;
    public MCTSNode(int[] playerPos, List<int> infectedNodes, int currentTurn)
    {
        GameController gcr = GameController.gameController;
        resultDict = new Dictionary<int, int>()
        // 0 = Trojan wins, 1 = Scanner wins
        {
            {0, 0}, 
            {1, 0}
        };
        this.childNodes = new();
        this.playerPos = (int[])playerPos.Clone();
        this.infectedNodes = new List<int>(infectedNodes);
        this.currentTurn = currentTurn;
    }
    public MCTSNode GetSpecialActionNode(int playerID, int actionsDone, int targetNodeID)
    {
        GameController gcr = GameController.gameController;
        if (actionsDone >= gcr.movesCount)
        {
            return null;
        }
        if (playerPos.Count() < playerID + 1 || playerID < 0)
        {
            return null;
        }
        if (playerID == 0 && !gcr.GetAdjacentNodes(playerPos[playerID]).Contains(targetNodeID))
        {
            return null;
        }
        else if (playerID != 0 && targetNodeID != playerPos[playerID])
        {
            return null;
        }
        if (infectedNodes.Contains(targetNodeID) && playerID == 0)
        {
            return null;
        }
        MCTSNode newNode = new(playerPos, infectedNodes, currentTurn)
        {
            lastActionTaken = (1, targetNodeID)
        };

        if (playerID == 0)
        {
            newNode.infectedNodes.Add(targetNodeID);
            if (gcr.targetNodeIDs.Except(newNode.infectedNodes).Count() <= 0)
            {
                newNode.curStateWinner = 0;
            }
        }
        newNode.curStateWinner = this.curStateWinner;
        if (playerID != 0 && playerPos[0] == playerPos[playerID])
        {
            newNode.curStateWinner = 1;
        }
        if (currentTurn >= gcr.maxTurnCount)
        {
            newNode.curStateWinner = 1;
        }
        newNode.parentNode = this;

        if(debugLogging)
        {
            Debug.Log($"Possible next action found: player {playerID} special action from {playerPos[playerID]} targeting {targetNodeID}");
        }
        return newNode;
    }
    public MCTSNode GetMoveNode(int playerID, int actionsDone, int targetNodeID)
    {
        GameController gcr = GameController.gameController;
        if (actionsDone > gcr.movesCount)
        {
            return null;
        }
        if (playerPos.Count() < playerID + 1 || playerID < 0)
        {
            return null;
        }
        if (!gcr.GetAdjacentNodes(playerPos[playerID]).Contains(targetNodeID))
        {
            return null;
        }
        if (infectedNodes.Contains(targetNodeID) && playerID != 0)
        {
            return null;
        }
        int[] newPlayerPos = (int[])playerPos.Clone();
        newPlayerPos[playerID] = targetNodeID;
        MCTSNode newNode = new(newPlayerPos, infectedNodes, currentTurn)
        {
            parentNode = this,
            lastActionTaken = (0, targetNodeID)
        };
        if (currentTurn >= gcr.maxTurnCount)
        {
            newNode.curStateWinner = 1;
        }
        if(debugLogging)
        {
            Debug.Log($"Possible next action found: Move player {playerID} from {playerPos[playerID]} to {targetNodeID}");
        }
        return newNode;
    }
    public MCTSNode[] GetPossibleNextActions()
    {
        GameController gcr = GameController.gameController;
        int newPlayerID = (this.actionsAsPlayer >= gcr.movesCount) ? (this.actingPlayer + 1) % gcr.playersCount : this.actingPlayer;
        int newMoveCount = (this.actionsAsPlayer >= gcr.movesCount) ? 0 : this.actionsAsPlayer+1;
        if (this.actingPlayer == 0 && this.actionsAsPlayer == 0){
            currentTurn++;
            ResetInfectedNodes();
        }
        List<MCTSNode> nodes = new();

        if (newPlayerID != 0)
        {
            MCTSNode newNode = GetSpecialActionNode(newPlayerID, newMoveCount, playerPos[newPlayerID]);
            if (newNode != null)
            {
                newNode.actingPlayer = newPlayerID;
                newNode.actionsAsPlayer = newMoveCount;
                nodes.Add(newNode);
            }
        }
        foreach (int v in gcr.GetAdjacentNodes(playerPos[newPlayerID]))
        {   
            if (newPlayerID == 0)
            {
                MCTSNode newActionNode  = GetSpecialActionNode(newPlayerID, newMoveCount, v);
                if (newActionNode != null)
                {
                    newActionNode.actingPlayer = newPlayerID;
                    newActionNode.actionsAsPlayer = newMoveCount;
                    nodes.Add(newActionNode);
                }
            }
            MCTSNode newNode = GetMoveNode(newPlayerID, newMoveCount, v);
            if (newNode != null)
            {
                newNode.actingPlayer = newPlayerID;
                newNode.actionsAsPlayer = newMoveCount;
                nodes.Add(newNode);
            }
        }
        return nodes.ToArray();
    }
    public MCTSNode SelectNextSimulationNode(MCTSNode[] nodes)
    {
        MCTSNode nextNode = nodes[Random.Range(0,nodes.Length)];
        return nextNode;
    }
    public int SimulateToEnd()
    {
        GameController gcr = GameController.gameController;
        MCTSNode curState = this;
        int depth = 0;
        while (curState.curStateWinner == -1)
        {
            if (depth > 10000)
            {
                return 1-this.actingPlayer;
            }
            MCTSNode nextNode = SelectNextSimulationNode(curState.GetPossibleNextActions().ToArray());
            curState = nextNode;
            depth++;
            if(debugLogging)
            {
                Debug.Log($"New action count: {curState.actionsAsPlayer}, New player: {curState.actingPlayer}, Turn {curState.currentTurn}");
                Debug.Log($"Infected Nodes: {string.Join(",", curState.infectedNodes)}");
                Debug.Log("Simulating to ending: Current node depth " + depth.ToString() + ", current state winner " + curState.curStateWinner);
            }

        }
        return curState.curStateWinner;
    }

    public void BackpropagateNode(int winner)
    {
        occurences++;
        resultDict[winner]++;
        if (this.parentNode != null)
        {
            this.parentNode.BackpropagateNode(winner);
        }
    }
    public MCTSNode ExploreTree()
    {
        GameController gcr = GameController.gameController;
        MCTSNode curState = this;
        int tries = 10000;
        while (curState.curStateWinner == -1)
        {
            if (tries < 0)
            {
                break;
            }
            if (this.untraversedNodes.Count > 0)
            {
                return untraversedNodes.Dequeue();
            }
            else
            {
                if (curState.uninited)
                {
                   MCTSNode[] nextNodes = curState.GetPossibleNextActions();
                    foreach( MCTSNode node in nextNodes)
                    {
                        curState.childNodes.Add(node);
                        curState.untraversedNodes.Enqueue(node);
                    }
                    curState.uninited = false;
                }
                else
                {
                    curState = curState.GetBestChildState();
                }
            }
            tries--;
        }
        return curState;
    }
    public MCTSNode GetBestChildState()
    {
        // return the child node with the best UCB value
        return childNodes.Aggregate((node1, node2) => MCTSNode.UCBvalue(node1) > MCTSNode.UCBvalue(node2) ? node1 : node2);
    }
    public static double UCBvalue (MCTSNode curState, double ucbConstant = 0.1)
    {
        if (curState.occurences == 0)
        {
            return -1;
        }
        int playerTeam = (curState.actingPlayer != 0) ? 1 : 0;
        int reward = curState.resultDict[playerTeam] - curState.resultDict[1 - playerTeam];
        return (reward / curState.occurences + ucbConstant * Math.Sqrt((2 * Math.Log(curState.parentNode.occurences/curState.occurences))));
    }
    public void ResetInfectedNodes()
    {
        GameController gcr = GameController.gameController;
        infectedNodes = infectedNodes.Intersect(gcr.targetNodeIDs).ToList();
    }
    public (int, int) GetBestNextAction()
    {
        int simulationCount = 100;
        for (int i = 0; i < simulationCount; i++)
        {
            Debug.Log($"Simulation {i}");
            MCTSNode checkNode = ExploreTree();
            checkNode.BackpropagateNode(checkNode.SimulateToEnd());
        }
        return GetBestChildState().lastActionTaken;
    }
}
