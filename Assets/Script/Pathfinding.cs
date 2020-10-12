using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;

public class Pathfinding : MonoBehaviour {

    public Transform seeker, target;


    PathRequestManager requestManager;
    Grid grid;

    void Awake()
    {
        requestManager = GetComponent<PathRequestManager>();
        grid = GetComponent<Grid>();   
    }

    
    void Update()
    {
        if(Input.GetButtonDown("Jump"))
            FindPath(seeker.position, target.position);
    }
    
    
    public void StartFindPath(Vector3 startPos, Vector3 targetPos)
    {
       FindPath(startPos, targetPos);
    }
    

    void FindPath(Vector3 startPos, Vector3 targetPos)
    {
        Stopwatch sw = new Stopwatch();
        sw.Start();
        Node startNode = grid.NodeFromWorldPoint(startPos);
        Node targetNode = grid.NodeFromWorldPoint(targetPos);

        List<Node> openSet = new List<Node>();
        HashSet<Node> closedSet = new HashSet<Node>();
        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            Node CurrentNode = openSet[0];
            UnityEngine.Debug.Log(openSet.Count);

            for(int i = 1; i<openSet.Count; i++)
            {
                if (openSet[i].fCost < CurrentNode.fCost || openSet[i].fCost == CurrentNode.fCost && openSet[i].hCost < CurrentNode.hCost)
                    CurrentNode = openSet[i];
            }

            openSet.Remove(CurrentNode);
            closedSet.Add(CurrentNode); 

            if(CurrentNode == targetNode)
            {
                sw.Stop();
                print("Path found :" + sw.ElapsedMilliseconds + " ms");
                RetracePath(startNode, targetNode);
                return;
            }

            foreach(Node neighbour in grid.GetNeighbours(CurrentNode))
            {
                if (!neighbour.walkable || closedSet.Contains(neighbour))
                    continue;



                int newMovementCostToNeighbour = CurrentNode.gCost + GetDistance(CurrentNode, neighbour);

                if(newMovementCostToNeighbour < neighbour.gCost || !openSet.Contains(neighbour))
                {
                    neighbour.gCost = newMovementCostToNeighbour;
                    neighbour.hCost = GetDistance(neighbour, targetNode);
                    neighbour.parent = CurrentNode;


                    if (!openSet.Contains(neighbour))
                        openSet.Add(neighbour);

                }
            }
        }
    }

    void RetracePath(Node startNode, Node endNode)
    {
        List<Node> path = new List<Node>();
        Node currentNode = endNode;

        while(currentNode != startNode)
        {
            path.Add(currentNode);
            currentNode = currentNode.parent;
        }

        path.Reverse();
        grid.path = path;

    }




    int GetDistance(Node nodeA, Node nodeB)
    {
        int dstX = Mathf.Abs(nodeA.gridX - nodeB.gridX);
        int dstY = Mathf.Abs(nodeA.gridY - nodeB.gridY);


        if (dstX > dstY)
            return 14 * dstY + 10 * (dstX - dstY);

        return 14 * dstX + 10 * (dstY - dstX);

    }


}
