using System.Collections;
using System.Collections.Generic;
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
    public float selfMoodFactor = 0.3f; // How much self-actions affect mood
    public float friendMoodFactor = 0.1f; // How much ally actions affect mood
    public float enemyMoodFactor = 0.3f; // How much enemy actions affect mood
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
        //Debug.Log($"Player {playerID}'s mood incremented by {val}");
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
    public IEnumerator ProcessTurn(int currentNodeID, int actionsLeft)
    {
        bool isHiddenBot = playerID == 0;
        actionLog.Clear();
        prevMood = currentMood;
        currentMood = 0;
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
                yield return new WaitForSeconds(0.5f);
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
                yield return new WaitForSeconds(0.5f);
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
                    yield return new WaitForSeconds(0.5f);
            }
        }
        OnPlayerTurnEnd();
        gcr.GenerateStatusString(playerID, GetActionLog(), personality);
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
                        if (GetSuspectedTrojanLocs() != null && GetSuspectedTrojanLocs().Count > 0)
                        {
                            lastActionString += $"Scanned for the Trojan, and now suspects the Trojan is at one of these nodes:({string.Join(",",GetSuspectedTrojanLocs())})\n";
                        }
                        else
                        {
                            lastActionString += $"Scanned for the Trojan, finding that they are in the direction of node(s) {string.Join(",", act.nodeTargets)}\n";
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
        Debug.Log($"Player {playerID} current mood: {currentMood} prev mood: {prevMood}");
        float othersMood = gcr.GetOthersAvgMood(playerID == 0 ? 0 : 1);
        if(prevMood < 0)
        {
            //Fear - Expected consequence negative
            if (currentMood > 0)
            {
                // Relief
                return "Relief";
            }
            else if (currentMood < 0)
            {
                //Resignation
                return "Resignation";
            }
            else{
                if (othersMood > 0)
                {
                    // Resentment
                    return "Resentment";
                }
                else if (othersMood < 0)
                {
                    // Gloating
                    return "Gloating";
                }
            }
        }
        else if(prevMood > 0)
        {
            //Hope - Expected consequence positive
            if (currentMood > 0)
            {
                // Satisfaction
                return "Content"; 
            }
            else if (currentMood < 0)
            {
                //Disappointment
                return "Disappointment";
            }
            else{
                if (othersMood > 0)
                {
                    // Resentment
                    return "Resentment";
                }
                else if (othersMood < 0)
                {
                    // Gloating
                    return "Gloating";
                }
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
        }
        return "";
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