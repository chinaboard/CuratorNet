namespace Org.Apache.CuratorNet.Framework.API.Transaction
{
    /**
     * Transaction operation types
     */
    public enum OperationType
    {
        /**
         * {@link CuratorTransaction#create()}
         */
        CREATE,

        /**
         * {@link CuratorTransaction#delete()}
         */
        DELETE,

        /**
         * {@link CuratorTransaction#setData()}
         */
        SET_DATA,

        /**
         * {@link CuratorTransaction#check()}
         */
        CHECK
    }

}
