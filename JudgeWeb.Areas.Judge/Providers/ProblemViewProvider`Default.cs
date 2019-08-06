﻿using JudgeWeb.Data;
using JudgeWeb.Features;
using JudgeWeb.Features.Problem;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JudgeWeb.Areas.Judge.Providers
{
    internal class DefaultProblemViewProvider : IProblemViewProvider
    {
        private IMarkdownService Markdown { get; }

        public DefaultProblemViewProvider(IMarkdownService markdownService)
        {
            Markdown = markdownService;
        }

        public StringBuilder Build(string description,
            string inputdesc, string outputdesc, string hint,
            Problem model, List<TestCase> samples)
        {
            var htmlBuilder = new StringBuilder();
            htmlBuilder.AppendLine($"<h1>{model.Title}</h1>");

            htmlBuilder.AppendLine("<ul class=\"list-unstyled\">");
            htmlBuilder.AppendLine("  <li>Input file: <em>standard input</em></li>");
            htmlBuilder.AppendLine("  <li>Output file: <em>standard output</em></li>");
            htmlBuilder.AppendLine($"  <li>Time limit: {model.TimeLimit}ms</li>");
            htmlBuilder.AppendLine($"  <li>Memory limit: {model.MemoryLimit}k</li>");
            //htmlBuilder.AppendLine("  <li>Submissions: {SubmissionDetails}</li>");
            htmlBuilder.AppendLine("</ul>");
            htmlBuilder.AppendLine();

            htmlBuilder.AppendLine("<div id=\"problem-descibe\">");

            if (!string.IsNullOrEmpty(description))
            {
                htmlBuilder.AppendLine("<h3>Description</h3>");
                htmlBuilder.AppendLine(Markdown.Render(description));
            }

            if (!string.IsNullOrEmpty(inputdesc))
            {
                htmlBuilder.AppendLine("<h3>Input</h3>");
                htmlBuilder.AppendLine(Markdown.Render(inputdesc));
            }

            if (!string.IsNullOrEmpty(outputdesc))
            {
                htmlBuilder.AppendLine("<h3>Output</h3>");
                htmlBuilder.AppendLine(Markdown.Render(outputdesc));
            }

            if (samples.Count > 0)
            {
                htmlBuilder.AppendLine("<h3>Sample</h3>");

                foreach (var item in samples)
                {
                    htmlBuilder.AppendLine("<div class=\"samp\">");

                    if (!string.IsNullOrEmpty(item.Input))
                    {
                        htmlBuilder.AppendLine("<div class=\"input\">");
                        htmlBuilder.AppendLine("<div class=\"title\">Input</div>");
                        htmlBuilder.AppendLine($"<pre>{item.Input}</pre>");
                        htmlBuilder.AppendLine("</div>");
                    }

                    htmlBuilder.AppendLine("<div class=\"output\">");
                    htmlBuilder.AppendLine("<div class=\"title\">Output</div>");
                    htmlBuilder.AppendLine($"<pre>{item.Output}</pre>");
                    htmlBuilder.AppendLine("</div>");
                    htmlBuilder.AppendLine("</div>");
                    htmlBuilder.AppendLine();
                }
            }

            if (!string.IsNullOrEmpty(hint))
            {
                htmlBuilder.AppendLine("<h3>Hint</h3>");
                htmlBuilder.AppendLine(Markdown.Render(hint));
                htmlBuilder.AppendLine();
            }

            if (!string.IsNullOrEmpty(model.Source))
            {
                htmlBuilder.AppendLine("<h3>Source</h3>");
                htmlBuilder.AppendLine($"<p>{model.Source}</p>");
            }

            htmlBuilder.AppendLine("</div>");
            return htmlBuilder;
        }
    }
}
