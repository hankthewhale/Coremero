﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Coremero.Commands;
using Coremero.Context;
using Coremero.Messages;
using Coremero.Services;
using Coremero.Utilities;
using MarkovSharpNetCore.TokenisationStrategies;

namespace Coremero.Plugin.Playground
{
    public class ImitateChat : IPlugin
    {
        readonly Dictionary<string, StringMarkov> _models = new Dictionary<string, StringMarkov>();

        public ImitateChat(IMessageBus messageBus)
        {
            messageBus.Received += MessageBus_Received;
        }

        private void MessageBus_Received(object sender, MessageReceivedEventArgs e)
        {
            IChannel channel = e.Context?.Channel;
            if (channel != null)
            {
                if (e.Context.User?.Name != e.Context.OriginClient.Username &&
                    !string.IsNullOrEmpty(e.Message.Text?.Trim()) && !e.Message.Text.IsCommand())
                {
                    if (_models.ContainsKey(e.Context.Channel.Name))
                    {
                        _models[channel.Name].Learn(e.Message.Text);
                    }
                }
            }
        }

        [Command("imichat")]
        public async Task<string> ImiChat(IInvocationContext context)
        {
            IBufferedChannel bufferedChannel = context.Channel as IBufferedChannel;
            if (bufferedChannel != null)
            {
                if (!_models.ContainsKey(bufferedChannel.Name))
                {
                    StringMarkov markov = new StringMarkov() { EnsureUniqueWalk = true };
                    DateTimeOffset offset = DateTimeOffset.UtcNow;
                    List<IBufferedMessage> messages = await bufferedChannel.GetMessagesAsync(offset, SearchDirection.Before, 5000);
                    markov.Learn(messages.Where(x => x.User.Name != context.OriginClient.Username && !string.IsNullOrEmpty(x.Text?.Trim()) && !x.Text.IsCommand()).Select(x => x.Text));
                    _models[bufferedChannel.Name] = markov;
                }

                return _models[context.Channel.Name].Walk(10).OrderByDescending(x => x.Length).Take(5).GetRandom();
            }
            throw new Exception("Not buffered channel.");
        }

        [Command("imiuser")]
        public string ImiUser(IInvocationContext context, string user)
        {
            IEntity entity = context.User as IEntity;
            if (entity != null)
            {
                return _userModels[entity.ID].Walk().First();
            }
            throw new Exception("Not an entity.");
        }

        Dictionary<ulong, StringMarkov> _userModels = new Dictionary<ulong, StringMarkov>();

        [Command("fillusermarkovs")]
        public async Task<string> FillUserMarkovs(IInvocationContext context)
        {
            string botName = context.OriginClient.Username;
            foreach (var server in context.OriginClient.Servers)
            {
                foreach (IChannel channel in server.Channels)
                {
                    IBufferedChannel bufferedChannel = channel as IBufferedChannel;
                    if (bufferedChannel != null)
                    {
                        foreach (IBufferedMessage message in await bufferedChannel.GetLatestMessagesAsync(10000))
                        {
                            if (message.User.Name == botName || string.IsNullOrEmpty(message.Text.Trim()) || message.Text.IsCommand())
                            {
                                continue;
                            }
                            IEntity entity = message.User as IEntity;
                            if (entity != null)
                            {
                                if (!_userModels.ContainsKey(entity.ID))
                                {
                                    _userModels[entity.ID] = new StringMarkov();
                                }

                                _userModels[entity.ID].Learn(message.Text);
                            }
                        }
                    }
                }
            }

            return $"Identified {_userModels.Count} users.";
        }

    }
}
