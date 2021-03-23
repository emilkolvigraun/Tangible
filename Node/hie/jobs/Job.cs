using System.Collections.Generic;

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
        public string[] PointIds {get; set;}

        // Indicates whether the job is under processing, not yet started or completed
        public Status StatusOf {get; set;}

        // Indicates whether this is a job to act as primary or secondary
        public Type TypeOf {get; set;}

        // Whether the job is to subscribe, read or write
        public RequestType TypeOfRequest {get; set;}

        public string Topic {get; set;}

        public string Value {get; set;} = null;

        public static Job CreateJob(ActionRequest request, KeyValuePair<string, List<string>> info)
        {

            string id = Utils.GetUniqueKey(size:10);
            Job job = new Job(){
                ID = id,
                Priority = request.Priority,
                Image = info.Key,
                PointIds = info.Value.ToArray(),
                StatusOf = Job.Status.NS,
                TypeOf = Job.Type.SD,
                TypeOfRequest = request.TypeOf,
                Value = request.Value
            };
            return job;
        }
    }
}