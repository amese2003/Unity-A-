using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grid : MonoBehaviour {

    public bool onlyDisplayPathGizmos;
    public LayerMask unwalkableMask;
    public Vector2 gridWorldSize;
    public TerrainType[] walkableRegions;
    public float nodeRadius;
    Node[,] grid;
    Dictionary<int, int> walkableRegionsDictionary = new Dictionary<int, int>();

    int obstacleProximityPenalty = 10;

    // 간격;
    public float nodeDiameter; //grid의 사이즈겸 간격 담당
    
    int gridSizeX, gridSizeY;
    LayerMask walkableMask;

    int penaltyMin = int.MaxValue;
    int penaltyMax = int.MinValue;


    void Awake()
    {
        nodeDiameter = nodeRadius * 2; // grid간의 간격 (중앙에서 중앙으로)
        gridSizeX = Mathf.RoundToInt(gridWorldSize.x / nodeDiameter);
        gridSizeY = Mathf.RoundToInt(gridWorldSize.y / nodeDiameter);

        foreach (TerrainType region in walkableRegions)
        {
            walkableMask.value += region.terrainMask.value;
            walkableRegionsDictionary.Add((int)Mathf.Log(region.terrainMask.value, 2), region.terrainPenalty);
        }
        CreateGrid();
    }

    public int MaxSize
    {
        get
        {
            return gridSizeX * gridSizeY;
        }
    }


    void CreateGrid()
    {
        grid = new Node[gridSizeX, gridSizeY];
        Vector3 worldBottomLeft = transform.position - Vector3.right * gridWorldSize.x / 2 - Vector3.forward * gridWorldSize.y / 2;

        for (int x = 0; x < gridSizeX; x++)
        {
            for(int y = 0; y < gridSizeY; y++)
            {
                Vector3 worldPoint = worldBottomLeft + Vector3.right * (x * nodeDiameter + nodeRadius) + Vector3.forward * (y * nodeDiameter + nodeRadius);
                bool walkable = !(Physics.CheckSphere(worldPoint, nodeRadius, unwalkableMask));

                int movementPanelty = 0;

                if (walkable)
                {
                    Ray ray = new Ray(worldPoint + Vector3.up * 50, Vector3.down);
                    RaycastHit hit;
                    if (Physics.Raycast(ray, out hit, 100, walkableMask))
                        walkableRegionsDictionary.TryGetValue(hit.collider.gameObject.layer, out movementPanelty);
                }

                if (!walkable)
                {
                    movementPanelty += obstacleProximityPenalty;
                }

                grid[x, y] = new Node(walkable, worldPoint, x, y, movementPanelty);
            }
        }

        BlurPenaltyMap(3);
    }

    public Node NodeFromWorldPoint(Vector3 worldPosition)
    {
        float percentX = (worldPosition.x + gridWorldSize.x / 2) / gridWorldSize.x;
        float percentY = (worldPosition.z + gridWorldSize.y / 2) / gridWorldSize.y;

        percentX = Mathf.Clamp01(percentX);
        percentY = Mathf.Clamp01(percentY);

        int x = Mathf.RoundToInt((gridSizeX - 1) * percentX);
        int y = Mathf.RoundToInt((gridSizeY - 1) * percentY);

        return grid[x, y];

    }

    void BlurPenaltyMap (int blurSize)
    {
        int kernalSize = blurSize * 2 + 1;
        int kernalExtends = (kernalSize - 1) / 2;

        int[,] penaltyHorizontalPass = new int[gridSizeX, gridSizeY];
        int[,] penaltyVerticalPass = new int[gridSizeX, gridSizeY];

        for(int col = 0; col < gridSizeY; col++)
        {
            for(int row = -kernalExtends; row <= kernalExtends; row++)
            {
                int sampleX = Mathf.Clamp(row, 0, kernalExtends);
                penaltyHorizontalPass[0, col] += grid[sampleX, col].movePanelty;
            }

            for(int row = 1; row < gridSizeX; row++)
            {
                int removeidx = Mathf.Clamp(row - kernalExtends - 1, 0, gridSizeX);
                int addIndex = Mathf.Clamp(row + kernalExtends, 0, gridSizeX - 1);

                penaltyHorizontalPass[row, col] = penaltyHorizontalPass[row - 1, col] - grid[removeidx, col].movePanelty + grid[addIndex, col].movePanelty;
            }
        }

        for (int row = 0; row < gridSizeX; row++)
        {
            for (int col = -kernalExtends; col <= kernalExtends; col++)
            {
                int sampleY = Mathf.Clamp(row, 0, kernalExtends);
                penaltyVerticalPass[row, 0] += penaltyHorizontalPass[row, sampleY];
            }

            for (int col = 1; col < gridSizeY; col++)
            {
                int removeidx = Mathf.Clamp(col - kernalExtends - 1, 0, gridSizeY);
                int addIndex = Mathf.Clamp(col + kernalExtends, 0, gridSizeY - 1);

                penaltyVerticalPass[row, col] = penaltyVerticalPass[row, col - 1] - penaltyHorizontalPass[row, removeidx] + penaltyHorizontalPass[row, addIndex];
                int blurredPanelty = Mathf.RoundToInt((float)penaltyVerticalPass[row, col] / (kernalSize * kernalSize));
                grid[row, col].movePanelty = blurredPanelty;

                if (blurredPanelty > penaltyMax)
                    penaltyMax = blurredPanelty;

                if (blurredPanelty < penaltyMin)
                    penaltyMin = blurredPanelty;
            }
        }
    }


    public List<Node> GetNeighbours(Node node)
    {
        List<Node> neighbours = new List<Node>();

        for(int x = -1; x<=1; x++)
        {
            for(int y=-1; y<=1; y++)
            {
                if (x == 0 && y == 0)
                    continue;

                int checkX = node.gridX + x;
                int checkY = node.gridY + y;

                if(checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY)
                {
                    neighbours.Add(grid[checkX, checkY]);
                }

            }
        }
        return neighbours;
    }


    public List<Node> path;

    void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(transform.position, new Vector3(gridWorldSize.x, 1, gridWorldSize.y));
        if (grid != null && onlyDisplayPathGizmos)
        {
            foreach (Node n in grid)
            {
                Gizmos.color = (n.walkable) ? Color.white : Color.red;
                Gizmos.DrawCube(n.worldPosition, Vector3.one * (nodeDiameter - .1f));
            }
        }
        //if (onlyDisplayPathGizmos)
        //{
        //    if (path != null)
        //    {
        //        foreach (Node n in path)
        //        {
        //            Gizmos.color = Color.black;
        //            Gizmos.DrawCube(n.worldPosition, Vector3.one * (nodeDiameter - .1f));
        //        }
        //    }

        //}
        //else
        //{

        //    if (grid != null)
        //    {
        //        Node playerNode = NodeFromWorldPoint(player.position);

        //        foreach (Node n in grid)
        //        {
        //            Gizmos.color = (n.walkable) ? Color.white : Color.red;

        //            if (path != null)
        //                if (path.Contains(n))
        //                    Gizmos.color = Color.yellow;

        //            if (playerNode == n)
        //                Gizmos.color = Color.cyan;

        //            Gizmos.DrawCube(n.worldPosition, Vector3.one * (nodeDiameter - .1f));
        //        }
        //    }
        //}
    }

    [System.Serializable]
    public class TerrainType
    {
        public LayerMask terrainMask;
        public int terrainPenalty;
    }


}
