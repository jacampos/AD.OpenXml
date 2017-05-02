﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using AD.IO;
using AD.Xml;
using JetBrains.Annotations;

namespace AD.OpenXml.Visitors
{
    /// <summary>
    /// Represents a visitor or rewriter for OpenXML documents.
    /// </summary>
    /// <remarks>
    /// This class is modeled after the <see cref="System.Linq.Expressions.ExpressionVisitor"/>. 
    /// 
    /// The goal is to encapsulate OpenXML manipulations within immutable objects. Every visit operation should be a pure function.
    /// Access to <see cref="XElement"/> objects should be done with care, ensuring that objects are cloned prior to any in-place mainpulations.
    /// 
    /// Implementers should derive two types of classes from <see cref="OpenXmlVisitor"/>: a visitor class, and one or more visitation classes.
    /// 
    /// The derived visitor class should provide:
    ///   1) A public constructor that implements <see cref="OpenXmlVisitor(DocxFilePath)"/>.
    ///   2) A private constructor that implements <see cref="OpenXmlVisitor(OpenXmlVisitor)"/>.
    ///   3) An override to the <see cref="Visit(DocxFilePath)"/> method that wraps the base implementation in the private constructor of the derived class.
    ///   4) An override to the <see cref="Visit(IEnumerable{DocxFilePath})"/> method that wraps the base implementation in the private constructor of the derived class.
    ///   5) An override for each desired visitor methods. The default implementations return a new deep copy of the submitted vistor.
    /// </remarks>
    [PublicAPI]
    public class OpenXmlVisitor
    {
        /// <summary>
        /// Represents the 'c:' prefix seen in the markup for chart[#].xml
        /// </summary>
        [NotNull]
        protected static readonly XNamespace C = XNamespaces.OpenXmlDrawingmlChart;

        /// <summary>
        /// Represents the 'r:' prefix seen in the markup of [Content_Types].xml
        /// </summary>
        [NotNull]
        protected static readonly XNamespace P = XNamespaces.OpenXmlPackageRelationships;

        /// <summary>
        /// Represents the 'r:' prefix seen in the markup of document.xml.
        /// </summary>
        [NotNull]
        protected static readonly XNamespace R = XNamespaces.OpenXmlOfficeDocumentRelationships;

        /// <summary>
        /// The namespace declared on the [Content_Types].xml
        /// </summary>
        [NotNull]
        protected static readonly XNamespace T = XNamespaces.OpenXmlPackageContentTypes;

        /// <summary>
        /// Represents the 'w:' prefix seen in raw OpenXML documents.
        /// </summary>
        [NotNull]
        protected static readonly XNamespace W = XNamespaces.OpenXmlWordprocessingmlMain;

        /// <summary>
        /// The source file for this <see cref="OpenXmlVisitor"/>.
        /// </summary>
        [NotNull]
        public DocxFilePath File { get; }

        /// <summary>
        /// Active version of 'word/document.xml'.
        /// </summary>
        [NotNull]
        public virtual XElement Document { get; }

        /// <summary>
        /// Active version of 'word/_rels/document.xml.rels'.
        /// </summary>
        [NotNull]
        public virtual XElement DocumentRelations { get; }

        /// <summary>
        /// Active version of '[Content_Types].xml'.
        /// </summary>
        [NotNull]
        public virtual XElement ContentTypes { get; }

        /// <summary>
        /// Active version of 'word/footnotes.xml'.
        /// </summary>
        [NotNull]
        public virtual XElement Footnotes { get; }

        /// <summary>
        /// Active version of 'word/_rels/footnotes.xml.rels'.
        /// </summary>
        [NotNull]
        public virtual XElement FootnoteRelations { get; }

        /// <summary>
        /// Active version of word/charts/chart#.xml.
        /// </summary>
        [NotNull]
        public virtual IEnumerable<ChartInformation> Charts { get; }

        /// <summary>
        /// Returns the last document relationship identifier in use by the container.
        /// </summary>
        public virtual int DocumentRelationId { get; }

        /// <summary>
        /// Returns the last footnote identifier currently in use by the container.
        /// </summary>
        public virtual int FootnoteId { get; }

        /// <summary>
        /// Returns the last footnote relationship identifier currently in use by the container.
        /// </summary>
        public virtual int FootnoteRelationId { get; }

