using EditorTest.Data;
using EditorTest.Syntax;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace EditorTest.QuickInfo
{
    [Export(typeof(IAsyncQuickInfoSourceProvider))]
    [Name("Robots.Txt Quick Info Provider")]
    [ContentType(EditorClassifier1.ContentType)]
    internal sealed class QuickInfoSourceProvider : IAsyncQuickInfoSourceProvider
    {
#pragma warning disable 649

        [Import]
        private IGlyphService glyphService;

        [Import]
        private IClassificationTypeRegistryService classificationRegistry;

        [Import]
        private IClassificationFormatMapService classificationFormatMapService;

#pragma warning restore 649


        public IAsyncQuickInfoSource TryCreateQuickInfoSource(ITextBuffer textBuffer)
        {
            return textBuffer.Properties.GetOrCreateSingletonProperty(
                creator: () => new RobotsTxtQuickInfoSource(
                    textBuffer,
                    glyphService,
                    classificationFormatMapService,
                    classificationRegistry
                )
            );
        }


        private sealed class RobotsTxtQuickInfoSource : IAsyncQuickInfoSource
        {
            public RobotsTxtQuickInfoSource(
                ITextBuffer buffer,
                IGlyphService glyphService,
                IClassificationFormatMapService classificationFormatMapService,
                IClassificationTypeRegistryService classificationRegistry
            )
            {
                _buffer = buffer;
                _glyphService = glyphService;
                _classificationFormatMapService = classificationFormatMapService;
                _classificationRegistry = classificationRegistry;
            }

            private readonly ITextBuffer _buffer;
            private readonly IGlyphService _glyphService;
            private readonly IClassificationFormatMapService _classificationFormatMapService;
            private readonly IClassificationTypeRegistryService _classificationRegistry;

            private static readonly DataTemplate Template;

            public async Task<QuickInfoItem> GetQuickInfoItemAsync(IAsyncQuickInfoSession session, CancellationToken cancellationToken)
            {
                var snapshot = _buffer.CurrentSnapshot;
                var triggerPoint = session.GetTriggerPoint(snapshot);
                if (triggerPoint == null)
                {
                    return null;
                }

                var syntaxTree = snapshot.GetSyntaxTree();
                var matches = syntaxTree.FindNodeAt(triggerPoint.Value.Position);

                QuickInfoItem? item;
                if (InspectForPropertyName(syntaxTree, snapshot, matches.Contains, out item))
                {
                    return item;
                }

                return null;
            }


            private bool InspectForPropertyName(SyntaxTree syntaxTree, ITextSnapshot snapshot, SnapshotNode? node, out QuickInfoItem? item)
            {
                item = null;
                if (node == null)
                {
                    return false;
                }

                if (node.Value.Node is not PropertyNameSyntax propertyName)
                {
                    return false;
                }

                if (!CssWebData.Index.Properties.TryGetValue(propertyName.NameToken.Value, out var definition))
                {
                    return false;
                }

                var trackingSpan = snapshot.CreateTrackingSpan(new(node.Value.Position + propertyName.LeadingTrivia.Width(), propertyName.NameToken.Width), SpanTrackingMode.EdgeInclusive);
                item = new QuickInfoItem(trackingSpan, String.Join(Environment.NewLine, definition.Name + ": " + definition.Syntax, definition.Description));
                return true;
            }

            void IDisposable.Dispose()
            { }

        }
    }
}
