using System;
using System.Collections.Generic;
using org.apache.zookeeper;
using org.apache.zookeeper.data;

namespace Org.Apache.CuratorNet.Framework.API
{
    /**
     * A super set of all the various Zookeeper events/background methods.
     *
     * IMPORTANT: the methods only return values as specified by the operation that generated them. Many methods
     * will return <tt>null</tt>
     */
    public interface ICuratorEvent
    {
        /**
         * check here first - this value determines the type of event and which methods will have
         * valid values
         *
         * @return event type
         */
        ICuratorEventType getType();

        /**
         * @return "rc" from async callbacks
         */
        int getResultCode();

        /**
         * @return the path
         */
        String getPath();

        /**
         * @return the context object passed to {@link Backgroundable#inBackground(Object)}
         */
        Object getContext();

        /**
         * @return any stat
         */
        Stat getStat();

        /**
         * @return any data
         */
        byte[] getData();

        /**
         * @return any name
         */
        String getName();

        /**
         * @return any children
         */
        List<String> getChildren();

        /**
         * @return any ACL list or null
         */
        List<ACL> getACLList();

        /**
         * If {@link #getType()} returns {@link CuratorEventType#WATCHED} this will
         * return the WatchedEvent
         *
         * @return any WatchedEvent
         */
        WatchedEvent getWatchedEvent();
    }

}
