using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MCTSTrojan : BotTemplate
{

    public (int,int) nextAction = (-1,-1);
    protected override void HandleAction(int playerID, int actionsLeft)
    {
        GameController gcr = GameController.gameController;
        List<int> playerPos = new()
        {
            currentLocation
        };
        playerPos.AddRange(gcr.GetHunterPos());
        MCTSNode node = new(playerPos.ToArray(), gcr.infectedNodeIDs, gcr.GetTurnNumber());

        int HeuristicFunction(MCTSNode curState)
        {
            // Heuristic value to figure out who is more favoured
            GameController gcr = GameController.gameController;
            int closestTargetDist = gcr.GetClosestTargetNodeDist(curState.playerPos[playerID]);
            float distFromHunters = gcr.GetDistFromHunters(curState.playerPos[playerID]);
            if (gcr.targetNodeIDs.Contains(curState.newlyInfectedNode))
            {
                return 0; // If a new target has been infected consider this an advantage to trojan.
            }
            // If we're closer to a target than the average distance from hunters, assume the trojan has advantage here.
            return (closestTargetDist <= distFromHunters) ? 0 : 1;
        }


        nextAction = node.GetBestNextAction(HeuristicFunction);
    }
    
    protected override int GetSpecialActionTarget()
    {
        GameController gcr = GameController.gameController;
        if (nextAction.Item1 == 1)
        {
            return nextAction.Item2;
        }
        return -1;
    }
    protected override int GetMovementTarget(int specActionTarget)
    {
        if (nextAction.Item1 == 0)
        {
            return nextAction.Item2;
        }
        return -1;
    }
}
