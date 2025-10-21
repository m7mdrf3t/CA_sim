using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Comprehensive reporting system for WireDistributor and HyparHangers.
/// Generates detailed reports about point placement, wire lengths, statistics, and exports data.
/// </summary>
[ExecuteAlways]
public class WireSystemReporter : MonoBehaviour
{
    #region Configuration

    [Header("System References")]
    [SerializeField, Tooltip("WireDistributor component to analyze")]
    private distributer distributor;
    
    [SerializeField, Tooltip("HyparHangers component to analyze")]
    private HyparHangers hangers;

    [Header("Report Options")]
    [SerializeField] private bool includePointCoordinates = true;
    [SerializeField] private bool includeWireLengths = true;
    [SerializeField] private bool includeStatistics = true;
    [SerializeField] private bool includeDistanceAnalysis = true;
    [SerializeField] private bool includeGroupAnalysis = true;
    [SerializeField] private bool sortByWireLength = false;

    [Header("Export Options")]
    [SerializeField] private bool exportToCSV = false;
    [SerializeField] private bool exportToJSON = false;
    [SerializeField] private string exportFileName = "WireSystemReport";

    [Header("Visualization")]
    [SerializeField] private bool showGizmoConnections = true;
    [SerializeField] private bool showDistanceLabels = false;
    [SerializeField] private Color connectionColor = new Color(1f, 0.5f, 0f, 0.5f);

    #endregion

    #region Data Structures

    [Serializable]
    public class WireReport
    {
        public string wireLabel;
        public string gridPoint;
        public Vector3 anchorPosition;
        public Vector3 endPosition;
        public float wireLength;
        public float actualDistance;
        public string group;
        public int columnIndex;
        public int rowIndex;
    }

    [Serializable]
    public class SystemStatistics
    {
        public int totalPoints;
        public int totalWires;
        public float minWireLength;
        public float maxWireLength;
        public float avgWireLength;
        public float totalWireLength;
        public float minPointDistance;
        public float maxPointDistance;
        public float avgPointDistance;
        public Vector3 systemCenter;
        public Vector3 systemSize;
        public float distributionArea;
        public Dictionary<string, int> groupCounts;
        public Dictionary<string, float> groupTotalLengths;
    }

    [Serializable]
    public class FullReport
    {
        public string generatedDate;
        public SystemStatistics statistics;
        public List<WireReport> wires;
        public DistributorInfo distributorInfo;
        public HangersInfo hangersInfo;
    }

    [Serializable]
    public class DistributorInfo
    {
        public float plateWidth;
        public float plateHeight;
        public float marginX;
        public float marginZ;
        public int gridColumns;
        public int gridRows;
        public float spacingX;
        public float spacingZ;
        public string snappingMode;
        public string curveMode;
    }

    [Serializable]
    public class HangersInfo
    {
        public string mappingMode;
        public float lengthConversion;
        public Vector3 hangDirection;
        public int totalMappings;
    }

    #endregion

    #region Report Generation

    /// <summary>
    /// Generates and logs a comprehensive report to the console.
    /// </summary>
    [ContextMenu("Generate Full Report")]
    public void GenerateFullReport()
    {
        if (!ValidateReferences()) return;

        var report = CollectReportData();
        string formattedReport = FormatConsoleReport(report);
        
        Debug.Log(formattedReport);

        if (exportToCSV) ExportToCSV(report);
        if (exportToJSON) ExportToJSON(report);
    }

    /// <summary>
    /// Generates a quick statistics summary.
    /// </summary>
    [ContextMenu("Quick Statistics")]
    public void GenerateQuickStats()
    {
        if (!ValidateReferences()) return;

        var stats = CalculateStatistics();
        string report = FormatQuickStats(stats);
        
        Debug.Log(report);
    }

    /// <summary>
    /// Generates a wire length distribution report.
    /// </summary>
    [ContextMenu("Wire Length Distribution")]
    public void GenerateWireLengthDistribution()
    {
        if (!ValidateReferences()) return;

        var wires = CollectWireData();
        string report = FormatLengthDistribution(wires);
        
        Debug.Log(report);
    }

    /// <summary>
    /// Generates a spatial distribution analysis.
    /// </summary>
    [ContextMenu("Spatial Distribution Analysis")]
    public void GenerateSpatialAnalysis()
    {
        if (!ValidateReferences()) return;

        var wires = CollectWireData();
        string report = FormatSpatialAnalysis(wires);
        
        Debug.Log(report);
    }

