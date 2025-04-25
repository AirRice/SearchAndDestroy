using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class GameHud : MonoBehaviour
{
    private int lastActivePlayer = -1;
    private int lastPlayerTurns = -1;
    private Label labelCurPlayer;
    private Label labelObjs;
    private List<Label> labelsMoveIndicator = new List<Label>();
    private Label labelBottomText;
    private Label labelCentreText;
    private Label labelTurnCounter;
    private Label labelGameoverText;
    private Label labelPlayerDialogue;
    private Button buttonEndTurn;
    private Button buttonPlayerAction;
    private Button buttonRestartGame;
    public bool playerActionButtonDown;
    private bool lastPlayerActionButtonDown;
    private float lastDisplayedCentreMessage;
    private VisualElement emotionImage;
    private int imagecounter = 0;
    private void OnEnable()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;
        VisualElement overlay = root.Q<VisualElement>("Overlay");
        
        labelCurPlayer = root.Q<Label>("LabelCurPlayer");
        labelObjs = root.Q<Label>("LabelObjs");
        labelTurnCounter = root.Q<Label>("LabelTurnCounter");
        buttonEndTurn = root.Q<Button>("ButtonEndTurn");
        buttonPlayerAction = root.Q<Button>("ButtonPlayerAction");
        labelsMoveIndicator.Add(root.Q<Label>("LabelMove1Image"));
        labelsMoveIndicator.Add(root.Q<Label>("LabelMove2Image"));
        labelsMoveIndicator.Add(root.Q<Label>("LabelMove3Image"));
        labelGameoverText = overlay.Q<Label>("LabelGameOver");
        labelCentreText = overlay.Q<Label>("LabelCentre");
        labelBottomText = overlay.Q<Label>("LabelBottomText");

        labelPlayerDialogue = root.Q<Label>("LabelPlayerDialogue");
        emotionImage = root.Q<VisualElement>("BottomChatAvatar");

        buttonRestartGame = overlay.Q<Button>("ButtonRestartGame");

        buttonEndTurn.RegisterCallback<ClickEvent>(EndTurnOnClicked);
        buttonPlayerAction.RegisterCallback<ClickEvent>(PlayerActionOnClicked);
        buttonRestartGame.RegisterCallback<ClickEvent>(RestartOnClicked);
    }

    private void EndTurnOnClicked(ClickEvent evt)
    {
        ScreenCapture.CaptureScreenshot($"screenshots/{imagecounter:0000}.png");
        imagecounter++;
        StartCoroutine(DelayAndProgressTurn());
    }
    IEnumerator DelayAndProgressTurn()
    {
        yield return new WaitForSeconds(0.5f);
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
            GameController.gameController.RestartGame();
    }
    // Start is called before the first frame update
    void Start()
    {
        emotionImage.style.backgroundImage = null;
        labelPlayerDialogue.text = "";
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
            bool curPlayerIsLocal = (gameCurrentTurnPlayer==gameLocalPlayer);

            buttonPlayerAction.visible = curPlayerIsLocal;
            
            foreach(Label moveIndicator in labelsMoveIndicator)
            {
                moveIndicator.visible = curPlayerIsLocal;
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
    public void UpdateObjectivesData(int total, int taken)
    {
        labelObjs.text = $"Objectives Infected by Trojan: {taken}/{total}";
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
    private readonly Dictionary<string, string> OCCEmotionToImage = new(){
        {"Relief", "Images/emotions/content"},
        {"Resignation", "Images/emotions/fear"}, // Resignation is Fears Confirmed in OCC Model.
        {"Content", "Images/emotions/content"}, //Content is Satisfaction in OCC Model
        {"Surprise", "Images/emotions/surprised"}, //External to OCC model - when expectation differs from new revelation dramatically
        {"Confusion", "Images/emotions/confusion"}, //Also when expectation differs from new revelation dramatically
        {"Disappointment", "Images/emotions/sad"},
        {"Joy", "Images/emotions/happy"},
        {"Distress", "Images/emotions/sad"},
        {"Gloating", "Images/emotions/gloating"},
        {"Resentment", "Images/emotions/angry"},
        {"Neutral", "Images/emotions/neutral"}
    };
    public void PlayerDialogue(int player, string emotion, string msg)
    {
        string emotionimgpath;
        if (OCCEmotionToImage.ContainsKey(emotion))
        {
            emotionimgpath = OCCEmotionToImage[emotion];
        }
        else
        {
            emotionimgpath = "Images/emotions/" + emotion;
        }
        Texture2D emotionImg = Resources.Load<Texture2D>(emotionimgpath);
        if (emotionImg)
        {
            emotionImage.style.backgroundImage = emotionImg;
        }
        else
        {
            emotionImage.style.backgroundImage = null;
        }
        labelPlayerDialogue.text = $"Player {player}: " + msg;
    }
    public void ShowCentreMessage(string msg)
    {
        labelCentreText.style.visibility = Visibility.Visible;
        labelCentreText.text = msg;
        lastDisplayedCentreMessage = Time.time;
    }
}
