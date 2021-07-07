﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.SqlServer.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;

namespace Microsoft.EntityFrameworkCore.SqlServer.Query.Internal
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public class SqlServerQueryableMethodTranslatingExpressionVisitor : RelationalQueryableMethodTranslatingExpressionVisitor
    {
        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public SqlServerQueryableMethodTranslatingExpressionVisitor(
            QueryableMethodTranslatingExpressionVisitorDependencies dependencies,
            RelationalQueryableMethodTranslatingExpressionVisitorDependencies relationalDependencies,
            QueryCompilationContext queryCompilationContext)
            : base(dependencies, relationalDependencies, queryCompilationContext)
        {
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        protected SqlServerQueryableMethodTranslatingExpressionVisitor(
            SqlServerQueryableMethodTranslatingExpressionVisitor parentVisitor)
            : base(parentVisitor)
        {
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        protected override QueryableMethodTranslatingExpressionVisitor CreateSubqueryVisitor()
            => new SqlServerQueryableMethodTranslatingExpressionVisitor(this);

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        protected override Expression VisitExtension(Expression extensionExpression)
        {
            if (extensionExpression is TemporalQueryRootExpression temporalQueryRootExpression)
            {
                // sql server model validator will throw if entity is mapped to multiple tables
                var table = temporalQueryRootExpression.EntityType.GetTableMappings().Single().Table;

                var temporalTableExpression = temporalQueryRootExpression switch
                {
                    TemporalRangeQueryRootExpression range => new TemporalTableExpression(
                        table,
                        range.From,
                        range.To,
                        range.TemporalOperationType),
                    TemporalAsOfQueryRootExpression asOf => new TemporalTableExpression(table, asOf.PointInTime),
                    // all
                    _ => new TemporalTableExpression(table),
                };

                var selectExpression = RelationalDependencies.SqlExpressionFactory.Select(
                    temporalQueryRootExpression.EntityType,
                    temporalTableExpression);

                return new ShapedQueryExpression(
                    selectExpression,
                    new RelationalEntityShaperExpression(
                        temporalQueryRootExpression.EntityType,
                        new ProjectionBindingExpression(
                            selectExpression,
                            new ProjectionMember(),
                            typeof(ValueBuffer)),
                        false));
            }

            return base.VisitExtension(extensionExpression);
        }
    }
}
