using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class SharedScan : BotTemplate
{
    public static Dictionary<int,bool> possibleLocations = new(); //Node ID dict: int node ID, bool is possible.
        
    public override void HandleTurn(int playerID, int currentNodeID, int actionsLeft)
    {
        GameController gcr = GameController.gameController;
        int mapSizeSq = GameController.gameController.mapSize * GameController.gameController.mapSize;

        // Reset this dict when a new round starts
        if (gcr.turnCount == 0)
        {
            possibleLocations = new();
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
        else
        {
            //Grow the search space size if a turn has passed, because the hidden player may have moved.
            foreach(int locID in locationsToPropagate)
            {
                foreach(int nodeID in gcr.GetAdjacentNodes(locID, 3))
                {
                    SharedScan.possibleLocations[nodeID] = true;
                }
            }
        }

        int currentLocation = currentNodeID;
        
        /**
        This algorithm works in the following order.
        1. Triangulate all potential locations from previous scan information, newly infected nodes this turn, and hidden player spawn position
        2. Randomly select one of the possible locations.
        3. Head to the location and scan the node, if it is possible to do so (if it is within range)
        4. If the scanning has not resulted in a scanner victory, try and repeat step 2-3 with another possible location. Do this until the action allocation runs out.
        5. When the hidden player's turn ends, add to the possible locations list all nodes of 3 distance away from previously possible locations
           and nodes up to (max movement limit - distance to last infected node from closest last possible node) distance away
           from the last infected node if one was infected last turn.
        **/

        while(actionsLeft > 0)
        {
            int scanTarget = -1;
            List<(int,int[])> prevScans = gcr.scanHistory;
            // If no info exists for the turn, do one just for some information
            if ((from kvp in SharedScan.possibleLocations where kvp.Value select kvp.Key).ToList().Count <= 0)
            {
                GameController.gameController.TrySpecialAction();
                Debug.Log($"Scanning as first action");
                actionsLeft--;
            }
            else
            {
                // prune out the possible locations if they do not meet the requirements
                foreach ((int,int[]) info in prevScans)
                {
                    //if(info.Item2.Count < 1)
                        // Ignore this 
                        //continue;
                    Debug.Log($"Previous scan info: Hidden player was in direction(s) of Node(s) {string.Join(" and ",info.Item2)} from starting node {info.Item1}");
                    int[] possLocs = (from kvp in SharedScan.possibleLocations where kvp.Value select kvp.Key).ToArray();
                    foreach (int curNode in possLocs)
                    {
                        bool nodeflag = false;
                        int[] pathto = gcr.GetCappedPath(curNode,info.Item1);
                        // Delete the node from the list if it's not in the direction of the given node + equal distance away from all given nodes (if multiple exist)
                        foreach (int nodeOfDir in info.Item2)
                        {
                            if(pathto.Contains(nodeOfDir) && info.Item2.All(id => gcr.GetPathLength(id,curNode) == gcr.GetPathLength(nodeOfDir,curNode)) && !gcr.infectedNodeIDs.Contains(curNode))
                            {
                                nodeflag = true;
                            }
                        }
                        if (!nodeflag)
                            SharedScan.possibleLocations[curNode] = false;
                    }
                }
                
                //Get an intersection of possible targets vs our movement range and select one randomly
                int[] possibleLocations_temp = (from kvp in SharedScan.possibleLocations where kvp.Value select kvp.Key).ToArray();
                int[] nodesInRange = gcr.GetAdjacentNodes(currentLocation,actionsLeft-1);
                List<int> targetLocs = nodesInRange.Intersect(possibleLocations_temp).ToList();
                int fallbackTarget = -1;
                if (targetLocs.Count > 0)
                {              
                    int rand_index = Random.Range(0, targetLocs.Count);
                    scanTarget = targetLocs[rand_index];
                    Debug.Log($"Potential Target found: node id {scanTarget}");
                }
                if (possibleLocations_temp.Count() > 0)
                {
                    int rand_index_fallback = Random.Range(0, possibleLocations_temp.Count());
                    fallbackTarget = possibleLocations_temp[rand_index_fallback];
                }

                // Move to the node and scan it if valid; If not then move to one of the further ones
                if(scanTarget!=-1)
                {
                    
                    Debug.Log($"Moving to Scan target {scanTarget}");
                    int finalLocation = gcr.TryMoveToNode(scanTarget);
                    if (finalLocation != -1)
                    {
                        actionsLeft-=gcr.GetPathLength(currentLocation,finalLocation);
                        currentLocation = finalLocation;

                        gcr.TrySpecialAction(Node.GetNode(scanTarget));
                        Debug.Log($"Scanning node id {scanTarget}");
                        actionsLeft--;
                    }
                }
                else if (fallbackTarget > 0 && fallbackTarget < mapSizeSq)
                {
                    int toMoveTo = fallbackTarget;
                    Debug.Log($"Moving to node id {toMoveTo}");

                    int finalLocation = gcr.TryMoveToNode(scanTarget);
                    if (finalLocation != -1)
                    {
                        actionsLeft-=gcr.GetPathLength(currentLocation,finalLocation);
                        currentLocation = finalLocation;
                    }
                }

            }
        }

        gcr.ProgressTurn();
    }
}