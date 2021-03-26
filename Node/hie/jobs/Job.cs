using System.Collections.Generic;
using System.Linq;

namespace Node 
{
    public class Job 
    {
        public enum Type
        {
            SD, // shadow operate
            OP // operative 
        }
        public enum Status
        {
            CP, // Complete
            OG, // Ongoing
            NS // Not started
        }
        // put the things you need from the request here
        public int Priority {get; set;}

        // The unique ID of this job
        public string ID {get; set;}

        // The particular driver to spin up
        public string Image {get; set;}

        // All of the ids of the points to execute the request
        public HashSet<string> PointIds {get; set;}

        // Indicates whether the job is under processing, not yet started or completed
        public Status StatusOf {get; set;}

        // Indicates whether this is a job to act as primary or secondary
        public Type TypeOf {get; set;}

        // only valid for subscriptions
        public  string CounterPart {get; set;} = null;

        // Whether the job is to subscribe, read or write
        public RequestType TypeOfRequest {get; set;}

        public string Topic {get; set;}

        public string Value {get; set;} = null;

        public static Job CreateJob(ActionRequest request, KeyValuePair<string, List<string>> info)
        {

            string id = Utils.GetUniqueKey(size:15);

            // It is important that we generate a unique job ID
            while (Ledger.Instance.GetAllJobIds.Contains(id))
            {
                id = Utils.GetUniqueKey(size:15);
            }

            Job job = new Job(){
                ID = id,
                Priority = request.Priority,
                Image = info.Key,
                PointIds = new HashSet<string>(info.Value),
                StatusOf = Job.Status.NS,
                TypeOf = Job.Type.SD,
                TypeOfRequest = request.TypeOf,
                Value = request.Value,
                Topic = request.ReturnTopic
            };
            return job;
        }
    }
}