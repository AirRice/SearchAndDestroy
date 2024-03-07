using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class GreedyAvoidTrojan : BotTemplate
{
    public readonly float risk_factor = 0.1f; // Chance of risk-taking: will ignore avoidance with this frequency
    public readonly int avoid_dist = 3;
    public override void HandleTurn(int playerID, int currentNodeID, int actionsLeft)
    {
        GameController gcr = GameController.gameController;
        int mapSizeSq = gcr.mapSize * gcr.mapSize;
        int currentLocation = currentNodeID;
        List<int> targets = gcr.targetNodeIDs;

        while(actionsLeft > 0)
        {   
            // First things first, if we're too close to the scanners, we should leave (depending on the risk factor)
            if (gcr.GetDistFromHunters(currentLocation) <= avoid_dist && Random.value > risk_factor)
            {
                int toMoveTo = -1;
                List<int> adjs = GameController.gameController.GetAdjacentNodes(currentNodeID,2).ToList();
                bool flag = false;
                while(!flag && adjs.Count > 0)
                {
                    //use random iteration to decide on a potential target.
                    int rand_index = Random.Range(0, adjs.Count);
                    int potential_target = adjs[rand_index];
                    if(gcr.GetDistFromHunters(currentLocation) > avoid_dist)
                    {
                        toMoveTo = potential_target;
                        flag = true;
                    }
                    else
                    {
                        adjs.Remove(potential_target);
                    }
                }
                if (toMoveTo > 0 && toMoveTo < mapSizeSq)
                {
                    Debug.Log($"Evading from scanner players to node id {toMoveTo}");
                    int finalLocation = gcr.TryMoveToNode(toMoveTo);
                    int movedist = gcr.GetPathLength(currentLocation,finalLocation);
                    currentLocation = finalLocation;
                    actionsLeft-=movedist;
                }
            }
            //Iterate over each target, selecting the closest one
            int min = -1;
            int mindist = 9999;
            foreach (int tgt in targets)
            {
                int pathLen = gcr.GetPathLength(currentNodeID,tgt);
                if(gcr.infectedNodeIDs.Contains(tgt) || (gcr.GetDistFromHunters(tgt) <= avoid_dist && Random.value > risk_factor))
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
                //Special action is only possible on adjacent spaces
                //Edge case: Target is same space as current node
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
                    actionsLeft-=(movedist-1);
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
}