        /// <summary>
        /// Initializes an <see cref="OpenXmlVisitor"/> by reading document parts into memory.
        /// </summary>
        /// <param name="result">
        /// The file to which changes can be saved.
        /// </param>
        protected OpenXmlVisitor([NotNull] DocxFilePath result)
        {
            File = result;

            Document = 
                result.ReadAsXml() ?? throw new FileNotFoundException("document.xml");

            ContentTypes = 
                result.ReadAsXml("[Content_Types].xml") ?? throw new FileNotFoundException("[Content_Types].xml");

            XElement footnotes =
                Footnotes =
                    result.ReadAsXml("word/footnotes.xml") ?? new XElement(W + "footnotes");

            XElement documentRelations =
                DocumentRelations =
                    result.ReadAsXml("word/_rels/document.xml.rels") ?? new XElement(P + "Relationships");

            XElement footnoteRelations =
                FootnoteRelations =
                    result.ReadAsXml("word/_rels/footnotes.xml.rels") ?? new XElement(P + "Relationships");

            Charts =
                documentRelations.Elements()
                                 .Select(x => x.Attribute("Target")?.Value)
                                 .Where(x => x?.StartsWith("charts/") ?? false)
                                 .Select(x => new ChartInformation(x, result.ReadAsXml($"word/{x}")))
                                 .ToImmutableList();

            DocumentRelationId =
                documentRelations.Elements(P + "Relationship")
                                 .Attributes("Id")
                                 .Select(x => x.Value.ParseInt() ?? 0)
                                 .DefaultIfEmpty(0)
                                 .Max();

            FootnoteId =
                footnotes.Elements(W + "footnote")
                         .Attributes(W + "id")
                         .Select(x => x.Value.ParseInt() ?? 0)
                         .DefaultIfEmpty(0)
                         .Max();

            FootnoteRelationId =
                footnoteRelations.Elements(P + "Relationship")
                                 .Attributes("Id")
                                 .Select(x => x.Value.ParseInt() ?? 0)
                                 .DefaultIfEmpty(0)
                                 .Max();
        }

        /// <summary>
        /// Initializes a new <see cref="OpenXmlVisitor"/> from an existing <see cref="OpenXmlVisitor"/>.
        /// </summary>
        /// <param name="subject">
        /// The <see cref="OpenXmlVisitor"/> to visit.
        /// </param>
        protected OpenXmlVisitor([NotNull] OpenXmlVisitor subject)
        {
            File = subject.File;
            Document = subject.Document.Clone();
            DocumentRelations = subject.DocumentRelations.Clone();
            ContentTypes = subject.ContentTypes.Clone();
            Footnotes = subject.Footnotes.Clone();
            FootnoteRelations = subject.FootnoteRelations.Clone();
            Charts = subject.Charts.Select(x => new ChartInformation(x.Name, x.Chart.Clone())).ToImmutableArray();
            FootnoteId = subject.FootnoteId;
            FootnoteRelationId = subject.FootnoteRelationId;
            DocumentRelationId = subject.DocumentRelationId;
        }

        /// <summary>
        /// Initializes a new <see cref="OpenXmlVisitor"/> from the supplied components. 
        /// This constructor should only be called within the base class.
        /// </summary>
        /// <param name="file">
        /// 
        /// </param>
        /// <param name="document">
        /// 
        /// </param>
        /// <param name="documentRelations">
        /// 
        /// </param>
        /// <param name="contentTypes">
        /// 
        /// </param>
        /// <param name="footnotes">
        /// 
        /// </param>
        /// <param name="foonoteRelations">
        /// 
        /// </param>
        /// <param name="charts">
        /// 
        /// </param>
        /// <param name="footnoteId">
        /// 
        /// </param>
        /// <param name="footnoteRelationId">
        /// 
        /// </param>
        /// <param name="documentRelationId">
        /// 
        /// </param>
        private OpenXmlVisitor([NotNull] DocxFilePath file, [NotNull] XElement document, [NotNull] XElement documentRelations, [NotNull] XElement contentTypes, [NotNull] XElement footnotes, [NotNull] XElement foonoteRelations, [NotNull] IEnumerable<ChartInformation> charts, int footnoteId, int footnoteRelationId, int documentRelationId)
        {
            File = file;
            Document = document.Clone();
            DocumentRelations = documentRelations.Clone();
            ContentTypes = contentTypes.Clone();
            Footnotes = footnotes.Clone();
            FootnoteRelations = foonoteRelations.Clone();
            Charts = charts.Select(x => new ChartInformation(x.Name, x.Chart.Clone())).ToImmutableArray();
            FootnoteRelationId = footnoteRelationId;
            DocumentRelationId = documentRelationId;
        }

