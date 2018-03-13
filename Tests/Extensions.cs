using System;
using System.Collections.Generic;

namespace UnitTests
{
    public static class Extensions
    {
        public static IEnumerable<Exception> GetInnerExceptions(this Exception ex)
        {
            if (ex == null)
            {
                throw new ArgumentNullException("ex");
            }

            var innerException = ex;
            do
            {
                yield return innerException;
                innerException = innerException.InnerException;
            } while (innerException != null);
        }
    }
}