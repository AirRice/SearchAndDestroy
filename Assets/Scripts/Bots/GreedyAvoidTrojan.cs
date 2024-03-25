using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Data.Common;
using Unity.VisualScripting;

public class GreedyAvoidTrojan : BotTemplate
{
    public readonly float risk_factor = 0.01f; // Chance of risk-taking: will ignore avoidance with this frequency
    public readonly int avoid_dist = 2;
    private int curInfectTarget = -1;
    protected override int GetSpecialActionTarget()
    {
        GameController gcr = GameController.gameController;
        List<int> targets = gcr.targetNodeIDs;

        if (curInfectTarget == -1)
        {
            //Iterate over each target, selecting the closest one                
            int min = -1;
            int mindist = 9999;
            foreach (int tgt in targets)
            {
                int pathLen = gcr.GetPathLength(currentLocation,tgt);
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

            curInfectTarget = min;
        }
        return curInfectTarget;
    }
    protected override void OnSpecialAction(int specActionTarget)
    {
        // Reset the current infection target after we perform the special action on it
        if (specActionTarget == curInfectTarget)
            curInfectTarget = -1;
    }
    protected override int GetMovementTarget(int specActionTarget)
    {
        GameController gcr = GameController.gameController;

        // First things first, if we're too close to the scanners, we should leave (depending on the risk factor)
        if (gcr.GetDistFromHunters(currentLocation) <= avoid_dist && Random.value > risk_factor)
        {
            int toMoveTo = -1;
            List<int> adjs = gcr.GetAdjacentNodes(currentLocation).ToList();
            float[] adjsDist = adjs.Select(id=> gcr.GetDistFromHunters(id)).ToArray();
            float minDist = adjsDist.Min();

            for(int i = 0; i < adjs.Count; i++){
                if(adjsDist[i] == minDist)
                {
                    toMoveTo = adjs[i];
                }
            }
            if (gcr.NodeIsValid(toMoveTo))
            {
                Debug.Log($"Evading from scanner players to node id {toMoveTo}");
                return toMoveTo;
            }
        }

        //Edge case: Scan target is same space as current node
        //scan is only possible on adjacent spaces
        if(currentLocation == specActionTarget)
        {
            int toMoveTo = SelectNextNodeRandom(currentLocation);
            return toMoveTo;
        }
        else if (specActionTarget != -1)
        {
            int toMoveTo = -1;
            List<int> nodesTowards = gcr.GetClosestAdjToDest(currentLocation,specActionTarget);

            if (nodesTowards.Count == 2 && gcr.GetDistFromHunters(nodesTowards[0]) > gcr.GetDistFromHunters(nodesTowards[1]) && Random.value > risk_factor)
            {
                toMoveTo = nodesTowards[1];
            }
            else if (nodesTowards.Count == 2 && gcr.GetDistFromHunters(nodesTowards[0]) < gcr.GetDistFromHunters(nodesTowards[1]) && Random.value > risk_factor)
            {
                toMoveTo = nodesTowards[0];
            }
            else
            {
                int rand_index = Random.Range(0, nodesTowards.Count);
                toMoveTo = nodesTowards[rand_index];
            }

            Debug.Log($"target node to infect is {specActionTarget}, heading to node {toMoveTo}");
            return toMoveTo;
        }
        else
        {
            // This shouldn't happen since the game would already be won by this point.
            int toMoveTo = SelectNextNodeRandom(currentLocation);
            Debug.Log($"Moving to node id {toMoveTo}");
            return toMoveTo;
        }
    }
}