﻿using System;
using System.Linq;
using Cadena.Data;
using StarryEyes.Filters.Expressions;
using StarryEyes.Globalization.Filters;
using StarryEyes.Models.Timelines.Tabs;

namespace StarryEyes.Filters.Sources
{
    /// <summary>
    /// General filter.
    /// </summary>
    public class FilterLocal : FilterSourceBase
    {
        private readonly string _tabName;

        private TabModel _tab;

        public FilterLocal()
        {
        }

        public FilterLocal(string tabName)
        {
            _tabName = tabName;
            ResolveTabReference();
        }

        public override Func<TwitterStatus, bool> GetEvaluator()
        {
            ResolveTabReference();
            return _tab?.FilterQuery == null
                ? FilterExpressionBase.Tautology
                : _tab.FilterQuery.GetEvaluator();
        }

        public override string GetSqlQuery()
        {
            ResolveTabReference();
            return _tab?.FilterQuery == null
                ? "1"
                : _tab.FilterQuery.GetSqlQuery();
        }

        public override string FilterKey => "local";

        public override string FilterValue => _tabName;

        private void ResolveTabReference()
        {
            if (string.IsNullOrEmpty(_tabName))
            {
                return;
            }

            if (_tab == null)
            {
                if (!TabManager.GetColumnInfoData().Any())
                {
                    // tabs are initializing, skip
                    return;
                }

                _tab = TabManager.GetColumnInfoData()
                                 .SelectMany(c => c.Tabs)
                                 .FirstOrDefault(t => t.Name == _tabName);

                if (_tab == null)
                {
                    throw new ArgumentException(FilterObjectResources.FilterLocalTabNotFound);
                }
            }

            if (EnumerableEx.Return(this)
                            .Expand(f => f._tab?.FilterQuery?.Sources.OfType<FilterLocal>() ??
                                         Enumerable.Empty<FilterLocal>()
                            )
                            .Skip(1)
                            .Any(f => f._tab == _tab)
            )
            {
                throw new ArgumentException(FilterObjectResources.FilterLocalTabIsRecursion);
            }
        }
    }
}