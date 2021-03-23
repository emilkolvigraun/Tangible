using System;
using System.Net.Security;
using System.Linq;
using Newtonsoft.Json;
using System.Dynamic;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net.NetworkInformation;

namespace Node
{
    static class Extensions
    {
        public static byte[] GetBytes(this string str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }
        public static string GetString(this byte[] bytes)
        {
            try 
            {
                char[] chars = new char[bytes.Length / sizeof(char)];
                System.Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
                return new string(chars);
            } catch (Exception)
            {
                try 
                {
                    char[] chars = new char[bytes.Length];
                    System.Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
                    return new string(chars);
                } catch(Exception e)
                {
                    Logger.Log("Utils", "GetString, "+e.Message, Logger.LogLevel.ERROR);
                    return null;
                }
            }
        }
        public static byte[] ReadRequest(this SslStream sslStream)
        {
            // Read the  message sent by the client.
            // The client signals the end of the message using the
            // "<EOF>" marker.
            byte[] b = new byte[2048];
            byte[] cb = null;
            int bytes = -1;
            do
            {
                bytes = sslStream.Read(b, 0, b.Length);
                byte[] rb = b.Take(bytes).ToArray();
                if (cb == null) cb = rb;
                else
                {
                    byte[] tb = new byte[cb.Length + rb.Length];
                    int offset = 0;
                    Buffer.BlockCopy(cb, 0, tb, offset, cb.Length);
                    offset += cb.Length;
                    Buffer.BlockCopy(rb, 0, tb, offset, rb.Length);
                    cb = tb;
                }

                if (bytes < 2048) break;

            } while (bytes != 0);
            return cb;
        }

        public static byte[] EncodeRequest(this IRequest request)
        {
            return EncodeRequestStr(request).GetBytes();
        }
        public static string EncodeRequestStr(this IRequest request)
        {
            try
            {
                return JsonConvert.SerializeObject(request, Formatting.None);
            } catch(Exception e)
            {
                Logger.Log("EncodeRequest", e.Message, Logger.LogLevel.ERROR);
                return null;
            }
        }
        public static dynamic DecodeRequest(this byte[] request)
        {
            return request.GetString().DecodeRequest();
        }
        public static dynamic DecodeRequest(this string request)
        {
            try 
            {
                return JsonConvert.DeserializeObject<ExpandoObject>(request);
            } catch (Exception e)
            {
                Logger.Log("DecodeRequest", e.Message, Logger.LogLevel.ERROR);
                return new EmptyRequest();
            }
        }
        public static RequestType GetRequestType(this string typeOf)
        {
            return (RequestType) Enum.Parse(typeof(RequestType), typeOf);
        }
        public static IRequest ParseRequest(this byte[] request)
        {
            dynamic r0 = request.DecodeRequest();
            string typeOf = r0.TypeOf.ToString();
            return GetRequest(typeOf, request.GetString());         
        }
        public static IRequest ParseRequest(this string request)
        {
            dynamic r0 = request.DecodeRequest();
            string typeOf = r0.TypeOf.ToString();
            return GetRequest(typeOf, request);
        }
        private static IRequest GetRequest(string typeOf, string request)
        {
            try 
            {
                switch (typeOf.GetRequestType())
                {
                    // Intercom requests
                    case RequestType.AE:
                        return JsonConvert.DeserializeObject<AppendEntriesRequest>(request);
                    case RequestType.AR:
                        return JsonConvert.DeserializeObject<AppendEntriesResponse>(request);
                    case RequestType.RS:
                        return JsonConvert.DeserializeObject<RegistrationRequest>(request);
                    case RequestType.CT:
                        return JsonConvert.DeserializeObject<CertificateResponse>(request);
                    case RequestType.VT:
                        return JsonConvert.DeserializeObject<VotingRequest>(request);
                    case RequestType.BC:
                        return JsonConvert.DeserializeObject<BroadcastRequest>(request);
                    case RequestType.ST:
                        return JsonConvert.DeserializeObject<StatusResponse>(request);
                    // Action requests
                    case RequestType.READ: 
                        goto case RequestType.CREATE_USER;
                    case RequestType.WRITE: 
                        goto case RequestType.CREATE_USER;
                    case RequestType.SUBSCRIBE: 
                        goto case RequestType.CREATE_USER;
                    case RequestType.CREATE_ROLE: 
                        goto case RequestType.CREATE_USER;
                    case RequestType.CREATE_USER:
                        return JsonConvert.DeserializeObject<ActionRequest>(request);
                    // default
                    default:
                        return JsonConvert.DeserializeObject<EmptyRequest>(request);
                }
            } catch (Exception e)
            {
                Logger.Log("GetRequest", e.Message, Logger.LogLevel.ERROR);
                return new EmptyRequest();
            }
        }
    
