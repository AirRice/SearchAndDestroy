using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class GameHud : MonoBehaviour
{
    private int lastActivePlayer = -1;
    private Label labelCurPlayer;
    private Button buttonEndTurn;
    private void OnEnable()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;

        labelCurPlayer = root.Q<Label>("LabelCurPlayer");
        buttonEndTurn = root.Q<Button>("ButtonEndTurn");
        
        buttonEndTurn.RegisterCallback<ClickEvent>(EndTurnOnClicked);
    }

    private void EndTurnOnClicked(ClickEvent evt)
    {
        GameController.gameController.ProgressTurn();
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        int gameCurrentTurnPlayer = GameController.gameController.currentTurnPlayer;
        //Debug.Log($"UIElement Update:{gameCurrentTurnPlayer}");
        if(lastActivePlayer!=gameCurrentTurnPlayer)
        {
            lastActivePlayer = gameCurrentTurnPlayer;
            labelCurPlayer.text = "Current Player: "+ (lastActivePlayer == 0 ? "The Trojan" : $"Scanner {lastActivePlayer}");
        }
    }
}
