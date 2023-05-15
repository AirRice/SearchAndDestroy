using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// Data class used to state a link between 2 stations on the map.
public class StationLink
{
    private int stationA;
    private int stationB;

    public StationLink(int a, int b)
    {
        stationA = a;
        stationB = b;
        Station stationAInstance = Station.getStation(stationA);
        Station stationBInstance = Station.getStation(stationB);
        if (stationAInstance != null && stationBInstance != null)
            stationAInstance.AddOtherStation(stationBInstance);
    }
    
    public override string ToString()
    {
      return base.ToString() + ", Link between" + stationA.ToString() + " and " + stationB.ToString();
    }
    public int? getOtherStation(int startStation)
    {
        if(stationA != startStation){
            if(stationB!=startStation)
                return null;
            else
                return stationA;
        }
        else
            return stationB;
    }
}