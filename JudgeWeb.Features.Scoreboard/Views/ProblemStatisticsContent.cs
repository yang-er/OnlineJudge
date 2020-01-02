using Microsoft.AspNetCore.Html;
using System.IO;
using System.Text.Encodings.Web;

namespace JudgeWeb.Features.Scoreboard
{
    public class ProblemStatisticsContent : IHtmlContent
    {
        private readonly ProblemStatisticsModel[] _model;
        private readonly int _totalPoint;

        public ProblemStatisticsContent(ProblemStatisticsModel[] model, int tot)
        {
            _model = model;
            _totalPoint = tot;
        }

        public void WriteTo(TextWriter writer, HtmlEncoder encoder) =>
            WriteTo(_model, _totalPoint, writer, encoder);

        public static void WriteTo(ProblemStatisticsModel[] model, int tot, TextWriter writer, HtmlEncoder encoder)
        {
            writer.Write("<tr style=\"border-top: 2px solid black;\">");
            writer.Write("<td id=\"scoresummary\" title=\"Summary\" colspan=\"4\">Summary</td>");
            writer.Write("<td title=\"total solved\" class=\"scorenc\">");
            writer.Write(tot);
            writer.Write("</td><td></td>");

            foreach (var item in model)
            {
                writer.Write("<td style=\"text-align: left;\"><a>");
                writer.Write("<i class=\"fas fa-thumbs-up fa-fw\"></i>");
                writer.Write("<span style=\"font-size:90%;\" title=\"number of accepted submissions\">");
                writer.Write(item.Accepted);
                writer.Write("</span><br/>");
                writer.Write("<i class=\"fas fa-thumbs-down fa-fw\"></i>");
                writer.Write("<span style=\"font-size:90%;\" title=\"number of rejected submissions\">");
                writer.Write(item.Rejected);
                writer.Write("</span><br/>");
                writer.Write("<i class=\"fas fa-question-circle fa-fw\"></i>");
                writer.Write("<span style=\"font-size:90%;\" title=\"number of pending submissions\">");
                writer.Write(item.Rejected);
                writer.Write("</span><br/>");
                writer.Write("<i class=\"fas fa-clock fa-fw\"></i>");
                writer.Write("<span style=\"font-size:90%;\" title=\"first solved\">");
                if (item.FirstSolve.HasValue)
                    writer.Write(item.FirstSolve.Value + "min");
                else
                    writer.Write("n/a");
                writer.Write("</span></a></td>");
            }

            writer.WriteLine("</tr>");
        }
    }
}
