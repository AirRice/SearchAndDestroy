using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class SharedScan : BotTemplate
{
    /**
    This algorithm works in the following order.
    1. Triangulate all potential locations from previous scan information, newly infected nodes this turn, and hidden player spawn position
    2. Select the closest location to the current player.
    3. Head to the location and scan the node, if it is possible to do so (if it is within range)
    4. If the scanning has not resulted in a scanner victory, try and repeat step 2-3 with another possible location. Do this until the action allocation runs out.
    5. When the hidden player's turn ends, add to the possible locations list all nodes of 3 distance away from previously possible locations
        and nodes up to (max movement limit - distance to last infected node from closest last possible node) distance away
        from the last infected node if one was infected last turn.
    **/
    public static Dictionary<int,bool> possibleLocations = new(); //Node ID dict: int node ID, bool is if possible. Initialised in this way to reduce overhead when iterating.
    public static int algoPlayerInTurn = 0; // player index only incremented when a player using this algorithm's turn comes around

    protected override int GetSpecialActionTarget()
    {
        GameController gcr = GameController.gameController;
        List<(int,int[])> prevScans = gcr.scanHistory;
        // If no info exists for the turn, do one just for some information
        if ((from kvp in SharedScan.possibleLocations where kvp.Value select kvp.Key).ToList().Count <= 0)
        {
            return currentLocation;
        }

        // prune out the possible locations if they do not meet the requirements
        foreach ((int,int[]) info in prevScans)
        {
            if(info.Item2.Length < 1)
                // Ignore this if the info is invalid
                continue;
        
            Debug.Log($"Previous scan info: Hidden player was in direction(s) of Node(s) {string.Join(" and ",info.Item2)} from starting node {info.Item1}");
            List<int> nodesInDir = gcr.GetDestsClosestToAdjs(info.Item1,info.Item2);
            foreach (int id in (from kvp in SharedScan.possibleLocations where kvp.Value select kvp.Key).ToList().Except(nodesInDir).ToArray())
            {
                possibleLocations[id] = false;
            }
        }
        //Get the closest possible location
        int[] possibleLocations_arr = (from kvp in SharedScan.possibleLocations where kvp.Value select kvp.Key).ToArray();
        if (possibleLocations_arr.Length > 0)
        {
            int closestTarget = possibleLocations_arr.Aggregate((id1, id2) => gcr.GetPathLength(id1, currentLocation) < gcr.GetPathLength(id2, currentLocation) ? id1 : id2);
            Debug.Log($"Closest potential Target found: node id {closestTarget}");
            return closestTarget;
        }
        else
        {
            Debug.Log($"No potential targets found.");
            return -1;
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
            Debug.Log($"target node to scan is {specActionTarget}, heading to node {potential_target}");
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

    protected override void HandleTurn(int playerID, int actionsLeft)
    {
        GameController gcr = GameController.gameController;
        int mapSizeSq = GameController.gameController.mapSize * GameController.gameController.mapSize;
        //Initial setup: only do this when directly following hidden turn
        if (SharedScan.algoPlayerInTurn == 0){
            // Reset this dict when a new round starts
            if (gcr.turnCount == 0)
            {
                SharedScan.possibleLocations = new();
            }
            //Make this list for the first time if it doesn't exist yet. 
            //Add the hidden player's spawn if it's the first time (first turn expected)
            if (SharedScan.possibleLocations.Count == 0)
            {
                for ( int i = 1; i <= mapSizeSq; i++ )
                {
                    SharedScan.possibleLocations.Add(i, false);
                }
                SharedScan.possibleLocations[gcr.hiddenSpawn.nodeID] = true;
            }

            int[] locationsToPropagate = (from kvp in SharedScan.possibleLocations where kvp.Value select kvp.Key).ToArray();
            //If a node was infected add the surrounding nodes to the possible locations info
            //This can be up to (max movement limit - distance to last infected node from closest last possible node) distance away
            int lastInfected = gcr.lastInfectedNode;
            if (lastInfected < mapSizeSq && lastInfected > 0 )
            {
                foreach(int nodeID in gcr.GetAdjacentNodes(lastInfected,3))
                {
                    SharedScan.possibleLocations[nodeID] = true;
                }
            }
            //Grow the search space size if a turn has passed, because the hidden player may have moved.
            foreach(int locID in locationsToPropagate)
            {
                foreach(int nodeID in gcr.GetAdjacentNodes(locID, 3))
                {
                    SharedScan.possibleLocations[nodeID] = true;
                }
            }
        }
        
    }

    protected override void OnPlayerTurnEnd()
    {
        SharedScan.algoPlayerInTurn++;
    }
}