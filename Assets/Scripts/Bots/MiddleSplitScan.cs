using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using static System.Math;
public class MiddleSplitScan : BotTemplate
{
    /**
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
    public int followDist = 1;
    // How far to go from previous scanners in the case of a diagonal scan ()
    private bool debugLogging = true;
    protected override int GetSpecialActionTarget()
    {
        GameController gcr = GameController.gameController;
        List<(int,int[])> prevScans = gcr.scanHistory;
        List<int> possibleLocationsList = (from kvp in MiddleSplitScan.possibleLocations where kvp.Value select kvp.Key).ToList();
        if (((double)possibleLocationsList.Count/gcr.mapSize * gcr.mapSize <= 0.5 || MiddleSplitScan.algoPlayerInTurn == gcr.playersCount-2) && possibleLocationsList.Count > 0) 
        {
            // If we are the last scanner or there's a small number of possible locations, scan the nearest
            int closestTarget = possibleLocationsList.Aggregate((id1, id2) => gcr.GetPathLength(id1, currentLocation) < gcr.GetPathLength(id2, currentLocation) ? id1 : id2);
            if(debugLogging)
            {
                Debug.Log($"Closest potential target found: node id {closestTarget}");
            }
            return closestTarget;
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
            foreach(int nodeID in gcr.GetAdjacentNodes(currentLocation, gcr.currentPlayerMoves - 1))
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
                if (Abs(sd - min) < 0.1 && Random.value >= 0.5)
                {
                    markAsMin = true;
                }
                else if (sd < min)
                {
                    markAsMin = true;
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
            return minNode;
        }
    }

    protected override void OnSpecialAction(int specActionTarget)
    {
        GameController gcr = GameController.gameController;
        if (gcr.scanHistory.Count == 0)
            return;
        (int,int[]) prevScan = gcr.scanHistory[^1];
        // recalculate the possible locations
        // prune if they do not meet the requirements
        if(prevScan.Item2.Length >= 1)
        {
            if(debugLogging)
            {
                Debug.Log($"Last scan info: Hidden player was in direction(s) of Node(s) {string.Join(" and ",prevScan.Item2)} from starting node {prevScan.Item1}");
            }
            List<int> nodesInDir = gcr.GetDestsClosestToAdjs(prevScan.Item1, prevScan.Item2);
            List<int> possibleLocsList = (from kvp in MiddleSplitScan.possibleLocations where kvp.Value select kvp.Key).ToList();
            int initialPossibleLocsCount = possibleLocsList.Count;
            if (possibleLocsList.Count > 0)
            {
                int[] possible_truncated = possibleLocsList.Except(nodesInDir).ToArray();
                foreach (int id in possible_truncated)
                {
                    MiddleSplitScan.possibleLocations[id] = false;
                }
            }
            else
            {
                foreach (int id in nodesInDir)
                {
                    MiddleSplitScan.possibleLocations[id] = true;
                }
            }

            possibleLocsList = (from kvp in MiddleSplitScan.possibleLocations where kvp.Value select kvp.Key).ToList();
            Vector3 offset = new(0,0.55f,0);
            int postPossibleLocsCount = possibleLocsList.Count;
            IncrementMood((postPossibleLocsCount <= initialPossibleLocsCount ? 1 : -1) * selfMoodFactor);
            IncrementMoodOthers(true, postPossibleLocsCount <= initialPossibleLocsCount);
            IncrementMoodOthers(false, postPossibleLocsCount > initialPossibleLocsCount);
            // Uncomment to see scan knowledge pool information
            
            foreach (int location in possibleLocsList)
            {
                DistanceTextPopup textPopup = Instantiate(gcr.textPopupPrefab, new Vector3(0, 0, 0), Quaternion.identity);
                textPopup.transform.position = Node.GetNode(location).transform.position + offset;
                textPopup.SetText(location.ToString(), gcr.mainCam);
                textPopup.SetColor(Color.red);
            }
        }
    }

    protected override int GetMovementTarget(int specActionTarget)
    {
        GameController gcr = GameController.gameController;
        List<int> possibleLocationsList = (from kvp in MiddleSplitScan.possibleLocations where kvp.Value select kvp.Key).ToList();
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
        MiddleSplitScan.algoPlayerLocs[MiddleSplitScan.algoPlayerInTurn] = moveTarget;
    }
    protected override void HandleTurn(int playerID, int actionsLeft)
    {
        GameController gcr = GameController.gameController;
        int mapSizeSq = GameController.gameController.mapSize * GameController.gameController.mapSize;
        MiddleSplitScan.algoPlayerLocs.Add(currentLocation);
        //Initial setup: only do this when directly following hidden turn
        if (MiddleSplitScan.algoPlayerInTurn == 0){
            // Reset this dict when a new round starts
            // Add the hidden player's spawn if it's the first time (first turn expected)
            if (MiddleSplitScan.possibleLocations.Count == 0)
            {
                for ( int i = 1; i <= mapSizeSq; i++ )
                {
                    MiddleSplitScan.possibleLocations.Add(i, false);
                }
            }
            if (gcr.turnCount == 1)
            {
                foreach(int nodeID in gcr.GetAdjacentNodes(gcr.hiddenSpawn.nodeID,3))
                {
                    MiddleSplitScan.possibleLocations[nodeID] = true;
                }
            }
            //If a node was infected add the surrounding nodes to the possible locations info
            int lastInfected = gcr.lastInfectedNode;
            if (lastInfected < mapSizeSq && lastInfected > 0 )
            {
                foreach(int nodeID in gcr.GetAdjacentNodes(lastInfected,3))
                {
                    MiddleSplitScan.possibleLocations[nodeID] = true;
                }
            }
            else
            // If the last infected node is not applicable
            {
                MiddleSplitScan.possibleLocations.Clear();
                for ( int i = 1; i <= mapSizeSq; i++ )
                {
                    MiddleSplitScan.possibleLocations.Add(i, false);
                }
            }
            int[] locationsToPropagate = (from kvp in MiddleSplitScan.possibleLocations where kvp.Value select kvp.Key).ToArray();
            //Grow the search space size if a turn has passed, because the hidden player may have moved.
            foreach(int locID in locationsToPropagate)
            {
                foreach(int nodeID in gcr.GetAdjacentNodes(locID, 3))
                {
                    MiddleSplitScan.possibleLocations[nodeID] = true;
                }
            }
        }
    }
    protected override void OnPlayerTurnEnd()
    {
        MiddleSplitScan.algoPlayerInTurn++;
    }
}