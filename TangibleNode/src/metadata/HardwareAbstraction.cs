using System.Collections.Generic;
using System;

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
                while(true)
                {
                    try 
                    {
                        Docker.Instance.RemoveStoppedContainers().GetAwaiter().GetResult();
                        break;
                    } catch 
                    {
                        Utils.Sleep(Utils.GetRandomInt(Params.ELECTION_TIMEOUT_START, Params.ELECTION_TIMEOUT_END));
                    }
                }
                Logger.Write(Logger.Tag.INFO, "Preparing warm start...");
                foreach(string image in Get_Images)
                {
                    Utils.Sleep(Utils.GetRandomInt(Params.ELECTION_TIMEOUT_START, Params.ELECTION_TIMEOUT_END));
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

        public void MarshallDataRequest(ESBDataRequest esbRequest)
        {
            Dictionary<string, List<string>> pointDetails = ExtractFromRDF(esbRequest.Benv);

            // List<Request> requests = new List<Request>();
            string assigned_to = StateLog.Instance.Nodes.ScheduleRequest();
            DataRequest action = new DataRequest(){
                Type = esbRequest.Type,
                PointDetails = pointDetails,
                Priority = esbRequest.Priority,
                Value = esbRequest.Value,
                ID = Utils.GenerateUUID(),
                Assigned = assigned_to,
                ReturnTopic = esbRequest.ReturnTopic,
                Received = Utils.Micros.ToString() // debugging
            };

            if (assigned_to != null)
            {
                // Logger.Write(Logger.Tag.WARN, "SCHEDULING to " + action.Assigned);
                StateLog.Instance.AppendAction(action);
                Call r0 = new Call() {
                    ID = Utils.GenerateUUID(),
                    Data = Encoder.EncodeDataRequest(action),
                    Type = Call._Type.DATA_REQUEST
                };
                StateLog.Instance.AddToAllBehind(r0);
                // requests.Add(r0);
            } else 
            {
                if (Params.TEST_RECEIVER_HOST!=string.Empty)
                {
                    try 
                    {
                        TestReceiverClient.Instance.AddEntryError("Failed to deploy driver and to process request.");
                    } catch (Exception e)
                    {
                        Logger.Write(Logger.Tag.ERROR, "ADD_ENTRY: " + e.ToString());
                    }
                }
            }


        }

        private Dictionary<string, List<string>> ExtractFromRDF(Location location)
        {
            List<string> points = ParseLocation(location);
            
            // extract image from RDF for each point

            Dictionary<string, List<string>> table = new Dictionary<string, List<string>>();
            table.Add("emilkolvigraun/tangible-test-driver", points);

            // foreach(KeyValuePair<string, List<string>> tableInfo in table)
            //     Console.WriteLine(tableInfo.Key + " " + tableInfo.Value);
            
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