using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Framework.Server;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Server;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MyTFSPlugin.StateChange
{
    abstract class StateChangeFacilitator
    {
        protected static TfsTeamProjectCollection tpc;
        protected static WorkItemStore workItemStore;
        protected static Project teamProject;
        protected static WorkItemStore wis;

        /// <summary>
        /// Hooking into TFS Hooks on the server
        /// </summary>
        static StateChangeFacilitator()
        {
            System.Threading.Tasks.Task.Factory.StartNew(() =>
            {
                try
                {
                    tpc = new TfsTeamProjectCollection(new Uri("http://localhost:8080/tfs/DefaultCollection"));
                    workItemStore = tpc.GetService<WorkItemStore>();
                    teamProject = workItemStore.Projects["STT"];
                    wis = tpc.GetService<WorkItemStore>();
                }
                catch (Exception ex)
                {
                    TeamFoundationApplicationCore.LogException("Error processing event", ex);
                }
            });
        }

        public abstract void HandleStateChange(StringField state, WorkItemChangedEvent ev);

        /// <summary>
        /// Get Parent Item
        /// </summary>
        /// <param name="wi">Child Work Item</param>
        /// <returns>Parent Work Item</returns>
        protected WorkItem GetParentItem(WorkItem wi)
        {
            var linkType = workItemStore.WorkItemLinkTypes[CoreLinkTypeReferenceNames.Hierarchy];
            int parentId = -1;
            foreach (var link in wi.WorkItemLinks)
            {
                if (link is WorkItemLink)
                {
                    var workItemLink = (WorkItemLink)link;
                    if (workItemLink.LinkTypeEnd == linkType.ReverseEnd)
                    {
                        parentId = workItemLink.TargetId;
                        break;
                    }
                }
            }
            if (parentId == -1)
                throw new Exception($"Could not find parent item of work item with id {wi.Id}.");

            var ar = wis.GetWorkItem(parentId);
            return ar;
        }

        /// <summary>
        /// Get Item From Work Item Changed Event
        /// </summary>
        /// <param name="ev">Work Item Changed Event</param>
        /// <returns>Changed Work Item</returns>
        protected WorkItem GetItemFromWorkItemChangedEvent(WorkItemChangedEvent ev)
        {
            int workItemId = ev.CoreFields.IntegerFields
                                          .Where(field => field.Name.Equals("ID"))
                                          .FirstOrDefault().NewValue;

            return wis.GetWorkItem(workItemId);
        }

        /// <summary>
        /// Get Work Items By Work Item Type And LinkType (Hierarchy Link Type)
        /// </summary>
        /// <param name="wi">Work Item</param>
        /// <param name="workItemType">Work Item Type string</param>
        /// <returns>Linked Work Item Collection</returns>
        protected List<WorkItem> GetWorkItemsByTypeAndLinkType(WorkItem wi, string workItemType)
        {
            var linkType = workItemStore.WorkItemLinkTypes[CoreLinkTypeReferenceNames.Hierarchy];
            List<WorkItem> Results = new List<WorkItem>();
            foreach (var link in wi.WorkItemLinks)
            {
                if (link is WorkItemLink)
                {
                    var workItemLink = (WorkItemLink)link;
                    if (workItemLink.LinkTypeEnd == linkType.ForwardEnd)
                    {
                        var possibleResult = wis.GetWorkItem(workItemLink.TargetId);
                        if (possibleResult.Type.Name == workItemType)
                        {
                            Results.Add(possibleResult);
                        }
                    }
                }
            }
            return Results;
        }

        /// <summary>
        /// Get WorkItems By Work Item Type And Predecessor Link Type
        /// </summary>
        /// <param name="wi">Work Item</param>
        /// <param name="workItemType">Work Item Type</param>
        /// <returns>Linked Work Item Collection</returns>
        protected List<WorkItem> GetWorkItemsByTypeAndPredecessorLinkType(WorkItem wi, string workItemType)
        {
            var linkType = workItemStore.WorkItemLinkTypes[CoreLinkTypeReferenceNames.Dependency];
            List<WorkItem> Results = new List<WorkItem>();
            foreach (var link in wi.WorkItemLinks)
            {
                if (link is WorkItemLink)
                {
                    var workItemLink = (WorkItemLink)link;
                    if (workItemLink.LinkTypeEnd.Name == "Predecessor" && (workItemLink.LinkTypeEnd == linkType.ForwardEnd || workItemLink.LinkTypeEnd == linkType.ReverseEnd))
                    {
                        var possibleResult = wis.GetWorkItem(workItemLink.TargetId);
                        if (possibleResult.Type.Name == workItemType)
                        {
                            Results.Add(possibleResult);
                        }
                    }
                }
            }
            return Results;
        }

        /// <summary>
        /// Update And Save Work Item
        /// </summary>
        /// <param name="wi">Work Item to Update and Save</param>
        /// <param name="state">Transition to State</param>
        /// <param name="reason">Transition to State's Reason</param>
        protected void UpdateStateAndSave(WorkItem wi, string state, string reason)
        {
            if (wi.State == state)
                return;

            try
            {
                wi.State = state;
                wi.Reason = reason;
                wi.ValidateAndSave();
            }
            catch (Exception ex)
            {
                var errors = string.Empty;
                foreach (var obj in wi.Validate())
                {
                    errors += Environment.NewLine + obj.ToString();
                }
                throw new Exception($"An error occurred while moving workitem '{wi.Id}' type '{wi.Fields[CoreField.WorkItemType].Value}' to state '{state}' with reason '{reason}' validation errors: {errors}", ex);
            }
            finally
            {
                wi.Store.RefreshCache(true);
                wi.SyncToLatest();
            }
        }
    }
}
