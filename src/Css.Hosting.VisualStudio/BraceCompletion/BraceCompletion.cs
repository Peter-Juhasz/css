using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.BraceCompletion;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using System;
using System.ComponentModel.Composition;

namespace EditorTest.BraceCompletion;

[Export(typeof(IBraceCompletionSessionProvider))]
[ContentType(EditorClassifier1.ContentType)]
[BracePair('"', '"')]
[BracePair('\'', '\'')]
[BracePair('(', ')')]
[BracePair('[', ']')]
[BracePair('{', '}')]
internal sealed class CssBraceCompletionSessionProvider : IBraceCompletionSessionProvider
{
#pragma warning disable 649

    [Import]
    private ITextBufferUndoManagerProvider _textBufferUndoManagerProvider;

    [Import]
    private IEditorOperationsFactoryService _editorOperationsFactoryService;

#pragma warning restore 649


    public bool TryCreateSession(ITextView textView, SnapshotPoint openingPoint, char openingBrace, char closingBrace, out IBraceCompletionSession session)
    {
        var snapshot = openingPoint.Snapshot;

        // get syntax tree
        /*SyntaxTree syntaxTree = snapshot.GetSyntaxTree();
        var matches = syntaxTree.FindNodeAt(openingPoint);
        
        if (matches.Contains is not { Node: StringExpressionSyntax syntax })
        {
            session = null;
            return false;
        }

        if (openingPoint.Position != syntax.LeadingTrivia.Width() + 1)
        {
            session = null;
            return false;
        }

        if (syntax.Token.IsClosed)
        {
            session = null;
            return false;
        }*/

        session = openingBrace switch
        {
            '(' => new ParenthesisCompletionSession(
                textView, openingPoint,
                _textBufferUndoManagerProvider.GetTextBufferUndoManager(textView.TextBuffer).TextBufferUndoHistory,
                _editorOperationsFactoryService.GetEditorOperations(textView)
            ),
            '[' => new BracketCompletionSession(
                textView, openingPoint,
                _textBufferUndoManagerProvider.GetTextBufferUndoManager(textView.TextBuffer).TextBufferUndoHistory,
                _editorOperationsFactoryService.GetEditorOperations(textView)
            ),
            '{' => new BraceCompletionSession(
                textView, openingPoint,
                _textBufferUndoManagerProvider.GetTextBufferUndoManager(textView.TextBuffer).TextBufferUndoHistory,
                _editorOperationsFactoryService.GetEditorOperations(textView)
            ),
            '"' or '\'' => new QuoteCompletionSession(
                openingBrace,
                textView, openingPoint,
                _textBufferUndoManagerProvider.GetTextBufferUndoManager(textView.TextBuffer).TextBufferUndoHistory,
                _editorOperationsFactoryService.GetEditorOperations(textView)
            ),
            _ => throw new InvalidOperationException()
        };
        return true;
    }


    private class ParenthesisCompletionSession(ITextView textView, SnapshotPoint openingPoint, ITextUndoHistory undoHistory, IEditorOperations editorOperations)
        : BraceCompletionSessionBase(textView, openingPoint, undoHistory, editorOperations)
    {
        public override char OpeningBrace => '(';
        public override char ClosingBrace => ')';
    }

    private class BracketCompletionSession(ITextView textView, SnapshotPoint openingPoint, ITextUndoHistory undoHistory, IEditorOperations editorOperations)
        : BraceCompletionSessionBase(textView, openingPoint, undoHistory, editorOperations)
    {
        public override char OpeningBrace => '[';
        public override char ClosingBrace => ']';
    }

    private class BraceCompletionSession(ITextView textView, SnapshotPoint openingPoint, ITextUndoHistory undoHistory, IEditorOperations editorOperations)
        : BraceCompletionSessionBase(textView, openingPoint, undoHistory, editorOperations)
    {
        public override char OpeningBrace => '{';
        public override char ClosingBrace => '}';
    }

    private class QuoteCompletionSession(char quote, ITextView textView, SnapshotPoint openingPoint, ITextUndoHistory undoHistory, IEditorOperations editorOperations)
        : BraceCompletionSessionBase(textView, openingPoint, undoHistory, editorOperations)
    {
        public override char OpeningBrace => quote;
        public override char ClosingBrace => quote;
    }

    private abstract class BraceCompletionSessionBase : IBraceCompletionSession
    {
        public BraceCompletionSessionBase(ITextView textView, SnapshotPoint openingPoint,
            ITextUndoHistory undoHistory, IEditorOperations editorOperations
        )
        {
            TextView = textView;
            SubjectBuffer = textView.TextBuffer;
            OpeningPoint = SubjectBuffer.CurrentSnapshot.CreateTrackingPoint(openingPoint, PointTrackingMode.Negative);
            ClosingPoint = SubjectBuffer.CurrentSnapshot.CreateTrackingPoint(openingPoint, PointTrackingMode.Positive);

            _undoHistory = undoHistory;
            _editorOperations = editorOperations;
        }

