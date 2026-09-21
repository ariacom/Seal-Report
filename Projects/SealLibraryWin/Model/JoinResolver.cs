//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Seal.Model
{
    /// <summary>
    /// Result of a joins resolution: the tree of joins linking all the tables of a model
    /// </summary>
    public class JoinTree
    {
        /// <summary>
        /// Root table of the tree
        /// </summary>
        public MetaTable Root;

        /// <summary>
        /// Joins of the tree listed from the root (parent before children). Each join is oriented: LeftTable is the parent and RightTable is the child.
        /// </summary>
        public List<MetaJoin> Joins = new List<MetaJoin>();

        /// <summary>
        /// Sum of the weights of the joins
        /// </summary>
        public int Weight;
    }

    /// <summary>
    /// Finds the joins to use to link the tables of a model.
    /// Tables are the nodes and joins are the edges of a directed graph: the result is the tree of minimal weight containing all the tables (Steiner tree).
    /// </summary>
    public class JoinResolver
    {
        /// <summary>
        /// Maximum number of tables to link with the exact search. Above it, an approximate search is used.
        /// </summary>
        public const int MaxExactTables = 12;

        const int WeightFactor = 1000;
        const int Infinite = int.MaxValue / 4;

        class Edge
        {
            public int from, to, cost;
            public MetaJoin join;
        }

        List<MetaTable> _nodes = new List<MetaTable>();
        Dictionary<string, int> _nodeIndexes = new Dictionary<string, int>();
        Dictionary<string, string> _tableKeys = new Dictionary<string, string>();
        List<Edge> _edges = new List<Edge>();
        List<int>[] _outEdges, _inEdges;
        bool[] _alive;
        int _tablesCount;
        bool _isLINQ;

        /// <summary>
        /// Returns the tree of joins of minimal weight linking all the tables, null if the tables cannot be linked
        /// </summary>
        public static JoinTree Resolve(IEnumerable<MetaJoin> joins, List<MetaTable> tables, bool isLINQ, StringBuilder logs)
        {
            return new JoinResolver().resolve(joins, tables, isLINQ, logs);
        }

        //For LINQ, all the tables of a SQL source are got in a single result table: they are the same node
        string getKey(MetaTable table)
        {
            if (!_isLINQ) return table.GUID;
            if (!_tableKeys.TryGetValue(table.GUID, out string result))
            {
                result = table.LINQResultName;
                _tableKeys.Add(table.GUID, result);
            }
            return result;
        }

        int getNodeIndex(MetaTable table)
        {
            var key = getKey(table);
            if (!_nodeIndexes.TryGetValue(key, out int result))
            {
                result = _nodes.Count;
                _nodes.Add(table);
                _nodeIndexes.Add(key, result);
            }
            return result;
        }

        void addEdge(MetaJoin join)
        {
            int from = getNodeIndex(join.LeftTable), to = getNodeIndex(join.RightTable);
            //Only the first join defined between 2 tables is used
            if (from == to || _edges.Exists(i => i.from == from && i.to == to)) return;
            //A right outer join has a small penalty to get first the joins in the direction they are defined
            int cost = WeightFactor * Math.Max(1, join.Weight) + (join.JoinType == JoinType.RightOuter ? 1 : 0);
            _edges.Add(new Edge() { from = from, to = to, cost = cost, join = join });
        }

        MetaJoin getReversedJoin(MetaJoin join)
        {
            var result = MetaJoin.Create();
            result.IsBiDirectional = false;
            result.GUID = join.GUID;
            result.Name = join.Name;
            result.Source = join.Source;
            result.Weight = join.Weight;
            result.LeftTableGUID = join.RightTableGUID;
            result.RightTableGUID = join.LeftTableGUID;

            //Invert left and right
            if (join.JoinType == JoinType.LeftOuter) result.JoinType = JoinType.RightOuter;
            else if (join.JoinType == JoinType.RightOuter) result.JoinType = JoinType.LeftOuter;
            else result.JoinType = join.JoinType;

            result.Clause = join.Clause;
            if (_isLINQ)
            {
                //invert also the clause using equals
                var clauses = join.Clause.Split(" equals ");
                if (clauses.Length == 2) result.Clause = clauses[1] + " equals " + clauses[0];
            }
            return result;
        }

        JoinTree resolve(IEnumerable<MetaJoin> joins, List<MetaTable> tables, bool isLINQ, StringBuilder logs)
        {
            var timer = DateTime.Now;
            _isLINQ = isLINQ;

            //Nodes: tables to link first, this gives them the priority when several trees have the same weight
            foreach (var table in tables) getNodeIndex(table);
            _tablesCount = _nodes.Count;

            //Edges: a bi-directional join gives 2 edges
            int joinsCount = 0;
            foreach (var join in joins.Where(i => i.LeftTable != null && i.RightTable != null))
            {
                joinsCount++;
                addEdge(join);
                if (join.IsBiDirectional) addEdge(getReversedJoin(join));
            }

            int n = _nodes.Count;
            _outEdges = new List<int>[n];
            _inEdges = new List<int>[n];
            for (int i = 0; i < n; i++)
            {
                _outEdges[i] = new List<int>();
                _inEdges[i] = new List<int>();
            }
            for (int i = 0; i < _edges.Count; i++)
            {
                _outEdges[_edges[i].from].Add(i);
                _inEdges[_edges[i].to].Add(i);
            }

            removeDeadEnds();

            if (logs != null)
            {
                logs.AppendFormat("Tables to link ({0}): {1}\r\n", _tablesCount, string.Join(", ", _nodes.Take(_tablesCount).Select(i => i.DisplayName)));
                logs.AppendFormat("Joins available: {0}, tables: {1} ({2} after removing the dead ends)\r\n", joinsCount, n, _alive.Count(i => i));
            }

            bool isExact = _tablesCount <= MaxExactTables;
            List<Edge> treeEdges = new List<Edge>();
            int root = isExact ? searchExact(treeEdges) : searchApproximate(treeEdges);

            if (logs != null)
            {
                logs.AppendFormat("Search: {0}\r\n", isExact ? "exact (tree of minimal weight)" : string.Format("approximate (more than {0} tables to link)", MaxExactTables));
                logs.AppendFormat("Time elapsed: {0:F0} ms\r\n", (DateTime.Now - timer).TotalMilliseconds);
            }
            if (root < 0)
            {
                if (logs != null) logs.AppendLine("\r\nNo tree found: the tables cannot be linked with the joins available.");
                return null;
            }

            //List the joins from the root, children in the order of the join definitions
            var result = new JoinTree() { Root = _nodes[root] };
            var printedTree = new StringBuilder();
            addJoins(result, treeEdges.OrderBy(i => _edges.IndexOf(i)).ToList(), root, 1, new HashSet<int>() { root }, printedTree);
            result.Weight = result.Joins.Sum(i => Math.Max(1, i.Weight));

            if (logs != null)
            {
                logs.AppendFormat("\r\nJOINS TREE: {0} join(s), total weight {1}\r\n", result.Joins.Count, result.Weight);
                logs.AppendLine(result.Root.DisplayName);
                logs.Append(printedTree);
            }
            return result;
        }

        void addJoins(JoinTree tree, List<Edge> treeEdges, int node, int level, HashSet<int> done, StringBuilder printedTree)
        {
            foreach (var edge in treeEdges.Where(i => i.from == node))
            {
                if (!done.Add(edge.to)) continue;
                tree.Joins.Add(edge.join);
                printedTree.AppendFormat("{0}{1} {2}  [Join '{3}', weight {4}]\r\n", new string(' ', 4 * level), edge.join.SQLJoinType, edge.join.RightTable.DisplayName, edge.join.Name, Math.Max(1, edge.join.Weight));
                addJoins(tree, treeEdges, edge.to, level + 1, done, printedTree);
            }
        }

        //An intermediate table linked to only one table cannot be part of the tree
        void removeDeadEnds()
        {
            int n = _nodes.Count;
            _alive = Enumerable.Repeat(true, n).ToArray();
            bool changed = true;
            while (changed)
            {
                changed = false;
                for (int v = _tablesCount; v < n; v++)
                {
                    if (!_alive[v]) continue;
                    var neighbors = new HashSet<int>();
                    foreach (var e in _outEdges[v]) if (_alive[_edges[e].to]) neighbors.Add(_edges[e].to);
                    foreach (var e in _inEdges[v]) if (_alive[_edges[e].from]) neighbors.Add(_edges[e].from);
                    if (neighbors.Count <= 1)
                    {
                        _alive[v] = false;
                        changed = true;
                    }
                }
            }
        }

        //Exact search (Dreyfus-Wagner): weights[S][v] is the minimal weight of a tree having the root v and containing the tables of the set S
        int searchExact(List<Edge> treeEdges)
        {
            int n = _nodes.Count, full = (1 << _tablesCount) - 1;
            var weights = new int[full + 1][];
            //origins[S][v] > 0: the tree is the merge of 2 trees having the root v (value is the first sub-set), < 0: the tree starts with an edge (value is -(edge index + 1))
            var origins = new int[full + 1][];
            var queue = new PriorityQueue<int, int>();

            for (int set = 1; set <= full; set++)
            {
                var w = Enumerable.Repeat(Infinite, n).ToArray();
                var o = new int[n];
                weights[set] = w;
                origins[set] = o;

                if ((set & (set - 1)) == 0)
                {
                    //only one table
                    w[System.Numerics.BitOperations.TrailingZeroCount(set)] = 0;
                }
                else
                {
                    //merge 2 trees having the same root
                    for (int subSet = (set - 1) & set; subSet > 0; subSet = (subSet - 1) & set)
                    {
                        int otherSet = set ^ subSet;
                        if (subSet < otherSet) break;
                        int[] w1 = weights[subSet], w2 = weights[otherSet];
                        for (int v = 0; v < n; v++)
                        {
                            int weight = w1[v] + w2[v];
                            if (weight < w[v])
                            {
                                w[v] = weight;
                                o[v] = subSet;
                            }
                        }
                    }
                }

                //extend the trees with an edge arriving to the root
                queue.Clear();
                for (int v = 0; v < n; v++) if (_alive[v] && w[v] < Infinite) queue.Enqueue(v, w[v]);
                while (queue.TryDequeue(out int node, out int nodeWeight))
                {
                    if (nodeWeight > w[node]) continue;
                    foreach (var e in _inEdges[node])
                    {
                        var edge = _edges[e];
                        if (!_alive[edge.from]) continue;
                        int weight = nodeWeight + edge.cost;
                        if (weight < w[edge.from])
                        {
                            w[edge.from] = weight;
                            o[edge.from] = -(e + 1);
                            queue.Enqueue(edge.from, weight);
                        }
                    }
                }
            }

            int root = -1;
            for (int v = 0; v < n; v++)
            {
                if (_alive[v] && weights[full][v] < Infinite && (root < 0 || weights[full][v] < weights[full][root])) root = v;
            }
            if (root >= 0) addTreeEdges(origins, full, root, treeEdges);
            return root;
        }

        void addTreeEdges(int[][] origins, int set, int node, List<Edge> treeEdges)
        {
            int origin = origins[set][node];
            if (origin > 0)
            {
                addTreeEdges(origins, origin, node, treeEdges);
                addTreeEdges(origins, set ^ origin, node, treeEdges);
            }
            else if (origin < 0)
            {
                var edge = _edges[-origin - 1];
                treeEdges.Add(edge);
                addTreeEdges(origins, set, edge.to, treeEdges);
            }
        }

        //Approximate search: for each possible root, the tree is built by adding the path to the nearest table not yet linked
        int searchApproximate(List<Edge> treeEdges)
        {
            int n = _nodes.Count, bestRoot = -1, bestWeight = Infinite;
            var queue = new PriorityQueue<int, int>();

            for (int root = 0; root < n; root++)
            {
                if (!_alive[root]) continue;

                var inTree = new bool[n];
                var edges = new List<Edge>();
                inTree[root] = true;
                int treeWeight = 0, linked = root < _tablesCount ? 1 : 0;

                while (linked < _tablesCount && treeWeight < bestWeight)
                {
                    //distances from the current tree
                    var distances = Enumerable.Repeat(Infinite, n).ToArray();
                    var origins = new int[n];
                    queue.Clear();
                    for (int v = 0; v < n; v++)
                    {
                        if (inTree[v])
                        {
                            distances[v] = 0;
                            queue.Enqueue(v, 0);
                        }
                    }
                    while (queue.TryDequeue(out int node, out int distance))
                    {
                        if (distance > distances[node]) continue;
                        foreach (var e in _outEdges[node])
                        {
                            var edge = _edges[e];
                            if (!_alive[edge.to]) continue;
                            if (distance + edge.cost < distances[edge.to])
                            {
                                distances[edge.to] = distance + edge.cost;
                                origins[edge.to] = e;
                                queue.Enqueue(edge.to, distances[edge.to]);
                            }
                        }
                    }

                    //nearest table not yet linked
                    int nearest = -1;
                    for (int v = 0; v < _tablesCount; v++)
                    {
                        if (!inTree[v] && distances[v] < Infinite && (nearest < 0 || distances[v] < distances[nearest])) nearest = v;
                    }
                    if (nearest < 0) break;

                    //add its path to the tree
                    treeWeight += distances[nearest];
                    for (int v = nearest; !inTree[v]; v = _edges[origins[v]].from)
                    {
                        inTree[v] = true;
                        if (v < _tablesCount) linked++;
                        edges.Add(_edges[origins[v]]);
                    }
                }

                if (linked == _tablesCount && treeWeight < bestWeight)
                {
                    bestRoot = root;
                    bestWeight = treeWeight;
                    treeEdges.Clear();
                    treeEdges.AddRange(edges);
                }
            }
            return bestRoot;
        }
    }
}
