using System;
using System.Collections.Generic;
using System.Reflection;

namespace Popcron.Console
{
    [Serializable]
    public class Owner
    {
        public string id;
        public object owner;
        public List<MethodInfo> methods;
    }
}
