using Css.Hosting.VisualStudio.Classification;
using Css.Syntax;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;

namespace Css.Hosting.VisualStudio;

public class ClassificationRegistry(IClassificationTypeRegistryService registry)
{
    public IClassificationType Test = registry.GetClassificationType("EditorClassifier1");

    public IClassificationType Comment = registry.GetClassificationType("CSS Comment");

    public IClassificationType Keyword = registry.GetClassificationType("CSS Keyword");

    public IClassificationType PropertyName = registry.GetClassificationType("CSS Property Name");

    public IClassificationType PropertyValue = registry.GetClassificationType("CSS Property Value");

    public IClassificationType WhiteSpace = registry.GetClassificationType(PredefinedClassificationTypeNames.WhiteSpace);

    public IClassificationType Punctuation = registry.GetClassificationType(PredefinedClassificationTypeNames.Punctuation);

    public IClassificationType Number = registry.GetClassificationType(PredefinedClassificationTypeNames.Number);

    public IClassificationType String = registry.GetClassificationType(PredefinedClassificationTypeNames.String);

    public IClassificationType NaturalLanguage = registry.GetClassificationType(PredefinedClassificationTypeNames.NaturalLanguage);

    public IClassificationType Identifier = registry.GetClassificationType(PredefinedClassificationTypeNames.Identifier);

    public IClassificationType Type = registry.GetClassificationType(PredefinedClassificationTypeNames.Type);

    public IClassificationType MarkupElement = registry.GetClassificationType(PredefinedClassificationTypeNames.MarkupNode);

    public IClassificationType MarkupAttribute = registry.GetClassificationType(PredefinedClassificationTypeNames.MarkupAttribute);

    public IClassificationType MarkupAttributeValue = registry.GetClassificationType(PredefinedClassificationTypeNames.MarkupAttributeValue);

    public IClassificationType Unnecessary = registry.GetClassificationType("UnnecessaryCodeDiagnostic");
}

/// <summary>
/// Classifier that classifies all text as an instance of the "EditorClassifier1" classification type.
/// </summary>
internal class EditorClassifier1 : IClassifier
{
    public const string ContentType = "CSS2";

    /// <summary>
    /// Classification type.
    /// </summary>
    private readonly ClassificationRegistry registry;

    /// <summary>
    /// Initializes a new instance of the <see cref="EditorClassifier1"/> class.
    /// </summary>
    /// <param name="registry">Classification registry.</param>
    internal EditorClassifier1(IClassificationTypeRegistryService registry)
    {
        this.registry = new(registry);
    }

    #region IClassifier

#pragma warning disable 67

    /// <summary>
    /// An event that occurs when the classification of a span of text has changed.
    /// </summary>
    /// <remarks>
    /// This event gets raised if a non-text change would affect the classification in some way,
    /// for example typing /* would cause the classification to change in C# without directly
    /// affecting the span.
    /// </remarks>
    public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;

#pragma warning restore 67

    /// <summary>
    /// Gets all the <see cref="ClassificationSpan"/> objects that intersect with the given range of text.
    /// </summary>
    /// <remarks>
    /// This method scans the given SnapshotSpan for potential matches for this classification.
    /// In this instance, it classifies everything and returns each span as a new ClassificationSpan.
    /// </remarks>
    /// <param name="span">The span currently being classified.</param>
    /// <returns>A list of ClassificationSpans that represent spans identified to be of this classification.</returns>
    public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
    {
        var syntaxTree = span.Snapshot.GetSyntaxTree();

        var classifierWalker = new ClassifierSyntaxWalker(span, registry);
        classifierWalker.Visit(syntaxTree);
        return classifierWalker.Results;
    }

    #endregion
}

[Export(typeof(IVsTextViewCreationListener))]
[ContentType(EditorClassifier1.ContentType)]
[TextViewRole(PredefinedTextViewRoles.Interactive)]
internal sealed class VsTextViewCreationListener : IVsTextViewCreationListener
{
    [Import]
    private IVsEditorAdaptersFactoryService AdaptersFactory;

    public void VsTextViewCreated(IVsTextView textViewAdapter)
    {
        IWpfTextView view = AdaptersFactory.GetWpfTextView(textViewAdapter);
        //view.TextDataModel.DocumentBuffer.Properties.AddProperty(typeof(SyntaxTreeCache), new SyntaxTreeCache());
    }
}
