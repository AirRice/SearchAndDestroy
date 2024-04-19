using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class SoloScan : BotTemplate
{
    /**
    This algorithm works in the following order.
    1. If no previous scan information/newly infected nodes exist for this turn, scan to gain some information.
    2. Using the info, locate the closest potential location the hidden player could be in at the current moment. Triangulate possible locations from all known info that turn.
    3. Head to the location and scan the node.
    4. If the scanning has not resulted in a scanner victory, carry out step 2-3 again.
    5. Do this until the action allocation runs out.
    **/
    private List<int> possibleLocations = new();
    protected override int GetMovementTarget(int specActionTarget)
    {
        GameController gcr = GameController.gameController;
        if (specActionTarget != -1)
        {
            List<int> nodesTowards = gcr.GetClosestAdjToDest(currentLocation,specActionTarget);
            int rand_index = Random.Range(0, nodesTowards.Count);
            int potential_target = nodesTowards[rand_index];
            //Debug.Log($"target node to scan is {specActionTarget}, heading to node {potential_target}");
            return potential_target;
        }
        else
        {
            // This shouldn't happen
            int toMoveTo = SelectNextNodeRandom(currentLocation);
            //Debug.Log($"Moving to node id {toMoveTo}");
            return toMoveTo;
        }
    }

    protected override int GetSpecialActionTarget()
    {
        GameController gcr = GameController.gameController;
        List<(int,int[])> prevScans = gcr.scanHistory;
        // If no previous scan info exists for the turn, do one just for some information
        if (prevScans.Count == 0)
        {
            //Debug.Log($"Scanning at node id {currentLocation} as first action");
            return currentLocation;
        }
        else
        {
            //Make this list for the first time
            if (possibleLocations.Count == 0)
            {
                for(int i = 1; i <= gcr.mapSize * gcr.mapSize; i++)
                {
                    possibleLocations.Add(i);
                }
            }
            //truncate this list by intersecting it with the possible directions from each scan info
            foreach ((int,int[]) info in prevScans)
            {
                //Debug.Log($"Previous scan info: Hidden player was in direction(s) of Node(s) {string.Join(" and ",info.Item2)} from starting node {info.Item1}");
                List<int> nodesInDir = gcr.GetDestsClosestToAdjs(info.Item1,info.Item2);
                
                possibleLocations = possibleLocations.Intersect(nodesInDir).ToList();
            }

            //Get the closest possible location
            if (possibleLocations.Count > 0)
            {
                int closestTarget = possibleLocations.Aggregate((id1, id2) => gcr.GetPathLength(id1, currentLocation) < gcr.GetPathLength(id2, currentLocation) ? id1 : id2);
                //Debug.Log($"Closest potential Target found: node id {closestTarget}");
                return closestTarget;
            }
            else
            {
                //Debug.Log($"No potential targets found.");
                return -1;
            }
        }
    }
}