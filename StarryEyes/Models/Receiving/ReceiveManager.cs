﻿using System;
using System.Collections.Generic;
using Cadena.Engine;
using StarryEyes.Models.Accounting;
using StarryEyes.Models.Receiving.Managers;

namespace StarryEyes.Models.Receiving
{
    public static class ReceiveManager
    {
        private static SearchReceiveManager _searchReceiveManager;
        private static ListReceiveManager _listReceiveManager;
        private static ListMemberReceiveManager _listMemberReceiveManager;
        private static UserReceiveManager _userReceiveManager;
        private static StreamTrackReceiveManager _streamTrackReceiveManager;

        public static readonly ReceiveEngine ReceiveEngine = new ReceiveEngine();

        public static event Action<long> UserStreamsConnectionStateChanged;

        public static event Action<Tuple<ListInfo, IEnumerable<long>>> ListMemberChanged;

        public static void Initialize()
        {
            BehaviorLogger.Log("RM", "initializing...");
            _userReceiveManager = new UserReceiveManager();
            _searchReceiveManager = new SearchReceiveManager();
            _listReceiveManager = new ListReceiveManager();
            _listMemberReceiveManager = new ListMemberReceiveManager();
            _listMemberReceiveManager.ListMemberChanged += li => ListMemberChanged?.Invoke(li);
            _streamTrackReceiveManager = new StreamTrackReceiveManager(_userReceiveManager);
            _userReceiveManager.ConnectionStateChanged += arg => UserStreamsConnectionStateChanged?.Invoke(arg.Id);
            BehaviorLogger.Log("RM", "init.");
            ReceiveEngine.Begin();
        }

        public static void RegisterSearchQuery(string query, ICollection<long> receiveCache)
        {
            _searchReceiveManager.RegisterSearchQuery(query, receiveCache);
        }

        public static void UnregisterSearchQuery(string query, ICollection<long> receiveCache)
        {
            _searchReceiveManager.UnregisterSearchQuery(query, receiveCache);
        }

        public static void RegisterStreamingQuery(string query)
        {
            _streamTrackReceiveManager.AddTrackKeyword(query);
        }

        public static void UnregisterStreamingQuery(string query)
        {
            _streamTrackReceiveManager.RemoveTrackKeyword(query);
        }

        public static void RegisterList(ListInfo info)
        {
            RegisterListMember(info);
            _listReceiveManager.StartReceive(info);
        }

        public static void RegisterList(string receiver, ListInfo info)
        {
            RegisterListMember(receiver, info);
            _listReceiveManager.StartReceive(info, receiver);
        }

        public static void RegisterList(TwitterAccount authInfo, ListInfo info)
        {
            RegisterListMember(authInfo, info);
            _listReceiveManager.StartReceive(info, authInfo);
        }

        public static void RegisterListMember(ListInfo info)
        {
            _listMemberReceiveManager.StartReceive(info);
        }

        public static void RegisterListMember(string receiver, ListInfo info)
        {
            _listMemberReceiveManager.StartReceive(receiver, info);
        }

        public static void RegisterListMember(TwitterAccount authInfo, ListInfo info)
        {
            _listMemberReceiveManager.StartReceive(authInfo, info);
        }

        public static void UnregisterList(ListInfo info)
        {
            UnregisterListMember(info);
            _listReceiveManager.StopReceive(info);
        }

        public static void UnregisterListMember(ListInfo info)
        {
            _listMemberReceiveManager.StopReceive(info);
        }

        public static void ReconnectUserStreams(long id)
        {
            _userReceiveManager.ReconnectStream(id);
        }

        public static void ReconnectUserStreams()
        {
            _userReceiveManager.ReconnectAllStreams();
        }

        public static UserStreamsConnectionState GetConnectionState(long id)
        {
            return _userReceiveManager.GetConnectionState(id);
        }
    }

    public enum UserStreamsConnectionState
    {
        Invalid = -1,
        Disconnected,
        Connecting,
        Connected,
        Waiting
    }
}