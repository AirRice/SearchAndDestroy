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
        this.playerPos = playerPos;
        this.infectedNodes = infectedNodes;
    }
    public MCTSNode GetSpecialActionNode(int playerID, int actionsDone, int targetNodeID)
    {
        GameController gcr = GameController.gameController;
        // if (actionsDone >= gcr.movesCount)
        // {
        //     return null;
        // }
        // if (playerPos.Count() < playerID + 1 || playerID < 0)
        // {
        //     return null;
        // }
        // if (playerID == 0 && !gcr.GetAdjacentNodes(playerPos[playerID]).Contains(targetNodeID))
        // {
        //     return null;
        // }
        // else if (playerID != 0 && targetNodeID != playerPos[playerID])
        // {
        //     return null;
        // }
        if (infectedNodes.Contains(targetNodeID) && playerID == 0)
        {
            return null;
        }
        MCTSNode newNode = new(playerPos, infectedNodes, (playerID == 0 && actionsDone == 0) ? currentTurn+1 : currentTurn);
        newNode.lastActionTaken = (1, targetNodeID);
        
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

        return newNode;
    }
    public MCTSNode GetMoveNode(int playerID, int actionsDone, int targetNodeID)
    {
        GameController gcr = GameController.gameController;
        // if (actionsDone >= gcr.movesCount)
        // {
        //     return null;
        // }
        // if (playerPos.Count() < playerID + 1 || playerID < 0)
        // {
        //     return null;
        // }
        // if (!gcr.GetAdjacentNodes(playerPos[playerID]).Contains(targetNodeID))
        // {
        //     return null;
        // }
        if (infectedNodes.Contains(targetNodeID) && playerID != 0)
        {
            return null;
        }
        int[] newPlayerPos = playerPos;
        newPlayerPos[playerID] = targetNodeID;
        MCTSNode newNode = new(newPlayerPos, infectedNodes, (playerID == 0 && actionsDone == 0) ? currentTurn+1 : currentTurn);
        newNode.parentNode = this;
        newNode.lastActionTaken = (0, targetNodeID);
        if (currentTurn >= gcr.maxTurnCount)
        {
            newNode.curStateWinner = 1;
        }
        return newNode;
    }
    public MCTSNode[] GetPossibleNextActions(int newMoveCount, int newPlayerID)
    {
        GameController gcr = GameController.gameController;
        List<MCTSNode> nodes = new();

        if (newPlayerID != 0)
        {
            MCTSNode newNode = GetSpecialActionNode(newPlayerID, newMoveCount, playerPos[newPlayerID]);
            if (newNode != null)
            {
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
                    newActionNode.actionsAsPlayer = newMoveCount;
                    nodes.Add(newActionNode);
                }
            }
            MCTSNode newNode = GetMoveNode(newPlayerID, newMoveCount, v);
            if (newNode != null)
            {
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
        while (curState.curStateWinner == -1)
        {
            int newPlayerID = (curState.actionsAsPlayer >= gcr.movesCount) ? (curState.actingPlayer + 1) % gcr.playersCount : curState.actingPlayer;
            int newMoveCount = curState.actionsAsPlayer+1;
            MCTSNode nextNode = SelectNextSimulationNode(curState.GetPossibleNextActions(newMoveCount,newPlayerID).ToArray());
            curState = nextNode;
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
        while (curState.curStateWinner == -1)
        {
            if (this.untraversedNodes.Count > 0)
            {
                return untraversedNodes.Dequeue();
            }
            else
            {
                if (curState.uninited)
                {
                    Debug.Log("init new node");
                    int newPlayerID = (curState.actionsAsPlayer >= gcr.movesCount) ? (curState.actingPlayer + 1) % gcr.playersCount : curState.actingPlayer;
                    int newMoveCount = curState.actionsAsPlayer+1;
                    MCTSNode[] nextNodes = curState.GetPossibleNextActions(newMoveCount, newPlayerID);
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
        int reward = curState.resultDict[curState.actingPlayer] - curState.resultDict[1 - curState.actingPlayer];
        return (reward / curState.occurences + ucbConstant * Math.Sqrt((2 * Math.Log(curState.parentNode.occurences/curState.occurences))));
    }
    public (int, int) GetBestNextAction()
    {
        int simulationCount = 100;
        for (int i = 0; i < simulationCount; i++)
        {
            MCTSNode checkNode = ExploreTree();
            checkNode.BackpropagateNode(checkNode.SimulateToEnd());
        }
        return GetBestChildState().lastActionTaken;
    }
}
