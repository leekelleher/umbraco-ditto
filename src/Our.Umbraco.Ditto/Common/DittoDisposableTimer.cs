﻿namespace Our.Umbraco.Ditto
{
    using System;
    using System.Web;
    using global::Umbraco.Core;

    internal class DittoDisposableTimer : DisposableTimer
    {
        public DittoDisposableTimer(Action<long> callback)
            : base(callback)
        {
        }

        public static new DisposableTimer DebugDuration<T>(string startMessage)
        {
            if (HttpContext.Current != null && HttpContext.Current.IsDebuggingEnabled)
            {
                return DisposableTimer.DebugDuration<T>(startMessage);
            }

            return new DittoDisposableTimer((x) => { });
        }
    }
}