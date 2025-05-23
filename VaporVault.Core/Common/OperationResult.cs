using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VaporVault.Core.Common
{
    public class OperationResult<T>
    {
        public bool Success { get; }
        public string ErrorMessage { get; }
        public T Data { get; }

        // ... constructors, static factory methods, etc.
    }

}
