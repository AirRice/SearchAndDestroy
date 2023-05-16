using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class GameHud : MonoBehaviour
{
    public static GameHud gameHud;
    private int lastActivePlayer = -1;
    private int lastPlayerTurns = -1;
    private Label labelCurPlayer;
    private List<Label> labelsMoveIndicator = new List<Label>();
    private Label labelBottomText;
    private Button buttonEndTurn;
    private Button buttonPlayerAction;
    public bool playerActionButtonDown;
    private bool lastPlayerActionButtonDown;
    private void OnEnable()
    {
        //enforce singleton
        if (gameHud != null)
            Destroy(gameHud.gameObject);
        gameHud = this;

        VisualElement root = GetComponent<UIDocument>().rootVisualElement;
        VisualElement overlay = root.Q<VisualElement>("Overlay");
        labelCurPlayer = root.Q<Label>("LabelCurPlayer");
        buttonEndTurn = root.Q<Button>("ButtonEndTurn");
        buttonPlayerAction = root.Q<Button>("ButtonPlayerAction");
        labelsMoveIndicator.Add(root.Q<Label>("LabelMove1Image"));
        labelsMoveIndicator.Add(root.Q<Label>("LabelMove2Image"));
        labelsMoveIndicator.Add(root.Q<Label>("LabelMove3Image"));
        labelBottomText = overlay.Q<Label>("LabelBottomText");
        buttonEndTurn.RegisterCallback<ClickEvent>(EndTurnOnClicked);
        buttonPlayerAction.RegisterCallback<ClickEvent>(PlayerActionOnClicked);
    }

    private void EndTurnOnClicked(ClickEvent evt)
    {
        GameController.gameController.ProgressTurn();
    }
    private void PlayerActionOnClicked(ClickEvent evt)
    {
        playerActionButtonDown = !playerActionButtonDown;
        
    }
    public void ResetPlayerActionButton()
    {
        playerActionButtonDown = false;
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    private string infectNodeDesc = "Infect an adjacent node on the map for the following turn. Infected nodes are not traversable by Scanner players.";
    private string scanNodeDesc = "Scan an adjacent node on the map and discover the distance to the Trojan player from the scan point.";
    void Update()
    {
        int gameCurrentTurnPlayer = GameController.gameController.currentTurnPlayer;
        int gameLocalPlayer = GameController.gameController.localPlayerID;
        int gameCurrentTurnMovesLeft = GameController.gameController.currentPlayerMoves;
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
                    labelsMoveIndicator[i].style.unityBackgroundImageTintColor = gameCurrentTurnMovesLeft < GameController.gameController.movesCount && i < (GameController.gameController.movesCount-gameCurrentTurnMovesLeft) ? Color.gray : Color.white;
                }
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
}
