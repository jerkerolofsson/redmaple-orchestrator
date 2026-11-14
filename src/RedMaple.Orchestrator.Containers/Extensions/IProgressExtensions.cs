using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace System
{
    internal class NullProgress<T> : IProgress<T>
    {
        public void Report(T value) { }
    }

    public static class IProgressExtensions
    {
        extension<T>(IProgress<T> progress)
        {
            /// <summary>
            /// Gets a null progress instance that discards all progress reports.
            /// </summary>
            /// <typeparam name="T">The type of progress data.</typeparam>
            /// <returns>A new NullProgress&lt;T&gt; instance.</returns>
            public static IProgress<T> NullProgress => new NullProgress<T>();

        }
    }
}
