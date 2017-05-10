﻿using System;
using System.Linq;
using AD.IO.Standard;
using AD.OpenXml.Standard.Visits;
using JetBrains.Annotations;

namespace AD.OpenXml.Standard.Visitors
{
    /// <summary>
    /// Represents a visitor or rewriter for OpenXML documents.
    /// </summary>
    [PublicAPI]
    public sealed class ReportVisitor : OpenXmlVisitor
    {
        /// <summary>
        /// Initialize a <see cref="ReportVisitor"/> based on the supplied <see cref="DocxFilePath"/>.
        /// </summary>
        /// <param name="result">
        /// The base path used to initialize the new <see cref="ReportVisitor"/>.
        /// </param>
        public ReportVisitor([NotNull] DocxFilePath result) : base(result) { }

        /// <summary>
        /// Initialize a new <see cref="ReportVisitor"/> from the supplied <see cref="OpenXmlVisitor"/>.
        /// </summary>
        /// <param name="subject">
        /// The <see cref="OpenXmlVisitor"/> used to initialize the new <see cref="ReportVisitor"/>.
        /// </param>
        private ReportVisitor([NotNull] IOpenXmlVisitor subject) : base(subject) { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="subject"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"/>
        protected override IOpenXmlVisitor Create(IOpenXmlVisitor subject)
        {
            if (subject is null)
            {
                throw new ArgumentNullException(nameof(subject));
            }

            return new ReportVisitor(subject);
        }

        /// <summary>
        /// Visit the <see cref="IOpenXmlVisitor.Document"/> of the subject.
        /// </summary>
        /// <param name="subject">
        /// The <see cref="IOpenXmlVisitor"/> to visit.
        /// </param>
        /// <param name="revisionId">
        /// The current revision number incremented by one.
        /// </param>
        /// <returns>
        /// A new <see cref="IOpenXmlVisitor"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException"/>
        [Pure]
        protected override IOpenXmlVisitor VisitDocument(IOpenXmlVisitor subject, int revisionId)
        {
            if (subject is null)
            {
                throw new ArgumentNullException(nameof(subject));
            }

            return Create(new DocumentVisit(subject, revisionId).Result);
        }

        /// <summary>
        /// Visit the <see cref="IOpenXmlVisitor.Footnotes"/> of the subject.
        /// </summary>
        /// <param name="subject">
        /// The <see cref="IOpenXmlVisitor"/> to visit.
        /// </param>
        /// <param name="footnoteId">
        /// The current footnote identifier.
        /// </param>
        /// <param name="revisionId">
        /// The current revision number incremented by one.
        /// </param>
        /// <returns>
        /// A new <see cref="IOpenXmlVisitor"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException"/>
        [Pure]
        protected override IOpenXmlVisitor VisitFootnotes(IOpenXmlVisitor subject, int footnoteId, int revisionId)
        {
            if (subject is null)
            {
                throw new ArgumentNullException(nameof(subject));
            }

            return new FootnoteVisit(subject, footnoteId, revisionId).Result;
        }

        /// <summary>
        /// Visit the <see cref="IOpenXmlVisitor.Document"/> and <see cref="IOpenXmlVisitor.DocumentRelations"/> of the subject to modify hyperlinks in the main document.
        /// </summary>
        /// <param name="subject">
        /// The <see cref="IOpenXmlVisitor"/> to visit.
        /// </param>
        /// <param name="documentRelationId">
        /// The current document relationship identifier.
        /// </param>
        /// <returns>
        /// A new <see cref="IOpenXmlVisitor"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException"/>
        [Pure]
        protected override IOpenXmlVisitor VisitDocumentRelations(IOpenXmlVisitor subject, int documentRelationId)
        {
            if (subject is null)
            {
                throw new ArgumentNullException(nameof(subject));
            }

            return new DocumentRelationVisit(subject, documentRelationId).Result;
        }

        /// <summary>
        /// Visit the <see cref="IOpenXmlVisitor.Footnotes"/> and <see cref="IOpenXmlVisitor.FootnoteRelations"/> of the subject to modify hyperlinks in the main document.
        /// </summary>
        /// <param name="subject">
        /// The <see cref="IOpenXmlVisitor"/> to visit.
        /// </param>
        /// <param name="footnoteRelationId">
        /// The current footnote relationship identifier.
        /// </param>
        /// <returns>
        /// A new <see cref="IOpenXmlVisitor"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException"/>
        [Pure]
        protected override IOpenXmlVisitor VisitFootnoteRelations(IOpenXmlVisitor subject, int footnoteRelationId)
        {
            if (subject is null)
            {
                throw new ArgumentNullException(nameof(subject));
            }

            return new FootnoteRelationVisit(subject, footnoteRelationId).Result;
        }

        /// <summary>
        /// Visit the <see cref="OpenXmlVisitor.Styles"/> of the subject.
        /// </summary>
        /// <param name="subject">
        /// The <see cref="OpenXmlVisitor"/> to visit.
        /// </param>
        /// <returns>
        /// A new <see cref="OpenXmlVisitor"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException"/>
        [Pure]
        protected override IOpenXmlVisitor VisitStyles(IOpenXmlVisitor subject)
        {
            if (subject is null)
            {
                throw new ArgumentNullException(nameof(subject));
            }

            return new StyleVisit(subject).Result;
        }

        /// <summary>
        /// Visit the <see cref="OpenXmlVisitor.Numbering"/> of the subject.
        /// </summary>
        /// <param name="subject">
        /// The <see cref="OpenXmlVisitor"/> to visit.
        /// </param>
        /// <returns>
        /// A new <see cref="OpenXmlVisitor"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException"/>
        [Pure]
        protected override IOpenXmlVisitor VisitNumbering(IOpenXmlVisitor subject)
        {
            if (subject is null)
            {
                throw new ArgumentNullException(nameof(subject));
            }

            return new NumberingVisit(subject).Result;
        }
    }
}