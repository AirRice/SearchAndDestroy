using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// Data class used to state a link between 2 nodes on the map.
public class NodeLink
{
    private int nodeA;
    private int nodeB;

    public NodeLink(int a, int b)
    {
        nodeA = a;
        nodeB = b;
        Node nodeAInstance = Node.getNode(nodeA);
        Node nodeBInstance = Node.getNode(nodeB);
        if (nodeAInstance != null && nodeBInstance != null)
            nodeAInstance.AddOtherNode(nodeBInstance);
    }
    
    public override string ToString()
    {
      return base.ToString() + ", Link between" + nodeA.ToString() + " and " + nodeB.ToString();
    }
    public int? getOtherNode(int startNode)
    {
        if(nodeA != startNode){
            if(nodeB!=startNode)
                return null;
            else
                return nodeA;
        }
        else
            return nodeB;
    }
    public bool IsLinkBetween(int a, int b)
    {
        return (nodeA == a && nodeB == b || nodeA == b && nodeB == a);
    }
}