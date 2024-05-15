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
    private Toggle hotSeatToggle;
    private Toggle logToCSVToggle;
    private Toggle smoothMoveToggle;
    private Toggle autoProcessTurnToggle;
    private List<DropdownField> botDropdownList = new();
    private RadioButtonGroup radioButtonGroupSelectRun;
    private Button buttonStartGame;
    private Button buttonAddRun;
    private Button buttonRemoveRun;
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

        hotSeatToggle = root.Q<Toggle>("hotSeatToggle");
        logToCSVToggle = root.Q<Toggle>("logToCSVToggle");   
        smoothMoveToggle = root.Q<Toggle>("smoothMoveToggle");
        autoProcessTurnToggle = root.Q<Toggle>("autoProcessTurnToggle");

        buttonStartGame = root.Q<Button>("buttonStartGame");
        buttonAddRun = root.Q<Button>("buttonAddRun");
        buttonRemoveRun = root.Q<Button>("buttonRemoveRun");

        radioButtonGroupSelectRun = root.Q<RadioButtonGroup>("radioGroupSelectRun");
        // Update this for when algorithms get added later
        List<string> trojanModes = new() { "GreedyTrojan", "GreedyAvoidTrojan", "MCTSTrojan", "HumanPlayer"};
        List<string> scannerModes = new() { "SoloScan", "SharedScan", "CollabScan", "HumanPlayer"};

        botDropdownList.Add(root.Q<DropdownField>("trojanBotDropdown"));
        botDropdownList.Add(root.Q<DropdownField>("hunterBot1Dropdown"));
        botDropdownList.Add(root.Q<DropdownField>("hunterBot2Dropdown"));
        botDropdownList.Add(root.Q<DropdownField>("hunterBot3Dropdown"));
        botDropdownList.Add(root.Q<DropdownField>("hunterBot4Dropdown"));
        for(int i = 0; i < botDropdownList.Count; i++)
        {
            botDropdownList[i].choices = i == 0 ? trojanModes : scannerModes;
        }
        maxRoundsField.RegisterCallback<ChangeEvent<string>>(MaxRoundsFieldOnChanged);
        buttonStartGame.RegisterCallback<ClickEvent>(StartGameOnClicked);
        radioButtonGroupSelectRun.RegisterValueChangedCallback(SelectRunOnChanged);
        buttonAddRun.RegisterCallback<ClickEvent>(AddRunOnClicked);
        buttonRemoveRun.RegisterCallback<ClickEvent>(RemoveRunOnClicked);
        playerCountSlider.RegisterValueChangedCallback(PlayerCountOnChanged);
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
        radioButtonGroupSelectRun.value = 0;
    }
    private void MaxRoundsFieldOnChanged(ChangeEvent<string> evt)
    {
        if (!int.TryParse(evt.newValue, out _))
        {
            maxRoundsField.SetValueWithoutNotify(evt.previousValue);
        }
    }
    private void StartGameOnClicked(ClickEvent evt)
    {
        ConfigDataList newcfglist = new()
        {
            configList = cfgList.ToArray()
        };
        LoadData.Save(newcfglist);
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
    }
    private void SelectRunOnChanged(ChangeEvent<int> evt)
    {
        if (evt.newValue != evt.previousValue)
        {
            ConfigData cfg = new ConfigData(
                mapSizeSlider.value, 
                playerCountSlider.value, 
                movesCountSlider.value, 
                maxTurnsSlider.value,
                int.Parse(maxRoundsField.value),
                maxObjectivesSlider.value,
                hotSeatToggle.value,
                logToCSVToggle.value,
                smoothMoveToggle.value,
                autoProcessTurnToggle.value,
                botDropdownList.Select(list=> list.value).ToArray()
            );
            if (evt.previousValue != -1 && evt.previousValue < cfgList.Count)
            {
                cfgList[evt.previousValue] = cfg;
            }

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
            hotSeatToggle.SetValueWithoutNotify(newcfg.hotSeatMode);
            logToCSVToggle.SetValueWithoutNotify(newcfg.logToCSV);
            smoothMoveToggle.SetValueWithoutNotify(newcfg.useSmoothMove);

            for(int i = 0; i < newcfg.playerBotType.Length; i++)
            {
                botDropdownList[i].SetValueWithoutNotify(newcfg.playerBotType[i]);
            }
            ConfigDataList newcfglist = new()
            {
                configList = cfgList.ToArray()
            };
            LoadData.Save(newcfglist);
        }
    }
}
