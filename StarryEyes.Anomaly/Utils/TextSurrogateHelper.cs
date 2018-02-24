﻿using System;
using System.Linq;
using JetBrains.Annotations;

namespace StarryEyes.Anomaly.Utils
{
    public static class TextSurrogateHelper
    {
        /// <summary>
        /// Get substring with considering surrogate pairs.
        /// </summary>
        /// <param name="str">source string</param>
        /// <param name="startIndex">surrogate-pair considered start index</param>
        /// <param name="length">surrogate-pair considered length</param>
        /// <returns>substring</returns>
        /// <remarks>This method cuts string with surrogate-pair considered parameters.</remarks>
        public static string SurrogatedSubstring([CanBeNull] this string str, int startIndex, int length = -1)
        {
            if (str == null) throw new ArgumentNullException("str");
            try
            {
                var ss = str.Substring(0, startIndex).UnsurrogatedLength();
                if (length == -1)
                {
                    return str.Substring(ss);
                }
                var sl = str.Substring(ss, length).UnsurrogatedLength();
                return str.Substring(ss, sl);
            }
            catch (ArgumentOutOfRangeException aoex)
            {
                throw new ArgumentOutOfRangeException(
                    "Exception has thrown during slicing text: " + str + " with " + startIndex + "-" + length,
                    aoex);
            }
        }

        /// <summary>
        /// Get decode length considered surrogate pairs.
        /// </summary>
        /// <param name="str">source string</param>
        /// <returns>invert surrogate-considered length</returns>
        public static int UnsurrogatedLength([CanBeNull] this string str)
        {
            if (str == null) throw new ArgumentNullException("str");
            return str.Sum(c => Char.IsHighSurrogate(c) ? 2 : 1);
        }

        /// <summary>
        /// Get length considered surrogate pairs.
        /// </summary>
        /// <param name="str">source string</param>
        /// <returns>surrogate-considered length</returns>
        public static int SurrogatedLength([CanBeNull] this string str)
        {
            if (str == null) throw new ArgumentNullException("str");
            return str.Sum(c => Char.IsHighSurrogate(c) ? 0 : 1);
        }
    }
}
