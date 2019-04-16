using Microsoft.TeamFoundation.Common;
using Microsoft.TeamFoundation.Framework.Server;
using Microsoft.TeamFoundation.WorkItemTracking.Server;
using MyTFSPlugin.StateChange;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace MyTFSPlugin
{
    public class WorkItemChangedEventHandler : ISubscriber
    {
        public WorkItemChangedEventHandler()
        {
            StateChangeFacilitators = new Dictionary<string, StateChangeFacilitator>();
            // Add the Different Work Item Types you want to customize
            StateChangeFacilitators.Add("PRODUCT BACKLOG", new ProductBacklogItem());
            //StateChangeFacilitators.Add("BUG", new Bug());
        }

        public string Name
        {
            get { return "WorkItemChangedEventHandler"; }
        }

        public SubscriberPriority Priority
        {
            get { return SubscriberPriority.Normal; }
        }

        Dictionary<string, StateChangeFacilitator> StateChangeFacilitators;

        public EventNotificationStatus ProcessEvent(
            IVssRequestContext requestContext,
            NotificationType notificationType,
            object notificationEventArgs,
            out int statusCode,
            out string statusMessage,
            out ExceptionPropertyCollection properties)
        {
            statusCode = 0;
            properties = null;
            statusMessage = String.Empty;

            WorkItemChangedEvent ev = null;

            try
            {
                // Get Notification from Work Item Changed Event
                if (notificationType == NotificationType.Notification && notificationEventArgs is WorkItemChangedEvent)
                {
                    ev = notificationEventArgs as WorkItemChangedEvent;

                    // Log the Change in the Application Event Log (Not Necessary but good to know when an item was changed)
                    if (ev.CoreFields.IntegerFields.Any(field => field.Name.Equals("ID")))
                    {
                        TeamFoundationApplicationCore.Log($"Collection Name: {requestContext.ServiceHost.Name} WorkItem {ev.CoreFields.IntegerFields.Where(field => field.Name.Equals("ID")).FirstOrDefault().NewValue} changed", 0, EventLogEntryType.Information);
                    }

                    if (requestContext.ServiceHost.Name != "DefaultCollection")
                    {
                        return EventNotificationStatus.ActionPermitted;
                    }

                    var workItemType = ev.CoreFields.StringFields.Single(e => e.Name == "Work Item Type").NewValue;

                    if (ev.ChangedFields != null)
                    {
                        foreach (var item in ev.ChangedFields.StringFields)
                        {
                            // Work Item State Change
                            if (item.Name.ToUpper() == "STATE")
                            {
                                if (StateChangeFacilitators.ContainsKey(workItemType.ToUpper()))
                                {
                                    StateChangeFacilitators[workItemType.ToUpper()].HandleStateChange(item, ev);
                                }
                            }
                            // You can place listeners to ant Field that has changed
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                int id = 0;
                if (ev != null && ev.CoreFields.IntegerFields.Any(field => field.Name.Equals("ID")))
                {
                    id = ev.CoreFields.IntegerFields
                            .Where(field => field.Name.Equals("ID"))
                            .FirstOrDefault().NewValue;
                };

                TeamFoundationApplicationCore.LogException($"Error processing event on task {id}", exception);
            }
            return EventNotificationStatus.ActionPermitted;
        }

        public Type[] SubscribedTypes()
        {
            return new Type[] { typeof(WorkItemChangedEvent) };
        }
    }
}