    #endregion

    #region Data Collection

    private bool ValidateReferences()
    {
        if (distributor == null)
        {
            Debug.LogError("[WireSystemReporter] Missing WireDistributor reference!");
            return false;
        }

        if (hangers == null)
        {
            Debug.LogError("[WireSystemReporter] Missing HyparHangers reference!");
            return false;
        }

        return true;
    }

    private FullReport CollectReportData()
    {
        var report = new FullReport
        {
            generatedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            wires = CollectWireData(),
            distributorInfo = CollectDistributorInfo(),
            hangersInfo = CollectHangersInfo()
        };

        report.statistics = CalculateStatistics(report.wires);

        return report;
    }

    private List<WireReport> CollectWireData()
    {
        var wireReports = new List<WireReport>();
        Transform hangerRoot = hangers.transform;

        foreach (Transform hanger in hangerRoot)
        {
            if (hanger == null) continue;

            var info = hanger.GetComponent<HangerInfo>();
            if (info == null) continue;

            var lineRenderer = hanger.GetComponent<LineRenderer>();
            if (lineRenderer == null || lineRenderer.positionCount < 2) continue;

            Vector3 start = lineRenderer.GetPosition(0);
            Vector3 end = lineRenderer.GetPosition(1);

            var parsed = ParseGridPoint(info.gridPointName);

            wireReports.Add(new WireReport
            {
                wireLabel = info.wireLabel,
                gridPoint = info.gridPointName,
                anchorPosition = start,
                endPosition = end,
                wireLength = info.wireLength,
                actualDistance = Vector3.Distance(start, end),
                group = GetWireGroup(info.wireLabel),
                columnIndex = parsed.col,
                rowIndex = parsed.row
            });
        }

        if (sortByWireLength)
            wireReports.Sort((a, b) => a.wireLength.CompareTo(b.wireLength));
        else
            wireReports.Sort((a, b) => string.Compare(a.wireLabel, b.wireLabel, StringComparison.OrdinalIgnoreCase));

        return wireReports;
    }

    private SystemStatistics CalculateStatistics(List<WireReport> wires = null)
    {
        if (wires == null) wires = CollectWireData();

        var stats = new SystemStatistics
        {
            totalWires = wires.Count,
            totalPoints = distributor.GetSpawnedCount(),
            groupCounts = new Dictionary<string, int>(),
            groupTotalLengths = new Dictionary<string, float>()
        };

        if (wires.Count == 0) return stats;

        // Wire length statistics
        stats.minWireLength = wires.Min(w => w.wireLength);
        stats.maxWireLength = wires.Max(w => w.wireLength);
        stats.avgWireLength = wires.Average(w => w.wireLength);
        stats.totalWireLength = wires.Sum(w => w.wireLength);

        // Spatial statistics
        Vector3 minPos = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        Vector3 maxPos = new Vector3(float.MinValue, float.MinValue, float.MinValue);

        foreach (var wire in wires)
        {
            minPos = Vector3.Min(minPos, wire.anchorPosition);
            maxPos = Vector3.Max(maxPos, wire.anchorPosition);

            // Group statistics
            if (!string.IsNullOrEmpty(wire.group))
            {
                if (!stats.groupCounts.ContainsKey(wire.group))
                {
                    stats.groupCounts[wire.group] = 0;
                    stats.groupTotalLengths[wire.group] = 0f;
                }
                stats.groupCounts[wire.group]++;
                stats.groupTotalLengths[wire.group] += wire.wireLength;
            }
        }

        stats.systemCenter = (minPos + maxPos) * 0.5f;
        stats.systemSize = maxPos - minPos;
        stats.distributionArea = stats.systemSize.x * stats.systemSize.z;

        // Point distance analysis
        var positions = wires.Select(w => w.anchorPosition).ToList();
        var distances = new List<float>();
        
        for (int i = 0; i < positions.Count; i++)
        {
            for (int j = i + 1; j < positions.Count; j++)
            {
                float dist = Vector3.Distance(positions[i], positions[j]);
                distances.Add(dist);
            }
        }

        if (distances.Count > 0)
        {
            stats.minPointDistance = distances.Min();
            stats.maxPointDistance = distances.Max();
            stats.avgPointDistance = distances.Average();
        }

        return stats;
    }

