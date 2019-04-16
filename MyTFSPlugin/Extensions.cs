using Microsoft.TeamFoundation.WorkItemTracking.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyTFSPlugin
{
    public static class Extensions
    {
        public static void ValidateAndSave(this WorkItem wi)
        {
            try
            {
                if (!wi.IsValid())
                {
                    var errors = string.Empty;
                    foreach (Field field in wi.Validate())
                    {
                        errors += Environment.NewLine + $"{field.Name} (value of {field.Value.ToString()}) has error {Enum.GetName(typeof(FieldStatus), field.Status)}";

                        if (field.Name == "Assigned To")
                        {
                            wi.Fields[CoreField.AssignedTo].Value = "";
                            if (wi.IsValid())
                            {
                                wi.Save();
                                return;
                            }
                        }
                    }
                    throw new Exception($"Validation errors: {errors}");
                }

                wi.Save();
            }
            catch (Exception ex)
            {
                throw new Exception($"An error occurred while saving Item ID: '{wi.Id}'; Work Item Type: {wi.Type};  Title: {wi.Title}; " +
                    $"Assigned To: {wi.Fields[CoreField.AssignedTo].Value}; Area Path: {wi.AreaPath}; " +
                    $"Iteration: {wi.IterationPath}; Reason: {wi.Reason}; State: {wi.State}; "
                    , ex);
            }
        }
    }
}
