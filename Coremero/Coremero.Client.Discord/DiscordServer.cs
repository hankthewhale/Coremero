﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Discord;

namespace Coremero.Client.Discord
{
    public class DiscordServer : IServer
    {
        private IGuild _guild;

        public DiscordServer(IGuild guild)
        {
            _guild = guild;
        }

        public string Name
        {
            get { return _guild.Name; }
        }

        public IEnumerable<IChannel> Channels
        {
            get { return _guild.GetTextChannelsAsync().Result.Select(x => new DiscordChannel(x)); }
        }
    }
}