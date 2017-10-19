// -----------------------------------------------------------------------
// <copyright file="HystrixRuntimeException.cs" company="Microsoft">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace CHystrix
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    public enum FailureType
    {
        COMMAND_EXCEPTION,
        TIMEOUT,
        SHORTCIRCUIT,
        REJECTED_THREAD_EXECUTION,
        REJECTED_SEMAPHORE_EXECUTION,
        REJECTED_SEMAPHORE_FALLBACK
    }

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class HystrixRuntimeException : Exception
    {

        private const long serialVersionUID = 5219160375476046229L;

        private readonly Type commandClass;
        private readonly Exception fallbackException;
        private readonly FailureType failureCause;


        public HystrixRuntimeException(FailureType failureCause, Type commandClass, string message, Exception cause, Exception fallbackException)
            : base(message, cause)
        {
            this.failureCause = failureCause;
            this.commandClass = commandClass;
            
            this.fallbackException = fallbackException;
        }

        /// <summary>
        /// The type of failure that caused this exception to be thrown.
        /// </summary>
        /// <returns> <seealso cref="FailureType"/> </returns>
        public virtual FailureType FailureType
        {
            get
            {
                return failureCause;
            }
        }

        /// <summary>
        /// The implementing class of the <seealso cref="HystrixCommand"/>.
        /// </summary>
        /// <returns> {@code Class<? extends HystrixCommand> } </returns>
        public virtual Type ImplementingClass
        {
            get
            {
                return commandClass;
            }
        }

        /// <summary>
        /// The <seealso cref="Throwable"/> that was thrown when trying to retrieve a fallback.
        /// </summary>
        /// <returns> <seealso cref="Throwable"/> </returns>
        public virtual Exception FallbackException
        {
            get
            {
                return fallbackException;
            }
        }

    }
}
