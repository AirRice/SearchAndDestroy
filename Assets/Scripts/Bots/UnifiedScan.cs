using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using static System.Math;
using Random = UnityEngine.Random;
public class UnifiedScan : BotTemplate
{
    //Node ID dict: int node ID, bool is if possible. Initialised in this way to reduce overhead when iterating.
    public static Dictionary<int,bool> possibleLocations = new();
    public float doClosestNodeRatio = 0.5f;
    public float smallPossibleLocsRatio = 0.25f;
    private bool debugLogging = false;
    public override List<int> GetSuspectedTrojanLocs()
    {
        return (from kvp in UnifiedScan.possibleLocations where kvp.Value select kvp.Key).ToList();
    }
    protected bool ShouldScanClosest()
    {
        GameController gcr = GameController.gameController;
        List<int> possibleLocationsList = GetSuspectedTrojanLocs();
        if (playerID == gcr.playersCount - 1)
        {
            //last scanner should ALWAYS scan closest
            return true;
        }
        //if number of possible locations is low scan closest
        else if (((double)possibleLocationsList.Count/(gcr.mapSize * gcr.mapSize) <= doClosestNodeRatio) && possibleLocationsList.Count > 0) 
        {  
            return true;
        }
        else if (Random.value >= cautious_factor)
        {
            return true;
        }
        return false;
    }
    protected override int GetSpecialActionTarget()
    {
        GameController gcr = GameController.gameController;
        List<(int,int[])> prevScans = gcr.scanHistory;
        List<int> possibleLocationsList = GetSuspectedTrojanLocs();
        // If no info exists for the turn, scan in place just for some information
        if (GetSuspectedTrojanLocs().Count <= 0)
        {
            if (debugLogging)
                Debug.Log("No info found - scanning in place");
            return currentLocation;
        }
        else if (ShouldScanClosest())
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
            // Out of the spaces within 2 nodes, find the space that gives the most information when splitting the currently assumed locations of the trojan.
            // (We calculate this with the standard deviation of the amounts of nodes per scan direction)
            // - If similar information can be found, pick one randomly.
            int[] allNodes = Enumerable.Range(1, gcr.mapSize*gcr.mapSize+1).ToArray();
            double min = 9999;
            int minNode = -1;
            foreach(int nodeID in gcr.GetAdjacentNodes(currentLocation, gcr.currentPlayerMoves - 1))
            {
                int[] hunterPositions = gcr.GetHunterPos();
                foreach(int allyPos in hunterPositions)
                {
                    if (allyPos != currentLocation && gcr.GetIsNodeDiagonalFromSource(allyPos, nodeID))
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

    public void ExternalSpecialAction(int specActionTarget)
    {
        OnSpecialAction(specActionTarget);
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
            actionLog.Add(new PlayerAction(1, prevScan.Item1, prevScan.Item2));
            if(debugLogging)
            {
                Debug.Log($"Last scan info: Hidden player was in direction(s) of Node(s) {string.Join(" and ",prevScan.Item2)} from starting node {prevScan.Item1}");
            }
            List<int> nodesInDir = gcr.GetDestsClosestToAdjs(prevScan.Item1, prevScan.Item2);
            List<int> possibleLocsList = GetSuspectedTrojanLocs();
            List<int> initialPossibleLocs = possibleLocsList;
            if (possibleLocsList.Count > 0)
            {
                int[] possible_truncated = possibleLocsList.Except(nodesInDir).ToArray();
                foreach (int id in possible_truncated)
                {
                    UnifiedScan.possibleLocations[id] = false;
                }
            }
            else
            {
                foreach (int id in nodesInDir)
                {
                    UnifiedScan.possibleLocations[id] = true;
                }
            }
            // Don't consider already infected nodes
            foreach (int id in possibleLocsList)
            {
                if (gcr.infectedNodeIDs.Contains(id))
                {
                    UnifiedScan.possibleLocations[id] = false;
                }
            }
            possibleLocsList = GetSuspectedTrojanLocs();
            List<int> postPossibleLocs = possibleLocsList;
            if (initialPossibleLocs.Intersect(postPossibleLocs).ToArray().Length <= initialPossibleLocs.Count * 0.1)
            {
                // Bad scan: The new scan area has almost nothing in common with the original scan.
                IncrementMood(selfMoodFactor * -1);
                IncrementMoodOthers(true, false);
                IncrementMoodOthers(false, true);
            }
            if (prevScan.Item2.Length == 1 || ((double)possibleLocsList.Count/(gcr.mapSize * gcr.mapSize) <= smallPossibleLocsRatio) && possibleLocsList.Count > 0)
            {
                // It's a lucky scan right on the diagonal or there is a small amount of possible locations, this gives a lot of info.
                IncrementMood(selfMoodFactor);
                IncrementMoodOthers(true, true);
                IncrementMoodOthers(false, false);
            }
            /*IncrementMood((postPossibleLocsCount <= initialPossibleLocsCount ? 1 : -1) * selfMoodFactor);
            IncrementMoodOthers(true, postPossibleLocsCount <= initialPossibleLocsCount);
            IncrementMoodOthers(false, postPossibleLocsCount > initialPossibleLocsCount);*/
          
            /*Vector3 offset = new(0,0.55f,0);
            foreach (int location in possibleLocsList)
            {
                DistanceTextPopup textPopup = Instantiate(gcr.textPopupPrefab, new Vector3(0, 0, 0), Quaternion.identity);
                textPopup.transform.position = Node.GetNode(location).transform.position + offset;
                textPopup.SetText(location.ToString(), gcr.mainCam);
                textPopup.SetColor(Color.red);
            }*/
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
            if (!gcr.infectedNodeIDs.Contains(potential_target))
            {
                if(debugLogging)
                {
                    Debug.Log($"target node to scan is {specActionTarget}, heading to node {potential_target}");
                }
                return potential_target;
            }
            else
            {
                int[] detourPath = gcr.GetCappedPath(currentLocation, specActionTarget, playerID, 3);
                if (detourPath.Length > 1)
                {
                    int detourPathTarget = detourPath[1];
                    if(debugLogging)
                    {
                        Debug.Log($"target node to scan is {specActionTarget}, heading to node {detourPathTarget}");
                    }
                    return detourPathTarget;
                }
                else
                {
                    // The pathfinding failed, somehow
                    int toMoveTo = SelectNextNodeRandom(currentLocation);
                    if(debugLogging)
                    {
                        Debug.Log($"Moving to node id {toMoveTo} randomly");
                    }
                    return toMoveTo;
                }
            }
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
    protected override void OnPlayerTurnEnd()
    {
        GameController gcr = GameController.gameController;
        float objsRatio = (float)gcr.infectedNodeIDs.Intersect(gcr.targetNodeIDs).ToArray().Length/gcr.maxObjectives;
        float turnRatio = (float)gcr.GetTurnNumber()/gcr.maxTurnCount;
        
        // Pressure the scanner: affect their mood based on the difference in ratio between current turn/max turn vs taken objectives/max objectives
        IncrementMood(selfMoodFactor * turnRatio * ((turnRatio > objsRatio) ? 1 : -1));
    }
    protected override void HandleTurn(int playerID, int actionsLeft)
    {
        GameController gcr = GameController.gameController;
        int mapSizeSq = GameController.gameController.mapSize * GameController.gameController.mapSize;
        //Initial setup: only do this when directly following hidden turn
        if (gcr.shouldUpdateScannerKnowledge){
            gcr.shouldUpdateScannerKnowledge = false;
            // Reset this dict when a new round starts
            if (gcr.turnCount == 1 || UnifiedScan.possibleLocations.Count == 0)
            {
                UnifiedScan.possibleLocations.Clear();
                // Add the hidden player's spawn if it's the first time (first turn expected)
                for ( int i = 1; i <= mapSizeSq; i++ )
                {
                    UnifiedScan.possibleLocations.Add(i, (i == gcr.hiddenSpawn.nodeID));
                }
            }

            //If a node was infected add the surrounding nodes to the possible locations info
            int lastInfected = gcr.lastInfectedNode;
            if (lastInfected < mapSizeSq && lastInfected > 0 )
            {
                foreach(int nodeID in gcr.GetAdjacentNodes(lastInfected,3))
                {
                    UnifiedScan.possibleLocations[nodeID] = true;
                }
            }

            int[] locationsToPropagate = GetSuspectedTrojanLocs().ToArray();
            //Grow the search space size if a turn has passed, because the hidden player may have moved.
            foreach(int locID in locationsToPropagate)
            {
                foreach(int nodeID in gcr.GetAdjacentNodes(locID, 3))
                {
                    UnifiedScan.possibleLocations[nodeID] = true;
                }
            }
        }
    }
}
