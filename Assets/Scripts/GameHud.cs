using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class GameHud : MonoBehaviour
{
    private int lastActivePlayer = -1;
    private int lastPlayerTurns = -1;
    private Label labelCurPlayer;
    private List<Label> labelsMoveIndicator = new List<Label>();
    private Label labelBottomText;
    private Label labelCentreText;
    private Label labelTurnCounter;
    private Label labelGameoverText;
    private Button buttonEndTurn;
    private Button buttonPlayerAction;
    private Button buttonRestartGame;
    public bool playerActionButtonDown;
    private bool lastPlayerActionButtonDown;
    private float lastDisplayedCentreMessage;
    private void OnEnable()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;
        VisualElement overlay = root.Q<VisualElement>("Overlay");
        labelCurPlayer = root.Q<Label>("LabelCurPlayer");
        labelTurnCounter = root.Q<Label>("LabelTurnCounter");
        buttonEndTurn = root.Q<Button>("ButtonEndTurn");
        buttonPlayerAction = root.Q<Button>("ButtonPlayerAction");
        labelsMoveIndicator.Add(root.Q<Label>("LabelMove1Image"));
        labelsMoveIndicator.Add(root.Q<Label>("LabelMove2Image"));
        labelsMoveIndicator.Add(root.Q<Label>("LabelMove3Image"));
        labelGameoverText = overlay.Q<Label>("LabelGameOver");
        labelCentreText = overlay.Q<Label>("LabelCentre");
        labelBottomText = overlay.Q<Label>("LabelBottomText");
        buttonRestartGame = overlay.Q<Button>("ButtonRestartGame");

        buttonEndTurn.RegisterCallback<ClickEvent>(EndTurnOnClicked);
        buttonPlayerAction.RegisterCallback<ClickEvent>(PlayerActionOnClicked);
        buttonRestartGame.RegisterCallback<ClickEvent>(RestartOnClicked);
    }

    private void EndTurnOnClicked(ClickEvent evt)
    {
        GameController.gameController.ProgressTurn();
    }
    private void PlayerActionOnClicked(ClickEvent evt)
    {
        int gameCurrentTurnPlayer = GameController.gameController.currentTurnPlayer;
        int gameLocalPlayer = GameController.gameController.localPlayerID;
        if (GameController.gameController.gameEnded || GameController.gameController.currentPlayerMoves < 1)
            return;
        // Testing for "track" type action
        if (gameCurrentTurnPlayer != 0 && gameLocalPlayer == gameCurrentTurnPlayer)
        {
            GameController.gameController.TrySpecialAction(Node.GetNode(GameController.gameController.GetActivePlayerPosition()));
            return;
        }
        playerActionButtonDown = !playerActionButtonDown;
    }
    public void ResetPlayerActionButton()
    {
        playerActionButtonDown = false;
    }
    private void RestartOnClicked(ClickEvent evt)
    {
        if (GameController.gameController.gameEnded)
            GameController.gameController.StartGame(true);
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    private readonly string infectNodeDesc = "Infect an adjacent node on the map for the following turn. Infected nodes are not traversable by Scanner players.";
    private readonly string scanNodeDesc = "Scan an adjacent node on the map and discover the distance to the Trojan player from the scan point.";
    void Update()
    {
        int gameCurrentTurnPlayer = GameController.gameController.currentTurnPlayer;
        int gameLocalPlayer = GameController.gameController.localPlayerID;
        int gameCurrentTurnMovesLeft = GameController.gameController.currentPlayerMoves;
        if(Time.time - lastDisplayedCentreMessage > 5.0f)
        {
            labelCentreText.style.visibility = Visibility.Hidden;
        }

        Visibility endOfGameVis = GameController.gameController.gameEnded ? Visibility.Visible : Visibility.Hidden;
        labelGameoverText.style.visibility = endOfGameVis;
        buttonRestartGame.style.visibility = endOfGameVis;

        if(lastPlayerActionButtonDown!=playerActionButtonDown)
        {
            lastPlayerActionButtonDown = playerActionButtonDown;
            buttonPlayerAction.style.backgroundColor = playerActionButtonDown ? Color.gray : Color.white;
            labelBottomText.text = playerActionButtonDown ? (gameCurrentTurnPlayer == 0 ? infectNodeDesc : scanNodeDesc) : ""; 
        }

        if(lastActivePlayer!=gameCurrentTurnPlayer)
        {
            lastActivePlayer = gameCurrentTurnPlayer;
            labelCurPlayer.text = "Current Player: "+ (gameCurrentTurnPlayer == 0 ? "The Trojan" : $"Scanner {gameCurrentTurnPlayer}");
            buttonPlayerAction.text = gameCurrentTurnPlayer == 0 ? "Infect Node" : "Scan"; 
            labelTurnCounter.text = "Turn: "+ (1+Mathf.FloorToInt(GameController.gameController.turnCount / GameController.gameController.playersCount));
            foreach(Label moveIndicator in labelsMoveIndicator)
            {
                moveIndicator.visible = (gameCurrentTurnPlayer==gameLocalPlayer);
            }
        }
        else
        {
            if(lastPlayerTurns!=gameCurrentTurnMovesLeft)
            {
                lastPlayerTurns = gameCurrentTurnMovesLeft;

                for(int i=0; i<GameController.gameController.movesCount; i++)
                {
                    Color labelcolor = gameCurrentTurnMovesLeft < GameController.gameController.movesCount && i < (GameController.gameController.movesCount-gameCurrentTurnMovesLeft) ? Color.gray : Color.white;
                    labelsMoveIndicator[i].style.unityBackgroundImageTintColor = labelcolor;
                }
                buttonPlayerAction.style.backgroundColor = (gameCurrentTurnMovesLeft < 1) ? Color.gray : Color.white;
            }       
        }
    }
    public void ShowMoveSpend(int tospend)
    {
        if(tospend>GameController.gameController.movesCount)
            tospend = GameController.gameController.movesCount;
        for(int i=0; i<GameController.gameController.movesCount; i++)
        {
            labelsMoveIndicator[i].style.left = i < tospend ? 24 : 0;
        }
    }
    public void ShowCentreMessage(string msg)
    {
        labelCentreText.style.visibility = Visibility.Visible;
        labelCentreText.text = msg;
        lastDisplayedCentreMessage = Time.time;
    }
}
