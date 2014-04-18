using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EncryptDecrypt
{

    /// <summary>
    /// Properties marked with this Attribute are not serialized in the payload when sent to the server
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class DoNotSerializeAttribute : Attribute
    {
    }

    /// <summary>
    /// Properties marked with this Attribute are encrypted when sent to the server
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class EncryptAttribute : Attribute
    {
    }
}
