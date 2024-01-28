using Css.Formatting;
using Css.Source;
using EditorTest.Extensions;
using EditorTest.Syntax;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace EditorTest.CodeCompletion;

[Export(typeof(IVsTextViewCreationListener))]
[ContentType(EditorClassifier1.ContentType)]
[TextViewRole(PredefinedTextViewRoles.Interactive)]
internal sealed class FormattingVsTextViewCreationListener : IVsTextViewCreationListener
{
    [Import]
    private IVsEditorAdaptersFactoryService AdaptersFactory;

    [Import]
    private ITextDocumentFactoryService TextDocumentFactoryService;

    public void VsTextViewCreated(IVsTextView textViewAdapter)
    {
        IWpfTextView view = AdaptersFactory.GetWpfTextView(textViewAdapter);
        Debug.Assert(view != null);

        ITextDocument document;
        if (!TextDocumentFactoryService.TryGetTextDocument(view.TextDataModel.DocumentBuffer, out document))
            return;

        CommandFilter filter = new CommandFilter(view);

        IOleCommandTarget next;
        ErrorHandler.ThrowOnFailure(textViewAdapter.AddCommandFilter(filter, out next));
        filter.Next = next;
    }


    private sealed class CommandFilter(IWpfTextView textView) : IOleCommandTarget
    {
        public IOleCommandTarget Next { get; set; }

        private void TryFormat(char triggerChar)
        {
            var caret = textView.Caret.Position.BufferPosition;
            var snapshot = caret.Snapshot;
            var syntaxTree = snapshot.GetSyntaxTree();
            var node = syntaxTree.FindNodeAt(caret);

            IReadOnlyList<SourceChange> changes = default;

            // format property
            if (triggerChar == ';')
            {
                _ = (node.Contains.HasValue && Formatter.TryFormatProperty(node.Contains.Value, caret.ToSourcePoint(), out changes)) ||
                    (node.Before.HasValue && Formatter.TryFormatProperty(node.Before.Value, caret.ToSourcePoint(), out changes));
            }

            // format declaration
            else if (triggerChar == '}')
            {
                _ = (node.Contains.HasValue && Formatter.TryFormatDeclaration(node.Contains.Value, caret.ToSourcePoint(), out changes)) ||
                    (node.Before.HasValue && Formatter.TryFormatDeclaration(node.Before.Value, caret.ToSourcePoint(), out changes));
            }

            // apply
            if (changes != null && changes.Any())
            {
                Apply(snapshot, changes);
            }
        }

        private void Apply(ITextSnapshot snapshot, IReadOnlyList<SourceChange> changes)
        {
            if (!changes.Any())
            {
                return;
            }

            using var edit = TextView.TextBuffer.CreateEdit();
            if (edit.Snapshot != snapshot)
            {
                return;
            }

            foreach (var change in changes.AsValueEnumerable())
            {
                edit.Apply(change);
            }

            edit.Apply();
        }

        private static char GetTypeChar(IntPtr pvaIn) => (char)(ushort)Marshal.GetObjectForNativeVariant(pvaIn);

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            bool handled = false;
            int hresult = VSConstants.S_OK;

            if (!handled)
                hresult = Next.Exec(pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);

            if (ErrorHandler.Succeeded(hresult))
            {
                if (pguidCmdGroup == VSConstants.VSStd2K)
                {
                    switch ((VSConstants.VSStd2KCmdID)nCmdID)
                    {
                        case VSConstants.VSStd2KCmdID.TYPECHAR:
                            char @char = GetTypeChar(pvaIn);

                            if (@char is ';' or '}')
                            {
                                TryFormat(@char);
                            }
                            break;
                    }
                }
            }

            return hresult;
        }

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            return Next.QueryStatus(pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }
    }
}
