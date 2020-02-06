using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using System;

namespace Markdig.Extensions.Toc
{
    public class HeadingIdExtension : IMarkdownExtension
    {
        private static void AddGuidIdAttribute(BlockProcessor blockProcessor, Block block)
        {
            var attrs = block.GetAttributes();
            attrs.Id = attrs.Id ?? Guid.NewGuid().ToString().Substring(0, 8);
        }

        public void Setup(MarkdownPipelineBuilder pipeline)
        {
            pipeline.BlockParsers.Find<HeadingBlockParser>()
                .Closed += AddGuidIdAttribute;
        }

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {
        }
    }
}
