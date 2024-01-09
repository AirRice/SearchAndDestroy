using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mindist : BotTemplate
{
    public static float dist_factor = 0.5f;
    public override void ProcessTurn(int playerID, int currentNodeID, int actionsLeft)
    {
        int currentLocation = currentNodeID;
        Debug.Log($"Processing turn for player {playerID}");
        if(playerID != 0)
            Debug.Log($"Warning: Player {playerID} is not a valid target for Mindist. Falling back to random...");
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
        float min = 9999.0f;
        int lowest = -1;
        for (int i = 1; i <= GameController.gameController.mapSize * GameController.gameController.mapSize; i++)
        {
            float hunterDist = GameController.gameController.GetDistFromHunters(i);
            if (min > hunterDist || (min <= hunterDist && min-hunterDist < 0.1f && RandomBool(dist_factor)))
            {
                min = hunterDist;
                lowest = i;
            }
        }
        if (lowest != -1)
        {
            int[] pathTo = GameController.gameController.GetCappedPath(currentNodeID,lowest);
            //Return the next in line
            int nextNode = pathTo[pathTo.Length > 1 ? 1 : 0];
            if (nextNode == currentNodeID)
                return SelectNextNodeRandom(currentNodeID);
            else
                return nextNode;
        }
        else
        {
            return -1;
        }
    }
}
