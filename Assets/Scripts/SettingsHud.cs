using System.Collections;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class SettingsHud : MonoBehaviour
{
    private Slider mapSizeSlider;
    private Slider maxObjectivesSlider;
    private Slider playerCountSlider;
    private Slider movesCountSlider;
    private Slider maxTurnsSlider;
    private TextField maxRoundsField;
    private Toggle hotSeatToggle;
    private Toggle logToCSVToggle;
    private Toggle smoothMoveToggle;
    private List<EnumField> botDropdownList = new();
    private Button buttonStartGame;

    private void OnEnable()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;
        VisualElement bg = root.Q<VisualElement>("menuBackground");
        mapSizeSlider = bg.Q<Slider>("mapSizeSlider");
        maxObjectivesSlider = bg.Q<Slider>("maxObjectivesSlider");
        playerCountSlider = bg.Q<Slider>("playerCountSlider");
        movesCountSlider = bg.Q<Slider>("movesCountSlider");
        maxTurnsSlider = bg.Q<Slider>("maxTurnsSlider");

        maxRoundsField = bg.Q<TextField>("maxRoundsField");

        hotSeatToggle = bg.Q<Toggle>("hotSeatToggle");
        logToCSVToggle = bg.Q<Toggle>("logToCSVToggle");   
        smoothMoveToggle = bg.Q<Toggle>("smoothMoveToggle");

        buttonStartGame = bg.Q<Button>("buttonStartGame");   
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }
}
