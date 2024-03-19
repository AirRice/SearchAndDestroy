using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadData : MonoBehaviour
{
    private static readonly string filePath = System.IO.Path.Combine(Application.dataPath, "config.json");
    public static ConfigData Load(){
        
        Debug.Log(filePath);
        string data = System.IO.File.ReadAllText(filePath);
        ConfigData cfgData = JsonUtility.FromJson<ConfigData>(data);
        return cfgData;
    }
    public static void Save(ConfigData cfgData)
    {
        string jsonString = JsonUtility.ToJson(cfgData);
        System.IO.File.WriteAllText(filePath, jsonString);
    }
}

[System.Serializable]
public class ConfigData{
    public int mapSize;
    public int playersCount;
    public int movesCount;
    public int maxTurnCount;
    public int maxRoundCount;
    public int maxObjectives;
    public bool hotSeatMode;
    public bool logToCSV;
    public bool useSmoothMove;
    public string[] playerBotType;

    public ConfigData(
        int mapSize,
        int playersCount,
        int movesCount,
        int maxTurnCount,
        int maxRoundCount,
        int maxObjectives,
        bool hotSeatMode,
        bool logToCSV,
        bool useSmoothMove,
        string[] playerBotType
    )
    {
        this.mapSize = mapSize;
        this.playersCount = playersCount;
        this.movesCount = movesCount;
        this.maxTurnCount = maxTurnCount;
        this.maxRoundCount = maxRoundCount;
        this.maxObjectives = maxObjectives;
        this.hotSeatMode = hotSeatMode;
        this.logToCSV = logToCSV;
        this.useSmoothMove = useSmoothMove;
        this.playerBotType = playerBotType;
    }
}