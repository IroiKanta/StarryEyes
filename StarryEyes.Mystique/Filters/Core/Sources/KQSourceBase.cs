﻿using System;
using StarryEyes.SweetLady.DataModel;

namespace StarryEyes.Mystique.Filters.Core
{
    /// <summary>
    /// Tweets source of status
    /// </summary>
    public abstract class KQSourceBase : IKQueryElement
    {
        public abstract string ToQuery();

        public abstract Func<TwitterStatus, bool> GetEvaluator();
    }
}