        /// <summary>
        /// Saves the current visitor to the <see cref="DocxFilePath"/>.
        /// </summary>
        /// <param name="resultPath">
        /// The path to which modified parts are written.
        /// </param>
        public void Save([NotNull] DocxFilePath resultPath)
        {
            Document.WriteInto(resultPath, "word/document.xml");
            Footnotes.WriteInto(resultPath, "word/footnotes.xml");
            ContentTypes.WriteInto(resultPath, "[Content_Types].xml");
            DocumentRelations.WriteInto(resultPath, "word/_rels/document.xml.rels");
            FootnoteRelations.WriteInto(resultPath, "word/_rels/footnotes.xml.rels");
            foreach (ChartInformation item in Charts)
            {
                item.Chart.WriteInto(resultPath, $"word/{item.Name}");
            }
        }

        /// <summary>
        /// Visit and join the component documents into the <see cref="OpenXmlVisitor"/>.
        /// </summary>
        /// <param name="files">
        /// The files to visit.
        /// </param>
        /// <exception cref="ArgumentNullException"/>
        [Pure]
        [NotNull]
        public virtual OpenXmlVisitor Visit([NotNull][ItemNotNull] IEnumerable<DocxFilePath> files)
        {
            if (files is null)
            {
                throw new ArgumentNullException(nameof(files));
            }

            return files.Aggregate(this, (current, next) => current.Visit(next));
        }


