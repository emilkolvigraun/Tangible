using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace Node 
{
    class Driver 
    {
        // Making sure that the container is thread-safe
        private readonly object _lock = new object();

        // The ID is assigned from docker
        public string ID {get; set;} = null;
        public string MachineName {get; set;} = null;

        // Used for communicating with the driver
        public string Host {get; set;} = null;
        public int Port {get; set;}

        // The Image is assigned when the container instance is created
        public string Image {get; set;} = null;

        // Jobs are added and removed based on their status
        public Dictionary<string, Job> Deployed {get;} = new Dictionary<string, Job>();
        private List<Execute> JobsNotSend = new List<Execute>();
        private List<RunAsRequest> ChangeStateNotSend = new List<RunAsRequest>();

        public bool IsRunning
        {
            get 
            {
                lock(_running_lock)
                {
                    return running;
                }
            }
        }

        private bool running = false;
        private bool started = false;

        public bool IsStarted
        {
            get 
            {
                lock(_started_lock)
                {
                    return started;
                }
            }
        }

        private readonly object _started_lock = new object();
        private readonly object _running_lock = new object();
        private readonly object _not_send_lock = new object();
        private readonly object _client_lock = new object();
        private readonly object _change_lock = new object();
        private NodeClient _client {get; set;}

        private NodeClient ContainerClient
        {
            get 
            {
                lock(_client_lock)
                {
                    if(_client == null) _client = new NodeClient();
                    return _client;
                }
            }
        }

        public Dictionary<string, Job> Jobs 
        {
            get 
            {
                lock (_lock)
                {
                    return Deployed;
                }
            }
        }

        public bool AppendJob(Job job)
        {
            lock (_not_send_lock) lock (_lock)
            {
                if (!Deployed.ContainsKey(job.ID))
                {
                    Deployed.Add(job.ID, job);
                    foreach (string sid in job.PointIds)
                    {
                        string _id = Utils.GetUniqueKey(size:10);
                        JobsNotSend.Add(new Execute()
                        {
                            ID = _id,
                            PointID = sid,
                            JobID = job.ID,
                            TypeOfAction = job.TypeOfRequest.GetActionType(),
                            JobType = job.TypeOf,
                            Value = job.Value
                        });
                    }
                    Logger.Log("Deploy", "Assigned job to driver [job:"+job.ID+", image:"+Image + "]", Logger.LogLevel.IMPOR);
                    return false;
                } else {
                    Logger.Log("Deploy", "Unable to deploy job: A similar job already exists", Logger.LogLevel.ERROR);
                    return true;
                }
            }
        }

        public bool IsVerifyingState
        {
            get 
            {
                lock(_state_lock)
                {
                    return verifying_state;
                }
            }
        }

        private readonly object _state_lock = new object();
        private bool verifying_state = false;
        public void VerifyState()
        {
            lock (_state_lock) if (verifying_state) return;

            Task.Run(() => {
                lock(_state_lock) verifying_state = true;
                bool running = DockerAPI.Instance.IsContainerRunning(ID, Image).GetAwaiter().GetResult();
                if (!running)
                {
                    // Reset running bool
                }
            });
        }

        public void UpdateCounterPart(string jobId, string newNode)
        {
            lock(_lock)
            {
                if (Deployed.ContainsKey(jobId))
                {
                    Deployed[jobId].CounterPart = (newNode, Deployed[jobId].CounterPart.JobId);
                    if (Deployed[jobId].TypeOf == Job.Type.SD)
                    {
                        lock(_change_lock)
                        {
                            Deployed[jobId].TypeOf = Job.Type.OP;
                            string _id = Utils.GetUniqueKey(size:10);
                            ChangeStateNotSend.Add(new RunAsRequest(){
                                JobID = jobId,
                                ID = _id
                            });
                        }
                    }
                }
        
            }
        }

        private bool _transmitting = false;
        private readonly object _transmit_lock = new object();
        public bool IsTransmitting
        {
            get 
            {
                lock(_transmit_lock)
                {
                    return _transmitting;
                }
            }
        }

        public void TransmitNewJobs()
        {
            lock (_transmit_lock)
            {
                lock(_not_send_lock) if (_transmitting || JobsNotSend.Count == 0) return;
                
                _transmitting = true;
                Task.Run(()=>{
                    lock (this._not_send_lock)
                    {
                        try 
                        {
                            List<Execute> jns = JobsNotSend.ToList();
                            foreach (Execute j0 in jns)
                            {
                                try 
                                {
                                    IRequest response = ContainerClient.Run(Host, Port, MachineName, j0);
                                    if (response.TypeOf != RequestType.ST || !((StatusResponse)response).Status)
                                    {
                                        break;
                                    }   
                                    Logger.Log("TransmitNewJobs", "Transmitted job: " + j0.ID, Logger.LogLevel.IMPOR);
                                    lock(_lock)
                                    {
                                        if (Deployed.ContainsKey(j0.ID) && Deployed[j0.ID].StatusOf == Job.Status.NS)
                                        {
                                            Deployed[j0.ID].StatusOf = Job.Status.OG;
                                        }
                                    }
                                    JobsNotSend.Remove(j0);
                                } catch (Exception)
                                {
                                    Logger.Log("TransmitNewJobs", "Driver not ready yet", Logger.LogLevel.DEBUG);
                                    break;
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Logger.Log("TransmitNewJobs", e.Message, Logger.LogLevel.ERROR);
                        }
                    }
                    lock(this._change_lock)
                    {
                        try 
                        {
                            List<RunAsRequest> rns = ChangeStateNotSend.ToList();
                            foreach (RunAsRequest r0 in rns)
                            {
                                try 
                                {
                                    IRequest response = ContainerClient.Run(Host, Port, MachineName, r0);
                                    if (response.TypeOf != RequestType.ST )//|| !((StatusResponse)response).Status)
                                    {
                                        break;
                                    }   
                                    Logger.Log("TransmitChange", "Transmitted change: " + r0.ID + ", " + r0.JobID + "->" + r0.TypeOf, Logger.LogLevel.IMPOR);
                                    lock(_lock)
                                    {
                                        if (Deployed.ContainsKey(r0.ID) && Deployed[r0.ID].StatusOf == Job.Status.NS)
                                        {
                                            Deployed[r0.ID].StatusOf = Job.Status.OG;
                                        }
                                    }
                                    ChangeStateNotSend.Remove(r0);
                                } catch (Exception)
                                {
                                    Logger.Log("TransmitChange", "Driver not ready yet", Logger.LogLevel.DEBUG);
                                    break;
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Logger.Log("TransmitChange", e.Message, Logger.LogLevel.ERROR);
                        }
                    }
                    lock(this._transmit_lock) this._transmitting = false;
                });
            }
        }

        public void StartDriver()
        {
            lock(_started_lock) started = true;
            Task.Run(()=>{
                this.ID = DockerAPI.Instance.Containerize(this.Image, this.Host, this.Port, this.MachineName).GetAwaiter().GetResult();
                if (this.ID == null) Logger.Log(this.ID, "Unable to deploy container", Logger.LogLevel.ERROR);
                else 
                {
                    lock(this._running_lock) this.running = true;
                    lock(this._started_lock) this.started = false;
                    lock(this._state_lock) this.verifying_state = false;
                    Logger.Log("StartDriver", "Started driver with [image:"+Image+", host:" + Host + ":" + Port.ToString() + ", id:" + this.ID + "]", Logger.LogLevel.INFO);
                }
            });
        }
    }
}