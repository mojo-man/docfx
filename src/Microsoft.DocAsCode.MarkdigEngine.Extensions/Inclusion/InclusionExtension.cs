// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using Markdig;
    using Markdig.Parsers;
    using Markdig.Parsers.Inlines;
    using Markdig.Renderers;
    using Markdig.Syntax;
    using Markdig.Syntax.Inlines;
    using Microsoft.DocAsCode.Common;

    /// <summary>
    /// Extension to enable extension IncludeFile.
    /// </summary>
    public class InclusionExtension : IMarkdownExtension
    {
        private MarkdownContext _context;

        public InclusionExtension(MarkdownContext context)
        {
            _context = context;
        }

        public void Setup(MarkdownPipelineBuilder pipeline)
        {
            pipeline.BlockParsers.AddIfNotAlready<InclusionBlockParser>();
            pipeline.InlineParsers.InsertBefore<LinkInlineParser>(new InclusionInlineParser());
        }

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {
            if (renderer is HtmlRenderer htmlRenderer)
            {
                if (!htmlRenderer.ObjectRenderers.Contains<HtmlInclusionInlineRenderer>())
                {
                    htmlRenderer.ObjectRenderers.Insert(0, new HtmlInclusionInlineRenderer(_context, pipeline));
                }

                if (!htmlRenderer.ObjectRenderers.Contains<HtmlInclusionBlockRenderer>())
                {
                    htmlRenderer.ObjectRenderers.Insert(0, new HtmlInclusionBlockRenderer(_context, pipeline));
                }
            }
        }

        public static ProcessDocumentDelegate GetProcessDocumentDelegate(MarkdownContext context)
        {
            return document => UpdateLinks(document, context);
        }

        private static void UpdateLinks(MarkdownObject markdownObject, MarkdownContext context)
        {
            if (markdownObject == null || context == null || string.IsNullOrEmpty(context.FilePath)) return;

            if (markdownObject is ContainerBlock containerBlock)
            {
                foreach (var subBlock in containerBlock)
                {
                    UpdateLinks(subBlock, context);
                }
            }
            else if (markdownObject is LeafBlock leafBlock)
            {
                if (leafBlock.Inline != null)
                {
                    foreach (var subInline in leafBlock.Inline)
                    {
                        UpdateLinks(subInline, context);
                    }
                }
            }
            else if (markdownObject is ContainerInline containerInline)
            {
                foreach (var subInline in containerInline)
                {
                    UpdateLinks(subInline, context);
                }

                if (markdownObject is LinkInline linkInline)
                {
                    var originalUrl = linkInline.Url;
                    if (RelativePath.IsRelativePath(originalUrl) && PathUtility.IsRelativePath(originalUrl) && !RelativePath.IsPathFromWorkingFolder(originalUrl) && !originalUrl.StartsWith("#"))
                    {
                        linkInline.GetDynamicUrl = () =>
                        {
                            return ((RelativePath)context.FilePath + (RelativePath)originalUrl).GetPathFromWorkingFolder();
                        };
                    }
                }
            }
        }
    }
}
