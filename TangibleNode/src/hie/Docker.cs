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
                return IsImagePulled(image);
            } catch (Exception e) 
            { 
                Logger.Write(Logger.Tag.ERROR, e.ToString());
                return false;
            }

            
        }  

        public bool IsImagePulled(string image)
        {
            IList<ImagesListResponse> images = _client.Images.ListImagesAsync(new ImagesListParameters{
                All = true
            }).GetAwaiter().GetResult();
            foreach (ImagesListResponse ilr in images)
            {
                if (ilr.ID.Contains(image)) 
                {
                    Logger.Write(Logger.Tag.INFO, "Pulled " + image);
                    return true;
                }
            }
            return false;
        }

        public async Task<string> Containerize(DriverConfig config)
        {
            string ID = null;
            bool status = await PullImage(config.Image);
            if (!status) return ID;
            
            // CancellationTokenSource cts = new CancellationTokenSource(8000);
            // cts.CancelAfter(8000);
            // Task.Run(async () => {
                var response = await _client.Containers.CreateContainerAsync(new CreateContainerParameters
                { 
                    Image = config.Image,  
                    Name = config.ID,
                    Hostname = "localhost",
                    Env = new string[]{
                        "HOST="+config.Host, 
                        "PORT="+config.Port.ToString(), 
                        "ID="+config.ID, 
                        "TIMEOUT="+Params.TIMEOUT.ToString(),
                        "BATCH_SIZE="+Params.BATCH_SIZE.ToString(),
                        "NODE_HOST="+config.Maintainer.Host, 
                        "NODE_NAME="+config.Maintainer.ID, 
                        "NODE_PORT="+config.Maintainer.Port.ToString()
                    }, 
                    ExposedPorts = new Dictionary<string, EmptyStruct>
                    {
                        {
                            config.Port.ToString(), default(EmptyStruct)
                        }
                    },
                    HostConfig = new HostConfig
                    {
                        PortBindings = new Dictionary<string, IList<PortBinding>>
                        {
                            {config.Port.ToString(), new List<PortBinding> {new PortBinding {HostPort = config.Port.ToString()}}}
                        },
                        PublishAllPorts = true
                    }
                } );      
                
                Logger.Write(Logger.Tag.INFO, "Created container: " + response.ID);
                ID = response.ID;

                // #pragma warning disable CS4014
                // _client.Containers.StartContainerAsync(response.ID, null, cts.Token).GetAwaiter().GetResult();
                _client.Containers.StartContainerAsync(response.ID, null).GetAwaiter().GetResult();

                Logger.Write(Logger.Tag.INFO, "Started container: " + response.ID);

                (bool running, string ID) info = await IsContainerRunning(config.ID);
                ID = info.ID;
                status = info.running;

                Logger.Write(Logger.Tag.COMMIT, "Container now running: " + response.ID);
            // // }).Wait(8000, cts.Token);
            // }).Wait(8000);

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
            IList<ContainerListResponse> t = await ListContainers();

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
            IList<ContainerListResponse> t = await ListContainers();

            List<string> ids = new List<string>();
            foreach (ContainerListResponse c in t)
            {
                foreach (string n in c.Names)
                {
                    if (n.ToString().Contains(name))
                    {
                        ids.Add(c.ID);
                        break;
                        // if (c.State == "running" || c.State == "created" || c.State == "exited") 
                        // {
                        //     break;
                        // }    
                    }
                }
                
            }
            return ids.ToArray();
        }  

        public async Task<IList<ContainerListResponse>> ListContainers()
        {
            var parameters = new ContainersListParameters
            {
                Filters = new Dictionary<string, IDictionary<string, bool>>
                {
                    {
                        "status", new Dictionary<string, bool>
                        {
                            { "running", true},
                            { "created", true},
                            { "dead", true},
                            { "exited", true},
                        }
                    }
                }
            };
            IList<ContainerListResponse> nl = new List<ContainerListResponse>();
            var containers = await _client.Containers.ListContainersAsync(parameters);
            foreach (ContainerListResponse c in containers)
            {
                foreach (string n in c.Names)
                {
                    if (n.ToString().Contains(Params.ID))
                    {
                        nl.Add(c);  
                    }
                }
            }
            return nl;
        }
        public async Task RemoveStoppedContainers()
        {
            try 
            {
                // string[] ids = await GetContainerIDs();
                // foreach(string id in ids)
                IList<ContainerListResponse> t = await ListContainers();
                foreach (ContainerListResponse c in t)
                {
                    await StopContainers(c.ID);
                    await _client.Containers.RemoveContainerAsync(c.ID, new ContainerRemoveParameters());
                    Logger.Write(Logger.Tag.WARN, "Removed container: " + c.ID);
                }
                // await _client.Containers.PruneContainersAsync();
            } catch(DockerApiException)
            {
                Utils.Sleep(10);
                await RemoveStoppedContainers();
            }
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