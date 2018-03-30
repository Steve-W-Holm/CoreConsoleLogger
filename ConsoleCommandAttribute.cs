using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Itea.Logger
{
    [System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class ConsoleCommandAttribute : Attribute
    {
        public ConsoleCommandAttribute()
        {

        }

        public ConsoleCommandAttribute(string CommandName)
        {
            this.CommandName = CommandName;
        }

        public ConsoleCommandAttribute(string CommandName, string Description)
        {
            this.CommandName = CommandName;
            this.Description = Description;
        }

        public ConsoleCommandAttribute(string CommandName, string Description, string UsageString)
        {
            this.CommandName = CommandName;
            this.Description = Description;
            this.UsageString = UsageString;
        }

        public string CommandName { get; set; }
        public string Description { get; set; }
        public string UsageString { get; set; }
    }
}
