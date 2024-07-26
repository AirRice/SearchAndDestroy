using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public abstract class BotTemplate : ScriptableObject
{
    protected int playerID;
    protected int currentLocation;
    protected abstract int GetMovementTarget(int specActionTarget);
    protected abstract int GetSpecialActionTarget();
    protected bool debugLogging = false;

    protected float currentMood = 0;
    protected float prevMood = 0;
    public float selfMoodFactor = 0.3f; // How much self-actions affect mood
    public float friendMoodFactor = 0.1f; // How much ally actions affect mood
    public float enemyMoodFactor = 0.3f; // How much enemy actions affect mood
    public void SetPlayerID(int ID)
    {
        playerID = ID;   
    }
    public void IncrementMood(float val)
    {
        currentMood += val;
    }
    public void IncrementMoodOthers(bool allies, bool positive)
    {
        GameController gcr = GameController.gameController;
        gcr.IncrementMoodMultiple(playerID, allies, positive);
    }
    public void ProcessTurn(int currentNodeID, int actionsLeft)
    {
        bool isHiddenBot = playerID == 0;
        prevMood = currentMood;
        currentMood = 0;
        if(debugLogging)
        {
            Debug.Log($"Processing turn for player {playerID}");
        }
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
            HandleAction(playerID, actionsLeft);
            int specActionTarget = GetSpecialActionTarget();
            if(isHiddenBot && gcr.GetPathLength(currentLocation,specActionTarget) == 1)
            {
                if(debugLogging)
                {
                    Debug.Log($"Infecting node id {specActionTarget}");
                }
                if (gcr.TrySpecialAction(Node.GetNode(specActionTarget))) {
                    break;
                }
                OnSpecialAction(specActionTarget);
                continue;
            }
            else if (!isHiddenBot && currentLocation == specActionTarget)
            {
                if(debugLogging)
                {
                    Debug.Log($"Scanning at node id {specActionTarget}");
                }
                if (gcr.TrySpecialAction()) {
                    break;
                }
                OnSpecialAction(specActionTarget);
                continue;
            }
            int moveTarget = GetMovementTarget(specActionTarget);
            if(moveTarget != -1)
            {
                gcr.TryMoveToNode(moveTarget);
                currentLocation = moveTarget;
                if(debugLogging)
                {
                    Debug.Log($"Moving to node id {moveTarget}");
                }
                OnMove(moveTarget);
            }
        }
        OnPlayerTurnEnd();
        if (gcr.autoProgressTurn)
        {
            gcr.ProgressTurn();
        }
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
    protected virtual void HandleAction(int playerID, int maxActionsLeft){
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

    // Horrible if-statement mess that I'll turn into modular files down the line...
    // Simple OCC based emotion simulation.
    public string MakeBark(int playerID)
    {
        //string filePathBarks = System.IO.Path.Combine(Application.dataPath, "Barks/", playerID == 0 ? "trojan/" : "scanner/");
        if(prevMood < 0)
        {
            //Fear - Expected consequence negative
            if (currentMood > 0)
            {
                // Relief, Gloating
                return (playerID == 0 ? "I thought you might get me there! I was getting worried." : "Thought you could get away, huh? We're on your trail again.");
            }
            else if (currentMood < 0)
            {
                //Fears confirmed, Resentment
                return (playerID == 0 ? "That's not good." : "Come on, where are you hiding?");
            }

        }
        else if(prevMood > 0)
        {
            //Hope - Expected consequence positive
            if (currentMood > 0)
            {
                // Satisfaction, Gloating
                return (playerID == 0 ? "Hahaha, I don't think you're even close to finding where I am." : "Yep, we still got a trail on you.");
            }
            else if (currentMood < 0)
            {
                //Disappointment, Resentment
                return (playerID == 0 ? "I'm gonna have to try something else... You're guarding too well." : "Huh? I really thought you were over there.");
            }
        }
        else
        {
            if (currentMood > 0)
            {
                // Joy
                return (playerID == 0 ? "One step closer to victory again!" : "Aha! There you are.");
            }
            else if (currentMood < 0)
            {
                // Distress
                return (playerID == 0 ? "Oh, this is getting tense. I hope you don't find me." : "I'm getting a bit worried. I have no idea where you could be.");
            }
            else return "";
        }
        return "";
    }
}