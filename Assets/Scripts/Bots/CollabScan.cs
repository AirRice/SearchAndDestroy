using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class CollabScan : BotTemplate
{
    /**
    This algorithm considers all available players with this algorithm applied as part of the process. it works in the following order.
    1. If we have up-to-date info on the trojan player's location from revealed information (newly infected node) this turn assume the relative sector from the centre as info.
      - However treat this as assumed information; if true information is found it supercedes this.
    2. First scanner player in the round: Move towards the centre of the map as far as possible, scan on arrival.
    3. next scanner player(s) in the round:
      - If the first player could not make it to the centre, move with them.
      - If the trojan player is found to be on an exact diagonal from the first scan: move n spaces perpendicular towards the diagonal from the previous scanner and scan.
      - If not: Move to the centre of the sector of the map we know the trojan player is located in and scan.
    4. repeat step 3 as much as required. 
    **/
    public static Dictionary<int,bool> possibleLocations = new(); //Node ID dict: int node ID, bool is if possible. Initialised in this way to reduce overhead when iterating.
    public static int algoPlayerInTurn = 0; // player index only incremented when a player using this algorithm's turn comes around
    public static List<int> algoPlayerLocs = new();
    public static List<int> tempAssumed = new();
    public static (int,int) tempAssumedSize = (0,0);
    public int followDist = 1;
    // How far to go from previous scanners in the case of a diagonal scan ()

    protected override int GetSpecialActionTarget()
    {
        GameController gcr = GameController.gameController;
        List<(int,int[])> prevScans = gcr.scanHistory;
        // If no info exists and first player:
        if (CollabScan.algoPlayerInTurn == 0 && prevScans.Count <= 0 && CollabScan.tempAssumed.Count <= 0)
        {
            int[] allNodes = Enumerable.Range(1, gcr.mapSize*gcr.mapSize+1).ToArray();
            int centreNode = gcr.GetCentreNode(allNodes);
            return centreNode;
        }
        else if (CollabScan.tempAssumed.Count > 0)
        {
            int centreNode = gcr.GetCentreNode(CollabScan.tempAssumed.ToArray());
            if (prevScans.Count > 0)
            {
                CollabScan.tempAssumed = new();
            }
            return centreNode;
        }
        else if ((from kvp in CollabScan.possibleLocations where kvp.Value select kvp.Key).ToList().Count > 1)
        {
            // In the case that there was a diagonal scan result but still unclear trojan position
            bool flag = false;
            int diagScanSource = -1;
            int diagScanDir = -1;
            foreach ((int, int[]) scan in prevScans)
            {
                if (scan.Item2.Length == 1)
                {
                    flag = true;
                    diagScanSource = scan.Item1;
                    diagScanDir = scan.Item2[0] - scan.Item1;
                    break;
                }
            }
            if (flag && CollabScan.algoPlayerInTurn > 0)
            {
                int dest = -1;
                for(int i = 0; i < followDist; i++)
                {
                    int temp = diagScanSource + diagScanDir * i;
                    if (temp > gcr.mapSize * gcr.mapSize || temp < 0)
                    {
                        break;
                    }
                    else
                    {
                        dest = temp;
                    }
                }
                int[] adjs = gcr.GetAdjacentNodes(dest);
                adjs = adjs.Where(adj => (adj-diagScanSource) % diagScanDir != 0).ToArray();
                return adjs[Random.Range(0,adjs.Length)];
            }
            else
            {
                int centreNode = gcr.GetCentreNode((from kvp in CollabScan.possibleLocations where kvp.Value select kvp.Key).ToArray());
                return centreNode;
            }
        }
        else if ((from kvp in CollabScan.possibleLocations where kvp.Value select kvp.Key).ToList().Count > 0)
        {
            return (from kvp in CollabScan.possibleLocations where kvp.Value select kvp.Key).ToList()[0];
        }
        return -1;
    }
    
    protected override int GetMovementTarget()
    {
        GameController gcr = GameController.gameController;

        if (GetSpecialActionTarget() != -1)
        {
            List<int> nodesTowards = gcr.GetClosestAdjToDest(currentLocation,GetSpecialActionTarget());
            int rand_index = Random.Range(0, nodesTowards.Count);
            int potential_target = nodesTowards[rand_index];
            Debug.Log($"target node to scan is {GetSpecialActionTarget()}, heading to node {potential_target}");
            return potential_target;
        }
        else
        {
            // This shouldn't happen
            int toMoveTo = SelectNextNodeRandom(currentLocation);
            Debug.Log($"Moving to node id {toMoveTo}");
            return toMoveTo;
        }
    }
    protected override void OnMove(int moveTarget)
    {
        CollabScan.algoPlayerLocs[CollabScan.algoPlayerInTurn] = moveTarget;
    }
    protected override void HandleTurn(int playerID, int actionsLeft)
    {
        GameController gcr = GameController.gameController;
        int mapSizeSq = GameController.gameController.mapSize * GameController.gameController.mapSize;
        List<(int,int[])> prevScans = gcr.scanHistory;
        CollabScan.algoPlayerLocs.Add(currentLocation);
        //Initial setup: only do this when directly following hidden turn
        if (CollabScan.algoPlayerInTurn == 0){
            // Reset this dict when a new round starts
            if (gcr.turnCount == 0)
            {
                CollabScan.possibleLocations = new();
            }
            //Make this list for the first time if it doesn't exist yet.
            if (CollabScan.possibleLocations.Count == 0)
            {
                for ( int i = 1; i <= mapSizeSq; i++ )
                {
                    CollabScan.possibleLocations.Add(i, false);
                }
            }
            int[] allNodes = Enumerable.Range(1, gcr.mapSize*gcr.mapSize+1).ToArray();
            int centreNode = gcr.GetCentreNode(allNodes);

            int[] fromCentreDir = gcr.GetClosestAdjToDest(centreNode, gcr.lastInfectedNode).ToArray();
            CollabScan.tempAssumed = gcr.GetDestsClosestToAdjs(centreNode, fromCentreDir);
        }

        // prune out the possible locations if they do not meet the requirements
        foreach ((int,int[]) info in prevScans)
        {
            if(info.Item2.Length < 1)
                // Ignore this if the info is invalid
                continue;
        
            Debug.Log($"Previous scan info: Hidden player was in direction(s) of Node(s) {string.Join(" and ",info.Item2)} from starting node {info.Item1}");
            List<int> nodesInDir = gcr.GetDestsClosestToAdjs(info.Item1,info.Item2);
            foreach (int id in (from kvp in CollabScan.possibleLocations where kvp.Value select kvp.Key).ToList().Except(nodesInDir).ToArray())
            {
                possibleLocations[id] = false;
            }
        }
    }
    protected override void OnPlayerTurnEnd()
    {
        CollabScan.algoPlayerInTurn++;
    }
}