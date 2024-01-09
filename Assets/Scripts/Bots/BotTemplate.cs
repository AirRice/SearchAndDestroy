using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public abstract class BotTemplate : ScriptableObject
{
    public abstract void ProcessTurn(int playerID, int currentNodeID, int actionsLeft);
    public abstract int SelectNextNode(int playerID, int currentNodeID);
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