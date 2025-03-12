using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Data.Common;
using Unity.VisualScripting;
using System;
using Random = UnityEngine.Random;

public class UnifiedTrojan : BotTemplate
{
    private int curInfectTarget = -1;
    protected override int GetSpecialActionTarget()
    {
        GameController gcr = GameController.gameController;
        List<int> targets = gcr.targetNodeIDs;

        if (curInfectTarget == -1)
        {
            int selected = -1;
            //Iterate over each target, selecting the furthest/closest one (if cautious/greedy)              
            float maxdist = -1;               
            int mindist = 9999;
            if (Random.value >= cautious_factor)
            {
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
                        selected = tgt;
                    }
                }
            }
            else
            {
                foreach (int tgt in targets)
                {
                    float pathLen = gcr.GetDistFromHunters(tgt);
                    if(gcr.infectedNodeIDs.Contains(tgt))
                    {
                        // Skip ones that already are done.
                        continue;
                    }
                    if(pathLen >= maxdist)
                    {
                        maxdist = pathLen;
                        selected = tgt;
                    }
                }
            }
            curInfectTarget = selected;
        }
        
        return curInfectTarget;
    }
    protected override void OnSpecialAction(int specActionTarget)
    {
        actionLog.Add(new PlayerAction(1, currentLocation, new int[] {specActionTarget}));
        // Reset the current infection target after we perform the special action on it
        if (specActionTarget == curInfectTarget)
        {
            curInfectTarget = -1;
            IncrementMood(selfMoodFactor);
        }
    }
    protected override void HandleTurn(int playerID, int actionsLeft)
    {
        GameController gcr = GameController.gameController;
        if (actionsLeft >= gcr.movesCount)
        {
            float cautionFactor = 0.25f + Math.Clamp(cautious_factor, 0f, 1f) * 0.25f;
            IncrementMood(selfMoodFactor * ((gcr.GetDistFromHunters(currentLocation) <= gcr.movesCount) ? -cautionFactor : cautionFactor));
        }
    }
    protected override int GetMovementTarget(int specActionTarget)
    {
        GameController gcr = GameController.gameController;
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
            bool useCautious = Random.value <= cautious_factor;
           
            if (useCautious && nodesTowards.Count == 2 && gcr.GetDistFromHunters(nodesTowards[0]) < gcr.GetDistFromHunters(nodesTowards[1]))
            {
                toMoveTo = nodesTowards[1];
            }
            else if (useCautious && nodesTowards.Count == 2 && gcr.GetDistFromHunters(nodesTowards[0]) > gcr.GetDistFromHunters(nodesTowards[1]))
            {
                toMoveTo = nodesTowards[0];
            }
            else
            {
                int rand_index = Random.Range(0, nodesTowards.Count);
                toMoveTo = nodesTowards[rand_index];
            }

            if (debugLogging)
                Debug.Log($"target node to infect is {specActionTarget}, heading to node {toMoveTo}");
            return toMoveTo;
        }
        else
        {
            // This shouldn't happen since the game would already be won by this point.
            int toMoveTo = SelectNextNodeRandom(currentLocation);
            if (debugLogging)
                Debug.Log($"Moving to node id {toMoveTo}");
            return toMoveTo;
        }
    }
}