using Css.Source;
using Css.Syntax;
using Css.Data;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Css.Hosting.VisualStudio.QuickInfo;

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
            creator: () => new QuickInfoSource(
                textBuffer,
                glyphService,
                classificationFormatMapService,
                classificationRegistry
            )
        );
    }


    private sealed class QuickInfoSource : IAsyncQuickInfoSource
    {
        public QuickInfoSource(
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
            var sourceTriggerPoint = triggerPoint.Value.ToSourcePoint();
            if (InspectForPropertyName(syntaxTree, snapshot, matches.Contains, sourceTriggerPoint, out item))
            {
                return item;
            }

            return null;
        }


        private bool InspectForPropertyName(SyntaxTree syntaxTree, ITextSnapshot snapshot, SnapshotNode? node, SourcePoint point, out QuickInfoItem? item)
        {
            item = null;
            if (node == null)
            {
                return false;
            }

            if (node.Value.Node is not PropertySyntax propertyName)
            {
				return false;
			}

			if (!propertyName.GetNameSpan().ToAbsolute(node.Value.Position).Contains(point))
			{
				return false;
			}

			if (!CssWebData.Index.Properties.TryGetValue(propertyName.NameToken.Value, out var definition))
            {
                return false;
            }

            var trackingSpan = snapshot.CreateTrackingSpan(new(node.Value.Position + propertyName.LeadingTrivia.Width(), propertyName.NameToken.Width), SpanTrackingMode.EdgeInclusive);

            object content = String.Join(Environment.NewLine, definition.Name + ": " + definition.Syntax, definition.Description);
            if (Template != null)
            {
                content = new ContentPresenter
                {
                    Content = new QuickInfoViewModel(_glyphService.GetGlyph(StandardGlyphGroup.GlyphGroupProperty, StandardGlyphItem.GlyphItemPublic), definition.Syntax, definition.Description),
                    ContentTemplate = Template
                };
            }

			item = new QuickInfoItem(trackingSpan, content);
            return true;
        }

        void IDisposable.Dispose()
        { }

		static QuickInfoSource()
		{
            try
            {
                var resources = new ResourceDictionary { Source = new Uri("pack://application:,,,/Css.Hosting.VisualStudio;component/Themes/Generic.xaml", UriKind.RelativeOrAbsolute) };

                Template = resources.Values.OfType<DataTemplate>().First();
            }
            catch
            {

            }
		}
	}
}

public record class QuickInfoViewModel(ImageSource Glyph, string Signature, string Documentation);