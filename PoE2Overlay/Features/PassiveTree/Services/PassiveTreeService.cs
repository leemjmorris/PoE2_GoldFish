using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using Serilog;

namespace PoE2Overlay.Features.PassiveTree.Services
{
    /// <summary>
    /// 패시브 트리 JSON에서 파싱된 단일 노드 정보.
    /// 좌표는 트리 데이터의 group + orbit 공식으로 계산된 절대 위치입니다.
    /// </summary>
    public class TreeNodeInfo
    {
        public int Id { get; init; }
        public string Name { get; init; } = "";
        public double X { get; init; }
        public double Y { get; init; }
        public bool IsKeystone { get; init; }
        public bool IsNotable { get; init; }
        public bool IsMastery { get; init; }
        public bool IsClassStart { get; init; }
        public List<int> Out { get; init; } = new();
    }

    /// <summary>
    /// 임베디드 리소스 tree.json을 로드하여 노드 딕셔너리를 제공합니다.
    /// GGG 공식 패시브 트리 JSON 포맷(nodes/groups/constants)을 지원합니다.
    /// </summary>
    public class PassiveTreeService
    {
        private Dictionary<int, TreeNodeInfo> _nodes = new();
        private double _minX, _minY, _maxX, _maxY;
        private bool _loaded;

        public bool IsLoaded => _loaded;
        public double MinX => _minX;
        public double MinY => _minY;
        public double MaxX => _maxX;
        public double MaxY => _maxY;
        public IReadOnlyDictionary<int, TreeNodeInfo> Nodes => _nodes;

        public TreeNodeInfo GetNode(int id) => _nodes.TryGetValue(id, out var n) ? n : null;

        /// <summary>
        /// 임베디드 리소스 'PoE2Overlay.Resources.PassiveTree.tree.json'을 파싱합니다.
        /// 백그라운드 스레드에서 호출해야 합니다.
        /// </summary>
        public void Load()
        {
            if (_loaded) return;

            try
            {
                var asm = Assembly.GetExecutingAssembly();
                using var stream = asm.GetManifestResourceStream("PoE2Overlay.Resources.PassiveTree.tree.json");
                if (stream == null)
                {
                    Log.Warning("Passive tree: embedded resource 'tree.json' not found");
                    return;
                }

                using var doc = JsonDocument.Parse(stream);
                var root = doc.RootElement;

                // ── constants ──────────────────────────────────────
                var skillsPerOrbit = new List<int>();
                var orbitRadii = new List<double>();
                if (root.TryGetProperty("constants", out var constants))
                {
                    if (constants.TryGetProperty("skillsPerOrbit", out var spo))
                        foreach (var v in spo.EnumerateArray()) skillsPerOrbit.Add(v.GetInt32());
                    if (constants.TryGetProperty("orbitRadii", out var orr))
                        foreach (var v in orr.EnumerateArray()) orbitRadii.Add(v.GetDouble());
                }

                // ── groups ─────────────────────────────────────────
                var groups = new Dictionary<int, (double x, double y)>();
                if (root.TryGetProperty("groups", out var grpsEl))
                {
                    foreach (var g in grpsEl.EnumerateObject())
                    {
                        if (!int.TryParse(g.Name, out int gid)) continue;
                        double gx = g.Value.TryGetProperty("x", out var xp) ? xp.GetDouble() : 0;
                        double gy = g.Value.TryGetProperty("y", out var yp) ? yp.GetDouble() : 0;
                        groups[gid] = (gx, gy);
                    }
                }

                // ── bounds ─────────────────────────────────────────
                _minX = root.TryGetProperty("min_x", out var mxp) ? mxp.GetDouble() : -7000;
                _minY = root.TryGetProperty("min_y", out var myp) ? myp.GetDouble() : -7000;
                _maxX = root.TryGetProperty("max_x", out var Mxp) ? Mxp.GetDouble() : 7000;
                _maxY = root.TryGetProperty("max_y", out var Myp) ? Myp.GetDouble() : 7000;

                // ── nodes ──────────────────────────────────────────
                var result = new Dictionary<int, TreeNodeInfo>();

                if (root.TryGetProperty("nodes", out var nodesEl))
                {
                    foreach (var n in nodesEl.EnumerateObject())
                    {
                        if (!int.TryParse(n.Name, out int id)) continue;
                        var nv = n.Value;

                        double nx, ny;

                        // Position: prefer explicit x/y, otherwise compute from group+orbit
                        if (nv.TryGetProperty("x", out var xProp) && nv.TryGetProperty("y", out var yProp))
                        {
                            nx = xProp.GetDouble();
                            ny = yProp.GetDouble();
                        }
                        else if (nv.TryGetProperty("g", out var gProp) &&
                                 nv.TryGetProperty("o", out var oProp) &&
                                 nv.TryGetProperty("oidx", out var oidxProp))
                        {
                            int gId = gProp.GetInt32();
                            int orbit = oProp.GetInt32();
                            int oidx = oidxProp.GetInt32();

                            if (groups.TryGetValue(gId, out var center) &&
                                orbit < orbitRadii.Count && orbit < skillsPerOrbit.Count)
                            {
                                int count = skillsPerOrbit[orbit];
                                double radius = orbitRadii[orbit];
                                double angle = count <= 1 ? 0 : 2 * Math.PI * oidx / count;
                                nx = center.x + radius * Math.Sin(angle);
                                ny = center.y - radius * Math.Cos(angle);
                            }
                            else
                            {
                                nx = ny = 0;
                            }
                        }
                        else
                        {
                            nx = ny = 0;
                        }

                        // Connections
                        var outs = new List<int>();
                        if (nv.TryGetProperty("out", out var outProp))
                            foreach (var c in outProp.EnumerateArray())
                                outs.Add(c.GetInt32());

                        string name = nv.TryGetProperty("name", out var nmProp) ? nmProp.GetString() ?? "" : "";
                        bool isKeystone = nv.TryGetProperty("isKeystone", out var kProp) && kProp.GetBoolean();
                        bool isNotable = nv.TryGetProperty("isNotable", out var ntProp) && ntProp.GetBoolean();
                        bool isMastery = nv.TryGetProperty("isMastery", out var mProp) && mProp.GetBoolean();
                        bool isClassStart = nv.TryGetProperty("classStartIndex", out var csProp) &&
                                           csProp.ValueKind != JsonValueKind.Null;

                        result[id] = new TreeNodeInfo
                        {
                            Id = id,
                            Name = name,
                            X = nx,
                            Y = ny,
                            IsKeystone = isKeystone,
                            IsNotable = isNotable,
                            IsMastery = isMastery,
                            IsClassStart = isClassStart,
                            Out = outs
                        };
                    }
                }

                // Update bounds from actual node positions if not in JSON
                if (!root.TryGetProperty("min_x", out _) && result.Count > 0)
                {
                    _minX = _minY = double.MaxValue;
                    _maxX = _maxY = double.MinValue;
                    foreach (var node in result.Values)
                    {
                        if (node.X < _minX) _minX = node.X;
                        if (node.Y < _minY) _minY = node.Y;
                        if (node.X > _maxX) _maxX = node.X;
                        if (node.Y > _maxY) _maxY = node.Y;
                    }
                }

                _nodes = result;
                _loaded = true;
                Log.Information("Passive tree loaded: {Count} nodes", _nodes.Count);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load passive tree JSON");
            }
        }
    }
}
