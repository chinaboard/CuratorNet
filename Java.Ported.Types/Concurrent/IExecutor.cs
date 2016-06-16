namespace Org.Apache.Java.Types.Concurrent
{
    public interface IExecutor
    {
        /// <summary>
        /// Executes the given command at some time in the future. 
        /// The command may execute in a new thread, in a pooled thread, 
        /// or in the calling thread, at the discretion of the Executor implementation.
        /// </summary>
        /// <param name="command"></param>
        void execute(IRunnable command);
    }
}