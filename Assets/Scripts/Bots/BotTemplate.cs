using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public abstract class BotTemplate : ScriptableObject
{
    protected bool isHiddenBot;
    protected int currentLocation;
    protected abstract int GetMovementTarget(int specActionTarget);
    protected abstract int GetSpecialActionTarget();
    public void SetHidden(bool b)
    {
        isHiddenBot = b;
    }
    public void ProcessTurn(int playerID, int currentNodeID, int actionsLeft)
    {
        Debug.Log($"Processing turn for player {playerID}");
        if((isHiddenBot && playerID != 0) || (!isHiddenBot && playerID == 0))
        {
            Debug.Log($"Warning: Player {playerID} is not a valid target for {GetType().Name}.");
            return;
        }
        currentLocation = currentNodeID;
        HandleTurn(playerID, actionsLeft);

        GameController gcr = GameController.gameController;
        for(int i = 0; i < actionsLeft; i++)
        {
            int specActionTarget = GetSpecialActionTarget();
            if(isHiddenBot && gcr.GetPathLength(currentLocation,specActionTarget) == 1)
            {
                Debug.Log($"Infecting node id {specActionTarget}");
                OnSpecialAction(specActionTarget);
                if (gcr.TrySpecialAction(Node.GetNode(specActionTarget))) {
                    return;
                }
                
                continue;
            }
            else if (!isHiddenBot && currentLocation == specActionTarget)
            {
                Debug.Log($"Scanning at node id {specActionTarget}");
                OnSpecialAction(specActionTarget);
                if (gcr.TrySpecialAction()) {
                    return;
                }
                continue;
            }
            int moveTarget = GetMovementTarget(specActionTarget);
            if(moveTarget != -1)
            {
                gcr.TryMoveToNode(moveTarget);
                currentLocation = moveTarget;
                Debug.Log($"Moving to node id {moveTarget}");
                OnMove(moveTarget);
            }
        }
        OnPlayerTurnEnd();
        gcr.ProgressTurn();
    }
    protected virtual void OnPlayerTurnEnd()
    {
        return;
    }
    protected virtual void OnSpecialAction(int specActionTarget)
    {
        return;
    }
    protected virtual void OnMove(int moveTarget)
    {
        return;
    }
    protected virtual void HandleTurn(int playerID, int maxActionsLeft){
        return;
    }

    public int SelectNextNodeRandom(int currentNodeID, int range = 1)
    {
        int[] adjs = GameController.gameController.GetAdjacentNodes(currentNodeID, range);
        return adjs[Random.Range(0,adjs.Length)];
    }
    public bool RandomBool(float prob = 0.5f)
    {
        return (Random.value > (1.0f-prob));
    }
}