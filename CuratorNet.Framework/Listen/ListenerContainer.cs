using System;
using System.Collections.Concurrent;
using NLog;
using Org.Apache.CuratorNet.Client.Utils;
using Org.Apache.CuratorNet.Framework.Listen;
using Org.Apache.Java.Types;
using Org.Apache.Java.Types.Concurrent;

namespace Org.Apache.CuratorNet.Framework
{
    /**
     * Abstracts an object that has listeners
     */
    public class ListenerContainer<T> : Listenable<T>
    {
        private static readonly SameThreadTaskExecutorService SameThreadTaskExecutorService = new SameThreadTaskExecutorService();
        private readonly Logger log = LogManager.GetCurrentClassLogger();
        private readonly ConcurrentDictionary<T, ListenerEntry<T>>      listeners = new ConcurrentDictionary<T, ListenerEntry<T>>();

        public void addListener(T listener)
        {
            addListener(listener, SameThreadTaskExecutorService);
        }

        public void addListener(T listener, IExecutor executor)
        {
            var entry = new ListenerEntry<T>(listener, executor);
            listeners.AddOrUpdate(listener, entry, (k,v) => entry);
        }

        public void removeListener(T listener)
        {
            ListenerEntry<T> value;
            listeners.TryRemove(listener,out value);
        }

        /**
         * Remove all listeners
         */
        public void clear()
        {
            listeners.Clear();
        }

        /**
         * Return the number of listeners
         *
         * @return number
         */
        public int size()
        {
            return listeners.Count;
        }

        /**
         * Utility - apply the given function to each listener. The function receives
         * the listener as an argument.
         *
         * @param function function to call for each listener
         */
        public void forEach(Func<T, object> function)
        {
            foreach (ListenerEntry< T > entry in listeners.Values)
            {
                entry.executor.execute
                (
                    RunnableUtils.FromFunc(() =>
                    {
                        try
                        {
                            function(entry.listener);
                        }
                        catch (Exception e)
                        {
                            ThreadUtils.checkInterrupted(e);
                            log.Error("Listener {0} threw an exception{1}{2}", 
                                        entry.listener, Environment.NewLine, e);
                        }
                    })
                );
            }
        }
    }
}