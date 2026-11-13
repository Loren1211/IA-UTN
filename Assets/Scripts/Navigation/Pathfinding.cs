using System.Collections.Generic;
using UnityEngine;

public static class Pathfinding
{
    public enum HeuristicMethod { Distance, Manhattan }

    public static float GetHeuristic(Vector3 posA, Vector3 posB, HeuristicMethod method)
    {
        return method switch
        {
            HeuristicMethod.Distance => (posA - posB).magnitude,
            HeuristicMethod.Manhattan => Mathf.Abs(posA.x - posB.x) + Mathf.Abs(posA.z - posB.z),
            _ => Vector3.Distance(posA, posB),
        };
    }

    public static List<Node> AStar(Node startingNode, Node endNode, bool avoidSafeZones = false,
                                   HeuristicMethod method = HeuristicMethod.Distance)
    {
        if (startingNode == null || endNode == null)
        {
            Debug.LogWarning("A*: nodo inicial o final inválido.");
            return new List<Node>();
        }

        if (Graph.Current == null)
        {
            Debug.LogWarning("A*: no hay un grafo activo.");
            return new List<Node>();
        }

        if (startingNode.Blocked || endNode.Blocked) return new List<Node>();
        if (avoidSafeZones && endNode.IsSafeZone) return new List<Node>();

        PriorityQueue<Node> frontier = new();
        frontier.Enqueue(startingNode, 0f);

        Dictionary<Node, Node> cameFrom = new()
        {
            [startingNode] = null
        };

        Dictionary<Node, float> costSoFar = new()
        {
            [startingNode] = 0f
        };

        Vector3 endPosition = endNode.transform.position;
        float heuristicScale = 1f / Mathf.Max(0.01f, Graph.Current.NodeOffset);

        while (frontier.Count != 0)
        {
            Node current = frontier.Dequeue();

            if (current == endNode)
            {
                List<Node> path = new();
                while (current != null)
                {
                    path.Add(current);
                    current = cameFrom[current];
                }

                path.Reverse();
                return path;
            }

            foreach (Node neighbor in Graph.Current.GetNeighbors(current))
            {
                if (neighbor == null || neighbor.Blocked) continue;
                if (avoidSafeZones && neighbor.IsSafeZone) continue;

                float newCost = costSoFar[current] + neighbor.Cost;
                if (cameFrom.ContainsKey(neighbor) && newCost >= costSoFar[neighbor]) continue;

                cameFrom[neighbor] = current;
                costSoFar[neighbor] = newCost;

                float heuristic = GetHeuristic(neighbor.transform.position, endPosition, method) * heuristicScale;
                frontier.Enqueue(neighbor, newCost + heuristic);
            }
        }

        return new List<Node>();
    }

    public static float GetPathLength(List<Node> path)
    {
        if (path == null || path.Count < 2) return 0f;

        float length = 0f;
        for (int i = 1; i < path.Count; i++)
        {
            Vector3 a = path[i - 1].transform.position;
            Vector3 b = path[i].transform.position;
            a.y = 0f;
            b.y = 0f;
            length += Vector3.Distance(a, b);
        }

        return length;
    }
}
