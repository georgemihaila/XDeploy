using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace XDeploy.Server
{
    /// <summary>
    /// Specifies to a middleware that data existence and data ownership should be checked before proceeding.
    /// </summary>
    /// <seealso cref="System.Attribute" />
    [AttributeUsage(AttributeTargets.Method)]
    public class ValidateDataOwnershipAndExistenceAttribute : Attribute
    {

    }
}