    private DistributorInfo CollectDistributorInfo()
    {
        // Use reflection to get private fields
        var type = typeof(distributer);
        
        return new DistributorInfo
        {
            plateWidth = GetFieldValue<float>(distributor, "plateWidthX"),
            plateHeight = GetFieldValue<float>(distributor, "plateHeightZ"),
            marginX = GetFieldValue<float>(distributor, "marginX"),
            marginZ = GetFieldValue<float>(distributor, "marginZ"),
            spacingX = 0f, // Will be calculated from grid
            spacingZ = 0f,
            snappingMode = GetFieldValue<object>(distributor, "snappingMode")?.ToString() ?? "Unknown",
            curveMode = GetFieldValue<object>(distributor, "topCurveMode")?.ToString() ?? "Unknown"
        };
    }

    private HangersInfo CollectHangersInfo()
    {
        return new HangersInfo
        {
            mappingMode = GetFieldValue<object>(hangers, "mappingMode")?.ToString() ?? "Unknown",
            lengthConversion = GetFieldValue<float>(hangers, "lengthToMeters"),
            hangDirection = GetFieldValue<Vector3>(hangers, "hangDirection"),
            totalMappings = 0
        };
    }

    private T GetFieldValue<T>(object obj, string fieldName)
    {
        var field = obj.GetType().GetField(fieldName, 
            System.Reflection.BindingFlags.NonPublic | 
            System.Reflection.BindingFlags.Public | 
            System.Reflection.BindingFlags.Instance);
        
        if (field != null)
        {
            var value = field.GetValue(obj);
            if (value is T typedValue)
                return typedValue;
        }
        
        return default(T);
    }

    private string GetWireGroup(string wireLabel)
    {
        // Extract letter prefix as group (A1 → A, AA5 → AA)
        int i = 0;
        while (i < wireLabel.Length && char.IsLetter(wireLabel[i])) i++;
        return i > 0 ? wireLabel.Substring(0, i) : "Ungrouped";
    }