        public static IRequest GetCreateRequest(this RequestType _type)
        {
            switch (_type)
            {
                // Intercom requests
                case RequestType.RS: return new RegistrationRequest();
                case RequestType.AE: return new AppendEntriesRequest();
                case RequestType.BC: return new BroadcastRequest();
                case RequestType.ST: return new StatusResponse();
                case RequestType.VT: return new VotingRequest();
                // Action requests
                case RequestType.READ: goto case RequestType.CREATE_USER;
                case RequestType.WRITE: goto case RequestType.CREATE_USER;
                case RequestType.SUBSCRIBE: goto case RequestType.CREATE_USER;
                case RequestType.CREATE_ROLE: goto case RequestType.CREATE_USER;
                case RequestType.CREATE_USER: return new ActionRequest();
                // default
                default: return new EmptyRequest();
            }
        }

        public static Dictionary<string, MetaNode> Copy(this Dictionary<string, MetaNode> d0)
        {
            return d0.ToDictionary(entry => entry.Key, entry => entry.Value);
        }
        public static Dictionary<string, int> Copy(this Dictionary<string, int> d0)
        {
            return d0.ToDictionary(entry => entry.Key, entry => entry.Value);
        }
        public static MetaNode[] AsNodeArray(this Dictionary<string, MetaNode> d0)
        {
            List<MetaNode> nodes = new List<MetaNode>();
            foreach(KeyValuePair<string, MetaNode> n0 in d0)
                nodes.Add(n0.Value);
            return nodes.ToArray();
        }

        public static string GetMajority(this List<string> votes)
        {
            // throws exception if length of votes is less than 1
            return votes.GroupBy( i => i ).OrderByDescending(group => group.Count()).ElementAt(0).Key;
        }

        public static string[] GetAsToString(this Dictionary<string, MetaNode> d0)
        {
            List<string> sl = new List<string>();
            foreach(KeyValuePair<string, MetaNode> n in d0){
                sl.Add(n.Value.Name+":"+n.Value.Jobs.Length.ToString());
            }
            return sl.ToArray();
        }
        public static string[] GetAsToString(this MetaNode[] mns)
        {
            List<string> sl = new List<string>();
            foreach(MetaNode n in mns){
                sl.Add(n.Name+":"+n.Jobs.Length.ToString());
            }
            return sl.ToArray();
        }
        public static List<string> GetAsToStringName(this MetaNode[] mns)
        {
            List<string> sl = new List<string>();
            foreach(MetaNode n in mns){
                sl.Add(n.Name);
            }
            return sl;
        }

        public static List<BasicNode> AsBasicNodes (this MetaNode[] nodes)
        {
            List<BasicNode> basicNodes = new List<BasicNode>();
            foreach(MetaNode node in nodes)
                basicNodes.Add(BasicNode.MakeBasicNode(node));
            return basicNodes;
        }

        public static bool ContainsKey(this IEnumerable<BasicNode> nodes, string n0)
        {
            foreach(BasicNode n1 in nodes)
            {
                if (n1.Name == n0) return true;
            }
            return false;
        }
        public static bool ContainsID(this IEnumerable<BasicNode> nodes, string n0)
        {
            foreach(BasicNode n1 in nodes)
            {
                if (n1.ID == n0) return true;
            }
            return false;
        }
        public static BasicNode GetByID(this IEnumerable<BasicNode> nodes, string n0)
        {
            foreach(BasicNode n1 in nodes)
            {
                if (n1.ID == n0) return n1;
            }
            return null;
        }
        public static bool ContainsKey(this IEnumerable<Job> jobs, Job j0)
        {
            foreach(Job j1 in jobs)
            {
                if (j1.ID == j0.ID) return true;
            }
            return false;
        }
        public static bool ContainsKey(this IEnumerable<Job> jobs, string j0)
        {
            foreach(Job j1 in jobs)
            {
                if (j1.ID == j0) return true;
            }
            return false;
        }
        public static bool ContainsKey(this IEnumerable<MetaNode> nodes, string key)
        {
            foreach(MetaNode n0 in nodes)
            {
                if (n0.Name == key) return true;
            }
            return false;
        }
        public static bool ContainsKey(this IEnumerable<PlainMetaNode> nodes, string key)
        {
            foreach(PlainMetaNode n0 in nodes)
            {
                if (n0.Name == key) return true;
            }
            return false;
        }

