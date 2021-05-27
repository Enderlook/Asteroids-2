﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if SPATIAL_GRID
namespace Spatial
{
    public sealed class SpatialGrid : MonoBehaviour
    {
        #region Variables

        //punto de inicio de la grilla en X
        public float x;

        //punto de inicio de la grilla en Z
        public float y;

        //ancho de las celdas
        public float cellWidth;

        //alto de las celdas
        public float cellHeight;

        //cantidad de columnas (el "ancho" de la grilla)
        public int width;

        //cantidad de filas (el "alto" de la grilla)
        public int height;

        //ultimas posiciones conocidas de los elementos, guardadas para comparación.
        private Dictionary<IGridEntity, Tuple<int, int>> lastPositions = new Dictionary<IGridEntity, Tuple<int, int>>();

        //los "contenedores"
        private HashSet<IGridEntity>[,] buckets;

        //el valor de posicion que tienen los elementos cuando no estan en la zona de la grilla.
        /*
         Const es implicitamente statica
         const tengo que ponerle el valor apenas la declaro, readonly puedo hacerlo en el constructor.
         Const solo sirve para tipos de dato primitivos.
         */
        readonly public Tuple<int, int> Outside = Tuple.Create(-1, -1);

        //Una colección vacía a devolver en las queries si no hay nada que devolver
        readonly public IGridEntity[] Empty = new IGridEntity[0];

        #endregion

        #region Funciones

        public void Generate()
        {
            buckets = new HashSet<IGridEntity>[width, height];

            //creamos todos los hashsets
            for (var i = 0; i < width; i++)
            {
                for (var j = 0; j < height; j++)
                {
                    buckets[i, j] = new HashSet<IGridEntity>();
                }
            }

            var ents = RecursiveWalker(transform)
                      .Select(n => n.GetComponent<IGridEntity>())
                      .Where(n => n != null);

            foreach (var e in ents)
            {
                e.OnMove += UpdateEntity;
                UpdateEntity(e);
            }
        }

        public void UpdateEntity(IGridEntity entity)
        {
            var lastPos = lastPositions.ContainsKey(entity) ? lastPositions[entity] : Outside;
            var currentPos = GetPositionInGrid(entity.Position);

            //Misma posición, no necesito hacer nada
            if (lastPos.Equals(currentPos))
                return;

            //Lo "sacamos" de la posición anterior
            if (IsInsideGrid(lastPos))
            {
                buckets[lastPos.Item1, lastPos.Item2].Remove(entity);
            }

            //Lo "metemos" a la celda nueva, o lo sacamos si salio de la grilla
            if (IsInsideGrid(currentPos))
            {
                buckets[currentPos.Item1, currentPos.Item2].Add(entity);
                lastPositions[entity] = currentPos;
            }
            else
                lastPositions.Remove(entity);
        }

        public IEnumerable<IGridEntity> Query(Vector2 aabbFrom, Vector2 aabbTo, Func<Vector2, bool> filterByPosition)
        {
            var from = new Vector2(Mathf.Min(aabbFrom.x, aabbTo.x), Mathf.Min(aabbFrom.y, aabbTo.y));
            var to = new Vector2(Mathf.Max(aabbFrom.x, aabbTo.x), Mathf.Max(aabbFrom.y, aabbTo.y));

            var fromCoord = GetPositionInGrid(from);
            var toCoord = GetPositionInGrid(to);

            fromCoord = Tuple.Create(Util.Clamp(fromCoord.Item1, 0, width), Util.Clamp(fromCoord.Item2, 0, height));
            toCoord = Tuple.Create(Util.Clamp(toCoord.Item1, 0, width), Util.Clamp(toCoord.Item2, 0, height));

            if (!IsInsideGrid(fromCoord) && !IsInsideGrid(toCoord))
                return Empty;

            // Creamos tuplas de cada celda
            var cols = Util.Generate(fromCoord.Item1, x => x + 1)
                           .TakeWhile(n => n < width && n <= toCoord.Item1);

            var rows = Util.Generate(fromCoord.Item2, y => y + 1)
                           .TakeWhile(y => y < height && y <= toCoord.Item2);

            var cells = cols.SelectMany(
                                        col => rows.Select(
                                                           row => Tuple.Create(col, row)
                                                          )
                                       );

            // Iteramos las que queden dentro del criterio
            return cells
                  .SelectMany(cell => buckets[cell.Item1, cell.Item2])
                  .Where(e =>
                             from.x <= e.Position.x && e.Position.x <= to.x &&
                             from.y <= e.Position.y && e.Position.y <= to.y
                        )
                  .Where(n => filterByPosition(n.Position));
        }

