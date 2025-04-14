using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class SoloScan : BotTemplate
{
    /**
    This algorithm works in the following order.
    1. If no previous scan information/newly infected nodes exist for this turn, scan to gain some information.
    2. Using the info, locate the closest potential location the hidden player could be in at the current moment. Triangulate possible locations from all known info that turn.
    3. Head to the location and scan the node.
    4. If the scanning has not resulted in a scanner victory, carry out step 2-3 again.
    5. Do this until the action allocation runs out.
    **/
    private Dictionary<int,bool> possibleLocations = new();
    public float smallPossibleLocsRatio = 0.25f;
    protected override int GetMovementTarget(int specActionTarget)
    {
        GameController gcr = GameController.gameController;
        if (specActionTarget != -1)
        {
            List<int> nodesTowards = gcr.GetClosestAdjToDest(currentLocation,specActionTarget);
            int rand_index = Random.Range(0, nodesTowards.Count);
            int potential_target = nodesTowards[rand_index];
            if (debugLogging)
                Debug.Log($"target node to scan is {specActionTarget}, heading to node {potential_target}");
            return potential_target;
        }
        else
        {
            // This shouldn't happen
            int toMoveTo = SelectNextNodeRandom(currentLocation);
            if (debugLogging)
                Debug.Log($"Moving to node id {toMoveTo}");
            return toMoveTo;
        }
    }

    public override List<int> GetSuspectedTrojanLocs()
    {
        return (from kvp in possibleLocations where kvp.Value select kvp.Key).ToList();
    }

    protected override int GetSpecialActionTarget()
    {
        GameController gcr = GameController.gameController;
        List<(int,int[])> prevScans = gcr.scanHistory;
        List<int> possibleLocationsList = GetSuspectedTrojanLocs();
        // If no info exists for the turn, scan in place just for some information
        if (possibleLocationsList.Count <= 0)
        {
            if (debugLogging)
                Debug.Log("No info found - scanning in place");
            return currentLocation;
        }
        else
        {
            //Get the closest possible location
            int closestTarget = possibleLocationsList.Aggregate((id1, id2) => gcr.GetNodeDist(id1, currentLocation) < gcr.GetNodeDist(id2, currentLocation) ? id1 : id2);
            if(debugLogging)
            {
                Debug.Log($"Closest potential target found: node id {closestTarget}");
            }
            return closestTarget;
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
        List<(int,int[])> prevScans = gcr.scanHistory;
        List<int> initialPossibleLocs = GetSuspectedTrojanLocs();
        // recalculate the possible locations
        //truncate this list by intersecting it with the possible directions from each scan info
        foreach ((int,int[]) info in prevScans)
        {
            List<int> possibleLocsList = GetSuspectedTrojanLocs();
            if (debugLogging)
                Debug.Log($"Previous scan info: Hidden player was in direction(s) of Node(s) {string.Join(" and ",info.Item2)} from starting node {info.Item1}");
            List<int> nodesInDir = gcr.GetDestsClosestToAdjs(info.Item1,info.Item2);
            int[] possible_truncated = possibleLocsList.Intersect(nodesInDir).ToArray();
            foreach (int id in possible_truncated)
            {
                possibleLocations[id] = false;
            }
        }
        // prune if they do not meet the requirements
        if(prevScan.Item2.Length >= 1)
        {
            actionLog.Add(new PlayerAction(1, prevScan.Item1, prevScan.Item2));
            if(debugLogging)
            {
                Debug.Log($"Last scan info: Hidden player was in direction(s) of Node(s) {string.Join(" and ",prevScan.Item2)} from starting node {prevScan.Item1}");
            }

            List<int> postPossibleLocs = GetSuspectedTrojanLocs();
            if (initialPossibleLocs.Intersect(postPossibleLocs).ToArray().Length <= initialPossibleLocs.Count * 0.1)
            {
                // Bad scan: The new scan area has almost nothing in common with the original scan.
                IncrementMood(selfMoodFactor * -1);
                IncrementMoodOthers(true, false);
                IncrementMoodOthers(false, true);
            }
            if (prevScan.Item2.Length == 1 || ((double)postPossibleLocs.Count/(gcr.mapSize * gcr.mapSize) <= smallPossibleLocsRatio) && postPossibleLocs.Count > 0)
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

    protected override void HandleTurn(int playerID, int actionsLeft)
    {
        GameController gcr = GameController.gameController;
        int mapSizeSq = GameController.gameController.mapSize * GameController.gameController.mapSize;
        //Initial setup: only do this when directly following hidden turn
        if (gcr.shouldUpdateScannerKnowledge){
            // Reset this dict when a new round starts
            if (gcr.turnCount == 1 || possibleLocations.Count == 0)
            {
                possibleLocations.Clear();
                // Add the hidden player's spawn if it's the first time (first turn expected)
                for ( int i = 1; i <= mapSizeSq; i++ )
                {
                    possibleLocations.Add(i, (i == gcr.hiddenSpawn.nodeID));
                }
            }

            //If a node was infected add the surrounding nodes to the possible locations info
            int lastInfected = gcr.lastInfectedNode;
            if (lastInfected < mapSizeSq && lastInfected > 0 )
            {
                foreach(int nodeID in gcr.GetAdjacentNodes(lastInfected,3))
                {
                    possibleLocations[nodeID] = true;
                }
            }

            int[] locationsToPropagate = GetSuspectedTrojanLocs().ToArray();
            //Grow the search space size if a turn has passed, because the hidden player may have moved.
            foreach(int locID in locationsToPropagate)
            {
                foreach(int nodeID in gcr.GetAdjacentNodes(locID, 3))
                {
                    possibleLocations[nodeID] = true;
                }
            }
        }
    }
}