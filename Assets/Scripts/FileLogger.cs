using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

public class FileLogger : MonoBehaviour
{
    public static FileLogger mainInstance;
    private string filePath;
    private bool headerDone = false;
    private int roundCount = 0;
    private void Awake()
    {
        //enforce singleton
        if (mainInstance == null)
            mainInstance = this;
        else
            Destroy(gameObject);
        DontDestroyOnLoad(gameObject);
    }

    // Returns the file path string. Called when awaken only.
    private string GetLogPath()
    {
        string datePatt = @"yy-MM-dd-hh-mm";
        DateTime timeNow = DateTime.Now;
        string timeString = timeNow.ToUniversalTime().ToString(datePatt);
        string botTypes = string.Join("-", GameController.gameController.playerBotType);
        return Application.persistentDataPath + "/Logs/" + timeString + botTypes + ".csv";
    }
    public void WriteLineToLog(string log_string, string filepath_override = null)
    {
        filePath ??= filePath ?? GetLogPath();
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
    public void IncrementRound()
    {
        roundCount++;
    }
    public int GetCurrentRoundCount()
    {
        return roundCount;
    }
}
