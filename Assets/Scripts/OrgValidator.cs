using System.Collections.Generic;
using UnityEngine;

public struct ScorecardOrganizacional
{
    public int estrellas; // 1, 2 o 3 estrellas
    public int costoEstructura; // Ponderación de gasto salarial / overhead administrativo
    public float distanciaMediaJerarquica; // Saltos promedio desde núcleo operativo al CEO
    public float eficienciaTramo; // % de aprovechamiento armónico de la supervisión
    public string evaluacionPedagogica; // Feedback teórico para el trabajo final
}

public static class OrgValidator
{
    /// <summary>
    /// Evita que un empleado se convierta en jefe de alguien que está por encima de él en la jerarquía.
    /// Valida tanto la línea directa como el reporte matricial secundario.
    /// </summary>
    public static bool CheckForCycles(EmployeeNode potentialManager, EmployeeNode newSubordinate)
    {
        if (potentialManager == null || newSubordinate == null) return false;

        Queue<EmployeeNode> cola = new Queue<EmployeeNode>();
        HashSet<EmployeeNode> visitados = new HashSet<EmployeeNode>();
        cola.Enqueue(potentialManager);

        int safetyCounter = 0;
        while (cola.Count > 0 && safetyCounter < 150)
        {
            EmployeeNode actual = cola.Dequeue();
            safetyCounter++;

            if (actual == newSubordinate)
            {
                return true; // Se detectó un ciclo cerrado
            }

            if (actual != null && !visitados.Contains(actual))
            {
                visitados.Add(actual);
                if (actual.manager != null) cola.Enqueue(actual.manager);
                if (actual.matrixProjectManager != null) cola.Enqueue(actual.matrixProjectManager);
            }
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

        Queue<EmployeeNode> cola = new Queue<EmployeeNode>();
        HashSet<EmployeeNode> visitados = new HashSet<EmployeeNode>();
        cola.Enqueue(node);

        int safetyCounter = 0;
        while (cola.Count > 0 && safetyCounter < 150)
        {
            EmployeeNode actual = cola.Dequeue();
            safetyCounter++;

            if (actual == rootNode) return true;

            if (actual != null && !visitados.Contains(actual))
            {
                visitados.Add(actual);
                if (actual.manager != null) cola.Enqueue(actual.manager);
                if (actual.matrixProjectManager != null) cola.Enqueue(actual.matrixProjectManager);
            }
        }
        return false;
    }

    /// <summary>
    /// Valida si el estado actual de todos los nodos cumple con las condiciones de victoria.
    /// </summary>
    public static bool ValidateWinCondition(List<EmployeeNode> levelNodes, EmployeeNode rootNode)
    {
        if (levelNodes == null || levelNodes.Count == 0) return false;

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

            // Falla si algún nodo supera su tramo de control permitido (salvo que sea staff de asesoría)
            if (!node.isStaffNode && node.directReports.Count > node.maxDirectReports)
                return false;

            // Verificamos que el nodo esté conectado y llegue por cadena de mando hasta la raíz (CEO)
            if (ConnectsToRoot(node, rootNode))
                connectedNodes++;
        }

        // Falla si hay nodos huérfanos o desconectados de la estructura principal
        return connectedNodes == levelNodes.Count;
    }

    /// <summary>
    /// Evalúa la estructura organizacional diseñada según criterios de Mintzberg y Robbins,
    /// computando KPIs (costo overhead, distancia jerárquica y balance de tramo) y otorgando de 1 a 3 estrellas.
    /// </summary>
    public static ScorecardOrganizacional EvaluarEstructura(List<EmployeeNode> nodos, EmployeeNode root, ModoAutoridad autoridad)
    {
        ScorecardOrganizacional scorecard = new ScorecardOrganizacional();
        if (nodos == null || nodos.Count == 0) return scorecard;

        int costo = 0;
        int totalOperativos = 0;
        int sumaProfundidad = 0;
        int mandosAlTope = 0;
        int totalMandos = 0;

        foreach (var n in nodos)
        {
            if (n == null) continue;

            // Costo salarial / overhead por nivel
            switch (n.nivel)
            {
                case NivelJerarquico.CEO: costo += 6; break;
                case NivelJerarquico.Director: costo += 4; break;
                case NivelJerarquico.Gerente: costo += 2; break;
                case NivelJerarquico.EmpleadoBase: costo += 1; break;
            }
            if (n.isStaffNode) costo += 2;

            // Conteo de mandos y estrés de supervisión
            if (n.maxDirectReports > 0)
            {
                totalMandos++;
                if (n.directReports.Count >= n.maxDirectReports)
                {
                    mandosAlTope++;
                }
            }

            // Distancia jerárquica de los empleados base al CEO
            if (n.nivel == NivelJerarquico.EmpleadoBase)
            {
                totalOperativos++;
                int saltos = 0;
                EmployeeNode cur = n.manager;
                while (cur != null && cur != root && saltos < 10)
                {
                    saltos++;
                    cur = cur.manager;
                }
                sumaProfundidad += (saltos + 1);
            }
        }

        scorecard.costoEstructura = costo;
        scorecard.distanciaMediaJerarquica = totalOperativos > 0 ? (float)sumaProfundidad / totalOperativos : 2.0f;
        scorecard.eficienciaTramo = totalMandos > 0 ? 1.0f - ((float)mandosAlTope / (totalMandos * 2f)) : 1.0f;

        // Cálculo de Estrellas (1 a 3)
        // 3 Estrellas: Tramo balanceado sin cuellos de botella y distancia jerárquica equilibrada
        // 2 Estrellas: Resuelto con mandos al límite de su capacidad
        // 1 Estrella: Estructura sobrecargada
        if (mandosAlTope <= 1)
        {
            scorecard.estrellas = 3;
            scorecard.evaluacionPedagogica = "<b>🌟🌟🌟 Diseño Organizacional Óptimo</b>\nLograste un equilibrio armónico entre la capacidad de supervisión de los mandos y la agilidad de comunicación hacia el núcleo operativo.";
        }
        else if (mandosAlTope <= (totalMandos / 2) + 1)
        {
            scorecard.estrellas = 2;
            scorecard.evaluacionPedagogica = "<b>🌟🌟 Diseño Funcional Válido</b>\nEl organigrama cumple las reglas de autoridad, aunque algunos mandos operan al límite de su tramo de control, reduciendo la flexibilidad de supervisión.";
        }
        else
        {
            scorecard.estrellas = 1;
            scorecard.evaluacionPedagogica = "<b>🌟 Diseño Ajustado</b>\nEstructura validada pero con cuellos de botella significativos. Considerá delegar autoridad táctica para aliviar la carga de supervisión.";
        }

        return scorecard;
    }
}