using System.Text;
using Intersect.Enums;
using Intersect.Framework.Core;
using Intersect.Framework.Core.GameObjects.Crafting;
using Intersect.Framework.Core.GameObjects.Variables;
using Intersect.GameObjects;
using Intersect.GameObjects.Events;
using Intersect.GameObjects.Events.Commands;
using Intersect.Server.Core.MapInstancing;
using Intersect.Server.Database;
using Intersect.Server.Database.PlayerData.Players;
using Intersect.Server.Database.PlayerData.Security;
using Intersect.Server.General;
using Intersect.Server.Localization;
using Intersect.Server.Maps;
using Intersect.Server.Networking;
using Intersect.Utilities;

namespace Intersect.Server.Entities.Events;


public static partial class CommandProcessing
{

    public static void ProcessCommand(EventCommand command, Player player, Event instance)
    {
        var stackInfo = instance.CallStack.Peek();
        stackInfo.WaitingForResponse = CommandInstance.EventResponse.None;
        stackInfo.WaitingOnCommand = null;
        stackInfo.BranchIds = null;

        ProcessCommand((dynamic)command, player, instance, instance.CallStack.Peek(), instance.CallStack);

        stackInfo.CommandIndex++;
    }

    //Show Text Command
    private static void ProcessCommand(
        ShowTextCommand command,
        Player player,
        Event instance,
        CommandInstance stackInfo,
        Stack<CommandInstance> callStack
    )
    {
        PacketSender.SendEventDialog(
            player, ParseEventText(command.Text, player, instance), command.Face, instance.PageInstance.Id
        );

        stackInfo.WaitingForResponse = CommandInstance.EventResponse.Dialogue;
    }

    //Show Options Command
    private static void ProcessCommand(
        ShowOptionsCommand command,
        Player player,
        Event instance,
        CommandInstance stackInfo,
        Stack<CommandInstance> callStack
    )
    {
        var txt = ParseEventText(command.Text, player, instance);
        var opt1 = ParseEventText(command.Options[0], player, instance);
        var opt2 = ParseEventText(command.Options[1], player, instance);
        var opt3 = ParseEventText(command.Options[2], player, instance);
        var opt4 = ParseEventText(command.Options[3], player, instance);
        PacketSender.SendEventDialog(player, txt, opt1, opt2, opt3, opt4, command.Face, instance.PageInstance.Id);
        stackInfo.WaitingForResponse = CommandInstance.EventResponse.Dialogue;
        stackInfo.WaitingOnCommand = command;
        stackInfo.BranchIds = command.BranchIds;
    }

    //Input Variable Command
    private static void ProcessCommand(
        InputVariableCommand command,
        Player player,
        Event instance,
        CommandInstance stackInfo,
        Stack<CommandInstance> callStack
    )
    {
        var title = ParseEventText(command.Title, player, instance);
        var txt = ParseEventText(command.Text, player, instance);
        var type = -1;

        if (command.VariableType == VariableType.PlayerVariable)
        {
            var variable = PlayerVariableDescriptor.Get(command.VariableId);
            if (variable != null)
            {
                type = (int)variable.DataType;
            }
        }
        else if (command.VariableType == VariableType.ServerVariable)
        {
            var variable = ServerVariableDescriptor.Get(command.VariableId);
            if (variable != null)
            {
                type = (int)variable.DataType;
            }
        }
        else if (command.VariableType == VariableType.GuildVariable)
        {
            var variable = GuildVariableDescriptor.Get(command.VariableId);
            type = (int)variable.DataType;
        }
        else if (command.VariableType == VariableType.UserVariable)
        {
            var variable = UserVariableDescriptor.Get(command.VariableId);
            type = (int)variable.DataType;
        }
        else if (type == -1)
        {
            var tmpStack = new CommandInstance(stackInfo.Page, command.BranchIds[1]);
            callStack.Push(tmpStack);
            return;
        }

        PacketSender.SendInputVariableDialog(player, title, txt, (VariableDataType)type, instance.PageInstance.Id);
        stackInfo.WaitingForResponse = CommandInstance.EventResponse.Dialogue;
        stackInfo.WaitingOnCommand = command;
        stackInfo.BranchIds = command.BranchIds;
    }

    //Show Add Chatbox Text Command
    private static void ProcessCommand(
        AddChatboxTextCommand command,
        Player player,
        Event instance,
        CommandInstance stackInfo,
        Stack<CommandInstance> callStack
    )
    {
        var txt = ParseEventText(command.Text, player, instance);
        var color = Color.FromName(command.Color, Strings.Colors.Presets);
        switch (command.Channel)
        {
            case ChatboxChannel.Player:
                PacketSender.SendChatMsg(player, txt, command.MessageType, color);

                break;
            case ChatboxChannel.Local:
                PacketSender.SendProximityMsg(txt, ChatMessageType.Local, player.MapId, color);

                break;
            case ChatboxChannel.Global:
                PacketSender.SendGlobalMsg(txt, color, string.Empty, ChatMessageType.Global);

                break;
            case ChatboxChannel.Party:
                if (player.Party?.Count > 0)
                {
                    PacketSender.SendPartyMsg(player, txt, color, player.Name);
                }

                break;
            case ChatboxChannel.Guild:
                if (player.Guild != null)
                {
                    PacketSender.SendGuildMsg(player, txt, color, player.Name);
                }
                break;
        }

        if (command.ShowChatBubble)
        {
            if (command.ShowChatBubbleInProximity)
            {

                PacketSender.SendChatBubbleToProximity(
                        player,
                        instance.PageInstance.Id,
                        instance.PageInstance.GetEntityType(),
                        txt,
                        instance.PageInstance.MapId
                    );
            }
            else
            {

                PacketSender.SendChatBubbleToPlayer(
                    player,
                    instance.PageInstance.Id,
                    instance.PageInstance.GetEntityType(),
                    txt,
                    instance.PageInstance.MapId
                );
            }
        }
    }

    //Set Variable Commands
    private static void ProcessCommand(
        SetVariableCommand command,
        Player player,
        Event instance,
        CommandInstance stackInfo,
        Stack<CommandInstance> callStack
    )
    {
        ProcessVariableModification(command, (dynamic)command.Modification, player, instance);
        player.UnequipInvalidItems();
    }

    //Set Self Switch Command
    private static void ProcessCommand(
        SetSelfSwitchCommand command,
        Player player,
        Event eventInstance,
        CommandInstance stackInfo,
        Stack<CommandInstance> callStack
    )
    {
        if (eventInstance.Global)
        {
            if (MapController.TryGetInstanceFromMap(eventInstance.MapId, player.MapInstanceId, out var instance))
            {
                var evts = instance.GlobalEventInstances.Values.ToList();
                for (var i = 0; i < evts.Count; i++)
                {
                    if (evts[i] != null && evts[i].BaseEvent == eventInstance.BaseEvent)
                    {
                        evts[i].SelfSwitch[command.SwitchId] = command.Value;
                    }
                }
            }
        }
        else
        {
            eventInstance.SelfSwitch[command.SwitchId] = command.Value;
        }
    }

    //Conditional Branch Command
    private static void ProcessCommand(
        ConditionalBranchCommand command,
        Player player,
        Event instance,
        CommandInstance stackInfo,
        Stack<CommandInstance> callStack
    )
    {
        var success = Conditions.MeetsCondition(command.Condition, player, instance, null);

        List<EventCommand> newCommandList = null;
        if (success && stackInfo.Page.CommandLists.ContainsKey(command.BranchIds[0]))
        {
            newCommandList = stackInfo.Page.CommandLists[command.BranchIds[0]];
        }

        if (!success && command.Condition.ElseEnabled && stackInfo.Page.CommandLists.ContainsKey(command.BranchIds[1]))
        {
            newCommandList = stackInfo.Page.CommandLists[command.BranchIds[1]];
        }

        if (newCommandList != null)
        {
            var tmpStack = new CommandInstance(stackInfo.Page)
            {
                CommandList = newCommandList,
                CommandIndex = 0,
            };

            callStack.Push(tmpStack);
        }
    }

