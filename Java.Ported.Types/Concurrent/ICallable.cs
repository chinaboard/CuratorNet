using System;

namespace Org.Apache.Java.Types.Concurrent
{
    /// <summary>
    /// A task that returns a result and may throw an exception.
    /// Implementors define a single method with no arguments called
    /// {@code call}.
    ///
    /// The {@code Callable} interface is similar to {@link
    /// java.lang.Runnable}, in that both are designed for classes whose
    /// instances are potentially executed by another thread.  A
    /// {@code Runnable}, however, does not return a result and cannot
    /// throw a checked exception.
    ///
    /// The {@link Executors} class contains utility methods to
    /// convert from other common forms to {@code Callable} classes.
    /// </summary>
    /// <typeparam name="T">the result type of method {@code call}</typeparam>
    public interface ICallable<T>
    {
        /// <summary>
        /// Computes a result, or throws an exception if unable to do so.
        /// </summary>
        /// <returns>computed result</returns>
        /// <exception cref="Exception">if unable to compute a result</exception>
        T call();
    }
}
