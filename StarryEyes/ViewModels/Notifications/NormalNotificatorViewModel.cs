﻿using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Media;
using Livet;
using Livet.Messaging.Windows;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Models.Subsystems.Notifications.UI;
using StarryEyes.Nightmare.Windows.Forms;
using StarryEyes.Views;
using StarryEyes.Views.Notifications;

namespace StarryEyes.ViewModels.Notifications
{
    public class NormalNotificatorViewModel : ViewModel
    {
        public static readonly List<bool> _slots = new List<bool>();

        public static void Initialize()
        {
            NormalNotificator.Instance.OnStatusReceived += Instance_OnStatusReceived;
            NormalNotificator.Instance.OnFavorited += Instance_OnFavorited;
            NormalNotificator.Instance.OnMentionReceived += Instance_OnMentionReceived;
            NormalNotificator.Instance.OnMessageReceived += Instance_OnMessageReceived;
            NormalNotificator.Instance.OnRetweeted += Instance_OnRetweeted;
            NormalNotificator.Instance.OnUserFollowed += Instance_OnUserFollowed;
        }

        static void Instance_OnUserFollowed(TwitterUser arg1, TwitterUser arg2)
        {
            Show(new NormalNotificatorViewModel(MetroColors.Cobalt,
                arg1, "followed", "@" + arg2.ScreenName + Environment.NewLine + arg2.Name));
        }

        static void Instance_OnFavorited(TwitterUser arg1, TwitterStatus arg2)
        {
            Show(new NormalNotificatorViewModel(MetroColors.Amber,
                arg1, "favorited", arg2.ToString()));
        }

        static void Instance_OnRetweeted(TwitterUser arg1, TwitterStatus arg2)
        {
            Show(new NormalNotificatorViewModel(MetroColors.Emerald,
                arg1, "retweeted", arg2.ToString()));
        }

        static void Instance_OnStatusReceived(TwitterStatus obj)
        {
            Show(new NormalNotificatorViewModel(MetroColors.Cyan,
                obj.User, "@" + obj.User.ScreenName, obj.GetEntityAidedText()));
        }

        static void Instance_OnMessageReceived(TwitterStatus obj)
        {
            Show(new NormalNotificatorViewModel(MetroColors.Magenta,
                obj.User, "message from @" + obj.User.ScreenName, obj.GetEntityAidedText()));
        }

        static void Instance_OnMentionReceived(TwitterStatus obj)
        {
            Show(new NormalNotificatorViewModel(MetroColors.Orange,
                obj.User, "mention from @" + obj.User.ScreenName, obj.GetEntityAidedText()));
        }

        static void Show(NormalNotificatorViewModel dataContext)
        {
            DispatcherHolder.Enqueue(() =>
            {
                var mwnd = Application.Current.MainWindow;
                if (mwnd != null && !mwnd.IsActive)
                {
                    dataContext.ReleaseSlot();
                    return;
                }
                new NormalNotificatorView
                {
                    DataContext = dataContext
                }.Show();
            });
        }

        private readonly Color _background;
        private readonly TwitterUser _user;
        private readonly string _header;
        private readonly string _description;
        private readonly int _slotIndex;
        private readonly int _left;
        private readonly int _top;

        public NormalNotificatorViewModel(Color background, TwitterUser user, string header, string description)
        {
            this._background = background;
            this._user = user;
            this._header = header;
            this._description = description;
            // acquire slot
            lock (_slots)
            {
                _slotIndex = 0;
                while (_slotIndex < _slots.Count)
                {
                    if (!_slots[_slotIndex]) break;
                    _slotIndex++;
                }
                if (_slotIndex < _slots.Count)
                {
                    _slots[_slotIndex] = true;
                }
                else
                {
                    _slots.Add(true);
                }
            }
            var bound = Screen.PrimaryScreen.WorkingArea;
            var wh = 80;
            var ww = 300;
            var ipl = (int)Math.Ceiling(bound.Height / wh);
            if (ipl == 0)
            {
                return;
            }
            _left = (int)(bound.Width - ww * (this._slotIndex / ipl + 1));
            _top = (int)(bound.Height - wh * (this._slotIndex % ipl + 1));
            System.Diagnostics.Debug.WriteLine("#N - " + _slotIndex + " / " + _left + ", " + _top);
        }

        public int Left
        {
            get { return _left; }
        }

        public int Top
        {
            get { return _top; }
        }

        public Color Background
        {
            get { return this._background; }
        }

        public Brush BackgroundBrush
        {
            get { return new SolidColorBrush(Background); }
        }

        public TwitterUser User
        {
            get { return this._user; }
        }

        public Uri UserImage
        {
            get { return User.ProfileImageUri; }
        }

        public string Header
        {
            get { return this._header; }
        }

        public string Description
        {
            get { return this._description; }
        }

        public void Shown()
        {
            Observable.Timer(TimeSpan.FromSeconds(3))
                      .Subscribe(_ =>
                      {
                          this.Messenger.RaiseAsync(new WindowActionMessage(WindowAction.Close));
                          ReleaseSlot();
                      });
        }

        public void ReleaseSlot()
        {

            lock (_slots)
            {
                _slots[_slotIndex] = false;
            }
        }
    }
}
