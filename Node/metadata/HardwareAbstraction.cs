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
        public List<(Job OP, Job SD)> CreateJobs(ActionRequest request)
        {
            // Extracts buildings environment
            string query = CreateQuery(request.Location);

            // Execute the query and obtain a table of sensors per image needed
            Dictionary<string, List<string>> table = ExecuteQuery(query);

            // Init the list of jobs to return
            List<(Job OP, Job SD)> _jobs = new List<(Job OP, Job SD)>();

            foreach(KeyValuePair<string, List<string>> image in table)
            {
                
                Job shadow = null;
                if (request.TypeOf == RequestType.SUBSCRIBE)
                {
                    shadow = Job.CreateJob(request, image);
                }

                Job operational = Job.CreateJob(request, image);

                _jobs.Add((operational, shadow));
            }

            // Return the jobs
            return _jobs;
        }

        private string CreateQuery(GraphLocation benv)
        {
            // TODO: IMPLEMENT QUERY CREATION
            return "";
        }

        private Dictionary<string, List<string>> ExecuteQuery(string query)
        {
            // Initialize the table
            Dictionary<string, List<string>> ImageTable = new Dictionary<string, List<string>>();
            
            // Execute the query on the RDF model
            // retrieve array of sensors and images
            // TODO: NOT YET IMPLEMENTED

            // Foreach sensor and image, fill in the dictionary
            string testImage = "test-image-docker";
            ImageTable.Add(testImage, new List<string>());
            ImageTable[testImage].Add("test-sensor-1");

            // Return the table
            return ImageTable;
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