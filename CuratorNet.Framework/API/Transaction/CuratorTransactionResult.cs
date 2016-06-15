using System;
using org.apache.zookeeper.data;

namespace Org.Apache.CuratorNet.Framework.API.Transaction
{
    /**
     * Holds the result of one transactional operation
     */
    public class CuratorTransactionResult
    {
        private readonly OperationType type;
        private readonly String        forPath;
        private readonly String        resultPath;
        private readonly Stat          resultStat;

        /**
         * Utility that can be passed to Google Guava to find a particular result. E.g.
         * <pre>
         * Iterables.find(results, CuratorTransactionResult.ofTypeAndPath(OperationType.CREATE, path))
         * </pre>
         *
         * @param type operation type
         * @param forPath path
         * @return predicate
         */
        public static Predicate<CuratorTransactionResult> ofTypeAndPath(OperationType type, String forPath)
        {
            return (CuratorTransactionResult result) 
                    => (result.getType() == type) && result.getForPath().Equals(forPath);
//            return new Predicate<CuratorTransactionResult>()
//            {
//                @Override
//                public boolean apply(CuratorTransactionResult result)
//                {
//                    return (result.getType() == type) && result.getForPath().equals(forPath);
//                }
//            };
        }

        public CuratorTransactionResult(OperationType type, 
                                        String forPath, 
                                        String resultPath, 
                                        Stat resultStat)
        {
            this.forPath = forPath;
            this.resultPath = resultPath;
            this.resultStat = resultStat;
            this.type = type;
        }

        /**
         * Returns the operation type
         *
         * @return operation type
         */
        public OperationType getType()
        {
            return type;
        }

        /**
         * Returns the path that was passed to the operation when added
         * 
         * @return operation input path
         */
        public String getForPath()
        {
            return forPath;
        }

        /**
         * Returns the operation generated path or <code>null</code>. i.e. {@link CuratorTransaction#create()}
         * using an EPHEMERAL mode generates the created path plus its sequence number.
         *
         * @return generated path or null
         */
        public String getResultPath()
        {
            return resultPath;
        }

        /**
         * Returns the operation generated stat or <code>null</code>. i.e. {@link CuratorTransaction#setData()}
         * generates a stat object.
         *
         * @return generated stat or null
         */
        public Stat getResultStat()
        {
            return resultStat;
        }
    }
}