        public static BasicNode GetByName(this List<BasicNode> nodes, string name)
        {
            foreach(BasicNode n in nodes)
            {
                if (n.Name == name) return n;
            }
            return null;
        }

        public static string[] GetIds(this Job[] jobs)
        {
            List<string> _ids = new List<string>();
            foreach (Job j0 in jobs)
            {
                _ids.Add(j0.ID);
            }
            return _ids.ToArray();
        }
        public static string[] GetIdsIfNotComplete(this Job[] jobs)
        {
            List<string> _ids = new List<string>();
            foreach (Job j0 in jobs)
            {
                if (j0.StatusOf != Job.Status.CP) _ids.Add(j0.ID);
            }
            return _ids.ToArray();
        }

        public static string[] GetIds(this Dictionary<string, MetaNode> nodes)
        {
            List<string> _ids = new List<string>();
            foreach (MetaNode n0 in nodes.Values)
            {
                _ids.Add(n0.ID);
            }
            return _ids.ToArray();
        }

        public static MetaNode Copy(this MetaNode n1)
        {
            return new MetaNode(){
                ID = n1.ID,
                Host = n1.Host,
                Port = n1.Port,
                Name = n1.Name,
                Jobs = n1.Jobs,
                Nodes = n1.Nodes
            };
        }

        public static TcpState GetState(this TcpClient tcpClient)
        {
        var foo = IPGlobalProperties.GetIPGlobalProperties()
            .GetActiveTcpConnections()
            .FirstOrDefault(x => x.LocalEndPoint.Equals(tcpClient.Client.LocalEndPoint));
        return foo != null ? foo.State : TcpState.Unknown;
        }

        public static (string node, string[] jobIds)[] GetLedger(this Dictionary<string, MetaNode> nodes)
        {
            List<(string node, string[] jobIds)> _ledger = new List<(string node, string[] jobIds)>();
            foreach (MetaNode n0 in nodes.Values)
            {
                _ledger.Add((n0.ID, n0.Jobs.GetIds()));
            }
            return _ledger.ToArray();
        }

        public static bool ContainsNode(this IEnumerable<(string node, string[] jobIds)> ledger, string id)
        {
            foreach ((string node, string[] jobIds) node in ledger)
            {
                if (node.node == id) return true;
            }
            return false;
        }

        public static bool JobEquality(this BasicNode node, string[] jobIds)
        {
            List<string> _jobIds = jobIds.ToList();
            foreach(Job job in node.Jobs)
            {
                if (!_jobIds.Contains(job.ID)) return false;
            }
            return true;
        }

        public static string[] GetJobIds(this IEnumerable<(string node, string[] jobIds)> ledger, string id)
        {
            foreach ((string node, string[] jobIds) node in ledger)
            {
                if (node.node == id) return node.jobIds;
            }
            return new string[]{};
        }

        public static BasicNode Copy(this BasicNode node, string[] jobs)
        {
            List<Job> nodeJobs = new List<Job>();
            List<string> copyJobs = jobs.ToList();
            foreach(Job job in node.Jobs)
            {
                if (copyJobs.Contains(job.ID))
                {
                    nodeJobs.Add(job);
                }
            }
            return new BasicNode(){
                ID = node.ID,
                Name = node.Name,
                Jobs = nodeJobs.ToArray()
            };
        }

        public static bool ContainsKey(this (string Node, Job[] jobs)[] _ledger, string key)
        {
            foreach((string Node, Job[] jobs) n0 in _ledger)
            {
                if (n0.Node == key) return true;
            } 
            return false;
        }

        public static Job[] GetJobs(this (string Node, Job[] jobs)[] _ledger, string key)
        {
            foreach((string Node, Job[] jobs) n0 in _ledger)
            {
                if (n0.Node == key) return n0.jobs;
            } 
            return new Job[]{};
        }

        public static Job GetJob(this Job[] jobs, string j0)
        {
            foreach (Job j1 in jobs)
            {
                if (j1.ID == j0) return j1;
            }
            return null;
        }
    }
}