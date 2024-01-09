using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoloScan : BotTemplate
{
    public override void ProcessTurn(int playerID, int currentNodeID, int actionsLeft)
    {
        Debug.Log($"Processing turn for player {playerID}");
        if(playerID == 0)
        {
            Debug.Log($"Warning: Player {playerID} is not a valid target for SoloScan.");
            return;
        }

        int currentLocation = currentNodeID;
        List<int> possibleLocations = new List<int>();
        /**
        This algorithm works in the following order.
        1. If no previous scan information for this turn exists, scan to gain some information.
        2. Using the info, locate a potential location the hidden player could be in at the current moment. Triangulate possible locations from all known info that turn.
        3. Head to the location and scan the node, if it is possible to do so (if it is within range)
        4. If the scanning has not resulted in a scanner victory, move to a random adjacent node and repeat.
        5. Do this until the action allocation runs out.
        **/

        while(actionsLeft > 0)
        {
            int scanTarget = -1;
            List<(int,int)> prevScans = GameController.gameController.scanHistory;

            // If no previous scan info exists for the turn, do one just for some information
            if (prevScans.Count == 0)
            {
                int scanTargetFirst = SelectNextNodeRandom(currentLocation);
                GameController.gameController.TrySpecialAction(Node.GetNode(scanTargetFirst));
                Debug.Log($"Scanning node id {scanTargetFirst} as first action");
                actionsLeft--;
            }
            else
            {
                //Make this list for the first time if it doesn't exist yet.
                if (possibleLocations.Count == 0)
                {
                    for(int i = 1; i <= GameController.gameController.mapSize * GameController.gameController.mapSize; i++)
                    {
                        possibleLocations.Add(i);
                    }
                }
                // Check if every known ground truth fits for the given node
                // I.e. check if the node being looked at fulfills all requirements
                // Iterate over this in reverse so it can properly be removed.
                for (int i = possibleLocations.Count-1; i >=0 ; i--)
                {
                    int truths = 0;
                    foreach ((int,int) info in prevScans)
                    {
                        Debug.Log($"Previous scan info: Node {info.Item1} is distance {info.Item2} away from hidden player, testing node {possibleLocations[i]}");
                        if (GameController.gameController.GetPathLength(info.Item1,possibleLocations[i]) == info.Item2)
                            truths++;
                    }
                    if (truths != prevScans.Count)
                        possibleLocations.Remove(possibleLocations[i]);
                }
                
                //Copy the locations list
                List<int> possibleLocations_temp = possibleLocations;
                bool flag = false;
                while(!flag && possibleLocations_temp.Count > 0)
                {
                    //use random iteration to decide on a potential target.
                    int rand_index = Random.Range(0, possibleLocations_temp.Count);
                    int potential_target = possibleLocations_temp[rand_index];
                    if((GameController.gameController.GetPathLength(currentLocation,potential_target) < actionsLeft)
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
        
                        GameController.gameController.TryMoveToNode(toMoveTo);
                        currentLocation = toMoveTo;
                        actionsLeft--;
                    }
                    else
                    {
                        int movedist = GameController.gameController.GetPathLength(currentLocation,scanTarget);
                        int[] toPrevNode = GameController.gameController.GetCappedPath(currentLocation,scanTarget,movedist-1);
                        int prevNode = toPrevNode[^1];
                        Debug.Log($"Scan target {scanTarget}, Previous node {prevNode}");
                        
                        GameController.gameController.TryMoveToNode(prevNode);
                        currentLocation = prevNode;
                        Debug.Log($"Moving to node id {prevNode}");
                        actionsLeft-=movedist-1;
                    }
                    GameController.gameController.TrySpecialAction(Node.GetNode(scanTarget));
                    Debug.Log($"Scanning node id {scanTarget}");
                    actionsLeft--;
                }
                else
                {
                    int toMoveTo = SelectNextNodeRandom(currentLocation);
                    Debug.Log($"Moving to node id {toMoveTo}");
    
                    GameController.gameController.TryMoveToNode(toMoveTo);
                    currentLocation = toMoveTo;
                    actionsLeft--;
                }

            }
        }

        GameController.gameController.ProgressTurn();
    }
    public override int SelectNextNode(int playerID, int currentNodeID)
    {
        return 0;
    }
}