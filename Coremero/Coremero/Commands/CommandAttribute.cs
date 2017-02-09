﻿using System;
using System.Collections.Generic;
using System.Linq;


namespace Coremero.Commands
{
    [AttributeUsage(AttributeTargets.Method)]
    public class CommandAttribute : Attribute
    {
        /// <summary>
        /// Name of the command.
        /// </summary>
        public readonly string Name;
        /// <summary>
        /// Description for using the command.
        /// </summary>
        public string Help { get; set; }
        
        /// <summary>
        /// A list of arguments.
        /// </summary>
        public List<String> Arguments { get; set; }

        /// <summary>
        /// True if the command works with external state (e.g. direct client manipulation) https://en.wikipedia.org/wiki/Side_effect_(computer_science)
        /// </summary>
        public bool HasSideEffects { get; set; }

        public CommandAttribute(string name, params string[] arguments)
        {
            this.Name = name;
            this.Arguments = arguments?.ToList();
        }
    }
}
