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

        // Recorremos la cadena de mando hacia arriba (recursividad iterativa)
        while (current != null)
        {
            if (current == newSubordinate)
            {
                return true; // Se detectó un ciclo cerrado
            }
            current = current.manager;
        }
        return false;
    }

    /// <summary>
    /// Valida si el estado actual de todos los nodos cumple con las condiciones de victoria.
    /// </summary>
    public static bool ValidateWinCondition(List<EmployeeNode> levelNodes, EmployeeNode rootNode)
    {
        int connectedNodes = 0;

        foreach (var node in levelNodes)
        {
            // Falla si algún nodo supera su tramo de control permitido
            if (node.directReports.Count > node.maxDirectReports)
                return false;

            // Contamos los nodos que están conectados al árbol principal
            if (node.manager != null || node == rootNode)
                connectedNodes++;
        }

        // Falla si hay nodos huérfanos (desconectados de la estructura)
        if (connectedNodes < levelNodes.Count)
            return false;

        return true; // El puzzle está resuelto correctamente
    }
}