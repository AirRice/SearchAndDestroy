using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;
public abstract class BotTemplate : ScriptableObject
{
    protected int playerID;
    protected string personality;
    protected int currentLocation;
    protected float cautious_factor;
    protected GenerationType generationType = GenerationType.Default;
    protected abstract int GetMovementTarget(int specActionTarget);
    protected abstract int GetSpecialActionTarget();
    protected bool debugLogging = false;
    protected List<PlayerAction> actionLog = new();
    protected float currentMood = 0;
    protected float prevMood = 0;
    public float turn_delay = 0.0f;
    public float selfMoodFactor = 0.15f; // How much self-actions affect mood
    public float friendMoodFactor = 0.1f; // How much ally actions affect mood
    public float enemyMoodFactor = 0.3f; // How much enemy actions affect mood
    public float useAltMoodsFactor = 0.5f; // Chance to use surprise/confusion/resentment/gloating.
    public void SetPlayerID(int ID)
    {
        playerID = ID;   
    }
    public void SetPersonalityParams(string personalityParams)
    {
        personality = personalityParams;
    }
    public void SetCautiousFactor(float cautiousFactor)
    {
        this.cautious_factor = cautiousFactor;
    }
    public void SetGenerationType(GenerationType generationType)
    {
        this.generationType = generationType;
    }
    public GenerationType GetGenerationType()
    {
        return generationType;
    }
    public void IncrementMood(float val)
    {
        if (debugLogging)
            Debug.Log($"Player {playerID}'s mood incremented by {val}");
        currentMood += val;
    }
    public void IncrementMoodOthers(bool allies, bool positive)
    {
        GameController gcr = GameController.gameController;
        gcr.IncrementMoodMultiple(playerID, allies, positive);
    }
    public virtual List<int> GetSuspectedTrojanLocs()
    {
        return null;
    }
    public void DecayMood(float mult = 0.5f)
    {
        currentMood *= mult;
    }
    public IEnumerator ProcessTurn(int currentNodeID, int actionsLeft)
    {
        bool isHiddenBot = playerID == 0;
        prevMood = currentMood;
        actionLog.Clear();
        if(debugLogging)
        {
            Debug.Log($"Processing turn for player {playerID}");
        }
        if((isHiddenBot && playerID != 0) || (!isHiddenBot && playerID == 0))
        {
            Debug.Log($"Warning: Player {playerID} is not a valid target for {GetType().Name}.");
            yield return null;
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
                yield return new WaitForSeconds(turn_delay);
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
                yield return new WaitForSeconds(turn_delay);
                continue;
            }
            int moveTarget = GetMovementTarget(specActionTarget);
            if(moveTarget != -1)
            {
                int prevLocation = currentLocation;
                gcr.TryMoveToNode(moveTarget);
                currentLocation = moveTarget;
                if(debugLogging)
                {
                    Debug.Log($"Moving to node id {moveTarget}");
                }
                OnMove(moveTarget);
                actionLog.Add(new PlayerAction(0, prevLocation, new int[] {moveTarget}));
                if ((playerID == 0 && gcr.localPlayerID == 0) || (playerID != 0))
                    yield return new WaitForSeconds(turn_delay);
            }
        }
        OnPlayerTurnEnd();
        if (gcr.autoProgressTurn)
        {
            gcr.ProgressTurn();
        }
        else
        {
            gcr.GenerateStatusString(playerID, GetActionLog(), personality);
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
        {"Resignation", "Barks/distress"}, // Resignation is Fears Confirmed in OCC Model. Combined to distress
        {"Content", "Barks/content"}, //Content is Satisfaction in OCC Model
        {"Surprise", "Barks/surprise"}, //External to OCC model - when expectation differs from new revelation dramatically
        {"Confusion", "Barks/surprise"}, // Combined to surprise although some differences exist.
        {"Disappointment", "Barks/disappointment"}, 
        {"Joy", "Barks/content"}, // Combined to content
        {"Distress", "Barks/distress"},
        {"Gloating", "Barks/gloating"},
        {"Resentment", "Barks/resentment"},
        {"Neutral", "Barks/neutral"}
    };
    
    public string GetRandomBark()
    {
        string filePathBarks = System.IO.Path.Combine(Application.dataPath, (EmotionBarkFilepaths[GetCurrentEmotion()] + (playerID == 0 ? "trojan.txt" : "scanner.txt")));
        string[] allLines = System.IO.File.ReadAllLines(filePathBarks);
        int randIndex = Random.Range(0,allLines.Length);
        return allLines[randIndex];
    }
    public float GetCurrentMood()
    {
        return currentMood;
    }
    public string GetActionLog()
    {
        string ActionsString = $"\nThis turn, Player {playerID} has, in order:\n";
        for (int i = 0; i < actionLog.Count; i++)
        {
            string lastActionString = $"{i+1}: ";
            PlayerAction act = actionLog[i];
            switch (act.actionType)
            {
                case 0:
                    lastActionString += $"Moved from node {act.nodeFrom} to {act.nodeTargets[0]}\n";
                    break;
                case 1:
                    if (playerID == 0)
                    {
                        lastActionString += $"Infected node {act.nodeTargets[0]}\n";
                    }
                    else
                    {
                        lastActionString += $"Scanned for the Trojan, finding that they are to the {GameController.gameController.GetTargetDirectionString(act.nodeFrom, act.nodeTargets)}\n";
                        if (GetSuspectedTrojanLocs() != null && GetSuspectedTrojanLocs().Count > 0)
                        {
                            lastActionString += $"The player now suspects the Trojan is at one of these nodes:({string.Join(",",GetSuspectedTrojanLocs())})\n";
                        }
                    }
                    break;
                default:
                    break;
            }
            ActionsString += lastActionString;
        }
        return ActionsString;
    }
    public string GetCurrentEmotion()
    {
        GameController gcr = GameController.gameController;
        if (debugLogging)
            Debug.Log($"Player {playerID} current mood: {currentMood} prev mood: {prevMood}");
        float othersMood = gcr.GetOthersAvgMood(playerID == 0 ? 0 : 1);
        if (Random.value > useAltMoodsFactor)
        {
            if (Math.Abs(currentMood - prevMood) > 0.3)
            {
                return currentMood >= 0 ? "Surprise" : "Confusion";
            }
            else if ((prevMood * currentMood) > 0)
            {
                if (currentMood < 0 && othersMood > 0)
                {
                    // Resentment
                    return "Resentment";
                }
                else if (currentMood > 0 && othersMood < 0)
                {
                    // Gloating
                    return "Gloating";
                }
            }
        }
        if (Math.Abs(prevMood) >= 0.05)
        {
            
            if (Math.Abs(currentMood) >= 0.05)
            {
                if(prevMood < 0)
                //Fear - Expected consequence negative
                {
                    return currentMood > 0 ? "Relief" : "Resignation";
                }
                else if(prevMood > 0)
                //Hope - Expected consequence positive
                {
                    return currentMood > 0 ? "Content" : "Disappointment";
                }
            }
        }
        else
        {
            // Neutral prev mood
            if (Math.Abs(currentMood) >= 0.05)
            {
                return currentMood > 0 ? "Joy" : "Distress";
            }
        }
        // Not feeling strongly at the moment
        return "Neutral";
    }
}

public class PlayerAction
{
    public int actionType;
    public int nodeFrom;
    public int[] nodeTargets;

    public PlayerAction(int actionType, int nodeFrom, int [] nodeTargets)
    {
        this.actionType = actionType;
        this.nodeFrom = nodeFrom;
        this.nodeTargets = nodeTargets;
    }
}