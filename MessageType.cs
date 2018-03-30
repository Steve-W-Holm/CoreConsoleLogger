using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
//using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;

namespace Itea.Logger
{
    /// <summary>
    /// Implements the Type-Safe Enum Pattern to provide MessageType definitions for logging.  Allows for inherited enum functionality.
    /// </summary>
    public class MessageType
    {
        public readonly string Name;
        public readonly string Character;
        public readonly long Value;

        private static ArrayList values;

        protected MessageType(long value, string character, string name)
        {
            if (values == null) values = new ArrayList();
            
            foreach( MessageType m in values)
            {
                //Debug.Assert(value != m.Value, "MessageType value '" + m.ToString() + "' already in use");
                //Debug.Assert(character != m.Character, "MessageType character '" + m.Character + "' already in use");
                //Debug.Assert(name != m.Name, "MessageType name '" + m.Name + "' already in use");
                if (value == m.Value)
                    throw new Exception("Invalid value provided for MessageType. '" + value.ToString() + "' already assigned.");
                if (character == m.Character)
                    throw new Exception("Invalid character provided for MessageType. '" + character + "' already in use.");
                if (name == m.Name)
                    throw new Exception("Invalid Name provided for MessageType. '" + name + "' already in use.");
            }

            Value = value;
            Name = name;
            Character = character;

            values.Add(this);
        }

        public static Array GetValues()
        {
            return values.ToArray(typeof(MessageType));
        }
        public static MessageType FromName(string name)
        {
            foreach (MessageType m in values)
            {
                if (m.Name == name)
                    return m;
            }

            return null;
        }
        public static MessageType FromCharacter(string character)
        {
            foreach (MessageType m in values)
            {
                if (m.Character == character)
                    return m;
            }

            return null;
        }
        public static MessageType FromValue(long value)
        {
            foreach (MessageType m in values)
            {
                if (m.Value == value)
                    return m;
            }

            return null;
        }

        // Enables implicit casting as a long
        public static implicit operator long(MessageType m)
        {
            if (m != null)
                return m.Value;
            else
                return 0;
        }

        #region Enumerated values
        public static readonly MessageType Error = new MessageType(0x00000001, "X", "Error");
        public static readonly MessageType MinorError = new MessageType(0x00000002, "x", "MinorError");
        public static readonly MessageType Status = new MessageType(0x00000004, "^", "Status");
        public static readonly MessageType Configuration = new MessageType(0x00000008, "!", "Configuration");
        public static readonly MessageType LogFile = new MessageType(0x00000010, "%", "Logfile");
        public static readonly MessageType Cmd = new MessageType(0x00000020, ":", "Cmd");
        public static readonly MessageType Pipe = new MessageType(0x00000040, "|", "Pipe");
        public static readonly MessageType Verbose = new MessageType(0x00000080, "~", "Verbose");
        public static readonly MessageType Debugging = new MessageType(0x00000100, "'", "Debugging");
        public static readonly MessageType Default = new MessageType(0x00000200, " ", "Default");
        public static readonly MessageType Threading = new MessageType(0x00000400, "h", "Threading");

        #endregion
    }
}
