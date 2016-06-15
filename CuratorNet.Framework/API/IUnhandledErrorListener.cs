using System;

namespace Org.Apache.CuratorNet.Framework.API
{
    public interface IUnhandledErrorListener
    {
        /**
         * Called when an exception is caught in a background thread, handler, etc. Before this
         * listener is called, the error will have been logged and a {@link ConnectionState#LOST}
         * event will have been queued for any {@link ConnectionStateListener}s.
         *
         * @param message Source message
         * @param e exception
         */
        void unhandledError(String message, Exception e);
    }
}