using System;
using System.Collections.Generic;
//using System.Collections.comparer;
using System.Linq;
using System.Text;

namespace Itea.Logger
{
    public delegate string ConsoleCommandDelegate(string[] args);

    public class ConsoleCommand : IComparable
    {
        private string _commandName;
        private ConsoleCommandDelegate _callbackMethod;
        private string _usageString;
        private string _description;

        #region Contructors

        public ConsoleCommand(ConsoleCommandDelegate CallbackMethod, string UsageString)
        {
            _callbackMethod = CallbackMethod;
            _usageString =_commandName = CallbackMethod.Method.Name.ToLower();
        }
        public ConsoleCommand(ConsoleCommandDelegate CallbackMethod, string CommandName, string Description)
        {
            _callbackMethod = CallbackMethod;
            _commandName = CommandName.ToLower();
            _description = Description;
            _usageString = CommandName;
        }
        public ConsoleCommand(ConsoleCommandDelegate CallbackMethod, string CommandName, string Description, string UsageString)
        {
            _callbackMethod = CallbackMethod;
            _commandName = CommandName.ToLower();
            _description = Description;
            _usageString = UsageString;
        }

        #endregion

        #region Properties

        public String CommandName
        {
            get { return _commandName; }
        }
        public ConsoleCommandDelegate CallbackMethod
        {
            get { return _callbackMethod; }
        }
        public string Description
        {
            get { return _description; }
        }
        public string UsageString
        {
            get { return _usageString; }
        }

        #endregion

        public override bool Equals(object obj)
        {
            //FIX: Type check 
            if (obj.GetType() != typeof(ConsoleCommand)) return false;

            ConsoleCommand o = (ConsoleCommand)obj;

            if (o.CommandName.ToLower() == this.CommandName.ToLower())
                return true;
            else
                return false;
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }


        #region IComparable Members

        public int CompareTo(object obj)
        {
            if (obj.GetType() != typeof(ConsoleCommand)) return 0;

            ConsoleCommand o = (ConsoleCommand)obj;

            return this.CommandName.ToLower().CompareTo( o.CommandName.ToLower() );
        }

        #endregion
    }
}
