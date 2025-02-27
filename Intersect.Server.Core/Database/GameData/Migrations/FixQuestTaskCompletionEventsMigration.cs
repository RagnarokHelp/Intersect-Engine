﻿using Intersect.Core;
using Intersect.Framework.Core.GameObjects.Events;
using Microsoft.Extensions.Logging;

namespace Intersect.Server.Database.GameData.Migrations;

public partial class FixQuestTaskCompletionEventsMigration
{

    public static void Run(GameContext context)
    {
        FixQuestTaskCompletionEvents(context);
    }

    public static void FixQuestTaskCompletionEvents(GameContext context)
    {
        ApplicationContext.Context.Value?.Logger.LogInformation("Checking for broken Quest Task Completion Events, this process might take several minutes depending on your quest count!");

        // Go through each and every quest to check if all the tasks have valid events.
        foreach (var quest in context.Quests)
        {
            foreach(var task in quest.Tasks)
            {
                // Determine whether the event Id is incorrect and if we can find an event with the task Id as intended.
                var incorrectEventId = task.CompletionEventId == null || task.CompletionEventId == Guid.Empty || context.Events.Where(e => e.Id == task.CompletionEventId).FirstOrDefault() == null;
                var foundEvent = context.Events.Where(e => e.Id == task.Id).FirstOrDefault();

                // If the event Id is incorrect and we can't find the event.. recreate it!
                if (incorrectEventId && foundEvent == null)
                {
                    var ev = new EventDescriptor(task.Id, Guid.Empty, 0, 0, false);
                    ev.CommonEvent = false;
                    ev.Name = $"Quest: {quest.Name} - Task Completion Event";
                    context.Events.Add(ev);
                    EventDescriptor.Lookup.Set(ev.Id, ev);
                    task.CompletionEventId = task.Id;

                    ApplicationContext.Context.Value?.Logger.LogInformation($"Fixed quest {quest.Name} ({quest.Id}) task {task.Id}, created new event {task.Id}.");
                }
                // if the Event ID is incorrect but we CAN find the event, link it!
                else if (incorrectEventId && foundEvent != null)
                {
                    task.CompletionEventId = foundEvent.Id;

                    ApplicationContext.Context.Value?.Logger.LogInformation($"Fixed quest {quest.Name} ({quest.Id}) task {task.Id}, linked up old event {foundEvent.Id}.");
                }
            }
        }

        // Track our changes and save them or the work we've just done is lost.
        context.ChangeTracker.DetectChanges();
        context.SaveChanges();
    }

}
