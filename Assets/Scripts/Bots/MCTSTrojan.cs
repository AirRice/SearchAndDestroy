using System.Collections;
using System.Collections.Generic;
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
        nextAction = node.GetBestNextAction();
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
