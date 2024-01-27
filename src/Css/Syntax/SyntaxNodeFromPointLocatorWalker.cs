using Css.Source;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace EditorTest.Syntax;

public class SyntaxNodeFromPointLocatorWalker(int position) : SyntaxLocatorWalker
{
    private SnapshotNode? _before;
    private SnapshotNode? _contains;
    private SnapshotNode? _after;

    public override void Visit(PropertyNameSyntax node)
    {
        Inspect(node);
        base.Visit(node);
    }

    public override void Visit(StringExpressionSyntax node)
    {
        Inspect(node);
        base.Visit(node);
    }

    public override void Visit(NumberWithUnitSyntax node)
    {
        Inspect(node);
        base.Visit(node);
    }

    public override void Visit(IdentifierExpressionSyntax node)
    {
        Inspect(node);
        base.Visit(node);
    }

    public override void Visit(FunctionCallExpressionSyntax node)
    {
        Inspect(node);
        base.Visit(node);
    }

    public override void Visit(AttributeSelectorSyntax node)
    {
        Inspect(node);
        base.Visit(node);
    }

    public override void Visit(RuleDeclarationSyntax node)
    {
        if (Consumed + node.Width < position)
        {
            MarkAsConsumed(node);
            return;
        }

        Inspect(node);
        base.Visit(node);
    }

    public override void Visit(PropertySyntax node)
    {
        if (Consumed + node.Width < position)
        {
            MarkAsConsumed(node);
            return;
        }

        Inspect(node);
        base.Visit(node);
    }

    public override void Visit(ElementSelectorSyntax node)
    {
        Inspect(node);
        base.Visit(node);
    }

    public override void Visit(IdSelectorSyntax node)
    {
        Inspect(node);
        base.Visit(node);
    }

    public override void Visit(ClassSelectorSyntax node)
    {
        Inspect(node);
        base.Visit(node);
    }

    public override void Visit(PseudoClassSelectorSyntax node)
    {
        Inspect(node);
        base.Visit(node);
    }

    public override void Visit(PseudoElementSelectorSyntax node)
    {
        Inspect(node);
        base.Visit(node);
    }

    private void Inspect(SyntaxNode node)
    {
        var width = node.Width;
        if (Consumed == position)
        {
            _after = new SnapshotNode(Consumed, node, SnapshotStack());
        }
        else if (Consumed < position && position < Consumed + width)
        {
            _contains = new SnapshotNode(Consumed, node, SnapshotStack());
        }
        else if (Consumed + width == position)
        {
            _before = new SnapshotNode(Consumed, node, SnapshotStack());
        }

        if (position + width < Consumed)
        {
            Cancel();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private IReadOnlyList<SyntaxNode> SnapshotStack() => Stack.Count == 0 ? Array.Empty<SyntaxNode>() : Stack.ToList();

    public SyntaxNodeSearchResult GetResult() => new(_before, _contains, _after);
}

public abstract class SyntaxNodeFinder<TSyntax>(Action<SnapshotNode<TSyntax>> found) : SyntaxLocatorWalker where TSyntax : AbstractSyntaxNode
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void Report(TSyntax node, int offset = 0) => found(CreateMatch(node, offset));
}


public record struct SnapshotNode(int Position, AbstractSyntaxNode Node, IReadOnlyList<SyntaxNode>? Ancestors = null);

public record struct SnapshotNode<TSyntax>(int Position, TSyntax Node, IReadOnlyList<SyntaxNode>? Ancestors = null) where TSyntax : AbstractSyntaxNode
{
    public SourceSpan Extent => new(Position, Node.Width);

    public bool TryFindFirstAncestorUpwards<TAncestor>( out TAncestor ancestor) where TAncestor : SyntaxNode
    {
        if (Ancestors == null)
        {
            ancestor = default;
            return false;
        }

        for (var i = 0; i < Ancestors.Count; i++)
        {
            if (Ancestors[i] is TAncestor found)
            {
                ancestor = found;
                return true;
            }
        }

        ancestor = default;
        return false;
    }
}

public record class SyntaxNodeSearchResult(SnapshotNode? Before, SnapshotNode? Contains, SnapshotNode? After)
{
    public bool Any() => Before != null || Contains != null || After != null;
}