    //Exit Event Process Command
    private static void ProcessCommand(
        ExitEventProcessingCommand command,
        Player player,
        Event instance,
        CommandInstance stackInfo,
        Stack<CommandInstance> callStack
    )
    {
        callStack.Clear();
    }

    //Label Command
    private static void ProcessCommand(
        LabelCommand command,
        Player player,
        Event instance,
        CommandInstance stackInfo,
        Stack<CommandInstance> callStack
    )
    {
        return; //Do Nothing.. just a label
    }

    //Go To Label Command
    private static void ProcessCommand(
        GoToLabelCommand command,
        Player player,
        Event instance,
        CommandInstance stackInfo,
        Stack<CommandInstance> callStack
    )
    {
        //Recursively search through commands for the label, and create a brand new call stack based on where that label is located.
        var newCallStack = LoadLabelCallstack(command.Label, stackInfo.Page);
        if (newCallStack != null)
        {
            newCallStack.Reverse();
            while (callStack.Peek().CommandListId != Guid.Empty)
            {
                callStack.Pop();
            }

            //also pop the current event
            callStack.Pop();
            foreach (var itm in newCallStack)
            {
                callStack.Push(itm);
            }
        }
    }

    //Start Common Event Command
    private static void ProcessCommand(
        StartCommmonEventCommand command,
        Player player,
        Event instance,
        CommandInstance stackInfo,
        Stack<CommandInstance> callStack
    )
    {
        if (!EventBase.TryGet(command.EventId, out var commonEvent))
        {
            return;
        }

        foreach (var page in commonEvent.Pages)
        {
            if (Conditions.CanSpawnPage(page, player, instance))
            {
                var commonEventStack = new CommandInstance(page);
                callStack.Push(commonEventStack);
            }
        }

        if (player.MapInstanceId == Guid.Empty && !command.AllowInOverworld)
        {
            return;
        }

        if (command.AllInInstance && InstanceProcessor.TryGetInstanceController(player.MapInstanceId, out var instanceController))
        {
            foreach (var instanceMember in instanceController.Players.ToArray())
            {
                if (instanceMember.Id == player.Id || !instanceMember.IsOnline)
                {
                    continue;
                }

                instanceMember.EnqueueStartCommonEvent(commonEvent);
            }
        }
    }

    //Restore Hp Command
    private static void ProcessCommand(
        RestoreHpCommand command,
        Player player,
        Event instance,
        CommandInstance stackInfo,
        Stack<CommandInstance> callStack
    )
    {
        if (command.Amount > 0)
        {
            player.AddVital(Vital.Health, command.Amount);
        }
        else if (command.Amount < 0)
        {
            player.SubVital(Vital.Health, -command.Amount);
            player.CombatTimer = Timing.Global.Milliseconds + Options.Instance.Combat.CombatTime;
            if (player.GetVital(Vital.Health) <= 0)
            {
                lock (player.EntityLock)
                {
                    player.Die();
                }
            }
        }
        else
        {
            player.RestoreVital(Vital.Health);
        }
    }

    //Restore Mp Command
    private static void ProcessCommand(
        RestoreMpCommand command,
        Player player,
        Event instance,
        CommandInstance stackInfo,
        Stack<CommandInstance> callStack
    )
    {
        if (command.Amount > 0)
        {
            player.AddVital(Vital.Mana, command.Amount);
        }
        else if (command.Amount < 0)
        {
            player.SubVital(Vital.Mana, -command.Amount);
            player.CombatTimer = Timing.Global.Milliseconds + Options.Instance.Combat.CombatTime;
        }
        else
        {
            player.RestoreVital(Vital.Mana);
        }
    }

    //Level Up Command
    private static void ProcessCommand(
        LevelUpCommand command,
        Player player,
        Event instance,
        CommandInstance stackInfo,
        Stack<CommandInstance> callStack
    )
    {
        player.AddLevels();
    }

    //Give Experience Command
    private static void ProcessCommand(
        GiveExperienceCommand command,
        Player player,
        Event instance,
        CommandInstance stackInfo,
        Stack<CommandInstance> callStack
    )
    {
        var quantity = command.Exp;
        if (command.UseVariable)
        {
            switch (command.VariableType)
            {
                case VariableType.PlayerVariable:
                    quantity = (int)player.GetVariableValue(command.VariableId).Integer;

                    break;
                case VariableType.ServerVariable:
                    quantity = (int)ServerVariableDescriptor.Get(command.VariableId)?.Value.Integer;

                    break;

                case VariableType.GuildVariable:
                    quantity = (int)player.Guild?.GetVariableValue(command.VariableId)?.Integer;

                    break;
            }
        }

        if(quantity > 0)
        {
            player.GiveExperience(quantity);
        }
        else if (quantity < 0)
        {
            player.TakeExperience(Math.Abs(quantity), command.EnableLosingLevels, force: true);
        }
    }

    //Change Level Command
    private static void ProcessCommand(
        ChangeLevelCommand command,
        Player player,
        Event instance,
        CommandInstance stackInfo,
        Stack<CommandInstance> callStack
    )
    {
        player.SetLevel(command.Level, true);
    }

    //Change Spells Command
    private static void ProcessCommand(
        ChangeSpellsCommand command,
        Player player,
        Event instance,
        CommandInstance stackInfo,
        Stack<CommandInstance> callStack
    )
    {
        //0 is add, 1 is remove
        var success = false;
        if (command.Add) //Try to add a spell
        {
            success = player.TryTeachSpell(new Spell(command.SpellId));
        }
        else
        {
            if (player.FindSpell(command.SpellId) > -1 && command.SpellId != Guid.Empty)
            {
                player.ForgetSpell(player.FindSpell(command.SpellId), command.RemoveBoundSpell);
                success = true;
            }
        }

        List<EventCommand> newCommandList = null;
        if (success && stackInfo.Page.CommandLists.ContainsKey(command.BranchIds[0]))
        {
            newCommandList = stackInfo.Page.CommandLists[command.BranchIds[0]];
        }

        if (!success && stackInfo.Page.CommandLists.ContainsKey(command.BranchIds[1]))
        {
            newCommandList = stackInfo.Page.CommandLists[command.BranchIds[1]];
        }

        var tmpStack = new CommandInstance(stackInfo.Page)
        {
            CommandList = newCommandList,
            CommandIndex = 0,
        };

        callStack.Push(tmpStack);
    }

    //Change Items Command
    private static void ProcessCommand(
        ChangeItemsCommand command,
        Player player,
        Event instance,
        CommandInstance stackInfo,
        Stack<CommandInstance> callStack
    )
    {
        var success = false;
        var skip = false;

        // Use the command quantity, unless we're using a variable for input!
        var quantity = command.Quantity;
        if (command.UseVariable)
        {
            switch (command.VariableType)
            {
                case VariableType.PlayerVariable:
                    quantity = (int)player.GetVariableValue(command.VariableId).Integer;

                    break;
                case VariableType.ServerVariable:
                    quantity = (int)ServerVariableDescriptor.Get(command.VariableId)?.Value.Integer;

                    break;
                case VariableType.GuildVariable:
                    quantity = (int)player.Guild?.GetVariableValue(command.VariableId)?.Integer;

                    break;
            }

            // The code further ahead converts 0 to quantity 1, due to some legacy junk where some editors would (maybe still do?) set quantity to 0 for non-stackable items.
            // but if we want to give a player no items through an event we should listen to that.
            if (quantity <= 0)
            {
                skip = true;
            }
        }

        if (!skip)
        {
            if (command.Add)
            {
                success = player.TryGiveItem(command.ItemId, quantity, command.ItemHandling);
            }
            else
            {
                success = player.TryTakeItem(command.ItemId, quantity, command.ItemHandling);
            }
        }
        else
        {
            // If we're skipping, this always succeeds.
            success = true;
        }

        List<EventCommand> newCommandList = null;
        if (success && stackInfo.Page.CommandLists.ContainsKey(command.BranchIds[0]))
        {
            newCommandList = stackInfo.Page.CommandLists[command.BranchIds[0]];
        }

        if (!success && stackInfo.Page.CommandLists.ContainsKey(command.BranchIds[1]))
        {
            newCommandList = stackInfo.Page.CommandLists[command.BranchIds[1]];
        }

        var tmpStack = new CommandInstance(stackInfo.Page)
        {
            CommandList = newCommandList,
            CommandIndex = 0,
        };

        callStack.Push(tmpStack);
    }

