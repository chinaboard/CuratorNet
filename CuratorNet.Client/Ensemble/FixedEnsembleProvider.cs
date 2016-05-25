using System;

namespace Org.Apache.CuratorNet.Client.Ensemble
{
    /**
     * Standard ensemble provider that wraps a fixed connection string
     */
    public class FixedEnsembleProvider : IEnsembleProvider
    {
        private readonly String connectionString;

        /**
         * The connection string to use
         *
         * @param connectionString connection string
         */
        public FixedEnsembleProvider(String connectionString)
        {
            if (String.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentNullException(nameof(connectionString), 
                                        "connectionString cannot be null");
            }
            this.connectionString = connectionString;
        }

        public void start()
        {
            // NOP
        }

        public void Dispose()
        {
            // NOP
        }

        public String getConnectionString()
        {
            return connectionString;
        }
    }
}
