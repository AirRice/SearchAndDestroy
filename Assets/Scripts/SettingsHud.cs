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
    private List<DropdownField> botDropdownList = new();
    private Button buttonStartGame;

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

        buttonStartGame = root.Q<Button>("buttonStartGame");
 
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
        playerCountSlider.RegisterValueChangedCallback(PlayerCountOnChanged);
    }
    // Start is called before the first frame update
    void Start()
    {
        ConfigData cfg = LoadData.Load();
        mapSizeSlider.SetValueWithoutNotify(cfg.mapSize);
        maxObjectivesSlider.SetValueWithoutNotify(cfg.maxObjectives);
        playerCountSlider.SetValueWithoutNotify(cfg.playersCount);
        movesCountSlider.SetValueWithoutNotify(cfg.movesCount);
        maxTurnsSlider.SetValueWithoutNotify(cfg.maxTurnCount);
        maxRoundsField.SetValueWithoutNotify(cfg.maxRoundCount.ToString());
        hotSeatToggle.SetValueWithoutNotify(cfg.hotSeatMode);
        logToCSVToggle.SetValueWithoutNotify(cfg.logToCSV);
        smoothMoveToggle.SetValueWithoutNotify(cfg.useSmoothMove);

        for(int i = 0; i < cfg.playerBotType.Length; i++)
        {
            botDropdownList[i].SetValueWithoutNotify(cfg.playerBotType[i]);
        }
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
            botDropdownList.Select(list=> list.value).ToArray()
        );
        LoadData.Save(cfg);

        SceneManager.LoadScene("MainSimulation", LoadSceneMode.Single);
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
}
