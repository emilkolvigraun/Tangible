using System.Collections.Generic;

namespace Node 
{
    class HardwareAbstraction
    {

        ///<summary>
        ///<para>Extracts the required images from the RDF model</para>
        ///<para>Returns a list of Jobs based on the request</para>
        ///<para>Subscribe jobs has a shadow operator, other only has one for operation</para>
        ///</summary>
        // public List<(Job OP, Job SD)> CreateJobs(ActionRequest request)
        // {
        //     // Extracts buildings environment
        //     string query = CreateQuery(request.Location);

        //     // Execute the query and obtain a table of sensors per image needed
        //     Dictionary<string, List<string>> table = ExecuteQuery(query);

        //     // Init the list of jobs to return
        //     List<(Job OP, Job SD)> _jobs = new List<(Job OP, Job SD)>();

        //     foreach(KeyValuePair<string, List<string>> image in table)
        //     {
                
        //         Job shadow = null;
        //         if (request.TypeOf == RequestType.SUBSCRIBE)
        //         {
        //             shadow = Job.CreateJob(request, image);
        //             shadow.TypeOf = Job.Type.SD;
        //         }

        //         Job operational = Job.CreateJob(request, image);
        //         operational.TypeOf = Job.Type.OP;

        //         _jobs.Add((operational, shadow));
        //     }

        //     // Return the jobs
        //     return _jobs;
        // }

        public List<Request> CreateRequests(ActionRequest request)
        {   
            // Extracts buildings environment
            string query = CreateQuery(request.Location);

            // Execute the query and obtain a table of sensors per image needed
            Dictionary<string, List<string>> table = ExecuteQuery(query, request.Location);

            // tb returned
            List<Request> requests = new List<Request>();

            foreach(KeyValuePair<string, List<string>> _set in table)
            {
                foreach(string pointid in _set.Value)
                {
                    string uuid = Utils.GetUniqueKey();
                    while (Ledger.Instance.RequestIDsContains(uuid) || RequestQueue.Instance.ContainsRequest(uuid))
                    {
                        uuid = Utils.GetUniqueKey();
                    }
                    requests.Add(new Request(){
                        Image = _set.Key,
                        ID = uuid,
                        PointID = pointid,
                        ReturnTopic = request.ReturnTopic,
                        TypeOf = request.Action,
                        Value = request.Value,
                        Timestamp = request.Timestamp
                    });
                }
            }
            return requests;
        }

        public static List<string> Images
        {
            get
            {
                // extract all images from rdf model
                return new List<string>{"emilkolvigraun/tangible-driver"};
            }
        }

        private string CreateQuery(GraphLocation benv)
        {
            // TODO: IMPLEMENT QUERY CREATION
            return "";
        }

        private Dictionary<string, List<string>> ExecuteQuery(string query, GraphLocation loc)
        {
            // Initialize the table
            Dictionary<string, List<string>> ImageTable = new Dictionary<string, List<string>>();
            
            // Execute the query on the RDF model
            // retrieve array of sensors and images
            // TODO: NOT YET IMPLEMENTED

            // Foreach sensor and image, fill in the dictionary
            string testImage = "emilkolvigraun/tangible-driver";
            ImageTable.Add(testImage, new List<string>());
            foreach (string p in ExtractPoints(loc))
                ImageTable[testImage].Add(p);

            // Return the table
            return ImageTable;
        }

        private List<string> ExtractPoints(GraphLocation loc)
        {
            List<string> points = new List<string>();
            if (loc.HasPoint != null)
                points.Add(loc.HasPoint.ID);
            if (loc.LocationOf != null)
                points.AddRange(ExtractPoints(loc.LocationOf));
            return points;
        }


        private static readonly object _lock = new object();
        private static HardwareAbstraction _instance = null;
        public static HardwareAbstraction Instance 
        {
            get 
            {
                if (_instance == null) _instance = new HardwareAbstraction();
                return _instance;
            }
        }
    }
}