using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GreedyTrojan : BotTemplate
{
    public override void ProcessTurn(int playerID, int currentNodeID, int actionsLeft)
    {
        GameController gcr = GameController.gameController;
        Debug.Log($"Processing turn for player {playerID}");
        if(playerID != 0)
        {
            Debug.Log($"Warning: Player {playerID} is not a valid target for SoloScan.");
            return;
        }

        int currentLocation = currentNodeID;
        List<int> targets = gcr.targetNodeIDs;

        while(actionsLeft > 0)
        {
            //Iterate over each target, selecting the closest one
                
            int min = -1;
            int mindist = 9999;
            foreach (int tgt in targets)
            {
                int pathLen = gcr.GetPathLength(currentNodeID,tgt);
                if(gcr.infectedNodeIDs.Contains(tgt))
                {
                    // Skip ones that already are done.
                    continue;
                }
                if(pathLen <= mindist)
                {
                    mindist = pathLen;
                    min = tgt;
                }
            }
            if(min!=-1)
            {
                //scan is only possible on adjacent spaces
                //Edge case: Scan target is same space as current node
                if(currentLocation == min)
                {
                    int toMoveTo = SelectNextNodeRandom(currentLocation);
                    Debug.Log($"Moving to node id {toMoveTo}");
    
                    gcr.TryMoveToNode(toMoveTo);
                    currentLocation = toMoveTo;
                    actionsLeft--;
                }
                else
                {
                    int movedist = gcr.GetPathLength(currentLocation,min);
                    int[] toPrevNode = gcr.GetCappedPath(currentLocation,min,movedist-1);
                    int prevNode = toPrevNode[^1];
                    Debug.Log($"target {min}, Previous node {prevNode}");
                    
                    gcr.TryMoveToNode(prevNode);
                    currentLocation = prevNode;
                    Debug.Log($"Moving to node id {prevNode}");
                    actionsLeft-=movedist-1;
                }
                gcr.TrySpecialAction(Node.GetNode(min));
                Debug.Log($"Infecting node id {min}");
                actionsLeft--;
            }
            else
            {
                // THis shouldn't happen since the game would already be won by this point.
                int toMoveTo = SelectNextNodeRandom(currentLocation);
                Debug.Log($"Moving to node id {toMoveTo}");

                gcr.TryMoveToNode(toMoveTo);
                currentLocation = toMoveTo;
                actionsLeft--;
            }
            
        }

        gcr.ProgressTurn();
    }
    public override int SelectNextNode(int playerID, int currentNodeID)
    {
        return 0;
    }
}