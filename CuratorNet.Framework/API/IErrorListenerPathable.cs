namespace Org.Apache.CuratorNet.Framework.API
{
    public interface IErrorListenerPathable<T> : IPathable<T>
    {
        /**
         * Set an error listener for this background operation. If an exception
         * occurs while processing the call in the background, this listener will
         * be called
         *
         * @param listener the listener
         * @return this for chaining
         */
        IPathable<T> withUnhandledErrorListener(IUnhandledErrorListener listener);
    }

}
