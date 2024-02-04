using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Css.Hosting.VisualStudio.CodeCompletion;

[Export(typeof(IVsTextViewCreationListener))]
[ContentType(EditorClassifier1.ContentType)]
[TextViewRole(PredefinedTextViewRoles.Interactive)]
internal sealed class CompletionVsTextViewCreationListener : IVsTextViewCreationListener
{
    [Import]
    private IVsEditorAdaptersFactoryService AdaptersFactory;

    [Import]
    private ICompletionBroker CompletionBroker;

    [Import]
    private ITextDocumentFactoryService TextDocumentFactoryService;

    public void VsTextViewCreated(IVsTextView textViewAdapter)
    {
        IWpfTextView view = AdaptersFactory.GetWpfTextView(textViewAdapter);
        Debug.Assert(view != null);

        ITextDocument document;
        if (!TextDocumentFactoryService.TryGetTextDocument(view.TextDataModel.DocumentBuffer, out document))
            return;

        CommandFilter filter = new CommandFilter(view, CompletionBroker);

        IOleCommandTarget next;
        ErrorHandler.ThrowOnFailure(textViewAdapter.AddCommandFilter(filter, out next));
        filter.Next = next;
    }


    private sealed class CommandFilter(IWpfTextView textView, ICompletionBroker broker) : IOleCommandTarget
    {
        private ICompletionSession _currentSession = null;

        public IWpfTextView TextView { get; private set; } = textView;
        public ICompletionBroker Broker { get; private set; } = broker;
        public IOleCommandTarget Next { get; set; }

        private static char GetTypeChar(IntPtr pvaIn)
        {
            return (char)(ushort)Marshal.GetObjectForNativeVariant(pvaIn);
        }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            bool handled = false;
            int hresult = VSConstants.S_OK;

            // pre-process
            if (pguidCmdGroup == VSConstants.VSStd2K)
            {
                switch ((VSConstants.VSStd2KCmdID)nCmdID)
                {
                    case VSConstants.VSStd2KCmdID.AUTOCOMPLETE:
                    case VSConstants.VSStd2KCmdID.COMPLETEWORD: // TODO: Commit if unique
                        handled = StartSession();
                        break;

                    case VSConstants.VSStd2KCmdID.RETURN:
                    case VSConstants.VSStd2KCmdID.TAB:
                        handled = Complete();
                        break;

                    case VSConstants.VSStd2KCmdID.TYPECHAR:
                        char @char = GetTypeChar(pvaIn);
                        if (!Char.IsLetter(@char) && @char != '-')
                            Complete();
                        break;

                    case VSConstants.VSStd2KCmdID.CANCEL:
                        handled = Cancel();
                        break;
                }
            }

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

                            if (Char.IsLetter(@char) || @char == '-' || @char == '#' || @char == '.' || @char == ':')
                            {
                                if (_currentSession != null)
                                    Filter();
                                else
                                    StartSession();
                            }
                            break;

                        case VSConstants.VSStd2KCmdID.BACKSPACE:
                            Filter();
                            break;
                    }
                }
            }

            return hresult;
        }

        private void Filter()
        {
            var session = _currentSession;
            if (session == null)
                return;

            if (session.IsDismissed)
                return;

            if (!session.IsStarted)
            {
                session.Start();
            }

            session.Filter();

            var set = session.SelectedCompletionSet;
            if (set != null)
            {
                set.SelectBestMatch();
            }
        }

        bool Cancel()
        {
            var session = _currentSession;
            if (session == null)
                return false;

            if (session.IsDismissed)
                return false;

            if (!session.IsStarted)
                return false;

            session.Dismiss();

            return true;
        }

        bool Complete()
        {
            var session = _currentSession;
            if (session == null)
                return false;

            if (session.IsDismissed)
                return false;

            if (!session.IsStarted)
                return false;

            var set = session.SelectedCompletionSet;
            if (set == null || !set.SelectionStatus.IsSelected)
            {
                session.Dismiss();
                return false;
            }

            session.Commit();
            return true;
        }

        bool StartSession()
        {
            if (_currentSession != null)
                return false;

            SnapshotPoint caret = TextView.Caret.Position.BufferPosition;
            ITextSnapshot snapshot = caret.Snapshot;

            if (!Broker.IsCompletionActive(TextView))
            {
                var session = Broker.CreateCompletionSession(TextView, snapshot.CreateTrackingPoint(caret, PointTrackingMode.Positive), true);
                _currentSession = session;
                session.Dismissed += OnDismissed;
                session.Start();
            }

            return true;
        }

        private void OnDismissed(object sender, EventArgs e)
        {
            _currentSession = null;
            ((ICompletionSession)sender).Dismissed -= OnDismissed;
        }

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            if (pguidCmdGroup == VSConstants.VSStd2K)
            {
                switch ((VSConstants.VSStd2KCmdID)prgCmds[0].cmdID)
                {
                    case VSConstants.VSStd2KCmdID.AUTOCOMPLETE:
                    case VSConstants.VSStd2KCmdID.COMPLETEWORD:
                        prgCmds[0].cmdf = (uint)OLECMDF.OLECMDF_ENABLED | (uint)OLECMDF.OLECMDF_SUPPORTED;
                        return VSConstants.S_OK;
                }
            }

            return Next.QueryStatus(pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }
    }
}
