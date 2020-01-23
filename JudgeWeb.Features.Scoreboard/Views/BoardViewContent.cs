using Microsoft.AspNetCore.Html;
using System.IO;
using System.Text.Encodings.Web;

namespace JudgeWeb.Features.Scoreboard
{
    public class BoardViewContent : IHtmlContent
    {
        private readonly BoardViewModel _model;
        private readonly bool _usefoot, _inJury;

        public BoardViewContent(BoardViewModel model, bool useFoot, bool inJury)
        {
            _model = model;
            _usefoot = useFoot;
            _inJury = inJury;
        }

        public void WriteTo(TextWriter writer, HtmlEncoder encoder)
        {
            writer.WriteLine("<table class=\"scoreboard center\">");
            writer.WriteLine("<colgroup><col id=\"scorerank\"/><col/><col id=\"scorelogos\"/><col id=\"scoreteamname\"/></colgroup>");
            writer.WriteLine("<colgroup><col id=\"scoresolv\"/><col id=\"scoretotal\"/></colgroup>");

            writer.Write("<colgroup>");
            foreach (var item in _model.Problems)
                writer.Write("<col class=\"scoreprob\"/>");
            writer.Write("</colgroup>");

            writer.WriteLine("<thead><tr class=\"scoreheader\">");
            writer.WriteLine("<th title=\"rank\" scope=\"col\">rank</th>");
            writer.WriteLine("<th title=\"team name\" scope=\"col\" colspan=\"3\">team</th>");
            writer.WriteLine("<th title=\"# solved / penalty time\" colspan=\"2\" scope=\"col\">score</th>");

            foreach (var prob in _model.Problems)
            {
                writer.Write("<th title=\"problem ");
                writer.Write(prob.Title);
                writer.Write("\" scope=\"col\">");
                writer.Write(prob.ShortName);
                writer.Write(" <div class=\"circle\" style=\"background:");
                writer.Write(prob.Color);
                writer.WriteLine(";\"></div></th>");
            }

            writer.WriteLine("</tr></thead>");

            foreach (var sortOrder in _model)
            {
                int totalPoints = 0;
                writer.WriteLine("<tbody>");

                foreach (var team in sortOrder)
                {
                    totalPoints += team.Points;
                    TeamScoreContent.WriteTo(team, writer, encoder, _inJury);
                    if (team.Category != null)
                        _model.ShowCategory.Add((team.CategoryColor, team.Category));
                }

                if (sortOrder.Statistics != null)
                    ProblemStatisticsContent.WriteTo(sortOrder.Statistics, totalPoints, writer, encoder);
                writer.WriteLine("</tbody>");
            }

            writer.Write("</table><style>");
            foreach (var (color, name) in _model.ShowCategory)
                writer.Write($".cl_{color.Substring(1)}{{background-color:{color};}}");
            writer.WriteLine("</style>");

            if (_usefoot)
                BoardFooterContent.WriteTo(_model.ShowCategory, writer, encoder);
        }
    }
}
