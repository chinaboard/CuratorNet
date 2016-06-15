namespace Org.Apache.CuratorNet.Framework.API
{
    public interface ErrorListenerPathAndBytesable<T> : IPathAndBytesable<T>
    {
        /**
         * Set an error listener for this background operation. If an exception
         * occurs while processing the call in the background, this listener will
         * be called
         *
         * @param listener the listener
         * @return this for chaining
         */
        IPathAndBytesable<T> withUnhandledErrorListener(UnhandledErrorListener listener);
    }
}