    private (int col, int row) ParseGridPoint(string gridPoint)
    {
        var match = System.Text.RegularExpressions.Regex.Match(gridPoint, @"C(\d+)-R(\d+)");
        if (match.Success)
        {
            return (int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value));
        }
        return (0, 0);
    }

    #endregion

    #region Report Formatting

    private string FormatConsoleReport(FullReport report)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("╔═══════════════════════════════════════════════════════════════════╗");
        sb.AppendLine("║           WIRE SYSTEM COMPREHENSIVE REPORT                        ║");
        sb.AppendLine("╚═══════════════════════════════════════════════════════════════════╝");
        sb.AppendLine();
        sb.AppendLine($"Generated: {report.generatedDate}");
        sb.AppendLine($"Unity Version: {Application.unityVersion}");
        sb.AppendLine($"Scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
        sb.AppendLine();

        // System Overview
        sb.AppendLine("═══ SYSTEM OVERVIEW ═══");
        sb.AppendLine($"Total Anchor Points: {report.statistics.totalPoints}");
        sb.AppendLine($"Total Wire Hangers: {report.statistics.totalWires}");
        sb.AppendLine($"Coverage Efficiency: {(report.statistics.totalWires / (float)report.statistics.totalPoints * 100):F1}%");
        sb.AppendLine();

        // Distributor Configuration
        if (includeStatistics)
        {
            sb.AppendLine("═══ DISTRIBUTOR CONFIGURATION ═══");
            sb.AppendLine($"Plate Dimensions: {report.distributorInfo.plateWidth:F3}m × {report.distributorInfo.plateHeight:F3}m");
            sb.AppendLine($"Margins: X={report.distributorInfo.marginX:F3}m, Z={report.distributorInfo.marginZ:F3}m");
            sb.AppendLine($"Snapping Mode: {report.distributorInfo.snappingMode}");
            sb.AppendLine($"Top Curve Mode: {report.distributorInfo.curveMode}");
            sb.AppendLine();
        }

        // Hangers Configuration
        if (includeStatistics)
        {
            sb.AppendLine("═══ HANGERS CONFIGURATION ═══");
            sb.AppendLine($"Mapping Mode: {report.hangersInfo.mappingMode}");
            sb.AppendLine($"Length Conversion: {report.hangersInfo.lengthConversion}× to meters");
            sb.AppendLine($"Hang Direction: {report.hangersInfo.hangDirection}");
            sb.AppendLine();
        }

        // Wire Length Statistics
        if (includeWireLengths)
        {
            sb.AppendLine("═══ WIRE LENGTH STATISTICS ═══");
            sb.AppendLine($"Total Wire Length: {report.statistics.totalWireLength:F3}m ({report.statistics.totalWireLength * 100:F1}cm)");
            sb.AppendLine($"Minimum Length: {report.statistics.minWireLength:F3}m");
            sb.AppendLine($"Maximum Length: {report.statistics.maxWireLength:F3}m");
            sb.AppendLine($"Average Length: {report.statistics.avgWireLength:F3}m");
            sb.AppendLine($"Length Range: {(report.statistics.maxWireLength - report.statistics.minWireLength):F3}m");
            sb.AppendLine();
        }

        // Spatial Statistics
        if (includeDistanceAnalysis)
        {
            sb.AppendLine("═══ SPATIAL DISTRIBUTION ═══");
            sb.AppendLine($"System Center: {report.statistics.systemCenter}");
            sb.AppendLine($"System Size: {report.statistics.systemSize.x:F3}m × {report.statistics.systemSize.z:F3}m");
            sb.AppendLine($"Distribution Area: {report.statistics.distributionArea:F4}m²");
            sb.AppendLine($"Point Density: {report.statistics.totalPoints / report.statistics.distributionArea:F2} points/m²");
            
            if (report.statistics.avgPointDistance > 0)
            {
                sb.AppendLine($"Min Point Distance: {report.statistics.minPointDistance:F4}m");
                sb.AppendLine($"Max Point Distance: {report.statistics.maxPointDistance:F4}m");
                sb.AppendLine($"Avg Point Distance: {report.statistics.avgPointDistance:F4}m");
            }
            sb.AppendLine();
        }

        // Group Analysis
        if (includeGroupAnalysis && report.statistics.groupCounts.Count > 0)
        {
            sb.AppendLine("═══ GROUP ANALYSIS ═══");
            sb.AppendLine($"{"Group",-10} {"Count",8} {"Total Length",15} {"Avg Length",12}");
            sb.AppendLine(new string('─', 50));
            
            foreach (var group in report.statistics.groupCounts.Keys.OrderBy(k => k))
            {
                int count = report.statistics.groupCounts[group];
                float total = report.statistics.groupTotalLengths[group];
                float avg = total / count;
                sb.AppendLine($"{group,-10} {count,8} {total,12:F3}m {avg,12:F3}m");
            }
            sb.AppendLine();
        }

        // Individual Wire Details
        if (includePointCoordinates)
        {
            sb.AppendLine("═══ INDIVIDUAL WIRE DETAILS ═══");
            sb.AppendLine($"{"Wire",-8} {"Grid",-10} {"Length",10} {"Position",35} {"End Position",35}");
            sb.AppendLine(new string('─', 110));
            
            foreach (var wire in report.wires)
            {
                sb.AppendLine($"{wire.wireLabel,-8} {wire.gridPoint,-10} {wire.wireLength,8:F3}m " +
                             $"{FormatVector3(wire.anchorPosition),35} {FormatVector3(wire.endPosition),35}");
            }
            sb.AppendLine();
        }

        // Material Requirements
        sb.AppendLine("═══ MATERIAL REQUIREMENTS ═══");
        sb.AppendLine($"Total Wire Needed: {report.statistics.totalWireLength:F3}m");
        sb.AppendLine($"With 10% Waste Factor: {report.statistics.totalWireLength * 1.1f:F3}m");
        sb.AppendLine($"With 20% Waste Factor: {report.statistics.totalWireLength * 1.2f:F3}m");
        sb.AppendLine($"End Weights Required: {report.statistics.totalWires}");
        sb.AppendLine();

        sb.AppendLine("╚═══════════════════════════════════════════════════════════════════╝");

        return sb.ToString();
    }

    private string FormatQuickStats(SystemStatistics stats)
    {
        return $"<b><color=cyan>═══ Quick Statistics ═══</color></b>\n" +
               $"Points: {stats.totalPoints} | Wires: {stats.totalWires}\n" +
               $"Total Length: {stats.totalWireLength:F2}m | Avg: {stats.avgWireLength:F2}m\n" +
               $"Range: {stats.minWireLength:F2}m - {stats.maxWireLength:F2}m\n" +
               $"Area: {stats.distributionArea:F2}m² | Density: {stats.totalPoints / stats.distributionArea:F2} pts/m²";
    }

    private string FormatLengthDistribution(List<WireReport> wires)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<b><color=cyan>═══ Wire Length Distribution ═══</color></b>\n");

        // Create histogram
        float min = wires.Min(w => w.wireLength);
        float max = wires.Max(w => w.wireLength);
        int bins = 10;
        float binSize = (max - min) / bins;

        var histogram = new int[bins];
        foreach (var wire in wires)
        {
            int binIndex = Mathf.Min((int)((wire.wireLength - min) / binSize), bins - 1);
            histogram[binIndex]++;
        }

        for (int i = 0; i < bins; i++)
        {
            float rangeStart = min + i * binSize;
            float rangeEnd = rangeStart + binSize;
            string bar = new string('█', (int)(histogram[i] / (float)wires.Count * 40));
            sb.AppendLine($"{rangeStart:F2}m - {rangeEnd:F2}m: {bar} ({histogram[i]})");
        }

        return sb.ToString();
    }

    private string FormatSpatialAnalysis(List<WireReport> wires)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<b><color=cyan>═══ Spatial Distribution Analysis ═══</color></b>\n");

        // Analyze by rows
        var rowGroups = wires.GroupBy(w => w.rowIndex).OrderBy(g => g.Key);
        sb.AppendLine("Distribution by Row:");
        foreach (var row in rowGroups)
        {
            float avgLength = row.Average(w => w.wireLength);
            sb.AppendLine($"  Row {row.Key}: {row.Count()} wires, Avg Length: {avgLength:F3}m");
        }
        sb.AppendLine();

        // Analyze by columns
        var colGroups = wires.GroupBy(w => w.columnIndex).OrderBy(g => g.Key);
        sb.AppendLine("Distribution by Column:");
        foreach (var col in colGroups)
        {
            float avgLength = col.Average(w => w.wireLength);
            sb.AppendLine($"  Column {col.Key}: {col.Count()} wires, Avg Length: {avgLength:F3}m");
        }

        return sb.ToString();
    }

    private string FormatVector3(Vector3 v)
    {
        return $"({v.x:F3}, {v.y:F3}, {v.z:F3})";
    }

    #endregion

    #region Export Functions

    private void ExportToCSV(FullReport report)
    {
        var sb = new StringBuilder();
        
        // Header
        sb.AppendLine("Wire Label,Grid Point,Column,Row,Wire Length (m),Anchor X,Anchor Y,Anchor Z,End X,End Y,End Z,Group");
        
        // Data rows
        foreach (var wire in report.wires)
        {
            sb.AppendLine($"{wire.wireLabel},{wire.gridPoint},{wire.columnIndex},{wire.rowIndex}," +
                         $"{wire.wireLength:F4},{wire.anchorPosition.x:F4},{wire.anchorPosition.y:F4},{wire.anchorPosition.z:F4}," +
                         $"{wire.endPosition.x:F4},{wire.endPosition.y:F4},{wire.endPosition.z:F4},{wire.group}");
        }

        string path = $"{Application.dataPath}/../{exportFileName}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
        System.IO.File.WriteAllText(path, sb.ToString());
        
        Debug.Log($"[WireSystemReporter] CSV exported to: {path}");
        
        #if UNITY_EDITOR
        EditorUtility.RevealInFinder(path);
        #endif
    }

    private void ExportToJSON(FullReport report)
    {
        string json = JsonUtility.ToJson(report, prettyPrint: true);
        string path = $"{Application.dataPath}/../{exportFileName}_{DateTime.Now:yyyyMMdd_HHmmss}.json";
        System.IO.File.WriteAllText(path, json);
        
        Debug.Log($"[WireSystemReporter] JSON exported to: {path}");
        
        #if UNITY_EDITOR
        EditorUtility.RevealInFinder(path);
        #endif
    }

    #endregion

    #region Gizmos

    private void OnDrawGizmosSelected()
    {
        if (!showGizmoConnections || hangers == null) return;

        var wires = CollectWireData();
        
        Gizmos.color = connectionColor;
        
        foreach (var wire in wires)
        {
            Gizmos.DrawLine(wire.anchorPosition, wire.endPosition);
            
            #if UNITY_EDITOR
            if (showDistanceLabels)
            {
                Vector3 midPoint = (wire.anchorPosition + wire.endPosition) * 0.5f;
                Handles.Label(midPoint, $"{wire.wireLength:F2}m");
            }
            #endif
        }
    }

    #endregion
}