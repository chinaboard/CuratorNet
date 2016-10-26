using System;
using org.apache.zookeeper;
using Org.Apache.CuratorNet.Client.Utils;
using Org.Apache.CuratorNet.Framework.API;
using Org.Apache.Java.Types;
using Org.Apache.Java.Types.Concurrent;

namespace Org.Apache.CuratorNet.Framework.Imps
{
    internal class Backgrounding
    {
        private readonly bool inBackgrnd;
        private readonly Object context;
        private readonly IBackgroundCallback callback;
        private readonly IUnhandledErrorListener errorListener;

        internal Backgrounding(Object context)
        {
            this.inBackgrnd = true;
            this.context = context;
            this.callback = null;
            errorListener = null;
        }

        internal Backgrounding(IBackgroundCallback callback)
        {
            this.inBackgrnd = true;
            this.context = null;
            this.callback = callback;
            errorListener = null;
        }

        internal Backgrounding(bool inBackground)
        {
            this.inBackgrnd = inBackground;
            this.context = null;
            this.callback = null;
            errorListener = null;
        }

        internal Backgrounding(IBackgroundCallback callback, Object context)
        {
            this.inBackgrnd = true;
            this.context = context;
            this.callback = callback;
            errorListener = null;
        }

        internal Backgrounding(CuratorFrameworkImpl client, 
                                IBackgroundCallback callback, 
                                Object context, 
                                IExecutor executor) 
            : this(wrapCallback(client, callback, executor), context) { }

        internal Backgrounding(CuratorFrameworkImpl client, 
                                IBackgroundCallback callback, 
                                IExecutor executor) 
            : this(wrapCallback(client, callback, executor)) { }

        internal Backgrounding(Backgrounding rhs, IUnhandledErrorListener errorListener)
        {
            if (rhs == null)
            {
                rhs = new Backgrounding();
            }
            this.inBackgrnd = rhs.inBackgrnd;
            this.context = rhs.context;
            this.callback = rhs.callback;
            this.errorListener = errorListener;
        }

        internal Backgrounding()
        {
            inBackgrnd = false;
            context = null;
            this.callback = null;
            errorListener = null;
        }

        internal bool inBackground()
        {
            return inBackgrnd;
        }

        internal Object getContext()
        {
            return context;
        }

        internal IBackgroundCallback getCallback()
        {
            return callback;
        }

        internal void checkError(Exception e)
        {
            if ( e != null )
            {
                if (errorListener != null)
                {
                    errorListener.unhandledError("n/a", e);
                }
                else
                {
                    throw e;
                }
            }
        }

        private class BackgroundCallback : IBackgroundCallback
        {
            private readonly CuratorFrameworkImpl _client;
            private readonly IBackgroundCallback _callback;
            private readonly IExecutor _service;

            public BackgroundCallback(CuratorFrameworkImpl client, 
                                        IBackgroundCallback callback, 
                                        IExecutor service)
            {
                _client = client;
                _callback = callback;
                _service = service;
            }

            public void processResult(CuratorFramework client, ICuratorEvent @event)
            {
                _service.execute(RunnableUtils.FromFunc(() =>
                {
                    try
                    {
                        _callback.processResult(_client, @event);
                    }
                    catch ( Exception e )
                    {
                        ThreadUtils.checkInterrupted(e);
                        var keeperException = e as KeeperException;
                        if ( keeperException != null )
                        {
                            _client.validateConnection(_client.codeToState(keeperException));
                        }
                        _client.logError("Background operation result handling threw exception", e);
                    }
                }));
            }
        }

        private static IBackgroundCallback wrapCallback(CuratorFrameworkImpl client, 
                                                        IBackgroundCallback callback, 
                                                        IExecutor executor)
        {
            return new BackgroundCallback(client,callback, executor);
        }
    }
}