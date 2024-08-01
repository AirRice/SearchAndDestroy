using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Data.Common;
using Unity.VisualScripting;

public class CautiousTrojan : BotTemplate
{
    public readonly float risk_factor = 0.3f; // Chance of risk-taking: will ignore avoidance with this frequency
    public readonly int avoid_dist = 2;
    private int curInfectTarget = -1;
    protected override int GetSpecialActionTarget()
    {
        GameController gcr = GameController.gameController;
        List<int> targets = gcr.targetNodeIDs;

        if (curInfectTarget == -1)
        {
            //Iterate over each target, selecting the furthest one from scanners                
            int max = -1;
            float maxdist = -1;
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
                    max = tgt;
                }
            }

            curInfectTarget = max;
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
            IncrementMoodOthers(false, false);
        }
    }
    protected override void HandleTurn(int playerID, int actionsLeft)
    {
        GameController gcr = GameController.gameController;
        if (actionsLeft >= gcr.movesCount)
        {
            IncrementMood(selfMoodFactor * ((gcr.GetDistFromHunters(currentLocation) <= gcr.movesCount) ? -0.5f : 0.5f));
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

            if (nodesTowards.Count == 2 && gcr.GetDistFromHunters(nodesTowards[0]) < gcr.GetDistFromHunters(nodesTowards[1]))
            {
                toMoveTo = nodesTowards[1];
            }
            else if (nodesTowards.Count == 2 && gcr.GetDistFromHunters(nodesTowards[0]) > gcr.GetDistFromHunters(nodesTowards[1]))
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