        public Tuple<int, int> GetPositionInGrid(Vector2 pos)
        {
            //quita la diferencia, divide segun las celdas y floorea
            return Tuple.Create(Mathf.FloorToInt((pos.x - x) / cellWidth),
                                Mathf.FloorToInt((pos.y - y) / cellHeight));
        }

        public bool IsInsideGrid(Tuple<int, int> position)
        {
            //si es menor a 0 o mayor a width o height, no esta dentro de la grilla
            return 0 <= position.Item1 && position.Item1 < width &&
                   0 <= position.Item2 && position.Item2 < height;
        }

        void OnDestroy()
        {
            var ents = RecursiveWalker(transform).Select(n => n.GetComponent<IGridEntity>())
                                                 .Where(n => n != null);

            foreach (var e in ents) e.OnMove -= UpdateEntity;
        }

        #region GENERATORS

        private static IEnumerable<Transform> RecursiveWalker(Transform parent)
        {
            foreach (Transform child in parent)
            {
                foreach (Transform grandchild in RecursiveWalker(child))
                    yield return grandchild;
                yield return child;
            }
        }

        #endregion

        #endregion

        #region GRAPHIC REPRESENTATION

        public bool areGizmosShutDown;
        public bool activatedGrid;
        public bool showLogs = true;

        private void OnDrawGizmos()
        {
            var rows = Util.Generate(y, curr => curr + cellHeight)
                           .Select(row => Tuple.Create(new Vector2(x, row),
                                                       new Vector2(x + cellWidth * width, row)));

            //equivalente de rows
            /*for (int i = 0; i <= height; i++)
            {
                Gizmos.DrawLine(new Vector3(x, 0, z + cellHeight * i), new Vector3(x + cellWidth * width,0, z + cellHeight * i));
            }*/

            var cols = Util.Generate(x, curr => curr + cellWidth)
                           .Select(col => Tuple.Create(new Vector2(col, y),
                                                       new Vector2(col, y + cellHeight * height)));

            var allLines = rows.Take(width + 1).Concat(cols.Take(height + 1));

            foreach (var elem in allLines)
            {
                Gizmos.DrawLine(elem.Item1, elem.Item2);
            }

            if (buckets == null || areGizmosShutDown) return;

            var originalCol = GUI.color;
            GUI.color = Color.red;
            if (!activatedGrid)
            {
                var allElems = new List<IGridEntity>();
                foreach (var elem in buckets)
                    allElems = allElems.Concat(elem).ToList();

                int connections = 0;
                foreach (var entity in allElems)
                {
                    foreach (var neighbour in allElems.Where(x => x != entity))
                    {
                        Gizmos.DrawLine(entity.Position, neighbour.Position);
                        connections++;
                    }

                    if (showLogs)
                        Debug.Log("tengo " + connections + " conexiones por individuo");
                    connections = 0;
                }
            }
            else
            {
                int connections = 0;
                foreach (var elem in buckets)
                {
                    foreach (var ent in elem)
                    {
                        foreach (var n in elem.Where(x => x != ent))
                        {
                            Gizmos.DrawLine(ent.Position, n.Position);
                            connections++;
                        }

                        if (showLogs)
                            Debug.Log("tengo " + connections + " conexiones por individuo");
                        connections = 0;
                    }
                }
            }

            GUI.color = originalCol;
            showLogs = false;
        }

        #endregion
    }
}
#endif