    //Equip Items Command
    private static void ProcessCommand(
        EquipItemCommand command,
        Player player,
        Event instance,
        CommandInstance stackInfo,
        Stack<CommandInstance> callStack
    )
    {
        var itm = ItemBase.Get(command.ItemId);

        if (command.Unequip)
        {
            if (command.IsItem && itm != default)
            {
                player.UnequipItem(command.ItemId);
            }
            else if(!command.IsItem && command.Slot > -1 && player.TryGetEquippedItem(command.Slot, out var equippedItem))
            {
                 player.UnequipItem(equippedItem.ItemId);
            }
        }
        else
        {
            if (itm == null) return;
            player.EquipItem(ItemBase.Get(command.ItemId), updateCooldown: command.TriggerCooldown);
        }
    }

    //Change Sprite Command
    private static void ProcessCommand(
        ChangeSpriteCommand command,
        Player player,
        Event instance,
        CommandInstance stackInfo,
        Stack<CommandInstance> callStack
    )
    {
        player.Sprite = command.Sprite;
        PacketSender.SendEntityDataToProximity(player);
    }

    //Change Face Command
    private static void ProcessCommand(
        ChangeFaceCommand command,
        Player player,
        Event instance,
        CommandInstance stackInfo,
        Stack<CommandInstance> callStack
    )
    {
        player.Face = command.Face;
        PacketSender.SendEntityDataToProximity(player);
    }

    //Change Gender Command
    private static void ProcessCommand(
        ChangeGenderCommand command,
        Player player,
        Event instance,
        CommandInstance stackInfo,
        Stack<CommandInstance> callStack
    )
    {
        player.Gender = command.Gender;
        PacketSender.SendEntityDataToProximity(player);
        player.UnequipInvalidItems();
    }

    //Change Name Color Command
    private static void ProcessCommand(
        ChangeNameColorCommand command,
        Player player,
        Event instance,
        CommandInstance stackInfo,
        Stack<CommandInstance> callStack
    )
    {
        if (command.Remove)
        {
            player.NameColor = null;
            PacketSender.SendEntityDataToProximity(player);

            return;
        }

        //Don't set the name color if it doesn't override admin name colors.
        if (player.Power != UserRights.None && !command.Override)
        {
            return;
        }

        player.NameColor = command.Color;
        PacketSender.SendEntityDataToProximity(player);
    }

    //Change Player Label Command
    private static void ProcessCommand(
        ChangePlayerLabelCommand command,
        Player player,
        Event instance,
        CommandInstance stackInfo,
        Stack<CommandInstance> callStack
    )
    {
        var label = ParseEventText(command.Value, player, instance);

        var color = command.Color;
        if (command.MatchNameColor)
        {
            color = Color.Transparent;
        }

        if (command.Position == 0) // Header
        {
            player.HeaderLabel = new Label(label, color);
        }
        else if (command.Position == 1) // Footer
        {
            player.FooterLabel = new Label(label, color);
        }

        PacketSender.SendEntityDataToProximity(player);
    }

    //Set Access Command (wtf why would we even allow this? lol)
    private static void ProcessCommand(
        SetAccessCommand command,
        Player player,
        Event instance,
        CommandInstance stackInfo,
        Stack<CommandInstance> callStack
    )
    {
        if (player.Client == null)
        {
            return;
        }

        switch (command.Access)
        {
            case Access.Moderator:
                player.Client.Power = UserRights.Moderation;

                break;
            case Access.Admin:
                player.Client.Power = UserRights.Admin;

                break;
            default:
                player.Client.Power = UserRights.None;

                break;
        }

        PacketSender.SendEntityDataToProximity(player);
        PacketSender.SendChatMsg(player, Strings.Player.PowerChanged, ChatMessageType.Notice, Color.Red);
        player.UnequipInvalidItems();
    }

    //Warp Player Command
    private static void ProcessCommand(
        WarpCommand command,
        Player player,
        Event instance,
        CommandInstance stackInfo,
        Stack<CommandInstance> callStack
    )
    {
        MapInstanceType? instanceType = null;
        if (command.ChangeInstance)
        {
            instanceType = command.InstanceType;
        }

        player.Warp(
            command.MapId, command.X, command.Y,
            command.Direction == WarpDirection.Retain ? player.Dir : (Direction)(command.Direction - 1),
            mapInstanceType: instanceType
        );
    }

    //Set Move Route Command
    private static void ProcessCommand(
        SetMoveRouteCommand command,
        Player player,
        Event instance,
        CommandInstance stackInfo,
        Stack<CommandInstance> callStack
    )
    {
        if (command.Route.Target == Guid.Empty)
        {
            player.MoveRoute = new EventMoveRoute();
            player.MoveRouteSetter = instance.PageInstance;
            player.MoveRoute.CopyFrom(command.Route);
            PacketSender.SendMoveRouteToggle(player, true);
        }
        else
        {
            foreach (var evt in player.EventLookup)
            {
                if (evt.Value.BaseEvent.Id == command.Route.Target)
                {
                    if (evt.Value.PageInstance != null)
                    {
                        evt.Value.PageInstance.MoveRoute.CopyFrom(command.Route);
                        evt.Value.PageInstance.MovementType = EventMovementType.MoveRoute;
                        if (evt.Value.PageInstance.GlobalClone != null)
                        {
                            evt.Value.PageInstance.GlobalClone.MovementType = EventMovementType.MoveRoute;
                        }
                    }
                }
            }
        }
    }

    //Wait for Route Completion Command
    private static void ProcessCommand(
        WaitForRouteCommand command,
        Player player,
        Event instance,
        CommandInstance stackInfo,
        Stack<CommandInstance> callStack
    )
    {
        if (command.TargetId == Guid.Empty)
        {
            stackInfo.WaitingForRoute = player.Id;
            stackInfo.WaitingForRouteMap = player.MapId;
        }
        else
        {
            foreach (var evt in player.EventLookup)
            {
                if (evt.Value.BaseEvent.Id == command.TargetId)
                {
                    stackInfo.WaitingForRoute = evt.Value.BaseEvent.Id;
                    stackInfo.WaitingForRouteMap = evt.Value.MapId;

                    break;
                }
            }
        }
    }

    //Spawn Npc Command
    private static void ProcessCommand(
        SpawnNpcCommand command,
        Player player,
        Event eventInstance,
        CommandInstance stackInfo,
        Stack<CommandInstance> callStack
    )
    {
        var npcId = command.NpcId;
        var mapId = command.MapId;
        var tileX = 0;
        var tileY = 0;
        Direction direction = (byte)Direction.Up;
        var targetEntity = (Entity)player;
        if (mapId != Guid.Empty)
        {
            tileX = command.X;
            tileY = command.Y;
            direction = command.Dir;
        }
        else
        {
            if (command.EntityId != Guid.Empty)
            {
                foreach (var evt in player.EventLookup)
                {
                    if (evt.Value.MapId != eventInstance.MapId)
                    {
                        continue;
                    }

                    if (evt.Value.BaseEvent.Id == command.EntityId)
                    {
                        targetEntity = evt.Value.PageInstance;

                        break;
                    }
                }
            }

            if (targetEntity != null)
            {
                int xDiff = command.X;
                int yDiff = command.Y;
                if (command.Dir == Direction.Down)
                {
                    var tmp = 0;
                    switch (targetEntity.Dir)
                    {
                        case Direction.Down:
                            yDiff *= -1;
                            xDiff *= -1;

                            break;
                        case Direction.Left:
                            tmp = yDiff;
                            yDiff = xDiff;
                            xDiff = tmp;

                            break;
                        case Direction.Right:
                            tmp = yDiff;
                            yDiff = xDiff;
                            xDiff = -tmp;

                            break;
                    }

                    direction = targetEntity.Dir;
                }

                mapId = targetEntity.MapId;
                tileX = (byte)(targetEntity.X + xDiff);
                tileY = (byte)(targetEntity.Y + yDiff);
            }
            else
            {
                return;
            }
        }

        var tile = new TileHelper(mapId, tileX, tileY);
        if (tile.TryFix() && MapController.TryGetInstanceFromMap(mapId, player.MapInstanceId, out var instance))
        {
            var npc = instance.SpawnNpc((byte)tileX, (byte)tileY, direction, npcId, true);
            player.SpawnedNpcs.Add(npc);
        }
    }

