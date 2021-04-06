using System.Collections.Generic;

namespace TangibleNode
{
    class HardwareAbstraction
    {
        public HardwareAbstraction(string pathToRdfModel)
        {
            // Load RDF model
        }

        public void LoadDriverImages(HardwareInteractionEnvironment hie)
        {
            if (Params.RUN_HIE)
            {
                Logger.Write(Logger.Tag.WARN, "Removing dangeling containers...");
                Docker.Instance.RemoveStoppedContainers().GetAwaiter().GetResult();
                Logger.Write(Logger.Tag.INFO, "Preparing warm start...");
                foreach(string image in Get_Images)
                {
                    hie.PrepareWarmStart(image);
                }
            }
        }

        private List<string> Get_Images
        {
            get 
            {
                // TODO: DESIGN AND IMPLEMENT
                return new List<string>{"emilkolvigraun/tangible-test-driver"};
            }
        }

        public List<Request> MarshallDataRequest(DataRequest dataRequest)
        {

            Dictionary<string, List<string>> table = ExtractFromRDF(dataRequest.Benv);

            List<Request> requests = new List<Request>();
            
            foreach(KeyValuePair<string, List<string>> r1 in table)
            {
                Action action = new Action(){
                    Type = dataRequest.Type,
                    PointID = r1.Value,
                    Image = r1.Key,
                    Priority = dataRequest.Priority,
                    Value = dataRequest.Value,
                    ID = Utils.GenerateUUID(),
                    Assigned = StateLog.Instance.Peers.ScheduleAction(),
                    T0 = dataRequest.T0,
                    T1 = Utils.Millis.ToString(),
                    ReturnTopic = dataRequest.ReturnTopic
                };
                StateLog.Instance.AppendAction(action);
                Request r0 = new Request() {
                    ID = Utils.GenerateUUID(),
                    Data = Encoder.EncodeAction(action),
                    Type = Request._Type.ACTION
                };

                StateLog.Instance.AddRequestBehindToAll(r0);
                
                requests.Add(r0);
            }
            return requests;
        }

        private Dictionary<string, List<string>> ExtractFromRDF(Location location)
        {
            List<string> points = ParseLocation(location);
            
            // extract image from RDF for each point

            Dictionary<string, List<string>> table = new Dictionary<string, List<string>>();
            table.Add("emilkolvigraun/tangible-test-driver", points);
            
            return table;
        }

        private List<string> ParseLocation(Location location0)
        {
            List<string> points = new List<string>();
            if(location0.HasPoint!=null)foreach (Point point in location0.HasPoint)
            {
                points.Add(point.ID);
            }
            if(location0.LocationOf!=null)foreach (Location location1 in location0.LocationOf)
            {
                points.AddRange(ParseLocation(location1));
            }
            return points;
        }
    }
}