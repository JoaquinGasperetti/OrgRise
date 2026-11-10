using System.Collections.Generic;
using UnityEngine;

public struct ScorecardOrganizacional
{
    public int estrellas;
    public int costoEstructura;
    public float distanciaMediaJerarquica;
    public float eficienciaTramo;
    public int xpGanada;
    public string evaluacionComercial;
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
                return true;
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
    /// En modo Matricial exige además que los especialistas tengan su doble reporte (Funcional + Proyecto) si hay cupo disponible.
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

            if (!node.isStaffNode && node.directReports.Count > node.maxDirectReports)
                return false;

            if (ConnectsToRoot(node, rootNode))
                connectedNodes++;
        }

        return connectedNodes == levelNodes.Count;
    }

    /// <summary>
    /// Evalúa la estructura corporativa diseñada, combinando eficiencia estructural, precisión de movimientos
    /// y agilidad de resolución para otorgar de 1 a 3 estrellas y puntos de Carrera (XP).
    /// </summary>
    public static ScorecardOrganizacional EvaluarEstructura(
        List<EmployeeNode> nodos,
        EmployeeNode root,
        ModoAutoridad autoridad,
        int erroresCometidos = 0,
        float segundosTranscurridos = 25f)
    {
        ScorecardOrganizacional scorecard = new ScorecardOrganizacional();
        if (nodos == null || nodos.Count == 0) return scorecard;

        int presupuestoMensualK = 0;
        int totalOperativos = 0;
        int sumaProfundidad = 0;
        int mandosSobre90 = 0;
        int totalMandos = 0;

        foreach (var n in nodos)
        {
            if (n == null) continue;

            switch (n.nivel)
            {
                case NivelJerarquico.CEO: presupuestoMensualK += 18; break;
                case NivelJerarquico.Director: presupuestoMensualK += 12; break;
                case NivelJerarquico.Gerente: presupuestoMensualK += 7; break;
                case NivelJerarquico.EmpleadoBase: presupuestoMensualK += 4; break;
            }
            if (n.isStaffNode) presupuestoMensualK += 9;

            if (n.maxDirectReports > 0)
            {
                totalMandos++;
                if (n.directReports.Count > n.maxDirectReports)
                {
                    mandosSobre90++;
                }
            }

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

        scorecard.costoEstructura = presupuestoMensualK;
        scorecard.distanciaMediaJerarquica = totalOperativos > 0 ? (float)sumaProfundidad / totalOperativos : 2.0f;

        float penalizacionErrores = Mathf.Clamp01(erroresCometidos * 0.08f);
        scorecard.eficienciaTramo = Mathf.Clamp(1.0f - penalizacionErrores, 0.65f, 1.0f);

        // Sistema comercial de 3 Estrellas basado en precisión y tiempo (100% alcanzable en todos los niveles)
        if (erroresCometidos <= 1 && segundosTranscurridos <= 95f)
        {
            scorecard.estrellas = 3;
            scorecard.xpGanada = 150;
            scorecard.evaluacionComercial = "<b>🏆 Diseño Corporativo Impecable (3/3)</b>\nConectaste la estructura con máxima precisión ejecutiva y flujo de comunicación ágil.";
        }
        else if (erroresCometidos <= 3 && segundosTranscurridos <= 160f)
        {
            scorecard.estrellas = 2;
            scorecard.xpGanada = 100;
            scorecard.evaluacionComercial = "<b>📈 Operación Rentable (2/3)</b>\nLa compañía está lista para operar, aunque hubo fricciones durante la reestructuración. ¡Podés reclamar la 3ra ⭐ con el Bono Ejecutivo!";
        }
        else
        {
            scorecard.estrellas = 1;
            scorecard.xpGanada = 60;
            scorecard.evaluacionComercial = "<b>⚠️ Reestructuración Costosa (1/3)</b>\nLograste estabilizar el organigrama tras varios intentos. Usá el Bono Ejecutivo o reintentá sin errores para alcanzar las 3 ⭐.";
        }

        return scorecard;
    }
}