    //Despawn Npcs Command
    private static void ProcessCommand(
        DespawnNpcCommand command,
        Player player,
        Event instance,
        CommandInstance stackInfo,
        Stack<CommandInstance> callStack
    )
    {
        foreach (var entity in player.SpawnedNpcs.ToArray())
        {
            if (entity is Npc npc && npc.Despawnable == true)
            {
                lock (player.EntityLock)
                {
                    npc.Die();
                }
            }
        }

        player.SpawnedNpcs.Clear();
    }

    //Play Animation Command
    private static void ProcessCommand(
        PlayAnimationCommand command,
        Player player,
        Event instance,
        CommandInstance stackInfo,
        Stack<CommandInstance> callStack
    )
    {
        //Playing an animations requires a target type/target or just a tile.
        //We need an animation number and whether or not it should rotate (and the direction I guess)
        var animId = command.AnimationId;
        var mapId = command.MapId;
        var tileX = 0;
        var tileY = 0;
        var direction = Direction.Up;
        var targetEntity = (Entity)player;
        if (mapId != Guid.Empty)
        {
            tileX = command.X;
            tileY = command.Y;
            direction = (Direction)command.Dir;
        }
        else
        {
            if (command.EntityId != Guid.Empty)
            {
                foreach (var evt in player.EventLookup)
                {
                    if (evt.Value.MapId != instance.MapId)
                    {
                        continue;
                    }

                    if (evt.Value.BaseEvent.Id == command.EntityId)
                    {
                        targetEntity = evt.Value.PageInstance;

                        break;
                    }
                }
            }

            if (targetEntity != null)
            {
                if (command.X == 0 && command.Y == 0 && command.Dir == 0)
                {
                    var targetType = targetEntity.GetEntityType() == EntityType.Event ? 2 : 1;
                    //Attach to entity instead of playing on tile
                    if (command.InstanceToPlayer)
                    {
                        PacketSender.SendAnimationTo(
                            animId, targetType, targetEntity.Id, targetEntity.MapId, 0, 0, 0, player
                        );
                    }
                    else
                    {
                        PacketSender.SendAnimationToProximity(
                            animId, targetType, targetEntity.Id,
                            targetEntity.MapId, 0, 0, 0, targetEntity.MapInstanceId
                        );
                    }

                    return;
                }

                int xDiff = command.X;
                int yDiff = command.Y;
                if (command.Dir == 1)
                {
                    var tmp = 0;
                    switch (targetEntity.Dir)
                    {
                        case Direction.Down:
                            yDiff *= -1;
                            xDiff *= -1;

                            break;
                        case Direction.Left:
                            tmp = yDiff;
                            yDiff = xDiff;
                            xDiff = tmp;

                            break;
                        case Direction.Right:
                            tmp = yDiff;
                            yDiff = xDiff;
                            xDiff = -tmp;

                            break;
                    }

                    direction = targetEntity.Dir;
                }

                mapId = targetEntity.MapId;
                tileX = targetEntity.X + xDiff;
                tileY = targetEntity.Y + yDiff;
            }
            else
            {
                return;
            }
        }

        var tile = new TileHelper(mapId, tileX, tileY);
        if (tile.TryFix())
        {
            if (command.InstanceToPlayer)
            {
                PacketSender.SendAnimationTo(
                    animId, -1, Guid.Empty, tile.GetMapId(), tile.GetX(), tile.GetY(), direction, player
                );
            }
            else
            {
                PacketSender.SendAnimationToProximity(
                    animId, -1, Guid.Empty, tile.GetMapId(), tile.GetX(), tile.GetY(), direction, player.MapInstanceId
                );
            }
        }
    }

    //Hold Player Command
    private static void ProcessCommand(
        HoldPlayerCommand command,
        Player player,
        Event instance,
        CommandInstance stackInfo,
        Stack<CommandInstance> callStack
    )
    {
        instance.HoldingPlayer = true;
        PacketSender.SendHoldPlayer(player, instance.BaseEvent.Id, instance.BaseEvent.MapId);
    }

    //Release Player Command
    private static void ProcessCommand(
        ReleasePlayerCommand command,
        Player player,
        Event instance,
        CommandInstance stackInfo,
        Stack<CommandInstance> callStack
    )
    {
        instance.HoldingPlayer = false;
        PacketSender.SendReleasePlayer(player, instance.BaseEvent.Id);
    }

    //Hide Player Command
    private static void ProcessCommand(
        HidePlayerCommand command,
        Player player,
        Event instance,
        CommandInstance stackInfo,
        Stack<CommandInstance> callStack
    )
    {
        player.HideEntity = true;
        player.HideName = true;
        PacketSender.SendEntityDataToProximity(player);
    }

    //Show Player Command
    private static void ProcessCommand(
        ShowPlayerCommand command,
        Player player,
        Event instance,
        CommandInstance stackInfo,
        Stack<CommandInstance> callStack
    )
    {
        player.HideEntity = false;
        player.HideName = false;
        PacketSender.SendEntityDataToProximity(player);
    }

    //Play Bgm Command
    private static void ProcessCommand(
        PlayBgmCommand command,
        Player player,
        Event instance,
        CommandInstance stackInfo,
        Stack<CommandInstance> callStack
    )
    {
        PacketSender.SendPlayMusic(player, command.File);
    }

    //Fadeout Bgm Command
    private static void ProcessCommand(
        FadeoutBgmCommand command,
        Player player,
        Event instance,
        CommandInstance stackInfo,
        Stack<CommandInstance> callStack
    )
    {
        PacketSender.SendFadeMusic(player);
    }

    //Play Sound Command
    private static void ProcessCommand(
        PlaySoundCommand command,
        Player player,
        Event instance,
        CommandInstance stackInfo,
        Stack<CommandInstance> callStack
    )
    {
        PacketSender.SendPlaySound(player, command.File);
    }

    //Stop Sounds Command
    private static void ProcessCommand(
        StopSoundsCommand command,
        Player player,
        Event instance,
        CommandInstance stackInfo,
        Stack<CommandInstance> callStack
    )
    {
        PacketSender.SendStopSounds(player);
    }

    //Show Picture Command
    private static void ProcessCommand(
        ShowPictureCommand command,
        Player player,
        Event instance,
        CommandInstance stackInfo,
        Stack<CommandInstance> callStack
    )
    {
        var id = Guid.Empty;
        var shouldWait = command.WaitUntilClosed && (command.Clickable || command.HideTime > 0);
        if (shouldWait)
        {
            id = instance.PageInstance.Id;
            stackInfo.WaitingForResponse = CommandInstance.EventResponse.Picture;
        }

        PacketSender.SendShowPicture(player, command.File, command.Size, command.Clickable, command.HideTime, id);
    }

    //Hide Picture Command
    private static void ProcessCommand(
        HidePictureCommmand command,
        Player player,
        Event instance,
        CommandInstance stackInfo,
        Stack<CommandInstance> callStack
    )
    {
        PacketSender.SendHidePicture(player);
    }

    //Wait Command
    private static void ProcessCommand(
        WaitCommand command,
        Player player,
        Event instance,
        CommandInstance stackInfo,
        Stack<CommandInstance> callStack
    )
    {
        instance.WaitTimer = Timing.Global.Milliseconds + command.Time;
        callStack.Peek().WaitingForResponse = CommandInstance.EventResponse.Timer;
    }

