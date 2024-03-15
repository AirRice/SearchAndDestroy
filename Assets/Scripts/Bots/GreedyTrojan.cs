using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GreedyTrojan : BotTemplate
{
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
    protected override int GetMovementTarget()
    {
        GameController gcr = GameController.gameController;
        //Edge case: Scan target is same space as current node
        //scan is only possible on adjacent spaces
        if(currentLocation == GetSpecialActionTarget())
        {
            int toMoveTo = SelectNextNodeRandom(currentLocation);
            return toMoveTo;
        }
        else if (GetSpecialActionTarget() != -1)
        {
            List<int> nodesTowards = gcr.GetClosestAdjToDest(currentLocation,GetSpecialActionTarget());
            int rand_index = Random.Range(0, nodesTowards.Count);
            int potential_target = nodesTowards[rand_index];
            Debug.Log($"target node to infect is {GetSpecialActionTarget()}, heading to node {potential_target}");
            return potential_target;
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