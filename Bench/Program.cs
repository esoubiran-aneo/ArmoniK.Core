// This file is part of the ArmoniK project
//
// Copyright (C) ANEO, 2021-$CURRENT_YEAR$. All rights reserved.
//   W. Kirschenmann   <wkirschenmann@aneo.fr>
//   J. Gurhem         <jgurhem@aneo.fr>
//   D. Dubuc          <ddubuc@aneo.fr>
//   L. Ziane Khodja   <lzianekhodja@aneo.fr>
//   F. Lemaitre       <flemaitre@aneo.fr>
//   S. Djebbar        <sdjebbar@aneo.fr>
//   J. Fonseca        <jfonseca@aneo.fr>
//   D. Brasseur       <dbrasseur@aneo.fr>
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY, without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
//
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Running;

using Microsoft.CodeAnalysis.CSharp.Syntax;
using LinqKit;

namespace Bench;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var summary = BenchmarkRunner.Run<Bench>(new Config());
        Console.WriteLine("finished");
        Thread.Sleep(3600 * 1000);
    }
}
public class Config : ManualConfig
{
  public IEnumerable<IExporter> GetMyExporters()
  {
    yield return MarkdownExporter.Default; // produces <BenchmarkName>-report-default.md
    yield return MarkdownExporter.GitHub; // produces <BenchmarkName>-report-github.md
                                          //yield return MarkdownExporter.StackOverflow; // produces <BenchmarkName>-report-stackoverflow.md
    yield return CsvExporter.Default; // produces <BenchmarkName>-report.csv
    yield return CsvMeasurementsExporter.Default; // produces <BenchmarkName>-measurements.csv
    yield return HtmlExporter.Default; // produces <BenchmarkName>-report.html
    yield return PlainExporter.Default; // produces <BenchmarkName>-report.txt
    yield return BenchmarkReportExporter.Default;
    yield return RPlotExporter.Default;
  }

  public Config()
  {
    GetMyExporters().ForEach(x => { AddExporter(x); });
    AddLogger(DefaultConfig.Instance.GetLoggers().ToArray());
    AddColumnProvider(DefaultConfig.Instance.GetColumnProviders().ToArray());
    //ArtifactsPath = "BenchmarkDotNet.Artifacts";
  }
}
