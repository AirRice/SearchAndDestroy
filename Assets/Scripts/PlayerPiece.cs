using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerPiece : MonoBehaviour
{
    public int playerID = 0;
    public Material hunterMat;
    public Material hiddenMat;
    public int currentStationID = 0;
    private int lastStationID = 0;
    private Vector3 offset = new Vector3(0,0.75f,0);
    private Dictionary<int, int[]> cachedPaths = new Dictionary<int, int[]>();

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(lastStationID!=currentStationID)
        {
            lastStationID = currentStationID;
            //Wipe the cached Paths from the previous location as it has now changed
            cachedPaths = new Dictionary<int, int[]>();
            //Debug.Log($"Wiped path caches for player{playerID}, new laststation = {lastStationID}, current = {currentStationID}");
        }
    }
    // Try handling path checks
    public void TryPathHighlight(Station destStation, bool toHighlight=true)
    {   
        int[] pathToHovered;
        int remainingMoves = GameController.gameController.currentPlayerMoves;
        Debug.Log(cachedPaths.ContainsKey(destStation.stationID));
        if(!cachedPaths.TryGetValue(destStation.stationID, out pathToHovered))
        {
            pathToHovered = GameController.gameController.searchPath(Station.getStation(this.currentStationID), destStation);
            Debug.Log($"Added cached path to station {destStation.stationID}, path:"+string.Join(" ",pathToHovered));
            cachedPaths.Add(destStation.stationID,pathToHovered);
        }
        if(pathToHovered.Length > 1)
        {
            for(int i = 0; i < pathToHovered.Length-1; i++)
            {   
                int node1 = pathToHovered[i];
                int node2 = pathToHovered[i+1];
                //Debug.Log("stationLine"+node2+"-"+node1);
                Station lineDestStation = Station.getStation(node2);
                GameObject lineDrawer = GameObject.Find("stationLine"+node1+"-"+node2);
                if(lineDrawer==null)
                    lineDrawer = GameObject.Find("stationLine"+node2+"-"+node1);
                    if(lineDrawer==null)
                        break;
                lineDrawer.GetComponent<LineRenderer> ().material = (toHighlight && remainingMoves > 0) ? lineDestStation.lineActiveMat : lineDestStation.lineMat;
                remainingMoves--;
            }
        }
    }
    public void SetHidden(bool hidden)
    {
        if(!hidden)
        {
            gameObject.GetComponent<MeshRenderer> ().material = hunterMat;
        }
        else
        {
            gameObject.GetComponent<MeshRenderer> ().material = hiddenMat;
        }
    }
    public void setStation(int stationid, bool nomove = false)
    {
        if (stationid != currentStationID)
        {
            currentStationID = stationid;
            GameObject stationObject = Station.getStationObject(stationid);
            if (stationObject != null)
            {
                if (!nomove)
                    transform.position = stationObject.transform.position + offset;
            }
        
        }
    }
    public void TrySmoothMove(int toMoveTo)
    {
        if(toMoveTo == currentStationID)
            return;
        int[] pathToHovered;
        if(!cachedPaths.TryGetValue(toMoveTo, out pathToHovered))
            pathToHovered = GameController.gameController.searchPath(Station.getStation(this.currentStationID), Station.getStation(toMoveTo));
        int remainingMoves = GameController.gameController.currentPlayerMoves;
        StartCoroutine(MoveToPosition(this.transform.position, pathToHovered, this.currentStationID, toMoveTo, 1.5f));
        //Debug.Log("Moving");
    }
    //Coroutine for smooth movement
    //TODO: Find way to make piece move over each node on path (may be solved?)
    public IEnumerator MoveToPosition(Vector3 lastPos, int[] dests, int startStationID, int finalDestID, float timeToMove)
    {
        Vector3 startPos = lastPos;
        timeToMove = timeToMove/Math.Max(dests.Length,1);
        foreach(int destID in dests)
        {

            Vector3 destPos = lastPos;
            if (destID == startStationID)
                continue;
            else
            {
                GameObject stationObject = Station.getStationObject(destID);
                destPos = stationObject.transform.position + this.offset;

                float t = 0f;
                while(t < 1)
                {
                    t += Time.deltaTime / timeToMove;
                    transform.position = Vector3.Lerp(startPos, destPos, t);
                    yield return null;
                }
            }
            startPos = destPos;
        }
        
    }
}
