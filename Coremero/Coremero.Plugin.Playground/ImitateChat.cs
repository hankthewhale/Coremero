﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Coremero.Commands;
using Coremero.Context;
using Coremero.Messages;
using Coremero.Utilities;
using MarkovSharpNetCore.TokenisationStrategies;

namespace Coremero.Plugin.Playground
{
    public class ImitateChat : IPlugin
    {
        [Command("imichat")]
        public async Task<string> ImiChat(IInvocationContext context)
        {
            IBufferedChannel bufferedChannel = context.Channel as IBufferedChannel;
            if (bufferedChannel != null)
            {
                StringMarkov markov = new StringMarkov(2) { EnsureUniqueWalk = true };
                List<IBufferedMessage> messages = await bufferedChannel.GetLatestMessagesAsync(300);
                markov.Learn(messages.Where(x => x.User.Name != context.OriginClient.Username && !string.IsNullOrEmpty(x.Text?.Trim()) && !x.Text.IsCommand()).Select(x => x.Text));
                return markov.Walk(10).OrderByDescending(x => x.Length).Take(5).GetRandom();
            }
            throw new Exception("Not buffered channel.");
        }

        [Command("imichatday", MinimumPermissionLevel = UserPermission.BotOwner)]
        public async Task<string> ImiChatDay(IInvocationContext context)
        {
            IBufferedChannel bufferedChannel = context.Channel as IBufferedChannel;
            if (bufferedChannel != null)
            {
                StringMarkov markov = new StringMarkov(2) { EnsureUniqueWalk = true };
                DateTimeOffset searchStart = DateTimeOffset.UtcNow;
                DateTimeOffset offset = DateTimeOffset.UtcNow;
                while (offset.Day == searchStart.Day)
                {
                    List<IBufferedMessage> messages = await bufferedChannel.GetMessagesAsync(offset, SearchDirection.Before);
                    offset = new DateTimeOffset(messages.Last().Timestamp);
                    markov.Learn(messages.Where(x => x.User.Name != context.OriginClient.Username && !string.IsNullOrEmpty(x.Text?.Trim()) && !x.Text.IsCommand()).Select(x => x.Text));
                }
                return markov.Walk(10).OrderByDescending(x => x.Length).Take(5).GetRandom();
            }
            throw new Exception("Not buffered channel.");
        }


    }
}