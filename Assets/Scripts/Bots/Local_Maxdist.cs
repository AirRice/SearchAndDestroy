using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Local_Maxdist : BotTemplate
{
    public static float dist_factor = 0.5f;
    public override void ProcessTurn(int playerID, int currentNodeID, int actionsLeft)
    {
        int currentLocation = currentNodeID;
        Debug.Log($"Processing turn for player {playerID}");
        if(playerID != 0)
            Debug.Log($"Warning: Player {playerID} is not a valid target for Local_Maxdist. Falling back to random...");
        while(actionsLeft > 0)
        {
            int toMoveTo = (playerID != 0) ? SelectNextNodeRandom(currentLocation) : SelectNextNode(playerID,currentLocation);
            Debug.Log($"Moving to node id {toMoveTo}");
            GameController.gameController.TryMoveToNode(toMoveTo);
            currentLocation = toMoveTo;
            actionsLeft--;
        }
        GameController.gameController.ProgressTurn();
    }
    public override int SelectNextNode(int playerID, int currentNodeID)
    {
        float max = -1.0f;
        int highest = -1;
        foreach (int i in GameController.gameController.GetAdjacentNodes(currentNodeID))
        {
            float hunterDist = GameController.gameController.GetDistFromHunters(i);
            if (max < hunterDist || (max >= hunterDist && max-hunterDist < 0.1f && RandomBool(dist_factor)))
            {
                max = hunterDist;
                highest = i;
            }
        }
        return highest;
    }
}
