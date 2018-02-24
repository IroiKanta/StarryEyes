﻿using System;
using System.Collections.Generic;
using System.Linq;
using Cadena.Data;
using StarryEyes.Models.Databases;

namespace StarryEyes.Filters.Expressions.Operators
{
    /// <summary>
    /// Contains as member
    /// </summary>
    public class FilterOperatorContains : FilterTwoValueOperator
    {
        protected override string OperatorString => "contains";

        private bool CompareAsString()
        {
            // lv: string -> string
            // lv: set|string -> rv: string -> string
            var lst = LeftValue.SupportedTypes.Memoize();
            var rst = RightValue.SupportedTypes.Memoize();
            return !lst.Contains(FilterExpressionType.Set) ||
                   (lst.Contains(FilterExpressionType.String) &&
                    !rst.Contains(FilterExpressionType.Set) && !rst.Contains(FilterExpressionType.Numeric));
        }

        public override Func<TwitterStatus, bool> GetBooleanValueProvider()
        {
            if (CompareAsString())
            {
                var haystack = LeftValue.GetStringValueProvider();
                var needle = RightValue.GetStringValueProvider();
                return t =>
                {
                    var h = haystack(t);
                    var n = needle(t);
                    if (h == null || n == null) return false;
                    return h.IndexOf(n, GetStringComparison()) >= 0;
                };
            }
            var lsp = LeftValue.GetSetValueProvider();
            if (RightValue.SupportedTypes.Contains(FilterExpressionType.Numeric))
            {
                var rnp = RightValue.GetNumericValueProvider();
                return _ => lsp(_).Contains(rnp(_));
            }
            var rsp = RightValue.GetSetValueProvider();
            return _ =>
            {
                var ls = lsp(_);
                return rsp(_).Any(ls.Contains);
            };
        }

        public override string GetBooleanSqlQuery()
        {
            if (CompareAsString())
            {
                return GetStringComparison() == StringComparison.CurrentCultureIgnoreCase
                    ? "LOWER(" + LeftValue.GetStringSqlQuery() + ") LIKE LOWER('%" +
                      RightValue.GetStringSqlQuery().Unwrap() + "%') escape '\\'"
                    : LeftValue.GetStringSqlQuery() + " LIKE '%" + RightValue.GetStringSqlQuery().Unwrap() +
                      "%' escape '\\'";
            }
            var lq = LeftValue.GetSetSqlQuery();
            if (RightValue.SupportedTypes.Contains(FilterExpressionType.Numeric))
            {
                return RightValue.GetNumericSqlQuery() + " IN " + lq;
            }
            var rq = RightValue.GetSetSqlQuery();
            // check intersection
            return "exists (" + lq.Unparenthesis() + " intersect " + rq.Unparenthesis() + ")";
        }

        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.Boolean; }
        }
    }

    /// <summary>
    /// Contained as member
    /// </summary>
    public class FilterOperatorContainedBy : FilterTwoValueOperator
    {
        protected override string OperatorString => "in";

        public override Func<TwitterStatus, bool> GetBooleanValueProvider()
        {
            var rsp = RightValue.GetSetValueProvider();
            if (LeftValue.SupportedTypes.Contains(FilterExpressionType.Numeric))
            {
                var lnp = LeftValue.GetNumericValueProvider();
                return _ => rsp(_).Contains(lnp(_));
            }
            var lsp = LeftValue.GetSetValueProvider();
            return _ =>
            {
                var rs = rsp(_);
                return lsp(_).Any(rs.Contains);
            };
        }

        public override string GetBooleanSqlQuery()
        {
            var rq = RightValue.GetSetSqlQuery();
            if (LeftValue.SupportedTypes.Contains(FilterExpressionType.Numeric))
            {
                return LeftValue.GetNumericSqlQuery() + " IN " + rq;
            }
            var lq = LeftValue.GetSetSqlQuery();
            // check intersection
            return "exists (" + lq.Unparenthesis() + " intersect " + rq.Unparenthesis() + ")";
        }

        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.Boolean; }
        }
    }
}