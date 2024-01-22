using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadData : MonoBehaviour
{
    public static ConfigData Load(){
        string filePath = System.IO.Path.Combine(Application.dataPath, "config.json");
        Debug.Log(filePath);
        string data = System.IO.File.ReadAllText(filePath);
        
        ConfigData cfgData = JsonUtility.FromJson<ConfigData>(data);
        return cfgData;
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
}