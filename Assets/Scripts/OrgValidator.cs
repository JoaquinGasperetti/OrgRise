using System.Collections.Generic;
using UnityEngine;

public static class OrgValidator
{
    /// <summary>
    /// Evita que un empleado se convierta en jefe de alguien que está por encima de él.
    /// </summary>
    public static bool CheckForCycles(EmployeeNode potentialManager, EmployeeNode newSubordinate)
    {
        EmployeeNode current = potentialManager;
        int safetyCounter = 0;

        while (current != null && safetyCounter < 100)
        {
            if (current == newSubordinate)
            {
                return true; // Se detectó un ciclo cerrado
            }
            current = current.manager;
            safetyCounter++;
        }
        return false;
    }

    /// <summary>
    /// Verifica que el nodo tenga una cadena de mando continua hasta la raíz (CEO).
    /// </summary>
    public static bool ConnectsToRoot(EmployeeNode node, EmployeeNode rootNode)
    {
        if (node == null || rootNode == null) return false;
        if (node == rootNode) return true;

        EmployeeNode current = node.manager;
        int safetyCounter = 0;

        while (current != null && safetyCounter < 100)
        {
            if (current == rootNode) return true;
            current = current.manager;
            safetyCounter++;
        }
        return false;
    }

    /// <summary>
    /// Valida si el estado actual de todos los nodos cumple con las condiciones de victoria.
    /// </summary>
    public static bool ValidateWinCondition(List<EmployeeNode> levelNodes, EmployeeNode rootNode)
    {
        if (levelNodes == null || levelNodes.Count == 0) return false;

        // Si no se especificó raíz, intentamos localizar el nodo raíz en la lista
        if (rootNode == null)
        {
            foreach (var n in levelNodes)
            {
                if (n != null && n.isRootNode)
                {
                    rootNode = n;
                    break;
                }
            }
        }

        if (rootNode == null) return false;

        int connectedNodes = 0;

        foreach (var node in levelNodes)
        {
            if (node == null) continue;

            // Falla si algún nodo supera su tramo de control permitido
            if (node.directReports.Count > node.maxDirectReports)
                return false;

            // Verificamos que el nodo esté conectado y llegue por cadena de mando hasta la raíz (CEO)
            if (ConnectsToRoot(node, rootNode))
                connectedNodes++;
        }

        // Falla si hay nodos huérfanos o desconectados de la estructura principal
        return connectedNodes == levelNodes.Count;
    }
}