    //Open Bank Command
    private static void ProcessCommand(
        OpenBankCommand command,
        Player player,
        Event instance,
        CommandInstance stackInfo,
        Stack<CommandInstance> callStack
    )
    {
        player.OpenBank();
        callStack.Peek().WaitingForResponse = CommandInstance.EventResponse.Bank;
    }

    //Open Shop Command
    private static void ProcessCommand(
        OpenShopCommand command,
        Player player,
        Event instance,
        CommandInstance stackInfo,
        Stack<CommandInstance> callStack
    )
    {
        player.OpenShop(ShopBase.Get(command.ShopId));
        callStack.Peek().WaitingForResponse = CommandInstance.EventResponse.Shop;
    }

    //Open Crafting Table Command
    private static void ProcessCommand(
        OpenCraftingTableCommand command,
        Player player,
        Event instance,
        CommandInstance stackInfo,
        Stack<CommandInstance> callStack
    )
    {
        player.OpenCraftingTable(CraftingTableBase.Get(command.CraftingTableId), command.JournalMode);
        callStack.Peek().WaitingForResponse = CommandInstance.EventResponse.Crafting;
    }

    //Set Class Command
    private static void ProcessCommand(
        SetClassCommand command,
        Player player,
        Event instance,
        CommandInstance stackInfo,
        Stack<CommandInstance> callStack
    )
    {
        if (ClassDescriptor.Get(command.ClassId) != null)
        {
            player.ClassId = command.ClassId;
            player.RecalculateStatsAndPoints();
            player.UnequipInvalidItems();
        }

        PacketSender.SendEntityDataToProximity(player);
    }

    //Start Quest Command
    private static void ProcessCommand(
        StartQuestCommand command,
        Player player,
        Event instance,
        CommandInstance stackInfo,
        Stack<CommandInstance> callStack
    )
    {
        var success = false;
        var quest = QuestBase.Get(command.QuestId);
        if (quest != null)
        {
            if (player.CanStartQuest(quest))
            {
                if (command.Offer)
                {
                    player.OfferQuest(quest);
                    stackInfo.WaitingForResponse = CommandInstance.EventResponse.Quest;
                    stackInfo.BranchIds = command.BranchIds;
                    stackInfo.WaitingOnCommand = command;

                    return;
                }
                else
                {
                    player.StartQuest(quest);
                    success = true;
                }
            }
        }

        List<EventCommand> newCommandList = null;
        if (success && stackInfo.Page.CommandLists.ContainsKey(command.BranchIds[0]))
        {
            newCommandList = stackInfo.Page.CommandLists[command.BranchIds[0]];
        }

        if (!success && stackInfo.Page.CommandLists.ContainsKey(command.BranchIds[1]))
        {
            newCommandList = stackInfo.Page.CommandLists[command.BranchIds[1]];
        }

        var tmpStack = new CommandInstance(stackInfo.Page)
        {
            CommandList = newCommandList,
            CommandIndex = 0,
        };

        callStack.Push(tmpStack);
    }

    //Complete Quest Task Command
    private static void ProcessCommand(
        CompleteQuestTaskCommand command,
        Player player,
        Event instance,
        CommandInstance stackInfo,
        Stack<CommandInstance> callStack
    )
    {
        player.CompleteQuestTask(command.QuestId, command.TaskId);
    }

    //End Quest Command
    private static void ProcessCommand(
        EndQuestCommand command,
        Player player,
        Event instance,
        CommandInstance stackInfo,
        Stack<CommandInstance> callStack
    )
    {
        player.CompleteQuest(command.QuestId, command.SkipCompletionEvent);
    }

    // Change Player Color Command
    private static void ProcessCommand(
        ChangePlayerColorCommand command,
        Player player,
        Event instance,
        CommandInstance stackInfo,
        Stack<CommandInstance> callStack
    )
    {
        player.Color = command.Color;
        PacketSender.SendEntityDataToProximity(player);
    }

    private static void ProcessCommand(
        ChangeNameCommand command,
        Player player,
        Event instance,
        CommandInstance stackInfo,
        Stack<CommandInstance> callStack
    )
    {
        var success = false;

        var variable = PlayerVariableDescriptor.Get(command.VariableId);
        if (variable != null)
        {
            if (variable.DataType == VariableDataType.String)
            {
                var data = player.GetVariable(variable.Id)?.Value;
                if (data != null)
                {
                    success = player.TryChangeName(data.String);
                }
            }
        }

        List<EventCommand> newCommandList = null;
        if (success && stackInfo.Page.CommandLists.ContainsKey(command.BranchIds[0]))
        {
            newCommandList = stackInfo.Page.CommandLists[command.BranchIds[0]];
        }

        if (!success && stackInfo.Page.CommandLists.ContainsKey(command.BranchIds[1]))
        {
            newCommandList = stackInfo.Page.CommandLists[command.BranchIds[1]];
        }

        var tmpStack = new CommandInstance(stackInfo.Page)
        {
            CommandList = newCommandList,
            CommandIndex = 0,
        };

        callStack.Push(tmpStack);
    }

    //Create Guild Command
    private static void ProcessCommand(
        CreateGuildCommand command,
        Player player,
        Event instance,
        CommandInstance stackInfo,
        Stack<CommandInstance> callStack
    )
    {
        var success = false;
        var playerVariable = PlayerVariableDescriptor.Get(command.VariableId);

        // We only accept Strings as our Guild Names!
        if (playerVariable.DataType == VariableDataType.String)
        {
            // Get our intended guild name
            var gname = player.GetVariable(playerVariable.Id)?.Value.String?.Trim();

            // Can we use this name according to our configuration?
            if (gname != null && FieldChecking.IsValidGuildName(gname, Strings.Regex.GuildName))
            {
                // Is the name already in use?
                if (Guild.GetGuild(gname) == null)
                {
                    // Is the player already in a guild?
                    if (player.Guild == null)
                    {
                        // Finally, we can actually MAKE this guild happen!
                        var guild = Guild.CreateGuild(player, gname);
                        if (guild != null)
                        {
                            // Send them a welcome message!
                            PacketSender.SendChatMsg(player, Strings.Guilds.Welcome.ToString(gname), ChatMessageType.Guild, CustomColors.Alerts.Success);

                            // Denote that we were successful.
                            success = true;
                        }
                    }
                    else
                    {
                        // This cheeky bugger is already in a guild, tell him so!
                        PacketSender.SendChatMsg(player, Strings.Guilds.AlreadyInGuild, ChatMessageType.Guild, CustomColors.Alerts.Error);
                    }
                }
                else
                {
                    // This name already exists, oh dear!
                    PacketSender.SendChatMsg(player, Strings.Guilds.GuildNameInUse, ChatMessageType.Guild, CustomColors.Alerts.Error);
                }
            }
            else
            {
                // Let our player know they need to adjust their name.
                PacketSender.SendChatMsg(player, Strings.Guilds.VariableInvalid, ChatMessageType.Guild, CustomColors.Alerts.Error);
            }
        }
        else
        {
            // Notify the user that something went wrong, the user really shouldn't see this.. Assuming the creator set up his events properly.
            PacketSender.SendChatMsg(player, Strings.Guilds.VariableNotString, ChatMessageType.Guild, CustomColors.Alerts.Error);
        }

        List<EventCommand> newCommandList = null;
        if (success && stackInfo.Page.CommandLists.ContainsKey(command.BranchIds[0]))
        {
            newCommandList = stackInfo.Page.CommandLists[command.BranchIds[0]];
        }

        if (!success && stackInfo.Page.CommandLists.ContainsKey(command.BranchIds[1]))
        {
            newCommandList = stackInfo.Page.CommandLists[command.BranchIds[1]];
        }

        var tmpStack = new CommandInstance(stackInfo.Page)
        {
            CommandList = newCommandList,
            CommandIndex = 0,
        };

        callStack.Push(tmpStack);
    }

