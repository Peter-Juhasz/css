using Css.Source;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Css.Syntax;

public abstract class SyntaxLocatorWalker(int initialPosition = 0) : SyntaxWalker
{
    private int _position = initialPosition;

    protected int Consumed => _position;
    protected bool IsCancelled { get; private set; }

    protected List<SyntaxNode> Stack = [];

    public override void VisitTrivia(SyntaxTrivia trivia)
    {
        base.VisitTrivia(trivia);
        MarkAsConsumed(trivia);
    }

    public override void VisitToken(SyntaxToken token)
    {
        VisitLeadingTrivia(token);
        VisitTokenCore(token);
        MarkAsConsumed(token.Text.Length);
        VisitTrailingTrivia(token);
    }

	public override void Visit(DirectiveSyntax node)
	{
		if (IsCancelled) return;

		Push(node);
		base.Visit(node);
		Pop();
	}

	public override void Visit(RuleDeclarationSyntax node)
    {
        if (IsCancelled) return;

        Push(node);
        base.Visit(node);
        Pop();
    }

    public override void Visit(PropertySyntax node)
    {
        if (IsCancelled) return;

        Push(node);
        base.Visit(node);
        Pop();
    }

    public override void Visit(CompoundSelectorSyntax node)
    {
        if (IsCancelled) return;

        Push(node);
        base.Visit(node);
        Pop();
    }

    public override void Visit(FunctionCallExpressionSyntax node)
    {
        if (IsCancelled) return;

        Push(node);
        base.Visit(node);
        Pop();
    }

	public override void Visit(KeyframeFrameDirectiveSyntax node)
	{
		if (IsCancelled) return;

		Push(node);
		base.Visit(node);
		Pop();
	}


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void Push(SyntaxNode node) => Stack.Add(node);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void Pop() => Stack.RemoveAt(Stack.Count - 1);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected SyntaxNode? Peek(int level = 0)
	{
        var stackIndex = Stack.Count - level - 1;
        if (stackIndex < 0) return null;

		return Stack[stackIndex];
	}

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void MarkAsConsumed(AbstractSyntaxNode node) => MarkAsConsumed(node.Width);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected void MarkAsConsumed(int width) => _position += width;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected SnapshotNode CreateMatch(AbstractSyntaxNode node) => new(Consumed, node);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected SnapshotNode<TSyntax> CreateMatch<TSyntax>(TSyntax node, int offset = 0) where TSyntax : AbstractSyntaxNode => new(Consumed + offset, node, [.. Stack]);

	protected void Cancel() => IsCancelled = true;
}