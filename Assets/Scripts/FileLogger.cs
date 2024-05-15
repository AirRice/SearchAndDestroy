using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

public class FileLogger : MonoBehaviour
{
    public static FileLogger mainInstance;
    private string fileName;
    private bool headerDone = false;
    private int roundCount = 0;
    private int runCount = 0;
    private void Awake()
    {
        //enforce singleton
        if (mainInstance == null)
        {
            mainInstance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);
        Reset();
    }

    public void Reset()
    // Resets the various values so the logging starts again on a new file.
    {
        headerDone = false;
        fileName = GetLogFileName();
        roundCount = 0;
        ConfigDataList cfgList = LoadData.Load();
        ConfigData cfg = new();
        int runNumber = GetCurrentRunCount();
        if (cfgList != null && cfgList.configList.Length > runNumber)
        {
            cfg = cfgList.configList[runNumber];
            WriteNewGameConfig(cfg);
        }
    }
    // Returns the file path string.
    private string GetLogFileName()
    {
        string datePatt = @"yyyy-MM-dd-hh-mm";
        DateTime timeNow = DateTime.Now;
        string timeString = timeNow.ToUniversalTime().ToString(datePatt);
        return timeString + "Run" + runCount.ToString();
    }
    private string GetLogPath()
    {
        return Application.persistentDataPath + "/Logs/" + fileName + ".csv";
    }
    public void WriteLineToLog(string log_string, string filepath_override = null)
    {
        string filePath = GetLogPath();
        //Debug.Log($"Writing File {filePath}");
        using (StreamWriter w = File.AppendText(filepath_override ?? filePath))
        {
            if (!headerDone){
                w.WriteLine($"sep=|");
                w.WriteLine("Round|Turn|Player|ActionType|PrevNode|TargetNode");
                headerDone = true;
            }
            w.WriteLine($"{roundCount}|"+log_string);
        }
    }
    public void WriteNewGameConfig(ConfigData cfg, string filepath_override = null)
    {
        string runHistoryFilePath = Application.persistentDataPath + "/Logs/runhistory.csv";
        if (!File.Exists(runHistoryFilePath))
        {
            // Create a file to write to.
            using (StreamWriter w = File.CreateText(runHistoryFilePath))
            {
                w.WriteLine($"sep=|");
                w.WriteLine("fileName|mapSize|playersCount|movesCount|maxTurnCount|maxRoundCount|maxObjectives|playerBotTypes");
            }	
        }
        // Assume header exists in this case
        using (StreamWriter w = File.AppendText(filepath_override ?? runHistoryFilePath))
        {
            w.WriteLine($"{fileName}|{cfg.mapSize}|{cfg.playersCount}|{cfg.movesCount}|{cfg.maxTurnCount}|{cfg.maxRoundCount}|{cfg.maxObjectives}|{string.Join(",",cfg.playerBotType)}");
        }
    }
    public void IncrementRound()
    {
        roundCount++;
    }
    public void IncrementRunCount()
    {
        runCount++;
    }
    public int GetCurrentRoundCount()
    {
        return roundCount;
    }
    public int GetCurrentRunCount()
    {
        return runCount;
    }
}
