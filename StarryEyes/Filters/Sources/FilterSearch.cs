﻿using System;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using Cadena.Api.Parameters;
using Cadena.Api.Rest;
using Cadena.Data;
using StarryEyes.Albireo.Collections;
using StarryEyes.Models.Receiving;
using StarryEyes.Settings;

namespace StarryEyes.Filters.Sources
{
    public class FilterSearch : FilterSourceBase
    {
        private readonly AVLTree<long> _acceptIds = new AVLTree<long>();
        private readonly string _query;

        public FilterSearch(string query)
        {
            _query = query;
        }

        public override Func<TwitterStatus, bool> GetEvaluator()
        {
            // accept all status via web.
            return status =>
            {
                lock (_acceptIds)
                {
                    return _acceptIds.Contains(status.Id);
                }
            };
        }

        public override string GetSqlQuery()
        {
            return "0"; // always return 'false'
        }

        protected override IObservable<TwitterStatus> ReceiveSink(long? maxId)
        {
            System.Diagnostics.Debug.WriteLine("RECEIVESINK SEARCH QUERY: " + _query);
            var sp = new SearchParameter(_query, maxId: maxId,
                lang: String.IsNullOrWhiteSpace(Setting.SearchLanguage.Value)
                    ? null
                    : Setting.SearchLanguage.Value,
                locale: String.IsNullOrWhiteSpace(Setting.SearchLocale.Value)
                    ? null
                    : Setting.SearchLocale.Value);
            return Observable.Start(() => Setting.Accounts.GetRandomOne())
                             .Where(a => a != null)
                             .SelectMany(a => a.CreateAccessor().SearchAsync(
                                                   sp, CancellationToken.None)
                                               .ToObservable())
                             .SelectMany(s => s.Result)
                             .Do(s =>
                             {
                                 lock (_acceptIds)
                                 {
                                     _acceptIds.Add(s.Id);
                                 }
                             });
        }

        private bool _isActivated;

        public override void Activate()
        {
            if (_isActivated) return;
            _isActivated = true;
            ReceiveManager.RegisterSearchQuery(_query, _acceptIds);
        }

        public override void Deactivate()
        {
            if (!_isActivated) return;
            _isActivated = false;
            ReceiveManager.UnregisterSearchQuery(_query, _acceptIds);
        }

        public override string FilterKey => "search";

        public override string FilterValue => _query;
    }
}