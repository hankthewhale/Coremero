﻿using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Coremero.Attachments;
using Coremero.Context;
using Coremero.Messages;
using Coremero.Registry;
using Coremero.Utilities;

namespace Coremero.Services
{
    /// <summary>
    /// Handles messages from the messagebus that may have a message and performs invocation.
    /// </summary>
    public class CommandHandler : ICommandHandler
    {
        private readonly IMessageBus _messageBus;
        private readonly CommandRegistry _commandRegistry;

        public CommandHandler(IMessageBus messageBus, CommandRegistry commandRegistry)
        {
            _messageBus = messageBus;
            _commandRegistry = commandRegistry;

            messageBus.Received += (sender, args) =>
            {
                try
                {
                    MessageBusOnReceived(sender, args);
                }
                catch
                {
                    // ignore
                }
            };
            messageBus.Sent += (sender, args) =>
            {
                try
                {
                    MessageBusOnSent(sender, args);
                }
                catch
                {
                    // ignore
                }
            };
        }

        private void MessageBusOnReceived(object sender, MessageReceivedEventArgs eventArgs)
        {
            // Unpack locally.
            IInvocationContext context = eventArgs.Context;
            IMessage message = eventArgs.Message;

            // Is it a command?
            if (message.Text?.IsCommand() != true)
            {
                return;
            }

            // Check if command exists.
            string command = message.Text.Split(' ').First().TrimStart('.', '!');

            if (!_commandRegistry.Exists(command))
            {
                if (command[0] != '.' || command[0] != '!')
                {
                    if (message is IReactableMessage reactableMessage)
                    {
                        reactableMessage.React("🚫").Forget();
                    }
                }
                return;
            }

            IChannelTypingIndicator typingChannel = context?.Channel as IChannelTypingIndicator;
            // Ensure we do not back up the rest of the command invocation queue.
            // TODO: Per-server task pools.
            Task.Run(async () =>
            {
                try
                {
                    typingChannel?.SetTyping(true);
                    IMessage result = await _commandRegistry.ExecuteCommandAsync(command, context, message);
                    if (result != null)
                    {
                        _messageBus.RaiseOutgoing(context?.Raiser, result);
                    }
                    typingChannel?.SetTyping(false);
                    if (message.Text[0] == '!')
                    {
                        if (message is IDeletableMessage deletableMessage)
                        {
                            deletableMessage.DeleteAsync().Forget();
                        }
                    }
                }
                catch (Exception e)
                {
                    typingChannel?.SetTyping(false);
                    // Quickly dispose any streams in memory.
                    message.Attachments?.ForEach(x => x.Contents?.Dispose());
                    Log.Error($"{command} FAIL: {e}");

                    if (message is IReactableMessage erroredReactableMessage)
                    {
                        erroredReactableMessage.React("💔").Forget();
                    }
                    else
                    {
                        // Check if there is a help function.
                        var help = _commandRegistry.GetHelp(command);
                        if (!string.IsNullOrEmpty(help))
                        {
                            _messageBus.RaiseOutgoing(context.Raiser, Message.Create(help));
                        }
                        else
                        {
#if DEBUG
                            _messageBus.RaiseOutgoing(context.Raiser,
                                Message.Create("```\n" + e.StackTrace + "\n```",
                                    new FileAttachment(Path.Combine(PathExtensions.AppDir, "error.jpg"))));
#endif
                        }
                    }
                }
            });
        }

        private void MessageBusOnSent(object sender, MessageSentEventArgs messageSentEventArgs)
        {
            // TODO: Move this to be per-client so they can format for their audience?
            if (messageSentEventArgs.Message != null)
            {
                messageSentEventArgs.Target.Send(messageSentEventArgs.Message);
            }
        }
    }
}