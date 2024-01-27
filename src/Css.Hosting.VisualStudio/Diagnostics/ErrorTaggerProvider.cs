using EditorTest.Syntax;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace EditorTest.Diagnostics;

[Export(typeof(ITaggerProvider))]
[TagType(typeof(IErrorTag))]
[ContentType(EditorClassifier1.ContentType)]
internal sealed class ErrorTaggerProvider : ITaggerProvider
{
    public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
    {
        return buffer.Properties.GetOrCreateSingletonProperty(
            creator: () => new Tagger()
        ) as ITagger<T>;
    }


    private sealed class Tagger : ITagger<IErrorTag>
    {
        public IEnumerable<ITagSpan<IErrorTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            var snapshot = spans.First().Snapshot;
            SyntaxTree syntaxTree = snapshot.GetSyntaxTree();

            List<ITagSpan<IErrorTag>>? results = null;

            var analyze = new DiagnosticsVisitor(diagnostic =>
            {
                results ??= [];
                results.Add(new TagSpan<IErrorTag>(new(snapshot, diagnostic.Span.ToSpan()), new DiagnosticErrorTag(diagnostic.Severity switch
                {
                    Severity.Error => PredefinedErrorTypeNames.SyntaxError,
                    Severity.Warning => PredefinedErrorTypeNames.Warning,
                    Severity.Hint => PredefinedErrorTypeNames.HintedSuggestion,
                }, diagnostic.Id, diagnostic.Description)));
            });
            analyze.Visit(syntaxTree);

            return results ?? Enumerable.Empty<ITagSpan<IErrorTag>>();
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
    }
}