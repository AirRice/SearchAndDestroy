using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadData : MonoBehaviour
{
    private static readonly string filePath = System.IO.Path.Combine(Application.dataPath, "config.json");
    public static ConfigDataList Load(){
        
        Debug.Log(filePath);
        string data = System.IO.File.ReadAllText(filePath);
        ConfigDataList cfglist = JsonUtility.FromJson<ConfigDataList>(data);
        return cfglist;
    }
    public static void Save(ConfigDataList cfgData)
    {
        string jsonString = JsonUtility.ToJson(cfgData);
        Debug.Log(jsonString);
        System.IO.File.WriteAllText(filePath, jsonString);
    }
}

[System.Serializable]
public class ConfigDataList{
    public ConfigData[] configList = {};
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
    public bool autoProcessTurn;
    public bool useSmoothMove;
    public string[] playerBotType;

    public ConfigData()
    {
        mapSize = 3;
        playersCount = 3;
        movesCount = 3;
        maxTurnCount = 15;
        maxRoundCount = 100;
        maxObjectives = 2;
        hotSeatMode = true;
        logToCSV = false;
        autoProcessTurn = true;
        useSmoothMove = true;
        playerBotType = new string[] { "", "", "" };
    }

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
        bool autoProcessTurn,
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
        this.autoProcessTurn = autoProcessTurn;
        this.useSmoothMove = useSmoothMove;
        this.playerBotType = playerBotType;
    }
}