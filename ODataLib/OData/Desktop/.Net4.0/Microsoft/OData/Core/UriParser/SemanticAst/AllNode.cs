//   OData .NET Libraries
//   Copyright (c) Microsoft Corporation
//   All rights reserved. 

//   Licensed under the Apache License, Version 2.0 (the ""License""); you may not use this file except in compliance with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0 

//   THIS CODE IS PROVIDED ON AN  *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT. 

//   See the Apache Version 2.0 License for specific language governing permissions and limitations under the License.

namespace Microsoft.OData.Core.UriParser.Semantic
{
    #region Namespaces

    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using Microsoft.OData.Core.UriParser.TreeNodeKinds;
    using Microsoft.OData.Core.UriParser.Visitors;
    using Microsoft.OData.Edm;
    using Microsoft.OData.Edm.Library;

    #endregion Namespaces

    /// <summary>
    /// Query node representing an All query.
    /// </summary>
    public sealed class AllNode : LambdaNode
    {
        /// <summary>
        /// Create an AllNode
        /// </summary>
        /// <param name="rangeVariables">The name of the rangeVariables list.</param>
        public AllNode(Collection<RangeVariable> rangeVariables) : this(rangeVariables, null)
        {
            Debug.Assert(false, "Don't ever call this, its for backcompat");
        }

        /// <summary>
        /// Create an AllNode
        /// </summary>
        /// <param name="rangeVariables">The name of the rangeVariables list.</param>
        /// <param name="currentRangeVariable">The new range variable being added by this all node</param>
        public AllNode(Collection<RangeVariable> rangeVariables, RangeVariable currentRangeVariable)
            : base(rangeVariables, currentRangeVariable)
        {
        }

        /// <summary>
        /// The resource type of the single value this node represents.
        /// </summary>
        public override IEdmTypeReference TypeReference
        {
            get { return EdmCoreModel.Instance.GetBoolean(true); }
        }

        /// <summary>
        /// Gets the kind of this node.
        /// </summary>
        internal override InternalQueryNodeKind InternalKind
        {
            get
            {
                return InternalQueryNodeKind.All;
            }
        }

        /// <summary>
        /// Accept a <see cref="QueryNodeVisitor{T}"/> that walks a tree of <see cref="QueryNode"/>s.
        /// </summary>
        /// <typeparam name="T">Type that the visitor will return after visiting this token.</typeparam>
        /// <param name="visitor">An implementation of the visitor interface.</param>
        /// <returns>An object whose type is determined by the type parameter of the visitor.</returns>
        /// <exception cref="System.ArgumentNullException">Throws if the input visitor is null</exception>
        public override T Accept<T>(QueryNodeVisitor<T> visitor)
        {
            ExceptionUtils.CheckArgumentNotNull(visitor, "visitor");
            return visitor.Visit(this);
        }
    }
}
