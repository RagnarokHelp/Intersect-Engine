﻿using Intersect.Examples.Plugin.Packets.Client;
using Intersect.Examples.Plugin.Packets.Server;
using Intersect.Examples.Plugin.Server.Networking.Handlers;
using Intersect.Examples.Plugin.Server.Networking.Hooks;
using Intersect.Network.Packets.Client;
using Intersect.Plugins;
using Intersect.Server.Plugins;
using Microsoft.Extensions.Logging;

namespace Intersect.Examples.Plugin.Server;

public class ExampleServerPluginEntry : ServerPluginEntry
{
    /// <inheritdoc />
    public override void OnBootstrap(IPluginBootstrapContext context)
    {
        base.OnBootstrap(context);

        context.Logging.Application.LogInformation(
            $@"{nameof(ExampleServerPluginEntry)}.{nameof(OnBootstrap)} writing to the application log!"
        );

        context.Logging.Plugin.LogInformation(
            $@"{nameof(ExampleServerPluginEntry)}.{nameof(OnBootstrap)} writing to the plugin log!"
        );

        context.Logging.Plugin.LogInformation("Registering packets...");
        if (!context.Packet.TryRegisterPacketType<ExamplePluginClientPacket>())
        {
            context.Logging.Plugin.LogError($"Failed to register {nameof(ExamplePluginClientPacket)} packet.");
            Environment.Exit(-3);
        }

        if (!context.Packet.TryRegisterPacketType<ExamplePluginServerPacket>())
        {
            context.Logging.Plugin.LogError($"Failed to register {nameof(ExamplePluginServerPacket)} packet.");
            Environment.Exit(-3);
        }

        context.Logging.Plugin.LogInformation("Registering packet handlers...");
        if (!context.Packet
                .TryRegisterPacketHandler<ExamplePluginClientPacketHandler, ExamplePluginClientPacket>(out _))
        {
            context.Logging.Plugin.LogError(
                $"Failed to register {nameof(ExamplePluginClientPacketHandler)} packet handler."
            );
            Environment.Exit(-4);
        }

        if (!context.Packet.TryRegisterPacketPostHook<ExamplePluginLoginPostHook, LoginPacket>(out _))
        {
            context.Logging.Plugin.LogError(
                $"Failed to register {nameof(ExamplePluginLoginPostHook)} packet post-hook handler."
            );
            Environment.Exit(-4);
        }
    }

    /// <inheritdoc />
    public override void OnStart(IServerPluginContext context)
    {
        context.Logging.Application.LogInformation(
            $@"{nameof(ExampleServerPluginEntry)}.{nameof(OnStop)} writing to the application log!"
        );

        context.Logging.Plugin.LogInformation(
            $@"{nameof(ExampleServerPluginEntry)}.{nameof(OnStop)} writing to the plugin log!"
        );
    }

    /// <inheritdoc />
    public override void OnStop(IServerPluginContext context)
    {
        context.Logging.Application.LogInformation(
            $@"{nameof(ExampleServerPluginEntry)}.{nameof(OnStop)} writing to the application log!"
        );

        context.Logging.Plugin.LogInformation(
            $@"{nameof(ExampleServerPluginEntry)}.{nameof(OnStop)} writing to the plugin log!"
        );
    }
}