using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using System.Linq;
public class SettingsHud : MonoBehaviour
{
    private SliderInt mapSizeSlider;
    private SliderInt maxObjectivesSlider;
    private SliderInt playerCountSlider;
    private SliderInt movesCountSlider;
    private SliderInt maxTurnsSlider;
    private TextField maxRoundsField;
    private TextField localPlayerIDField;
    private Toggle hotSeatToggle;
    private Toggle logToCSVToggle;
    private Toggle smoothMoveToggle;
    private Toggle autoProcessTurnToggle;
    private List<DropdownField> botDropdownList = new();
    private RadioButtonGroup radioButtonGroupSelectRun;
    private Button buttonStartGame;
    private Button buttonAddRun;
    private Button buttonRemoveRun;
    private Button buttonStressTest;
    private List<ConfigData> cfgList = new();
    private void OnEnable()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;
        mapSizeSlider = root.Q<SliderInt>("mapSizeSlider");
        maxObjectivesSlider = root.Q<SliderInt>("maxObjectivesSlider");
        playerCountSlider = root.Q<SliderInt>("playerCountSlider");
        movesCountSlider = root.Q<SliderInt>("movesCountSlider");
        maxTurnsSlider = root.Q<SliderInt>("maxTurnsSlider");

        maxRoundsField = root.Q<TextField>("maxRoundsField");
        localPlayerIDField = root.Q<TextField>("localPlayerIDField");
        hotSeatToggle = root.Q<Toggle>("hotSeatToggle");
        logToCSVToggle = root.Q<Toggle>("logToCSVToggle");   
        smoothMoveToggle = root.Q<Toggle>("smoothMoveToggle");
        autoProcessTurnToggle = root.Q<Toggle>("autoProcessTurnToggle");

        buttonStartGame = root.Q<Button>("buttonStartGame");
        buttonAddRun = root.Q<Button>("buttonAddRun");
        buttonRemoveRun = root.Q<Button>("buttonRemoveRun");
        buttonStressTest = root.Q<Button>("buttonStressTest");
        radioButtonGroupSelectRun = root.Q<RadioButtonGroup>("radioGroupSelectRun");
        
        List<string> botProfilesList = new(); 
        foreach (BotProfile pf in GameController.BotProfiles)
        {
            botProfilesList.Add(pf.name);
        }
        botProfilesList.Add("Human");

        botDropdownList.Add(root.Q<DropdownField>("trojanBotDropdown"));
        botDropdownList.Add(root.Q<DropdownField>("scannerBot1Dropdown"));
        botDropdownList.Add(root.Q<DropdownField>("scannerBot2Dropdown"));
        botDropdownList.Add(root.Q<DropdownField>("scannerBot3Dropdown"));
        botDropdownList.Add(root.Q<DropdownField>("scannerBot4Dropdown"));
        for(int i = 0; i < botDropdownList.Count; i++)
        {
            botDropdownList[i].choices = botProfilesList;
        }
        localPlayerIDField.RegisterCallback<ChangeEvent<string>>(LocalPlayerIDFieldOnChanged);
        maxRoundsField.RegisterCallback<ChangeEvent<string>>(MaxRoundsFieldOnChanged);
        buttonStartGame.RegisterCallback<ClickEvent>(StartGameOnClicked);
        radioButtonGroupSelectRun.RegisterValueChangedCallback(SelectRunOnChanged);
        buttonAddRun.RegisterCallback<ClickEvent>(AddRunOnClicked);
        buttonRemoveRun.RegisterCallback<ClickEvent>(RemoveRunOnClicked);
        buttonStressTest.RegisterCallback<ClickEvent>(StressTestOnClicked);
        playerCountSlider.RegisterValueChangedCallback(PlayerCountOnChanged);

        mapSizeSlider.RegisterValueChangedCallback(OnNormalSliderChanged);
        maxObjectivesSlider.RegisterValueChangedCallback(OnNormalSliderChanged);
        movesCountSlider.RegisterValueChangedCallback(OnNormalSliderChanged);
        maxTurnsSlider.RegisterValueChangedCallback(OnNormalSliderChanged);

