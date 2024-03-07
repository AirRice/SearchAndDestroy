using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public abstract class BotTemplate : ScriptableObject
{
    private readonly bool hiddenBot = false;
    //
    public void ProcessTurn(int playerID, int currentNodeID, int actionsLeft)
    {
        Debug.Log($"Processing turn for player {playerID}");
        if((hiddenBot && playerID != 0) || (!hiddenBot && playerID == 0))
        {
            Debug.Log($"Warning: Player {playerID} is not a valid target for {GetType().Name}.");
            return;
        }
        HandleTurn(playerID, currentNodeID, actionsLeft);
    }
    public abstract void HandleTurn(int playerID, int currentNodeID, int actionsLeft);
    public int SelectNextNodeRandom(int currentNodeID)
    {
        int[] adjs = GameController.gameController.GetAdjacentNodes(currentNodeID);
        return adjs[Random.Range(0,adjs.Length)];
    }
    public bool RandomBool(float prob = 0.5f)
    {
        return (Random.value > (1.0f-prob));
    }
}