    private static void ProcessCommand(
        DisbandGuildCommand command,
        Player player,
        Event instance,
        CommandInstance stackInfo,
        Stack<CommandInstance> callStack
    )
    {
        var success = false;

        // Is this player in a guild?
        if (player.Guild != null)
        {
            // Send the members a notification, then start wiping the guild from existence through sheer willpower!
            PacketSender.SendGuildMsg(player, Strings.Guilds.DisbandGuild.ToString(player.Guild.Name), CustomColors.Alerts.Info);
            Guild.DeleteGuild(player.Guild, player);
            player.UnequipInvalidItems();

            // :(
            success = true;
        }
        else
        {
            // They're not in a guild.. tell them?
            PacketSender.SendChatMsg(player, Strings.Guilds.NotInGuild, ChatMessageType.Guild, CustomColors.Alerts.Error);
        }

        List<EventCommand> newCommandList = null;
        if (success && stackInfo.Page.CommandLists.ContainsKey(command.BranchIds[0]))
        {
            newCommandList = stackInfo.Page.CommandLists[command.BranchIds[0]];
        }

        if (!success && stackInfo.Page.CommandLists.ContainsKey(command.BranchIds[1]))
        {
            newCommandList = stackInfo.Page.CommandLists[command.BranchIds[1]];
        }

        var tmpStack = new CommandInstance(stackInfo.Page)
        {
            CommandList = newCommandList,
            CommandIndex = 0,
        };

        callStack.Push(tmpStack);
    }

    //Open Guild Bank Command
    private static void ProcessCommand(
        OpenGuildBankCommand command,
        Player player,
        Event instance,
        CommandInstance stackInfo,
        Stack<CommandInstance> callStack
    )
    {
        player.OpenBank(true);
        callStack.Peek().WaitingForResponse = CommandInstance.EventResponse.Bank;
    }


    //Open Guild Bank Slots Count Command
    private static void ProcessCommand(
        SetGuildBankSlotsCommand command,
        Player player,
        Event instance,
        CommandInstance stackInfo,
        Stack<CommandInstance> callStack
    )
    {
        var quantity = 0;
        switch (command.VariableType)
        {
            case VariableType.PlayerVariable:
                quantity = (int)player.GetVariableValue(command.VariableId).Integer;

                break;
            case VariableType.ServerVariable:
                quantity = (int)ServerVariableDescriptor.Get(command.VariableId)?.Value.Integer;

                break;
            case VariableType.GuildVariable:
                quantity = (int)player.Guild?.GetVariableValue(command.VariableId)?.Integer;

                break;
        }
        var guild = player.Guild;
        if (quantity > 0 && guild != null && guild.BankSlotsCount != quantity)
        {
            guild.ExpandBankSlots(quantity);
        }
    }

    //Reset Stat Point Allocations Command
    private static void ProcessCommand(
        ResetStatPointAllocationsCommand command,
        Player player,
        Event instance,
        CommandInstance stackInfo,
        Stack<CommandInstance> callStack
    )
    {
        for (var i = 0; i < Enum.GetValues<Stat>().Length; i++)
        {
            player.StatPointAllocations[i] = 0;
        }
        player.RecalculateStatsAndPoints();
        player.UnequipInvalidItems();
        PacketSender.SendEntityDataToProximity(player);
    }

    // Cast Spell On command
    private static void ProcessCommand(
       CastSpellOn command,
       Player player,
       Event instance,
       CommandInstance stackInfo,
       Stack<CommandInstance> callStack
   )
    {
        if (player == null || !player.IsOnline)
        {
            return;
        }

        List<Player> affectedPlayers = new List<Player>();

        if (command.Self)
        {
            affectedPlayers.Add(player);
        }

        if (command.PartyMembers && player.IsInParty)
        {
            affectedPlayers.AddRange(player.Party.Where(pl => pl.Id != player.Id));
        }

        if (command.GuildMembers && player.IsInGuild)
        {
            affectedPlayers.AddRange(player.Guild.FindOnlineMembers().Where(pl => pl.Id != player.Id));
        }

        foreach (var affectedPlayer in affectedPlayers.DistinctBy(pl => pl.Id))
        {
            if (!affectedPlayer.IsOnline)
            {
                continue;
            }

            affectedPlayer?.CastSpell(command.SpellId);
        }
    }

    private static void ProcessCommand(
       ScreenFadeCommand command,
       Player player,
       Event instance,
       CommandInstance stackInfo,
       Stack<CommandInstance> callStack
    )
    {
        if (player == null || !player.IsOnline)
        {
            return;
        }

        player.IsFading = command.WaitForCompletion;
        PacketSender.SendFade(player, command.FadeType, command.WaitForCompletion, command.DurationMs);

        if (command.WaitForCompletion)
        {
            callStack.Peek().WaitingForResponse = CommandInstance.EventResponse.Fade;
        }
    }

    private static Stack<CommandInstance> LoadLabelCallstack(string label, EventPage currentPage)
    {
        var newStack = new Stack<CommandInstance>();
        newStack.Push(new CommandInstance(currentPage)); //Start from the top
        if (FindLabelResursive(newStack, currentPage, newStack.Peek().CommandList, label))
        {
            return newStack;
        }

        return null;
    }

    private static bool FindLabelResursive(
        Stack<CommandInstance> stack,
        EventPage page,
        List<EventCommand> commandList,
        string label
    )
    {
        if (page.CommandLists.ContainsValue(commandList))
        {
            while (stack.Peek().CommandIndex < commandList.Count)
            {
                var command = commandList[stack.Peek().CommandIndex];
                var branchIds = new List<Guid>();
                switch (command.Type)
                {
                    case EventCommandType.ShowOptions:
                        branchIds.AddRange(((ShowOptionsCommand)command).BranchIds);

                        break;
                    case EventCommandType.InputVariable:
                        branchIds.AddRange(((InputVariableCommand)command).BranchIds);

                        break;
                    case EventCommandType.ConditionalBranch:
                        branchIds.AddRange(((ConditionalBranchCommand)command).BranchIds);

                        break;
                    case EventCommandType.ChangeSpells:
                        branchIds.AddRange(((ChangeSpellsCommand)command).BranchIds);

                        break;
                    case EventCommandType.ChangeItems:
                        branchIds.AddRange(((ChangeItemsCommand)command).BranchIds);

                        break;
                    case EventCommandType.StartQuest:
                        branchIds.AddRange(((StartQuestCommand)command).BranchIds);

                        break;
                    case EventCommandType.Label:
                        //See if we found the label!
                        if (((LabelCommand)command).Label == label)
                        {
                            return true;
                        }

                        break;
                }

                //Test each branch
                foreach (var branch in branchIds)
                {
                    if (page.CommandLists.ContainsKey(branch))
                    {
                        var tmpStack = new CommandInstance(page)
                        {
                            CommandList = page.CommandLists[branch],
                            CommandIndex = 0,
                        };

                        stack.Peek().CommandIndex++;
                        stack.Push(tmpStack);
                        if (FindLabelResursive(stack, page, tmpStack.CommandList, label))
                        {
                            return true;
                        }

                        stack.Peek().CommandIndex--;
                    }
                }

                stack.Peek().CommandIndex++;
            }

            stack.Pop(); //We made it through a list
        }

        return false;
    }

