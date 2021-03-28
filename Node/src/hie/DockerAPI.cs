using Docker.DotNet;
using System;
using System.Threading.Tasks;
using System.Threading;
using Docker.DotNet.Models;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Node 
{
    class DockerAPI 
    {
        DockerClient _client;

        DockerAPI()
        {
            _client = new DockerClientConfiguration(
                new Uri(Params.DOCKER_ADVERTISED_HOST_NAME))
                .CreateClient();
            _client.DefaultTimeout = TimeSpan.FromMilliseconds(500);
        }

        public async Task<bool> PullImage(string image) 
        {
            try
            {
                AuthConfig auth = null;

                if (Params.DOCKER_USER != null && Params.DOCKER_PASSWORD != null && Params.DOCKER_EMAIL != null)
                {
                    auth = new AuthConfig()
                    {
                        Email = Params.DOCKER_EMAIL,
                        Username = Params.DOCKER_USER,
                        Password = Params.DOCKER_PASSWORD
                    };
                }

                await _client.Images.CreateImageAsync(new ImagesCreateParameters
                    {
                        FromImage = image,
                        Tag = "latest"
                    },
                        auth,
                        new Progress<JSONMessage>((message)=>Logger.Log("DOCKER", JsonConvert.SerializeObject(message), Logger.LogLevel.DEBUG))
                );
                Logger.Log("PullImage", "Pulled " + image, Logger.LogLevel.INFO);
                return true;
            } catch (Exception e) 
            { 
                Logger.Log("PullImage", e.Message, Logger.LogLevel.ERROR);
                return false;
            }
        }  

        public async Task<string> Containerize(string image, string host, int port, string machineName, int instance)
        {
            string ID = null;
            bool status = await PullImage(image);
            if (!status) return ID;
            
            CancellationTokenSource cts = new CancellationTokenSource(8000);
            cts.CancelAfter(8000);
            Task.Run(async () => {
                string _name = Params.NODE_NAME+"_"+image.Replace("/","_")+"_"+instance.ToString();
                var response = await _client.Containers.CreateContainerAsync(new CreateContainerParameters
                { 
                    Image = image,  
                    Name = _name,
                    Hostname = "localhost",
                    Env = new string[]{"HOST="+host, "PORT="+port.ToString(), "NAME="+machineName, "NODE_HOST="+Params.ADVERTISED_HOST_NAME, "NODE_NAME="+Params.NODE_NAME, "NODE_PORT="+Params.PORT_NUMBER.ToString(), "IMAGE="+image}, 
                    // HostConfig = new HostConfig { NetworkMode = "host" },
                    ExposedPorts = new Dictionary<string, EmptyStruct>
                    {
                        {
                            port.ToString(), default(EmptyStruct)
                        }
                    },
                    HostConfig = new HostConfig
                    {
                        PortBindings = new Dictionary<string, IList<PortBinding>>
                        {
                            {port.ToString(), new List<PortBinding> {new PortBinding {HostPort = port.ToString()}}}
                        },
                        PublishAllPorts = true
                    }
                });      

                Logger.Log("CreateImage", "Created container: " + response.ID, Logger.LogLevel.INFO);
                ID = response.ID;

                #pragma warning disable CS4014
                _client.Containers.StartContainerAsync(response.ID, null, cts.Token);

                Utils.Wait(300);

                Logger.Log("CreateImage", "Started container: " + response.ID, Logger.LogLevel.INFO);


                (bool running, string ID) info = await IsContainerRunning(_name);
                ID = info.ID;
                status = info.running;

                Logger.Log("CreateImage", "Container now running: " + response.ID, Logger.LogLevel.INFO);
            }).Wait(8000, cts.Token);

            if (ID == null || !status) Logger.Log("Containerize", "Did not manage to spin up container.", Logger.LogLevel.ERROR);
            
            return ID;
        }   

        public async Task StopContainers(string id)
        {
            if (id == null) return;
            try 
            {
                await _client.Containers.StopContainerAsync(
                    id,
                    new ContainerStopParameters
                    {
                        WaitBeforeKillSeconds = 0,
                    });
            } catch (Exception) {}
        }

        public async Task<bool> IsContainerRunning(string id, string image)  
        {  
            if (id == null || image == null) return false;

            IList<ContainerListResponse> t = await _client.Containers.ListContainersAsync(
                new ContainersListParameters
                {
                    Filters = new Dictionary<string, IDictionary<string, bool>>
                    {
                        ["id"] = new Dictionary<string, bool>
                        {
                            [id] = true
                        }
                    },
                    Limit = 1
                }); 

            foreach (ContainerListResponse c in t)
            {
                if (c.ID == id)
                // https://docs.docker.com/engine/api/v1.41/#operation/ContainerList
                {
                    if (c.State == "exited" || c.State == "dead") return false;
                    return true;
                }
            }
            return false;
        }  

        public async Task<(bool running, string id)> IsContainerRunning(string name)  
        {  
            IList<ContainerListResponse> t = await _client.Containers.ListContainersAsync(
                new ContainersListParameters
                {
                    Filters = new Dictionary<string, IDictionary<string, bool>>
                    {
                        ["name"] = new Dictionary<string, bool>
                        {
                            [name] = true
                        }
                    },
                    Limit = 1
                }); 

            foreach (ContainerListResponse c in t)
            {
                if (c.Names.Contains("/"+name))
                // https://docs.docker.com/engine/api/v1.41/#operation/ContainerList
                {
                    if (c.State == "exited" || c.State == "dead") return (false, c.ID);
                    return (true, c.ID);
                }
            }
            return (false, null);
        }  

        public async Task<string[]> GetContainerIDs(string name = "")  
        {  
            if (name=="") name = Params.NODE_NAME;
            IList<ContainerListResponse> t = await _client.Containers.ListContainersAsync(
                new ContainersListParameters()); 

            List<string> ids = new List<string>();
            foreach (ContainerListResponse c in t)
            {
                foreach (string n in c.Names)
                {
                    if (n.ToString().Contains(name))
                    {
                        if (c.State == "running" || c.State == "created") 
                        {
                            ids.Add(c.ID);
                            break;
                        }    
                    }
                }
                
            }
            return ids.ToArray();
        }  

        public async Task RemoveStoppedContainers()
        {
            string[] ids = await GetContainerIDs();
            foreach(string id in ids)
                await StopContainers(id);
            IList<ContainerListResponse> t = await _client.Containers.ListContainersAsync(new ContainersListParameters()); 
            foreach (ContainerListResponse c in t)
            {
                if (c.State == "exited" || c.State == "dead") 
                {
                    await _client.Containers.RemoveContainerAsync(c.ID, new ContainerRemoveParameters());
                    Logger.Log("RemoveContainer", "Removed container: " + c.ID, Logger.LogLevel.INFO);
                }
            }
            await _client.Containers.PruneContainersAsync();
        }

        private static readonly object _lock = new object();
        private static DockerAPI _instance = null;

        public static DockerAPI Instance 
        {
            get 
            {
                if (_instance == null) _instance = new DockerAPI();
                return _instance;
            }
        }
    }
}