﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using StarryEyes.Anomaly.Ext;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.Utils;

namespace StarryEyes.Anomaly.TwitterApi.Rest.Infrastructure
{
    public static class ResultHandlers
    {
        public static async Task<TwitterUser> ReadAsUserAsync(this HttpResponseMessage response)
        {
            var json = await response.ReadAsStringAsync();
            return await Task.Run(() => new TwitterUser(DynamicJson.Parse(json)));
        }

        public static async Task<TwitterStatus> ReadAsStatusAsync(this HttpResponseMessage response)
        {
            var json = await response.ReadAsStringAsync();
            return await Task.Run(() => new TwitterStatus(DynamicJson.Parse(json)));
        }

        public static async Task<TwitterList> ReadAsListAsync(this HttpResponseMessage response)
        {
            var json = await response.ReadAsStringAsync();
            return await Task.Run(() => new TwitterList(DynamicJson.Parse(json)));
        }

        public static async Task<IEnumerable<TwitterUser>> ReadAsUserCollectionAsync(
            this HttpResponseMessage response)
        {
            var json = await response.ReadAsStringAsync();
            return await ParseAndMapCollectionAsync(json, u => new TwitterUser(u));
        }

        public static async Task<IEnumerable<TwitterList>> ReadAsListCollectionAsync(
            this HttpResponseMessage response)
        {
            var json = await response.ReadAsStringAsync();
            return await ParseAndMapCollectionAsync(json, l => new TwitterList(l));
        }

        private static async Task<IEnumerable<T>> ParseAndMapCollectionAsync<T>(string json, Func<dynamic, T> factory)
        {
            return await Task.Run(
                () => (((dynamic[])DynamicJson.Parse(json))
                          .Select(list => (T)factory(list))));
        }


        public static async Task<IEnumerable<TwitterStatus>> ReadAsStatusCollectionAsync(
            this HttpResponseMessage response)
        {
            return await Task.Run(async () =>
            {
                var json = await response.ReadAsStringAsync();
                var parsed = DynamicJson.Parse(json);
                if (parsed.statuses())
                {
                    parsed = parsed.statuses;
                }
                return ((dynamic[])parsed).Select(status => new TwitterStatus(status));
            });
        }


        public static async Task<TwitterFriendship> ReadAsFriendshipAsync(
            this HttpResponseMessage response)
        {
            var json = await response.ReadAsStringAsync();
            return await Task.Run(() => new TwitterFriendship(DynamicJson.Parse(json)));
        }

        public static async Task<string> ReadAsStringAsync(this HttpResponseMessage response)
        {
            return await response.EnsureSuccessStatusCode().Content.ReadAsStringAsync();
        }

        public static async Task<ICursorResult<IEnumerable<long>>> ReadAsCursoredIdsAsync(
            this HttpResponseMessage response)
        {
            return await Task.Run(async () =>
            {
                var json = await response.ReadAsStringAsync();
                var parsed = DynamicJson.Parse(json);
                var converteds = ((string[])parsed.ids)
                    .Select(s => s.ParseLong());
                return new CursorResult<IEnumerable<long>>(
                    converteds,
                    parsed.previous_cursor_str, parsed.next_cursor_str);
            });
        }

        public static async Task<ICursorResult<IEnumerable<TwitterUser>>> ReadAsCursoredUsersAsync(
            this HttpResponseMessage response)
        {
            return await Task.Run(async () =>
            {
                var json = await response.ReadAsStringAsync();
                var parsed = DynamicJson.Parse(json);
                var converteds = ((dynamic[])parsed.users)
                    .Select(d => new TwitterUser(d));
                return new CursorResult<IEnumerable<TwitterUser>>(
                    converteds,
                    parsed.previous_cursor_str, parsed.next_cursor_str);
            });
        }
    }
}