    public static string ParseEventText(string input, Player player, Event instance)
    {
        if (input == null)
        {
            input = string.Empty;
        }

        if (player != null && input.Contains("\\"))
        {
            var sb = new StringBuilder(input);
            var time = Time.GetTime();
            var replacements = new Dictionary<string, string>()
            {
                { Strings.Events.PlayerNameCommand, player.Name },
                { Strings.Events.PlayerGuildCommand, player.Guild?.Name ?? "" },
                { Strings.Events.TimeHour, Time.Hour },
                { Strings.Events.MilitaryHour, Time.MilitaryHour },
                { Strings.Events.TimeMinute, Time.Minute },
                { Strings.Events.TimeSecond, Time.Second },
                { Strings.Events.TimePeriod, time.Hour >= 12 ? Strings.Events.PeriodEvening : Strings.Events.PeriodMorning },
                { Strings.Events.OnlineCountCommand, Player.OnlineCount.ToString() },
                { Strings.Events.OnlineListCommand, input.Contains(Strings.Events.OnlineListCommand) ? string.Join(", ", Player.OnlinePlayers.ToArray().Select(p => p.Name)) : string.Empty },
                { Strings.Events.EventNameCommand, instance?.PageInstance?.Name ?? "" },
                { Strings.Events.CommandParameter, instance?.PageInstance?.Param ?? "" },
                { Strings.Events.EventParameters, (instance != null && input.Contains(Strings.Events.EventParameters)) ? instance.FormatParameters(player) : "" },

            };

            foreach (var val in replacements)
            {
                if (input.Contains(val.Key))
                    sb.Replace(val.Key, val.Value);
            }

            foreach (var val in DbInterface.ServerVariableEventTextLookup)
            {
                if (input.Contains(val.Key))
                    sb.Replace(val.Key, (val.Value).Value.ToString((val.Value).DataType));
            }

            foreach (var val in DbInterface.PlayerVariableEventTextLookup)
            {
                if (input.Contains(val.Key))
                    sb.Replace(val.Key, player.GetVariableValue(val.Value.Id).ToString((val.Value).DataType));
            }

            foreach (var val in DbInterface.GuildVariableEventTextLookup)
            {
                if (input.Contains(val.Key))
                    sb.Replace(val.Key, (player.Guild?.GetVariableValue(val.Value.Id) ?? new VariableValue()).ToString((val.Value).DataType));
            }

            foreach (var val in DbInterface.UserVariableEventTextLookup)
            {
                if (input.Contains(val.Key))
                    sb.Replace(val.Key, (player.User.GetVariableValue(val.Value.Id) ?? new VariableValue()).ToString((val.Value).DataType));
            }

            if (instance != null)
            {
                var parms = instance.GetParams(player);
                foreach (var val in parms)
                {
                    sb.Replace(Strings.Events.EventParameter + "{" + val.Key + "}", val.Value);
                }
            }

            return sb.ToString();
        }

        return input;
    }

    private static void ProcessVariableModification(
        SetVariableCommand command,
        GameObjects.Events.VariableMod mod,
        Player player,
        Event instance
    )
    {
    }

    private static void ProcessVariableModification(
        SetVariableCommand command,
        BooleanVariableMod mod,
        Player player,
        Event instance
    )
    {
        VariableValue value = null;
        Guild guild = null;

        if (command.VariableType == VariableType.PlayerVariable)
        {
            value = player.GetVariableValue(command.VariableId);
        }
        else if (command.VariableType == VariableType.ServerVariable)
        {
            value = ServerVariableDescriptor.Get(command.VariableId)?.Value;
        }
        else if (command.VariableType == VariableType.GuildVariable)
        {
            guild = player.Guild;
            if (guild == null)
            {
                return;
            }
            value = guild.GetVariableValue(command.VariableId) ?? new VariableValue();
        }
        else if (command.VariableType == VariableType.UserVariable)
        {
            value = player.User.GetVariableValue(command.VariableId);
        }

        if (value == null)
        {
            value = new VariableValue();
        }

        var originalValue = value.Boolean;

        if (mod.DuplicateVariableId != Guid.Empty)
        {
            if (mod.DupVariableType == VariableType.PlayerVariable)
            {
                value.Boolean = player.GetVariableValue(mod.DuplicateVariableId).Boolean;
            }
            else if (mod.DupVariableType == VariableType.ServerVariable)
            {
                var variable = ServerVariableDescriptor.Get(mod.DuplicateVariableId);
                if (variable != null)
                {
                    value.Boolean = ServerVariableDescriptor.Get(mod.DuplicateVariableId).Value.Boolean;
                }
            }
            else if (mod.DupVariableType == VariableType.GuildVariable)
            {
                if (player.Guild != null)
                {
                    value.Boolean = player.Guild.GetVariableValue(mod.DuplicateVariableId).Boolean;
                }
            }
            else if (mod.DupVariableType == VariableType.UserVariable)
            {
                var variable = UserVariableDescriptor.Get(mod.DuplicateVariableId);
                if (variable != null)
                {
                    value.Boolean = player.User.GetVariableValue(mod.DuplicateVariableId).Value.Boolean;
                }
            }
        }
        else
        {
            value.Boolean = mod.Value;
        }

        var changed = value.Boolean != originalValue;

        if (command.VariableType == VariableType.PlayerVariable)
        {
            if (changed)
            {

            }

            // Set the party member switches too if Sync Party enabled!
            if (command.SyncParty)
            {
                if (changed)
                {
                    player.StartCommonEventsWithTrigger(CommonEventTrigger.PlayerVariableChange, "", command.VariableId.ToString());
                }

                foreach (var partyMember in player.Party)
                {
                    if (partyMember != player)
                    {
                        partyMember.SetSwitchValue(command.VariableId, mod.Value);
                    }
                }
            }
        }
        else if (command.VariableType == VariableType.ServerVariable)
        {
            if (changed)
            {
                Player.StartCommonEventsWithTriggerForAll(CommonEventTrigger.ServerVariableChange, "", command.VariableId.ToString());
                DbInterface.UpdatedServerVariables.AddOrUpdate(command.VariableId, ServerVariableDescriptor.Get(command.VariableId), (key, oldValue) => ServerVariableDescriptor.Get(command.VariableId));
            }
        }
        else if (command.VariableType == VariableType.GuildVariable)
        {
            if (changed)
            {
                guild.StartCommonEventsWithTriggerForAll(CommonEventTrigger.GuildVariableChange, "", command.VariableId.ToString());
                guild.UpdatedVariables.AddOrUpdate(command.VariableId, GuildVariableDescriptor.Get(command.VariableId), (key, oldValue) => GuildVariableDescriptor.Get(command.VariableId));
            }
        }
        else if (command.VariableType == VariableType.UserVariable)
        {
            if (changed)
            {
                player.User.StartCommonEventsWithTriggerForAll(CommonEventTrigger.UserVariableChange, "", command.VariableId.ToString());
                player.User.UpdatedVariables.AddOrUpdate(command.VariableId, UserVariableDescriptor.Get(command.VariableId), (key, oldValue) => UserVariableDescriptor.Get(command.VariableId));
            }
        }
    }

