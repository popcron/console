using System;
using UnityEngine;

namespace Popcron.Console
{
    [Serializable]
    public class SearchResult
    {
        [SerializeField]
        private string text;

        [SerializeField]
        private string command;

        public string Text => text;
        public string Command => command;

        public SearchResult(string text, string command)
        {
            this.text = text;
            this.command = command;
        }
    }
}