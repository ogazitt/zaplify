using System;
using System.Threading;

namespace BuiltSteady.Zaplify.ServiceUtilities.Bing
{
    internal sealed class BingResult : IAsyncResult
    {
        #region Private data

        private readonly object _state;

        #endregion Private data

        #region Constructor

        public BingResult(object state)
        {
            _state = state;
        }

        #endregion Constructor

        #region Properties

        public IAsyncResult InnerResult { get; set; }

        #endregion Properties

        #region IAsyncResult implementation

        public object AsyncState { get { return _state; } }
        public WaitHandle AsyncWaitHandle { get { return InnerResult.AsyncWaitHandle; } }
        public bool CompletedSynchronously { get { return InnerResult.CompletedSynchronously; } }
        public bool IsCompleted { get { return InnerResult.IsCompleted; } }

        #endregion IAsyncResult implementation
    }
}