    private static void ProcessVariableModification(
        SetVariableCommand command,
        IntegerVariableMod mod,
        Player player,
        Event instance
    )
    {
        VariableValue value = null;
        Guild guild = null;

        if (command.VariableType == VariableType.PlayerVariable)
        {
            value = player.GetVariableValue(command.VariableId);
        }
        else if (command.VariableType == VariableType.ServerVariable)
        {
            value = ServerVariableDescriptor.Get(command.VariableId)?.Value;
        }
        else if (command.VariableType == VariableType.GuildVariable)
        {
            guild = player.Guild;
            if (guild == null)
            {
                return;
            }
            value = guild.GetVariableValue(command.VariableId);
        }
        else if (command.VariableType == VariableType.UserVariable)
        {
            value = player.User.GetVariableValue(command.VariableId);
        }

        if (value == null)
        {
            value = new VariableValue();
        }

        var originalValue = value.Integer;

        switch (mod.ModType)
        {
            case VariableModType.Set:
                value.Integer = mod.Value;

                break;
            case VariableModType.Add:
                value.Integer += mod.Value;

                break;
            case VariableModType.Subtract:
                value.Integer -= mod.Value;

                break;
            case VariableModType.Multiply:
                value.Integer *= mod.Value;

                break;
            case VariableModType.Divide:
                if (mod.Value != 0)  //Idiot proofing divide by 0 LOL
                {
                    value.Integer /= mod.Value;
                }

                break;
            case VariableModType.LeftShift:
                value.Integer <<= (int)mod.Value;

                break;
            case VariableModType.RightShift:
                value.Integer >>= (int)mod.Value;

                break;
            case VariableModType.Random:
                //TODO: Fix - Random doesnt work with longs lolz
                value.Integer = Randomization.Next((int)mod.Value, (int)mod.HighValue + 1);

                break;
            case VariableModType.SystemTime:
                value.Integer = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                break;
            case VariableModType.DupPlayerVar:
                value.Integer = player.GetVariableValue(mod.DuplicateVariableId).Integer;

                break;
            case VariableModType.DupGlobalVar:
                var dupServerVariable = ServerVariableDescriptor.Get(mod.DuplicateVariableId);
                if (dupServerVariable != null)
                {
                    value.Integer = dupServerVariable.Value.Integer;
                }

                break;
            case VariableModType.AddPlayerVar:
                value.Integer += player.GetVariableValue(mod.DuplicateVariableId).Integer;

                break;
            case VariableModType.AddGlobalVar:
                var asv = ServerVariableDescriptor.Get(mod.DuplicateVariableId);
                if (asv != null)
                {
                    value.Integer += asv.Value.Integer;
                }

                break;
            case VariableModType.SubtractPlayerVar:
                value.Integer -= player.GetVariableValue(mod.DuplicateVariableId).Integer;

                break;
            case VariableModType.SubtractGlobalVar:
                var ssv = ServerVariableDescriptor.Get(mod.DuplicateVariableId);
                if (ssv != null)
                {
                    value.Integer -= ssv.Value.Integer;
                }

                break;
            case VariableModType.MultiplyPlayerVar:
                value.Integer *= player.GetVariableValue(mod.DuplicateVariableId).Integer;

                break;
            case VariableModType.MultiplyGlobalVar:
                var msv = ServerVariableDescriptor.Get(mod.DuplicateVariableId);
                if (msv != null)
                {
                    value.Integer *= msv.Value.Integer;
                }

                break;
            case VariableModType.DividePlayerVar:
                if (player.GetVariableValue(mod.DuplicateVariableId).Integer != 0) //Idiot proofing divide by 0 LOL
                {
                    value.Integer /= player.GetVariableValue(mod.DuplicateVariableId).Integer;
                }

                break;
            case VariableModType.DivideGlobalVar:
                var dsv = ServerVariableDescriptor.Get(mod.DuplicateVariableId);
                if (dsv != null)
                {
                    if (dsv.Value != 0) //Idiot proofing divide by 0 LOL
                    {
                        value.Integer /= dsv.Value.Integer;
                    }
                }

                break;
            case VariableModType.LeftShiftPlayerVar:
                value.Integer = value.Integer << (int)player.GetVariableValue(mod.DuplicateVariableId).Integer;

                break;
            case VariableModType.LeftShiftGlobalVar:
                var lhsv = ServerVariableDescriptor.Get(mod.DuplicateVariableId);
                if (lhsv != null)
                {
                    value.Integer = value.Integer << (int)lhsv.Value.Integer;
                }

                break;
            case VariableModType.RightShiftPlayerVar:
                value.Integer = value.Integer >> (int)player.GetVariableValue(mod.DuplicateVariableId).Integer;

                break;
            case VariableModType.RightShiftGlobalVar:
                var rhsv = ServerVariableDescriptor.Get(mod.DuplicateVariableId);
                if (rhsv != null)
                {
                    value.Integer = value.Integer >> (int)rhsv.Value.Integer;
                }

                break;
        }

        var changed = value.Integer != originalValue;

        if (command.VariableType == VariableType.PlayerVariable)
        {
            if (changed)
            {
                player.StartCommonEventsWithTrigger(CommonEventTrigger.PlayerVariableChange, "", command.VariableId.ToString());
            }

            // Set the party member switches too if Sync Party enabled!
            if (command.SyncParty)
            {
                foreach (var partyMember in player.Party)
                {
                    if (partyMember != player)
                    {
                        partyMember.SetVariableValue(command.VariableId, value.Integer);
                        partyMember.StartCommonEventsWithTrigger(CommonEventTrigger.PlayerVariableChange, string.Empty, command.VariableId.ToString());
                    }
                }
            }
        }
        else if (command.VariableType == VariableType.ServerVariable)
        {
            if (changed)
            {
                Player.StartCommonEventsWithTriggerForAll(CommonEventTrigger.ServerVariableChange, "", command.VariableId.ToString());
                DbInterface.UpdatedServerVariables.AddOrUpdate(command.VariableId, ServerVariableDescriptor.Get(command.VariableId), (key, oldValue) => ServerVariableDescriptor.Get(command.VariableId));
            }
        }
        else if (command.VariableType == VariableType.GuildVariable)
        {
            if (changed)
            {
                guild.StartCommonEventsWithTriggerForAll(CommonEventTrigger.GuildVariableChange, "", command.VariableId.ToString());
                guild.UpdatedVariables.AddOrUpdate(command.VariableId, GuildVariableDescriptor.Get(command.VariableId), (key, oldValue) => GuildVariableDescriptor.Get(command.VariableId));
            }
        }
        else if (command.VariableType == VariableType.UserVariable)
        {
            if (changed)
            {
                player.User.StartCommonEventsWithTriggerForAll(CommonEventTrigger.UserVariableChange, "", command.VariableId.ToString());
                player.User.UpdatedVariables.AddOrUpdate(command.VariableId, UserVariableDescriptor.Get(command.VariableId), (key, oldValue) => UserVariableDescriptor.Get(command.VariableId));
            }
        }
    }

    private static void ProcessVariableModification(
        SetVariableCommand command,
        StringVariableMod mod,
        Player player,
        Event instance
    )
    {
        VariableValue value = null;
        Guild guild = null;

        if (command.VariableType == VariableType.PlayerVariable)
        {
            value = player.GetVariableValue(command.VariableId);
        }
        else if (command.VariableType == VariableType.ServerVariable)
        {
            value = ServerVariableDescriptor.Get(command.VariableId)?.Value;
        }
        else if (command.VariableType == VariableType.GuildVariable)
        {
            guild = player.Guild;
            if (guild == null)
            {
                return;
            }
            value = guild.GetVariableValue(command.VariableId);
        }
        else if (command.VariableType == VariableType.UserVariable)
        {
            value = player.User.GetVariableValue(command.VariableId);
        }

        if (value == null)
        {
            value = new VariableValue();
        }

        var originalValue = value.String;

        switch (mod.ModType)
        {
            case VariableModType.Set:
                value.String = ParseEventText(mod.Value, player, instance);

                break;
            case VariableModType.Replace:
                var find = ParseEventText(mod.Value, player, instance);
                var replace = ParseEventText(mod.Replace, player, instance);
                value.String = value.String.Replace(find, replace);

                break;
        }

        var changed = value.String != originalValue;

        if (command.VariableType == VariableType.PlayerVariable)
        {
            if (changed)
            {
                player.StartCommonEventsWithTrigger(CommonEventTrigger.PlayerVariableChange, "", command.VariableId.ToString());
            }

            // Set the party member switches too if Sync Party enabled!
            if (command.SyncParty)
            {
                foreach (var partyMember in player.Party)
                {
                    if (partyMember != player)
                    {
                        partyMember.SetVariableValue(command.VariableId, value.String);
                    }
                }
            }
        }
        else if (command.VariableType == VariableType.ServerVariable)
        {
            if (changed)
            {
                Player.StartCommonEventsWithTriggerForAll(CommonEventTrigger.ServerVariableChange, "", command.VariableId.ToString());
                DbInterface.UpdatedServerVariables.AddOrUpdate(command.VariableId, ServerVariableDescriptor.Get(command.VariableId), (key, oldValue) => ServerVariableDescriptor.Get(command.VariableId));
            }
        }
        else if (command.VariableType == VariableType.GuildVariable)
        {
            if (changed)
            {
                guild.StartCommonEventsWithTriggerForAll(CommonEventTrigger.GuildVariableChange, "", command.VariableId.ToString());
                guild.UpdatedVariables.AddOrUpdate(command.VariableId, GuildVariableDescriptor.Get(command.VariableId), (key, oldValue) => GuildVariableDescriptor.Get(command.VariableId));
            }
        }
        else if (command.VariableType == VariableType.UserVariable)
        {
            if (changed)
            {
                player.User.StartCommonEventsWithTriggerForAll(CommonEventTrigger.UserVariableChange, "", command.VariableId.ToString());
                player.User.UpdatedVariables.AddOrUpdate(command.VariableId, UserVariableDescriptor.Get(command.VariableId), (key, oldValue) => UserVariableDescriptor.Get(command.VariableId));
            }
        }
    }

}
