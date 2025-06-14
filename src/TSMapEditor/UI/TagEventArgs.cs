using System;
using TSMapEditor.Models;

namespace TSMapEditor.UI
{
    public class TagEventArgs : EventArgs
    {
        public TagEventArgs(Tag tag)
        {
            Tag = tag;
        }

        public Tag Tag { get; }
    }
}
