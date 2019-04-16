using Microsoft.TeamFoundation.WorkItemTracking.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Server;

namespace MyTFSPlugin.StateChange
{
    class ProductBacklogItem : StateChangeFacilitator
    {
        public override void HandleStateChange(StringField state, WorkItemChangedEvent ev)
        {
            var workItem = GetItemFromWorkItemChangedEvent(ev);

            // To customize the process of the work item
            switch (state.NewValue.ToUpper())
            {
                case "APPROVED":
                    MovedToApproved(state.OldValue, ev, workItem);
                    break;
                case "REMOVED":
                    MovedToRemoved(state.OldValue, ev, workItem);
                    break;
                case "COMITTED":
                    MovedToComitted(state.OldValue, ev, workItem);
                    break;
                case "DONE":
                    MovedToDone(state.OldValue, ev, workItem);
                    break;
                default:
                    break;
            }
        }
        
        /// <summary>
        /// Moved to Done
        /// </summary>
        /// <param name="oldValue">Previous State Value</param>
        /// <param name="ev">Work Item Changed Event</param>
        /// <param name="workItem">Work Item</param>
        private void MovedToDone(string oldValue, WorkItemChangedEvent ev, WorkItem workItem)
        {
            // The item has entered the end of its life cycle
        }

        /// <summary>
        /// Moved to Commited
        /// </summary>
        /// <param name="oldValue">Previous State Value</param>
        /// <param name="ev">Work Item Changed Event</param>
        /// <param name="workItem">Work Item</param>
        private void MovedToComitted(string oldValue, WorkItemChangedEvent ev, WorkItem workItem)
        {
            UpdateStateAndSave(workItem, "Done", "Completed");
        }

        /// <summary>
        /// Moved To Removed
        /// </summary>
        /// <param name="oldValue">Previous State Value</param>
        /// <param name="ev">Work Item Changed Event</param>
        /// <param name="workItem">Work Item</param>
        private void MovedToRemoved(string oldValue, WorkItemChangedEvent ev, WorkItem workItem)
        {
            UpdateStateAndSave(workItem, "Done", "Completed");
        }

        /// <summary>
        /// Moved to Approved
        /// </summary>
        /// <param name="oldValue">Previous State Value</param>
        /// <param name="ev">Work Item Changed Event</param>
        /// <param name="workItem">Work Item</param>
        private void MovedToApproved(string oldValue, WorkItemChangedEvent ev, WorkItem workItem)
        {
            UpdateStateAndSave(workItem, "Comitted", "Changes Made");
        }
    }
}
