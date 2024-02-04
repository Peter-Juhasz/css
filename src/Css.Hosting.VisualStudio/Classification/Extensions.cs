using Css.Source;
using Microsoft.VisualStudio.Text;
using System.Runtime.CompilerServices;

namespace Css.Syntax;

public static partial class Extensions
{
    public static SyntaxTree GetSyntaxTree(this ITextSnapshot snapshot)
    {
        lock (snapshot)
        {
            return snapshot.TextBuffer.Properties.GetOrCreateSingletonProperty<SyntaxTreeCache>(() => new()).GetOrCreate(snapshot.Version, () => Parser.ParseDocument(snapshot.GetText()));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Span ToSpan(this SourceSpan span) => new(span.Position, span.Length);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SourcePoint ToSourcePoint(this SnapshotPoint span) => new(span.Position);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SnapshotSpan ToSnapshotSpan(this SourceSpan span, ITextSnapshot snapshot) => new(snapshot, span.Position, span.Length);

    public static void Apply(this ITextEdit edit, SourceChange change)
    {
        edit.Replace(change.Span.ToSpan(), change.NewText);
    }
}