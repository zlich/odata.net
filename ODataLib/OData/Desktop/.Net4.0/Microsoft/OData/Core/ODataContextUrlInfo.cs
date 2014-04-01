//   OData .NET Libraries
//   Copyright (c) Microsoft Corporation
//   All rights reserved. 

//   Licensed under the Apache License, Version 2.0 (the ""License""); you may not use this file except in compliance with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0 

//   THIS CODE IS PROVIDED ON AN  *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT. 

//   See the Apache Version 2.0 License for specific language governing permissions and limitations under the License.

namespace Microsoft.OData.Core
{
    #region Namespaces

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Microsoft.OData.Core.Metadata;
    using Microsoft.OData.Core.UriParser;
    using Microsoft.OData.Core.UriParser.Semantic;
    using Microsoft.OData.Edm;
    #endregion Namespaces

    /// <summary>
    /// Class representing required infomation for context URL.
    /// </summary>
    internal sealed class ODataContextUrlInfo
    {
        /// <summary>Whether target is contained</summary>
        private bool isContained;

        /// <summary>The navigation soruce for current target</summary>
        private string navigationSource;

        /// <summary>ODataUri information for context Url</summary>
        private ODataUri odataUri;

        /// <summary>Name of navigation path used for building context Url</summary>
        internal string NavigationPath
        {
            get
            {
                string navigationPath = null;
                if (this.isContained && this.odataUri != null && this.odataUri.Path != null)
                {
                    ODataPath odataPath = this.odataUri.Path.TrimEndingTypeSegment().TrimEndingKeySegment();
                    if (!(odataPath.LastSegment is NavigationPropertySegment))
                    {
                        throw new ODataException(Strings.ODataContextUriBuilder_ODataPathInvalidForContainedElement(odataPath.ToResourcePathString()));
                    }

                    navigationPath = odataPath.ToResourcePathString();
                }

                return navigationPath ?? this.navigationSource;
            }
        }

        /// <summary>ResourcePath used for building context Url</summary>
        internal string ResourcePath
        {
            get
            {
                if (this.odataUri != null && this.odataUri.Path != null && this.odataUri.Path.IsIndividualProperty())
                {
                    return this.odataUri.Path.ToResourcePathString();
                }

                return string.Empty;
            }
        }

        /// <summary>Query clause used for building context Url</summary>
        internal string QueryClause
        {
            get
            {
                return this.odataUri != null ? CreateSelectExpandContextUriSegment(this.odataUri.SelectAndExpand) : null;
            }
        }

        /// <summary>Entity type name used for building context Url</summary>
        internal string TypeName { get; private set; }

        /// <summary>TypeCast segment used for building context Url</summary>
        internal string TypeCast { get; private set; }

        /// <summary>Whether context Url is for single item used for building context Url</summary>
        internal bool IncludeFragmentItemSelector { get; private set; }

        /// <summary>
        /// Create ODataContextUrlInfo for OdataValue.
        /// </summary>
        /// <param name="value">The ODataValue to be used.</param>
        /// <param name="odataUri">The odata uri info for current query.</param>
        /// <returns>The generated ODataContextUrlInfo.</returns>
        internal static ODataContextUrlInfo Create(ODataValue value, ODataUri odataUri = null)
        {
            return new ODataContextUrlInfo()
            {
                TypeName = GetTypeNameForValue(value),
                odataUri = odataUri
            };
        }

        /// <summary>
        /// Create ODataContextUrlInfo from ODataCollectionStartSerializationInfo
        /// </summary>
        /// <param name="info">The ODataCollectionStartSerializationInfo to be used.</param>
        /// <param name="itemTypeReference">ItemTypeReference specifying element type.</param>
        /// <returns>The generated ODataContextUrlInfo.</returns>
        internal static ODataContextUrlInfo Create(ODataCollectionStartSerializationInfo info, IEdmTypeReference itemTypeReference)
        {
            string collectionTypeName = null;
            if (info != null)
            {
                collectionTypeName = info.CollectionTypeName;
            }
            else if (itemTypeReference != null)
            {
                collectionTypeName = EdmLibraryExtensions.GetCollectionTypeName(itemTypeReference.ODataFullName());
            }

            return new ODataContextUrlInfo()
            {
                TypeName = collectionTypeName,
            };
        }

        /// <summary>
        /// Create ODataContextUrlInfo from basic information
        /// </summary>
        /// <param name="navigationSource">Navigation source for current element.</param>\
        /// <param name="expectedEntityTypeName">The expectedEntity for current element.</param>
        /// <param name="isSingle">Whether target is single item.</param>
        /// <param name="odataUri">The odata uri info for current query.</param>
        /// <returns>The generated ODataContextUrlInfo.</returns>
        internal static ODataContextUrlInfo Create(IEdmNavigationSource navigationSource, string expectedEntityTypeName, bool isSingle, ODataUri odataUri)
        {
            EdmNavigationSourceKind kind = navigationSource.NavigationSourceKind();
            string navigationSourceEntityType = navigationSource.EntityType().FullName();
            return new ODataContextUrlInfo()
            {
                isContained = kind == EdmNavigationSourceKind.ContainedEntitySet,
                navigationSource = navigationSource.Name,
                TypeCast = navigationSourceEntityType == expectedEntityTypeName ? null : expectedEntityTypeName,
                TypeName = navigationSourceEntityType,
                IncludeFragmentItemSelector = isSingle && kind != EdmNavigationSourceKind.Singleton,
                odataUri = odataUri
            };
        }

