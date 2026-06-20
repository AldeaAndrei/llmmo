using BalanceSim.Metrics;
using System.Text;

namespace BalanceSim.Export;

public static class ChartReportGenerator
{
    public static string Generate(
        string outputDir,
        IReadOnlyList<TimeseriesPoint> points,
        ProgressionConfig? progression = null)
    {
        var chartsDir = Path.Combine(outputDir, "charts");
        Directory.CreateDirectory(chartsDir);
        var path = Path.Combine(chartsDir, "report.html");

        progression ??= ProgressionConfig.LoadDefault();
        var days = ChartDataBuilder.DayLabels();
        var dayLabelsJson = string.Join(",", days);

        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html><head><meta charset=\"utf-8\"/>");
        sb.AppendLine("<title>Balance Sim Report</title>");
        sb.AppendLine("<script src=\"https://cdn.jsdelivr.net/npm/chart.js@4\"></script>");
        sb.AppendLine("<style>body{font-family:system-ui,sans-serif;margin:24px;background:#0f1117;color:#e8eaed;}");
        sb.AppendLine(".chart-box{background:#1a1d27;border-radius:8px;padding:16px;margin-bottom:24px;max-width:960px;}");
        sb.AppendLine("h2{margin:0 0 12px;font-size:1rem;font-weight:600;}</style>");
        sb.AppendLine("</head><body><h1>Balance Simulation Report</h1>");

        sb.AppendLine("<div class=\"chart-box\"><h2>Progression Curve (vs Target)</h2><canvas id=\"progression\"></canvas></div>");
        sb.AppendLine("<div class=\"chart-box\"><h2>Wealth Components</h2><canvas id=\"components\"></canvas></div>");
        sb.AppendLine("<div class=\"chart-box\"><h2>Resources</h2><canvas id=\"resources\"></canvas></div>");
        sb.AppendLine("<div class=\"chart-box\"><h2>Building Levels</h2><canvas id=\"buildings\"></canvas></div>");

        sb.AppendLine("<script>");
        sb.Append("const days=[").Append(dayLabelsJson).AppendLine("];");
        sb.AppendLine("""
            const chartDefaults={
              responsive:true,
              scales:{
                x:{
                  ticks:{color:'#aaa'},
                  grid:{color:'#333'},
                  title:{display:true,text:'Day',color:'#aaa'}
                },
                y:{ticks:{color:'#aaa'},grid:{color:'#333'}}
              }
            };
            function lineChart(id,datasets,yLabel){
              new Chart(document.getElementById(id),{
                type:'line',
                data:{labels:days,datasets},
                options:{
                  ...chartDefaults,
                  scales:{
                    ...chartDefaults.scales,
                    y:{...chartDefaults.scales.y,title:{display:!!yLabel,text:yLabel||'',color:'#aaa'}}
                  }
                }
              });
            }
            """);

        AppendProgressionChart(sb, points, progression);
        AppendSimpleChart(sb, "components", points, progression,
        [
            ("Sunk Capital", p => p.Wealth.SunkCapital, "#ce93d8"),
            ("Productive Capacity", p => p.Wealth.ProductiveCapacity, "#80cbc4"),
            ("Liquid Assets", p => p.Wealth.LiquidAssets, "#fff176"),
        ]);
        AppendSimpleChart(sb, "resources", points, progression,
        [
            ("Gold", p => p.Resources.Gold, "#ffd54f"),
            ("Food", p => p.Resources.Food, "#a5d6a7"),
        ]);

        AppendBuildingChart(sb, points, progression);

        sb.AppendLine("</script></body></html>");
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        return path;
    }

    private static void AppendProgressionChart(
        StringBuilder sb,
        IReadOnlyList<TimeseriesPoint> points,
        ProgressionConfig progression)
    {
        var total = Join(ChartDataBuilder.SampleWealth(points, progression, w => w.TotalPct));
        var economic = Join(ChartDataBuilder.SampleWealth(points, progression, w => w.EconomicPct));
        var military = Join(ChartDataBuilder.SampleWealth(points, progression, w => w.MilitaryPct));
        var targetTotal = Join(ChartDataBuilder.TargetWealth(progression, c => c.TotalWealthPct));
        var targetEconomic = Join(ChartDataBuilder.TargetWealth(progression, c => c.EconomicWealthPct));
        var targetMilitary = Join(ChartDataBuilder.TargetWealth(progression, c => c.MilitaryWealthPct));

        sb.Append("lineChart('progression',[");
        sb.Append("{label:'Total %',data:[").Append(total).Append("],borderColor:'#4fc3f7',tension:0.2,pointRadius:3},");
        sb.Append("{label:'Economic %',data:[").Append(economic).Append("],borderColor:'#81c784',tension:0.2,pointRadius:3},");
        sb.Append("{label:'Military %',data:[").Append(military).Append("],borderColor:'#ffb74d',tension:0.2,pointRadius:3},");
        sb.Append("{label:'Target Total %',data:[").Append(targetTotal).Append("],borderColor:'#ef5350',borderDash:[6,4],pointRadius:0,tension:0.2},");
        sb.Append("{label:'Target Economic %',data:[").Append(targetEconomic).Append("],borderColor:'#66bb6a',borderDash:[6,4],pointRadius:0,tension:0.2},");
        sb.Append("{label:'Target Military %',data:[").Append(targetMilitary).Append("],borderColor:'#ffa726',borderDash:[6,4],pointRadius:0,tension:0.2}");
        sb.AppendLine("],'Wealth %');");
    }

    private static void AppendSimpleChart(
        StringBuilder sb,
        string id,
        IReadOnlyList<TimeseriesPoint> points,
        ProgressionConfig progression,
        (string Label, Func<TimeseriesPoint, double> Selector, string Color)[] series)
    {
        sb.Append($"lineChart('{id}',[");
        for (var i = 0; i < series.Length; i++)
        {
            var (label, selector, color) = series[i];
            var data = Join(ChartDataBuilder.DayLabels().Select(day =>
            {
                var pt = points.FirstOrDefault(p => p.Day >= day) ?? points.LastOrDefault();
                return pt is null ? 0 : selector(pt);
            }));

            sb.Append("{label:'").Append(label).Append("',data:[").Append(data)
                .Append("],borderColor:'").Append(color).Append("',tension:0.2,pointRadius:3}");
            if (i < series.Length - 1) sb.Append(',');
        }

        sb.AppendLine("],null);");
    }

    private static void AppendBuildingChart(
        StringBuilder sb,
        IReadOnlyList<TimeseriesPoint> points,
        ProgressionConfig progression)
    {
        if (points.Count == 0)
        {
            sb.AppendLine("lineChart('buildings',[],null);");
            return;
        }

        var types = points[^1].BuildingLevels.Keys.OrderBy(k => k).ToList();
        sb.Append("lineChart('buildings',[");
        for (var i = 0; i < types.Count; i++)
        {
            var type = types[i];
            var data = Join(ChartDataBuilder.SampleInt(points, progression, p => p.BuildingLevels.GetValueOrDefault(type, 0)));
            sb.Append("{label:'").Append(type).Append("',data:[").Append(data)
                .Append("],borderColor:'hsl(").Append(i * 45).Append(",70%,60%)',tension:0.2,pointRadius:3}");
            if (i < types.Count - 1) sb.Append(',');
        }

        sb.AppendLine("],'Level');");
    }

    private static string Join(IEnumerable<double> values) =>
        string.Join(",", values.Select(v => v.ToString("F1")));

    private static string Join(IEnumerable<int> values) =>
        string.Join(",", values);
}