        /// <summary>
        /// Visit and join the component document into the <see cref="OpenXmlVisitor"/>.
        /// </summary>
        /// <param name="file">
        /// The files to visit.
        /// </param>
        /// <exception cref="ArgumentNullException"/>
        [Pure]
        [NotNull]
        public virtual OpenXmlVisitor Visit([NotNull] DocxFilePath file)
        {
            if (file is null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            OpenXmlVisitor subject = new OpenXmlVisitor(file);
            OpenXmlVisitor documentVisitor = VisitDocument(subject);
            OpenXmlVisitor footnoteVisitor = VisitFootnotes(documentVisitor, FootnoteId);
            OpenXmlVisitor footnoteHyperlinkVisitor = VisitFootnoteHyperlinks(footnoteVisitor, FootnoteRelationId);
            OpenXmlVisitor documentHyperlinkVisitor = VisitDocumentHyperlinks(footnoteHyperlinkVisitor, DocumentRelationId);
            OpenXmlVisitor chartsVisitor = VisitCharts(documentHyperlinkVisitor);
            
            XElement document =
                new XElement(
                    Document.Name,
                    Document.Attributes(),
                    new XElement(
                        Document.Elements().First().Name,
                        Document.Elements().First().Elements(),
                        chartsVisitor.Document
                                     .Elements()
                                     .First()
                                     .Elements()));

            XElement footnotes =
                new XElement(
                    Footnotes.Name,
                    Footnotes.Attributes(),
                    Footnotes.Elements()
                             .Union(
                                 chartsVisitor.Footnotes.Elements(),
                                 XNode.EqualityComparer));

            XElement footnoteRelations =
                new XElement(
                    FootnoteRelations.Name,
                    FootnoteRelations.Attributes(),
                    FootnoteRelations.Elements()
                                     .Union(
                                         chartsVisitor.FootnoteRelations.Elements(),
                                         XNode.EqualityComparer));

            XElement documentRelations =
                new XElement(
                    DocumentRelations.Name,
                    DocumentRelations.Attributes(),
                    DocumentRelations.Elements()
                                     .Union(
                                         chartsVisitor.DocumentRelations.Elements(),
                                         XNode.EqualityComparer));

            XElement contentTypes =
                new XElement(
                    ContentTypes.Name,
                    ContentTypes.Attributes(),
                    ContentTypes.Elements()
                                .Union(
                                    chartsVisitor.ContentTypes.Elements(),
                                    XNode.EqualityComparer));

            IEnumerable<ChartInformation> charts =
                Charts.Union(
                    chartsVisitor.Charts,
                    ChartInformation.Comparer);

            int footnoteId =
                chartsVisitor.FootnoteId;

            int footnoteRelationId =
                chartsVisitor.FootnoteRelationId;

            int documentRelationId =
                chartsVisitor.DocumentRelationId;

            return
                new OpenXmlVisitor(
                    File,
                    document,
                    documentRelations,
                    contentTypes,
                    footnotes,
                    footnoteRelations,
                    charts,
                    footnoteId,
                    footnoteRelationId,
                    documentRelationId);
        }

        /// <summary>
        /// Visit the <see cref="Document"/> of the subject.
        /// </summary>
        /// <param name="subject">
        /// The <see cref="OpenXmlVisitor"/> to visit.
        /// </param>
        /// <returns>
        /// A new <see cref="OpenXmlVisitor"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException"/>
        [Pure]
        [NotNull]
        protected virtual OpenXmlVisitor VisitDocument([NotNull] OpenXmlVisitor subject)
        {
            if (subject is null)
            {
                throw new ArgumentNullException(nameof(subject));
            }

            return new OpenXmlVisitor(subject);
        }

        /// <summary>
        /// Visit the <see cref="Footnotes"/> of the subject.
        /// </summary>
        /// <param name="subject">
        /// The <see cref="OpenXmlVisitor"/> to visit.
        /// </param>
        /// <param name="footnoteId">
        /// The current footnote identifier.
        /// </param>
        /// <returns>
        /// A new <see cref="OpenXmlVisitor"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException"/>
        [Pure]
        [NotNull]
        protected virtual OpenXmlVisitor VisitFootnotes([NotNull] OpenXmlVisitor subject, int footnoteId)
        {
            if (subject is null)
            {
                throw new ArgumentNullException(nameof(subject));
            }

            return new OpenXmlVisitor(subject);
        }

        /// <summary>
        /// Visit the <see cref="Document"/> and <see cref="DocumentRelations"/> of the subject to modify hyperlinks in the main document.
        /// </summary>
        /// <param name="subject">
        /// The <see cref="OpenXmlVisitor"/> to visit.
        /// </param>
        /// <param name="documentRelationId">
        /// The current document relationship identifier.
        /// </param>
        /// <returns>
        /// A new <see cref="OpenXmlVisitor"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException"/>
        [Pure]
        [NotNull]
        protected virtual OpenXmlVisitor VisitDocumentHyperlinks([NotNull] OpenXmlVisitor subject, int documentRelationId)
        {
            if (subject is null)
            {
                throw new ArgumentNullException(nameof(subject));
            }

            return new OpenXmlVisitor(subject);
        }

        /// <summary>
        /// Visit the <see cref="Footnotes"/> and <see cref="FootnoteRelations"/> of the subject to modify hyperlinks in the main document.
        /// </summary>
        /// <param name="subject">
        /// The <see cref="OpenXmlVisitor"/> to visit.
        /// </param>
        /// <param name="footnoteRelationId">
        /// The current footnote relationship identifier.
        /// </param>
        /// <returns>
        /// A new <see cref="OpenXmlVisitor"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException"/>
        [Pure]
        [NotNull]
        protected virtual OpenXmlVisitor VisitFootnoteHyperlinks([NotNull] OpenXmlVisitor subject, int footnoteRelationId)
        {
            if (subject is null)
            {
                throw new ArgumentNullException(nameof(subject));
            }

            return new OpenXmlVisitor(subject);
        }

        /// <summary>
        /// Visit the <see cref="Charts"/> and <see cref="DocumentRelations"/> of the subject to modify charts in the document.
        /// </summary>
        /// <param name="subject">
        /// The <see cref="OpenXmlVisitor"/> to visit.
        /// </param>
        /// <returns>
        /// A new <see cref="OpenXmlVisitor"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException"/>
        [Pure]
        [NotNull]
        protected virtual OpenXmlVisitor VisitCharts([NotNull] OpenXmlVisitor subject)
        {
            if (subject is null)
            {
                throw new ArgumentNullException(nameof(subject));
            }

            return new OpenXmlVisitor(subject);
        }
    }
}