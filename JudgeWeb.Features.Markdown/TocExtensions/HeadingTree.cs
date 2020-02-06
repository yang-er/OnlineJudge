using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Markdig.Extensions.Toc
{
    public class HeadingNode
    {
        public int Level { get; set; }

        public string Id { get; set; }

        public string Title { get; set; }

        public List<HeadingNode> Children { get; }

        public HeadingNode()
        {
            Children = new List<HeadingNode>();
        }

        public void Insert(HeadingNode leaf)
        {
            if (Children.Count == 0 || Children.Last().Level >= leaf.Level)
            {
                Children.Add(leaf);
            }
            else
            {
                Children.Last().Insert(leaf);
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            if (Level == 0)
            {
                sb.AppendLine("<ul class=\"section-nav\">");
                foreach (var child in Children)
                    sb.Append(child.ToString());
                sb.AppendLine("</ul>");
            }
            else
            {
                if (Title is null) throw new InvalidOperationException();
                sb.Append($"<li class=\"toc-entry toc-h{Level}\"><a href=\"#{Id}\">{Title}</a>");

                if (Children.Count > 0)
                {
                    sb.AppendLine("<ul>");
                    foreach (var child in Children)
                        sb.Append(child.ToString());
                    sb.AppendLine("</ul>");
                }

                sb.AppendLine("</li>");
            }

            return sb.ToString();
        }
    }
}
