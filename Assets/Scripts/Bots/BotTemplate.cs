using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
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
    private readonly Dictionary<string, string> EmotionBarkFilepaths = new(){
        {"Relief", "Barks/relief"},
        {"Resignation", "Barks/fearsconfirmed"}, // Resignation is Fears Confirmed in OCC Model.
        {"Content", "Barks/content"}, //Content is Satisfaction in OCC Model
        {"Disappointment", "Barks/disappointment"},
        {"Joy", "Barks/joy"},
        {"Distress", "Barks/distress"},
        {"Gloating", "Barks/gloating"},
        {"Resentment", "Barks/resentment"}
    };
    
    public string GetRandomBark()
    {
        string filePathBarks = System.IO.Path.Combine(Application.dataPath, (EmotionBarkFilepaths[GetCurrentEmotion()] + (playerID == 0 ? "trojan.txt" : "scanner.txt")));
        string[] allLines = System.IO.File.ReadAllLines(filePathBarks);
        int randIndex = Random.Range(0,allLines.Length);
        return allLines[randIndex];
    }

    public string GetCurrentEmotion()
    {
        Debug.Log($"Player {playerID} current mood: {currentMood} prev mood: {prevMood}");
        if(prevMood < 0)
        {
            //Fear - Expected consequence negative
            if (currentMood > 0)
            {
                // Relief, Gloating
                return "Relief";
            }
            else if (currentMood < 0)
            {
                //Resignation, Resentment
                return "Resignation";
            }

        }
        else if(prevMood > 0)
        {
            //Hope - Expected consequence positive
            if (currentMood > 0)
            {
                // Satisfaction, Gloating
                return "Content"; 
            }
            else if (currentMood < 0)
            {
                //Disappointment, Resentment
                return "Disappointment";
            }
        }
        else
        {
            if (currentMood > 0)
            {
                // Joy
                return "Joy";
            }
            else if (currentMood < 0)
            {
                // Distress
                return "Distress";
            }
            else return "";
        }
        return "";
    }
}