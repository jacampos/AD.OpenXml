﻿using System.IO;
using System.Linq;
using AD.IO;
using AD.OpenXml.Documents;
using AD.OpenXml.Visitors;
using JetBrains.Annotations;

namespace AD.OpenXml.Tests
{
    [UsedImplicitly]
    public static class Program
    {
        [UsedImplicitly]
        public static void Main()
        {
            // Declare working directory
            const string workingDirectory = "z:\\records\\operations\\economics\\sec 332\\active cases\\otap 2016\\draft report\\content review";

            // Declare version
            const string version = "7_4";

            ProcessChapter(version,"C:\\Users\\adren\\Desktop\\OTAP 2016\\ch2");
            System.Environment.Exit(0);

            // Process chapters
            ProcessChapter(version, $"{workingDirectory}\\ch0");
            ProcessChapter(version, $"{workingDirectory}\\ch1");
            ProcessChapter(version, $"{workingDirectory}\\ch2");
            ProcessChapter(version, $"{workingDirectory}\\ch3");
            ProcessChapter(version, $"{workingDirectory}\\ch4");
            ProcessChapter(version, $"{workingDirectory}\\ch5");
            ProcessChapter(version, $"{workingDirectory}\\ch6");
            ProcessChapter(version, $"{workingDirectory}\\ch7");
            
            // Copy new files into report folder
            foreach (string chapter in new string[] { "ch0", "ch1", "ch2", "ch3", "ch4", "ch5", "ch6", "ch7" })
            {
                foreach (string file in Directory.GetFiles($"{workingDirectory}\\{chapter}", "*.docx", SearchOption.TopDirectoryOnly).Where(x => !x.Contains("~")))
                {
                    File.Copy(file, $"{workingDirectory}\\_Report\\{Path.GetFileName(file)}", true);
                }
            }

            // Process report
            ProcessChapter(version, $"{workingDirectory}\\_Report");

            // Delete old files in report folder
            foreach (string section in Directory.GetFiles($"{workingDirectory}\\_Report", "*.docx", SearchOption.TopDirectoryOnly))
            {
                File.Delete(section);
            }
        }

        private static void ProcessChapter(string version, string workingDirectory)
        {
            // Create output directory
            Directory.CreateDirectory($"{workingDirectory}\\output");

            // Create result file
            DocxFilePath result = DocxFilePath.Create($"{workingDirectory}\\output\\OTAP_2016_v_{version}.docx", true);

            // Add footnotes file
            result.AddFootnotes();

            DocxFilePath[] files =
                Directory.GetFiles(workingDirectory, "*.docx", SearchOption.TopDirectoryOnly)
                         .Where(
                             x => !x.Contains('~'))
                         .OrderBy(
                             x =>
                                 Path.GetFileNameWithoutExtension(x)
                                     .TakeWhile(y => char.IsNumber(y) || char.IsPunctuation(y))
                                     .Aggregate(default(string), (current, next) => current + next)
                                     .ParseDouble())
                         .Select(
                             x => (DocxFilePath) x)
                         .ToArray();

            // Create container to encapsulate volatile operations
            OpenXmlVisitor container = new OpenXmlVisitor(result);

            // Merge files into container
            OpenXmlVisitor mergedContainer = container.Visit(files);

            // Save container to result path
            mergedContainer.Save(result);

            // Create custom styles
            result.AddStyles();

            // Add headers
            result.AddHeaders("The Year in Trade 2016");

            // Add footers
            result.AddFooters();

            // Set all chart objects inline
            result.PositionChartsInline();

            // Set the inner positions of chart objects
            result.PositionChartsInner();

            // Set the outer positions of chart objects
            result.PositionChartsOuter();

            // Set the style of bar chart objects
            result.ModifyBarChartStyles();

            // Set the style of line chart objects
            result.ModifyLineChartStyles();
            
            // Set the style of area chart objects
            result.ModifyAreaChartStyles();

            // Remove duplicate section properties
            result.RemoveDuplicateSectionProperties();

            // Write document.xml to XML file
            XmlFilePath xml = XmlFilePath.Create($"{workingDirectory}\\output\\OTAP_2016_v_{version}.xml");
            result.ReadAsXml().Elements().WriteXml(xml);

            // Write document.xml to HTML file
            HtmlFilePath html = HtmlFilePath.Create($"{workingDirectory}\\output\\OTAP_2016_v_{version}.html");
            result.ReadAsXml().ProcessHtml().WriteHtml(html);
        }
    }
}