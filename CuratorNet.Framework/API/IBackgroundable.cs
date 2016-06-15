using System;
using Org.Apache.Java.Types.Concurrent;

namespace Org.Apache.CuratorNet.Framework.API
{
    public interface IBackgroundable<T>
    {
        /**
         * Perform the action in the background
         *
         * @return this
         */
        T inBackground();

        /**
         * Perform the action in the background
         *
         * @param context context object - will be available from the event sent to the listener
         * @return this
         */
        T inBackground(Object context);

        /**
         * Perform the action in the background
         *
         * @param callback a functor that will get called when the operation has completed
         * @return this
         */
        T inBackground(BackgroundCallback callback);

        /**
         * Perform the action in the background
         *
         * @param callback a functor that will get called when the operation has completed
         * @param context context object - will be available from the event sent to the listener
         * @return this
         */
        T inBackground(BackgroundCallback callback, Object context);

        /**
         * Perform the action in the background
         *
         * @param callback a functor that will get called when the operation has completed
         * @param executor executor to use for the background call
         * @return this
         */
        T inBackground(BackgroundCallback callback, IExecutorService executor);

        /**
         * Perform the action in the background
         *
         * @param callback a functor that will get called when the operation has completed
         * @param context context object - will be available from the event sent to the listener
         * @param executor executor to use for the background call
         * @return this
         */
        T inBackground(BackgroundCallback callback, Object context, IExecutorService executor);
    }
}
