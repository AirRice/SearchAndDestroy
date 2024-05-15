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

    Adjusted algorithm:
    1. If we have up-to-date info on the trojan player's location from revealed information (newly infected node) this turn assume the trojan is within 3 nodes of the infected location.
    2. Out of the spaces within 2 nodes, find the space that gives the most information when splitting the currently assumed locations of the trojan.
     - If similar information can be found, pick one randomly.
    3. Intersect the existing possible tojan locations with the nodes pointed at by the new scan.
    4. If the last scanner, move towards and scan (if possible) the closest node out of the possible locations.
     - Otherwise, repeat steps 2 and 3, disregarding nodes that are on a diagonal from any previous scanner. 
    **/
    public static Dictionary<int,bool> possibleLocations = new(); //Node ID dict: int node ID, bool is if possible. Initialised in this way to reduce overhead when iterating.
    public static int algoPlayerInTurn = 0; // player index only incremented when a player using this algorithm's turn comes around
    public static List<int> algoPlayerLocs = new();
    private int currentScanTarget = -1;
    public int followDist = 1;
    // How far to go from previous scanners in the case of a diagonal scan ()

    protected override int GetSpecialActionTarget()
    {
        GameController gcr = GameController.gameController;
        List<(int,int[])> prevScans = gcr.scanHistory;
        List<int> possibleLocationsList = (from kvp in CollabScan.possibleLocations where kvp.Value select kvp.Key).ToList();
        if (currentScanTarget != -1)
        {
            return currentScanTarget;
        }
        // If the last player
        if (gcr.currentTurnPlayer == gcr.playersCount-1) 
        {
            if (possibleLocationsList.Count > 0)
            {
                int closestTarget = possibleLocationsList.Aggregate((id1, id2) => gcr.GetPathLength(id1, currentLocation) < gcr.GetPathLength(id2, currentLocation) ? id1 : id2);
                if(debugLogging)
                {
                    Debug.Log($"Closest potential target found: node id {closestTarget}");
                }
                return closestTarget;
            }
        }
        else
        {
            // Step 2
            // Out of the spaces within 2 nodes, find the space that gives the most information when splitting the currently assumed locations of the trojan.
            // (We calculate this with the standard deviation of the amounts of nodes per scan direction)
            // - If similar information can be found, pick one randomly.
            int[] allNodes = Enumerable.Range(1, gcr.mapSize*gcr.mapSize+1).ToArray();
            double min = 9999;
            int minNode = -1;
            foreach(int nodeID in gcr.GetAdjacentNodes(currentLocation,2))
            {
                foreach(int allyPos in algoPlayerLocs)
                {
                    if (gcr.GetIsNodeDiagonalFromSource(allyPos, nodeID))
                    {
                        continue;
                    }
                }
                bool markAsMin = false;
                double sd = gcr.ScanStandardDeviation(possibleLocationsList.Count <= 0 ? allNodes : possibleLocationsList.ToArray() ,nodeID);
                if (sd < min)
                {
                    markAsMin = true;
                }
                else if (sd == min)
                {
                    if (algoPlayerInTurn == 0 && Random.value >= 0.5)
                    {
                        markAsMin = true;
                    }
                }
                if (markAsMin)
                {
                    min = sd;
                    minNode = nodeID;
                }           
            }
            if(debugLogging)
            {
                Debug.Log($"Scanning Node {minNode}");
            }
            currentScanTarget = minNode;
            return minNode;
        }
        return -1;
    }

    protected override void OnSpecialAction(int specActionTarget)
    {
        GameController gcr = GameController.gameController;
        if (gcr.scanHistory.Count == 0)
         return;
        (int,int[]) prevScan = gcr.scanHistory[gcr.scanHistory.Count -1];
        currentScanTarget = -1;
        // recalculate the possible locations
        // prune if they do not meet the requirements
        if(prevScan.Item2.Length >= 1)
        {
            if(debugLogging)
            {
                Debug.Log($"Last scan info: Hidden player was in direction(s) of Node(s) {string.Join(" and ",prevScan.Item2)} from starting node {prevScan.Item1}");
            }
            List<int> nodesInDir = gcr.GetDestsClosestToAdjs(prevScan.Item1, prevScan.Item2);
            List<int> possibleLocsList = (from kvp in CollabScan.possibleLocations where kvp.Value select kvp.Key).ToList();
            foreach (int id in (possibleLocsList.Count > 0) ? possibleLocsList.Except(nodesInDir).ToList() : nodesInDir)
            {
                possibleLocations[id] = false;
            }

        }
    }

    protected override int GetMovementTarget(int specActionTarget)
    {
        GameController gcr = GameController.gameController;
        if (specActionTarget != -1)
        {
            List<int> nodesTowards = gcr.GetClosestAdjToDest(currentLocation, specActionTarget);
            int rand_index = Random.Range(0, nodesTowards.Count);
            int potential_target = nodesTowards[rand_index];
            if(debugLogging)
            {
                Debug.Log($"target node to scan is {specActionTarget}, heading to node {potential_target}");
            }
            return potential_target;
        }
        else
        {
            // This shouldn't happen
            int toMoveTo = SelectNextNodeRandom(currentLocation);
            if(debugLogging)
            {
                Debug.Log($"Moving to node id {toMoveTo} randomly");
            }
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
            //If a node was infected add the surrounding nodes to the possible locations info
            int lastInfected = gcr.lastInfectedNode;
            if (lastInfected < mapSizeSq && lastInfected > 0 )
            {
                foreach(int nodeID in gcr.GetAdjacentNodes(lastInfected,3))
                {
                    CollabScan.possibleLocations[nodeID] = true;
                }
            }
        }
    }
    protected override void OnPlayerTurnEnd()
    {
        CollabScan.algoPlayerInTurn++;
    }
}