using UnityEngine;

namespace Popcron.Console
{
    public static class EnsureConsoleExists
    {
        static EnsureConsoleExists()
        {
            ConsoleWindow.GetOrCreate();
        }

        [RuntimeInitializeOnLoadMethod]
        private static void Initialize()
        {
            ConsoleWindow.GetOrCreate();
        }
    }
}