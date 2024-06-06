using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class DistanceTextPopup : MonoBehaviour
{
    static readonly float displayTime = 8.0f;
    float timeLeft = 0;
    int lastCurrentPlayer = -1;
    Canvas localCanvas;
    // Start is called before the first frame update
    void Awake()
    {
        timeLeft = displayTime;
        lastCurrentPlayer = GameController.gameController.currentTurnPlayer;
        localCanvas = GetComponent<Canvas>();
    }

    // Update is called once per frame
    void Update()
    {
        timeLeft -= Time.deltaTime;
        if (timeLeft < 0 || GameController.gameController.currentTurnPlayer != lastCurrentPlayer)
        {
             Destroy(gameObject);
        }
    }

    public void SetText(string text, Camera cam)
    {
        localCanvas.worldCamera = cam;
        TextMeshProUGUI textMesh = GetComponentInChildren<TextMeshProUGUI>();
        textMesh.text = text;
    }

    public void SetColor(Color c)
    {
        TextMeshProUGUI textMesh = GetComponentInChildren<TextMeshProUGUI>();
        textMesh.color = c;     
    }
}
