using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class SharedScan : BotTemplate
{
    public static List<int> possibleLocations = new List<int>(); //Node ID list
        
    public override void ProcessTurn(int playerID, int currentNodeID, int actionsLeft)
    {
        GameController gcr = GameController.gameController;
        int mapSizeSq = GameController.gameController.mapSize * GameController.gameController.mapSize;
        Debug.Log($"Processing turn for player {playerID}");
        if(playerID == 0)
        {
            Debug.Log($"Warning: Player {playerID} is not a valid target for SharedScan.");
            return;
        }
        else if(playerID == 1)
        {
            for (int i = SharedScan.possibleLocations.Count-1; i >=0 ; i--)
            {
                int curNode = SharedScan.possibleLocations[i];
                bool nodeflag = false;
                int[] pathto = gcr.GetCappedPath(curNode,info.Item1);
                int nodeOfDir = info.Item2[0];
                // Delete the node from the list if it's not in the direction of the given node + equal distance away from all given nodes (if multiple exist)
                if(pathto.Contains(nodeOfDir) && info.Item2.All(id => gcr.GetPathLength(id,curNode) == gcr.GetPathLength(nodeOfDir,curNode)))
                {
                    nodeflag = true;
                }
                if (!nodeflag)
                    SharedScan.possibleLocations.RemoveAll(r => r == curNode);
            }
        } 
        int currentLocation = currentNodeID;
        
        /**
        This algorithm works in the following order.
        1. If no previous scan information/newly infected nodes exist for this turn, scan to gain some information.
        2. Using the info, locate all potential locations the hidden player could be in at the current moment. 
           Triangulate possible locations from all known info that turn.
        3. Randomly select one of the possible locations.
        4. Head to the location and scan the node, if it is possible to do so (if it is within range)
        5. If the scanning has not resulted in a scanner victory, try and repeat step 3-4 with another possible location.
        6. Do this until the action allocation runs out.
        7. When the hidden player's turn ends, add to the possible locations list all nodes of 3 distance away from previously possible locations
           and nodes up to (max movement limit - distance to last infected node from closest last possible node) distance away
           from the last infected node if one was infected last turn.
        **/

        while(actionsLeft > 0)
        {
            //Make this list for the first time if it doesn't exist yet.
            if (SharedScan.possibleLocations.Count == 0)
            {
                for(int i = 1; i <= mapSizeSq; i++)
                {
                    SharedScan.possibleLocations.Add(i);
                }
            }
            int scanTarget = -1;
            List<(int,int[])> prevScans = gcr.scanHistory;
            int lastInfected = gcr.lastInfectedNode;
            // If no previous scan info exists for the turn, do one just for some information
            if (prevScans.Count == 0 || lastInfected > gcr.mapSize * gcr.mapSize || lastInfected < 0 )
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
                    for (int i = SharedScan.possibleLocations.Count-1; i >=0 ; i--)
                    {
                        int curNode = SharedScan.possibleLocations[i];
                        bool nodeflag = false;
                        int[] pathto = gcr.GetCappedPath(curNode,info.Item1);
                        int nodeOfDir = info.Item2[0];
                        // Delete the node from the list if it's not in the direction of the given node + equal distance away from all given nodes (if multiple exist)
                        if(pathto.Contains(nodeOfDir) && info.Item2.All(id => gcr.GetPathLength(id,curNode) == gcr.GetPathLength(nodeOfDir,curNode)))
                        {
                            nodeflag = true;
                        }
                        if (!nodeflag)
                            SharedScan.possibleLocations.RemoveAll(r => r == curNode);
                    }
                }
                
                //Copy the locations list
                List<int> possibleLocations_temp = possibleLocations;
                bool flag = false;
                while(!flag && possibleLocations_temp.Count > 0)
                {
                    //use random iteration to decide on a potential target.
                    int rand_index = Random.Range(0, possibleLocations_temp.Count);
                    int potential_target = possibleLocations_temp[rand_index];
                    if((gcr.GetPathLength(currentLocation,potential_target) < actionsLeft)
                        || (currentLocation == potential_target && actionsLeft >= 2))
                    {
                        scanTarget = potential_target;
                        Debug.Log($"Potential Target found: node id {scanTarget}");
                        flag = true;
                    }
                    else
                    {
                        possibleLocations_temp.Remove(potential_target);
                    }
                }
                if(scanTarget!=-1)
                {
                    //scan is only possible on adjacent spaces
                    //Edge case: Scan target is same space as current node
                    if(currentLocation == scanTarget)
                    {
                        int toMoveTo = SelectNextNodeRandom(currentLocation);
                        Debug.Log($"Moving to node id {toMoveTo}");
        
                        gcr.TryMoveToNode(toMoveTo);
                        currentLocation = toMoveTo;
                        actionsLeft--;
                    }
                    else
                    {
                        int movedist = gcr.GetPathLength(currentLocation,scanTarget);
                        int[] toPrevNode = gcr.GetCappedPath(currentLocation,scanTarget,movedist-1);
                        int prevNode = toPrevNode[^1];
                        Debug.Log($"Scan target {scanTarget}, Previous node {prevNode}");
                        
                        gcr.TryMoveToNode(prevNode);
                        currentLocation = prevNode;
                        Debug.Log($"Moving to node id {prevNode}");
                        actionsLeft-=movedist-1;
                    }
                    gcr.TrySpecialAction(Node.GetNode(scanTarget));
                    Debug.Log($"Scanning node id {scanTarget}");
                    actionsLeft--;
                }
                else
                {
                    int toMoveTo = SelectNextNodeRandom(currentLocation);
                    Debug.Log($"Moving to node id {toMoveTo}");
    
                    gcr.TryMoveToNode(toMoveTo);
                    currentLocation = toMoveTo;
                    actionsLeft--;
                }

            }
        }

        gcr.ProgressTurn();
    }
    public override int SelectNextNode(int playerID, int currentNodeID)
    {
        return 0;
    }
}