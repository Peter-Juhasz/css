using Css.Source;
using EditorTest.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Css.Formatting;

public class Formatter
{
    public static bool TryFormatProperty(SnapshotNode node, SourcePoint triggerPoint, out IReadOnlyList<SourceChange> changes)
    {
        if (node.Node is not PropertySyntax propertySyntax)
        {
            changes = Array.Empty<SourceChange>();
            return false;
        }

        if (triggerPoint.Position != node.Position + propertySyntax.Width - propertySyntax.TrailingTrivia.Width())
        {
            changes = Array.Empty<SourceChange>();
            return false;
        }

        List<SourceChange>? edit = null;
        var formatter = new FormatWalker(change =>
        {
            edit ??= [];
            edit.Add(change);
        }, node.Position);
        formatter.Visit(propertySyntax);

        changes = edit;
        return true;
    }

    public static bool TryFormatDeclaration(SnapshotNode node, SourcePoint triggerPoint, out IReadOnlyList<SourceChange> changes)
    {
        if (node.Node is not RuleDeclarationSyntax propertySyntax)
        {
            changes = Array.Empty<SourceChange>();
            return false;
        }

        if (triggerPoint.Position != node.Position + propertySyntax.Width - propertySyntax.TrailingTrivia.Width())
        {
            changes = Array.Empty<SourceChange>();
            return false;
        }

        List<SourceChange>? edit = null;
        var formatter = new FormatWalker(change =>
        {
            edit ??= [];
            edit.Add(change);
        }, node.Position);
        formatter.Visit(propertySyntax);

        changes = edit;
        return true;
    }
}