        public abstract char OpeningBrace { get; }
        public abstract char ClosingBrace { get; }

        public ITrackingPoint OpeningPoint { get; private set; }
        public ITrackingPoint ClosingPoint { get; private set; }
        public ITextBuffer SubjectBuffer { get; }
        public ITextView TextView { get; }

        private readonly ITextUndoHistory _undoHistory;
        private readonly IEditorOperations _editorOperations;

        public void Start()
        {
            // this is where the caret should go after the change
            SnapshotPoint beforePoint = TextView.Caret.Position.BufferPosition;
            ITrackingPoint beforeTrackingPoint = beforePoint.Snapshot.CreateTrackingPoint(beforePoint.Position, PointTrackingMode.Negative);

            ITextSnapshot snapshot = SubjectBuffer.CurrentSnapshot;

            using (ITextUndoTransaction transaction = _undoHistory.CreateTransaction("Brace Completion"))
            {
                // insert closing pair
                using (ITextEdit edit = SubjectBuffer.CreateEdit())
                {
                    SnapshotPoint closingSnapshotPoint = ClosingPoint.GetPoint(snapshot);

                    // insert closing pair
                    edit.Insert(closingSnapshotPoint, ClosingBrace.ToString());

                    snapshot = edit.Apply();
                }

                // switch from positive to negative tracking so it stays against the closing brace
                ClosingPoint = SubjectBuffer.CurrentSnapshot.CreateTrackingPoint(ClosingPoint.GetPoint(snapshot), PointTrackingMode.Negative);

                // move caret between the braces
                beforePoint = beforeTrackingPoint.GetPoint(TextView.TextSnapshot);
                TextView.Caret.MoveTo(beforePoint);

                transaction.Complete();
            }
        }

        public void PreOverType(out bool handledCommand)
        {
            handledCommand = false;

            // get caret position
            SnapshotPoint? caret = TextView.Caret.Position.Point.GetPoint(SubjectBuffer, PositionAffinity.Predecessor);
            if (caret == null)
                return;

            // map brace points to current snapshot
            ITextSnapshot snapshot = SubjectBuffer.CurrentSnapshot;
            SnapshotPoint closingPoint = ClosingPoint.GetPoint(snapshot);

            // check caret position
            SnapshotSpan inBetween = new SnapshotSpan(caret.Value, closingPoint - 1);
            if (!String.IsNullOrWhiteSpace(inBetween.GetText()))
                return;

            // move caret after the closing brace
            TextView.Caret.MoveTo(closingPoint);
            handledCommand = true; // skip insertion
        }

        public void PreBackspace(out bool handledCommand)
        {
            handledCommand = false;

            // get caret position
            SnapshotPoint? caret = TextView.Caret.Position.Point.GetPoint(SubjectBuffer, PositionAffinity.Predecessor);
            if (caret == null)
                return;

            // map brace points to current snapshot
            ITextSnapshot snapshot = SubjectBuffer.CurrentSnapshot;
            SnapshotPoint openingPoint = OpeningPoint.GetPoint(snapshot);
            SnapshotPoint closingPoint = ClosingPoint.GetPoint(snapshot);
            SnapshotSpan span = new SnapshotSpan(openingPoint, closingPoint);

            // check caret position
            if (caret != openingPoint + 1)
                return;

            // check there is no content between the braces
            SnapshotSpan innerSpan = new SnapshotSpan(span.Start + 1, span.End - 1);
            if (!String.IsNullOrWhiteSpace(innerSpan.GetText()))
                return;

            using (ITextUndoTransaction undo = _undoHistory.CreateTransaction("Brace Completion"))
            {
                using (ITextEdit edit = SubjectBuffer.CreateEdit())
                {
                    // delete both braces
                    edit.Delete(span);

                    edit.Apply();

                    // handle the command so the backspace does 
                    // not go through since we've already cleared the braces
                    handledCommand = true;
                }

                undo.Complete();
            }
        }

        public void PreTab(out bool handledCommand)
        {
            handledCommand = false;

            // map brace points to current snapshot
            ITextSnapshot snapshot = SubjectBuffer.CurrentSnapshot;
            SnapshotPoint closingPoint = ClosingPoint.GetPoint(snapshot);

            // move caret after the closing brace
            TextView.Caret.MoveTo(closingPoint);
            handledCommand = true;
        }

        public void Finish()
        {
        }


        public void PostBackspace()
        {
        }

        public void PostDelete()
        {
        }

        public void PostOverType()
        {
        }

        public void PostReturn()
        {
        }

        public void PostTab()
        {
        }

        public void PreDelete(out bool handledCommand)
        {
            handledCommand = false;
        }

        public void PreReturn(out bool handledCommand)
        {
            handledCommand = false;
        }
    }
}