        hotSeatToggle.RegisterValueChangedCallback(OnNormalToggleChanged);
        logToCSVToggle.RegisterValueChangedCallback(OnNormalToggleChanged);
        smoothMoveToggle.RegisterValueChangedCallback(OnNormalToggleChanged);
        autoProcessTurnToggle.RegisterValueChangedCallback(OnNormalToggleChanged);

    }
    // Start is called before the first frame update
    void Start()
    {
        ConfigDataList cfgraw = LoadData.Load();
        if (cfgraw != null && cfgraw.configList.Length > 0)
        {
            cfgList = cfgraw.configList.ToList();
        }
        else
        {
            ConfigData cfg = new();
            cfgList = new(){ cfg };
        }
        List<string> runs = new();
        for (int i = 0; i < cfgList.Count(); i++)
        {
            runs.Add("Run " + i.ToString());
            
        }
        radioButtonGroupSelectRun.choices = runs;
        radioButtonGroupSelectRun.SetValueWithoutNotify(0);

        ConfigData newcfg = new();
        if(cfgList[0] != null)
        {
            newcfg = cfgList[0];
        }
        mapSizeSlider.SetValueWithoutNotify(newcfg.mapSize);
        maxObjectivesSlider.SetValueWithoutNotify(newcfg.maxObjectives);
        playerCountSlider.SetValueWithoutNotify(newcfg.playersCount);
        movesCountSlider.SetValueWithoutNotify(newcfg.movesCount);
        maxTurnsSlider.SetValueWithoutNotify(newcfg.maxTurnCount);
        maxRoundsField.SetValueWithoutNotify(newcfg.maxRoundCount.ToString());
        localPlayerIDField.SetValueWithoutNotify(newcfg.localPlayerID.ToString());
        hotSeatToggle.SetValueWithoutNotify(newcfg.hotSeatMode);
        logToCSVToggle.SetValueWithoutNotify(newcfg.logToCSV);
        autoProcessTurnToggle.SetValueWithoutNotify(newcfg.autoProcessTurn);
        smoothMoveToggle.SetValueWithoutNotify(newcfg.useSmoothMove);

        for(int i = 0; i < newcfg.playerBotType.Length; i++)
        {
            botDropdownList[i].SetValueWithoutNotify(newcfg.playerBotType[i]);
        }
    }
    private void MaxRoundsFieldOnChanged(ChangeEvent<string> evt)
    {
        if (!int.TryParse(evt.newValue, out _))
        {
            maxRoundsField.SetValueWithoutNotify(evt.previousValue);
        }
        SaveCurrentConfigs();
    }
    private void LocalPlayerIDFieldOnChanged(ChangeEvent<string> evt)
    {
        if (!int.TryParse(evt.newValue, out _))
        {
            localPlayerIDField.SetValueWithoutNotify(evt.previousValue);
        }
        SaveCurrentConfigs();
    }
    private void StartGameOnClicked(ClickEvent evt)
    {
        SaveCurrentConfigs();
        SceneManager.LoadScene("MainSimulation", LoadSceneMode.Single);
    }
    private void AddRunOnClicked(ClickEvent evt){
        List<string> runs = new();
        for (int i = 0; i < cfgList.Count(); i++)
        {
            runs.Add("Run " + i.ToString());
        }
        runs.Add("Run " + cfgList.Count());
        radioButtonGroupSelectRun.choices = runs;
        cfgList.Add(null);
        radioButtonGroupSelectRun.value = radioButtonGroupSelectRun.choices.ToArray().Length-1;
    }
    private void RemoveRunOnClicked(ClickEvent evt){
        List<string> runs = radioButtonGroupSelectRun.choices.ToList();
        runs.RemoveAt(runs.Count - 1);
        radioButtonGroupSelectRun.choices = runs;
        cfgList.RemoveAt(cfgList.Count - 1);
        radioButtonGroupSelectRun.value = radioButtonGroupSelectRun.choices.ToArray().Length-1;
    }
    private void StressTestOnClicked(ClickEvent evt){
        int i = 0;
        List<ConfigData>cfgList = new();
        for (int mapsize = 3; mapsize <= 20; mapsize++)
        {
            for (int objPairs = 1; objPairs <= mapsize/2; objPairs++)
            {
                for(int plCount = 2; plCount <= 5; plCount++)
                {
                    foreach(string tProf in new string[] { "GreedyTrojanE", "CautiousTrojanE"})
                    {
                        foreach(string sProf in new string[] { "ClosestScannerE", "MiddleSplitScannerE", "SoloScannerE"})
                        {          
                            if(!(tProf.Equals("GreedyTrojanE") && sProf.Equals("SoloScannerE")))
                            {
                                List<string> bots = new(){tProf};
                                for (int j = 0; j < (plCount-1); j++)
                                {
                                    bots.Add(sProf);
                                }
                                cfgList.Add(new ConfigData(mapsize, plCount, 3, 100, 250, objPairs, 0, true, true, false, true, bots.ToArray()));
                            }
                        }
                    }
                }
            }
        }
        ConfigDataList cfgDataList = new();
        cfgDataList.configList = cfgList.ToArray();
        LoadData.Save(cfgDataList);
        SceneManager.LoadScene("MainSimulation", LoadSceneMode.Single);
    }
    private void OnNormalSliderChanged(ChangeEvent<int> evt)
    {
        if (evt.newValue == evt.previousValue)
            return;
        SaveCurrentConfigs();
    }
    private void OnNormalToggleChanged(ChangeEvent<bool> evt)
    {
        if (evt.newValue == evt.previousValue)
            return;
        SaveCurrentConfigs();
    }
    private void PlayerCountOnChanged(ChangeEvent<int> evt)
    {
        if (evt.newValue == evt.previousValue)
            return;
        int max = evt.newValue > evt.previousValue ? evt.newValue : evt.previousValue;
        for(int i = 0; i < max; i++)
        {
            if(i < evt.newValue)
            {
                botDropdownList[i].style.visibility = Visibility.Visible;
            }
            else
            {
                botDropdownList[i].style.visibility = Visibility.Hidden;
                botDropdownList[i].SetValueWithoutNotify("");
            }
        }
        SaveCurrentConfigs();
    }
    private void SaveCurrentConfigs(int id = -1)
    {
        if (id == -1)
            id = radioButtonGroupSelectRun.value;
        ConfigData cfg = new ConfigData(
            mapSizeSlider.value, 
            playerCountSlider.value, 
            movesCountSlider.value, 
            maxTurnsSlider.value,
            int.Parse(maxRoundsField.value),
            maxObjectivesSlider.value,
            int.Parse(localPlayerIDField.value),
            hotSeatToggle.value,
            logToCSVToggle.value,
            smoothMoveToggle.value,
            autoProcessTurnToggle.value,
            botDropdownList.Select(list=> list.value).ToArray()
        );
        if (id != -1 && id < cfgList.Count)
        {
            cfgList[id] = cfg;
        }
        ConfigDataList newcfglist = new()
        {
            configList = cfgList.ToArray()
        };
        LoadData.Save(newcfglist);
    }
    private void SelectRunOnChanged(ChangeEvent<int> evt)
    {
        if (evt.newValue != evt.previousValue)
        {
            SaveCurrentConfigs(evt.previousValue);
            ConfigData newcfg = new();
            if(cfgList[evt.newValue] != null)
            {
                newcfg = cfgList[evt.newValue];
            }
            mapSizeSlider.SetValueWithoutNotify(newcfg.mapSize);
            maxObjectivesSlider.SetValueWithoutNotify(newcfg.maxObjectives);
            playerCountSlider.SetValueWithoutNotify(newcfg.playersCount);
            movesCountSlider.SetValueWithoutNotify(newcfg.movesCount);
            maxTurnsSlider.SetValueWithoutNotify(newcfg.maxTurnCount);
            maxRoundsField.SetValueWithoutNotify(newcfg.maxRoundCount.ToString());
            localPlayerIDField.SetValueWithoutNotify(newcfg.localPlayerID.ToString());
            hotSeatToggle.SetValueWithoutNotify(newcfg.hotSeatMode);
            logToCSVToggle.SetValueWithoutNotify(newcfg.logToCSV);
            autoProcessTurnToggle.SetValueWithoutNotify(newcfg.autoProcessTurn);
            smoothMoveToggle.SetValueWithoutNotify(newcfg.useSmoothMove);

            for(int i = 0; i < newcfg.playerBotType.Length; i++)
            {
                botDropdownList[i].SetValueWithoutNotify(newcfg.playerBotType[i]);
            }
            SaveCurrentConfigs();
        }
    }
}
