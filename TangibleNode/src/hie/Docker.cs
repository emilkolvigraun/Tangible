using Docker.DotNet;
using System;
using System.Threading.Tasks;
using System.Threading;
using Docker.DotNet.Models;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace TangibleNode
{
    class Docker 
    {
        DockerClient _client;

        Docker()
        {
            _client = new DockerClientConfiguration(
                new Uri(Params.DOCKER_REMOTE_HOST))
                .CreateClient();
            _client.DefaultTimeout = TimeSpan.FromMilliseconds(500);
        }

        public async Task<bool> PullImage(string image) 
        {
            try
            {
                AuthConfig auth = null;

                await _client.Images.CreateImageAsync(new ImagesCreateParameters
                    {
                        FromImage = image,
                        Tag = "latest"
                    },
                        auth,
                        new Progress<JSONMessage>()//(message)=>Logger.Write(Logger.Tag.ERROR, JsonConvert.SerializeObject(message)))
                );
                Logger.Write(Logger.Tag.INFO, "Pulled " + image);
                return true;
            } catch (Exception e) 
            { 
                Logger.Write(Logger.Tag.ERROR, e.ToString());
                return false;
            }
        }  

        public async Task<string> Containerize(DriverConfig config)
        {
            string ID = null;
            bool status = await PullImage(config.Image);
            if (!status) return ID;
            
            CancellationTokenSource cts = new CancellationTokenSource(8000);
            cts.CancelAfter(8000);
            Task.Run(async () => {
                var response = await _client.Containers.CreateContainerAsync(new CreateContainerParameters
                { 
                    Image = config.Image,  
                    Name = config.ID,
                    Hostname = "localhost",
                    Env = new string[]{"HOST="+config.Host, "PORT="+config.Port.ToString(), "ID="+config.ID, "NODE_HOST="+config.Maintainer.Host, "NODE_NAME="+config.Maintainer.ID, "NODE_PORT="+config.Maintainer.Port.ToString()}, 
                    // HostConfig = new HostConfig { NetworkMode = "host" },
                    ExposedPorts = new Dictionary<string, EmptyStruct>
                    {
                        {
                            config.Maintainer.Port.ToString(), default(EmptyStruct)
                        }
                    },
                    HostConfig = new HostConfig
                    {
                        PortBindings = new Dictionary<string, IList<PortBinding>>
                        {
                            {config.Maintainer.Port.ToString(), new List<PortBinding> {new PortBinding {HostPort = config.Maintainer.Port.ToString()}}}
                        },
                        PublishAllPorts = true
                    }
                });      

                
                Logger.Write(Logger.Tag.INFO, "Created container: " + response.ID);
                ID = response.ID;

                #pragma warning disable CS4014
                _client.Containers.StartContainerAsync(response.ID, null, cts.Token).GetAwaiter().GetResult();

                Logger.Write(Logger.Tag.INFO, "Started container: " + response.ID);

                (bool running, string ID) info = await IsContainerRunning(config.ID);
                ID = info.ID;
                status = info.running;

                Logger.Write(Logger.Tag.COMMIT, "Container now running: " + response.ID);
            }).Wait(8000, cts.Token);

            if (ID == null || !status) Logger.Write(Logger.Tag.ERROR, "Did not manage to spin up container."); 
            
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
            if (name=="") name = Params.ID;
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
                    Logger.Write(Logger.Tag.INFO, "Removed container: " + c.ID);
                }
            }
            await _client.Containers.PruneContainersAsync();
        }

        private static readonly object _lock = new object();
        private static Docker _instance = null;

        public static Docker Instance 
        {
            get 
            {
                if (_instance == null) _instance = new Docker();
                return _instance;
            }
        }
    }
}