        /// <summary>
        /// Create ODataContextUrlInfo from ODataFeedAndEntryTypeContext
        /// </summary>
        /// <param name="typeContext">The ODataFeedAndEntryTypeContext to be used.</param>
        /// <param name="isSingle">Whether target is single item.</param>
        /// <param name="odataUri">The odata uri info for current query.</param>
        /// <returns>The generated ODataContextUrlInfo.</returns>
        internal static ODataContextUrlInfo Create(ODataFeedAndEntryTypeContext typeContext, bool isSingle, ODataUri odataUri = null)
        {
            Debug.Assert(typeContext != null, "typeContext != null");

            return new ODataContextUrlInfo()
                {
                    isContained = typeContext.NavigationSourceKind == EdmNavigationSourceKind.ContainedEntitySet,
                    navigationSource = typeContext.NavigationSourceName,
                    TypeCast = typeContext.NavigationSourceEntityTypeName == typeContext.ExpectedEntityTypeName ? null : typeContext.ExpectedEntityTypeName,
                    TypeName = typeContext.NavigationSourceEntityTypeName,
                    IncludeFragmentItemSelector = isSingle && typeContext.NavigationSourceKind != EdmNavigationSourceKind.Singleton,
                    odataUri = odataUri
                };
        }

        /// <summary>
        /// Gets the type name based on the given odata value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The type name for the context URI.</returns>
        private static string GetTypeNameForValue(ODataValue value)
        {
            if (value == null)
            {
                return null;
            }

            // special identifier for null values.
            if (value.IsNullValue)
            {
                return ODataConstants.ContextUriFragmentNull;
            }

            var typeAnnotation = value.GetAnnotation<SerializationTypeNameAnnotation>();
            if (typeAnnotation != null && !string.IsNullOrEmpty(typeAnnotation.TypeName))
            {
                return typeAnnotation.TypeName;
            }

            var complexValue = value as ODataComplexValue;
            if (complexValue != null)
            {
                return complexValue.TypeName;
            }

            var collectionValue = value as ODataCollectionValue;
            if (collectionValue != null)
            {
                return EdmLibraryExtensions.GetCollectionTypeFullName(collectionValue.TypeName);
            }

            var enumValue = value as ODataEnumValue;
            if (enumValue != null)
            {
                return enumValue.TypeName;
            }

            ODataPrimitiveValue primitive = value as ODataPrimitiveValue;
            if (primitive == null)
            {
                Debug.Assert(value is ODataStreamReferenceValue, "value is ODataStreamReferenceValue");
                throw new ODataException(Strings.ODataContextUriBuilder_StreamValueMustBePropertiesOfODataEntry);
            }

            return EdmLibraryExtensions.GetPrimitiveTypeReference(primitive.Value.GetType()).ODataFullName();
        }

        #region SelectAndExpand Convert
        /// <summary>
        /// Build the expand clause for a given level in the selectExpandClause
        /// </summary>
        /// <param name="selectExpandClause">the current level select expand clause</param>
        /// <returns>the select and expand segment for context url in this level.</returns>
        private static string CreateSelectExpandContextUriSegment(SelectExpandClause selectExpandClause)
        {
            if (selectExpandClause != null)
            {
                string contextUri;
                selectExpandClause.Traverse(ProcessSubExpand, CombineSelectAndExpandResult, out contextUri);
                if (!string.IsNullOrEmpty(contextUri))
                {
                    return ODataConstants.ContextUriProjectionStart + contextUri + ODataConstants.ContextUriProjectionEnd;
                }
            }

            return string.Empty;
        }

        /// <summary>Process sub expand node, contact with subexpand result</summary>
        /// <param name="expandNode">The current expanded node.</param>
        /// <param name="subExpand">Generated sub expand node.</param>
        /// <returns>The generated expand string.</returns>
        private static string ProcessSubExpand(string expandNode, string subExpand)
        {
            return string.IsNullOrEmpty(subExpand) ? null : expandNode + ODataConstants.ContextUriProjectionStart + subExpand + ODataConstants.ContextUriProjectionEnd;
        }

        /// <summary>Create combined result string using selected items list and expand items list.</summary>
        /// <param name="selectList">A list of selected item names.</param>
        /// <param name="expandList">A list of sub expanded item names.</param>
        /// <returns>The generated expand string.</returns>
        private static string CombineSelectAndExpandResult(IList<string> selectList, IList<string> expandList)
        {
            string currentExpandClause = string.Empty;
            if (selectList.Any())
            {
                currentExpandClause += String.Join(ODataConstants.ContextUriProjectionPropertySeparator, selectList.ToArray());
            }

            if (expandList.Any())
            {
                if (!string.IsNullOrEmpty(currentExpandClause))
                {
                    currentExpandClause += ODataConstants.ContextUriProjectionPropertySeparator;
                }

                currentExpandClause += String.Join(ODataConstants.ContextUriProjectionPropertySeparator, expandList.ToArray());
            }

            return currentExpandClause;
        }
        #endregion SelectAndExpand Convert
    }
}
