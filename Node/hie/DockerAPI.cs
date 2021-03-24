using Docker.DotNet;
using System;
using System.Threading.Tasks;
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

        public async Task<string> Containerize(string image, string host, int port, string machineName)
        {
            bool status = await PullImage(image);
            if (!status) return null;

            string ID = null;
            var t = Task.Run(async () => {
                    var response = await _client.Containers.CreateContainerAsync(new CreateContainerParameters
                    { 
                        Image = image,  
                        Hostname = "localhost",
                        Env = new string[]{"HOST="+host, "PORT="+port.ToString(), "NAME="+machineName}, 
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

                    await _client.Containers.StartContainerAsync(response.ID, null);
                    ID = response.ID;
                });
            bool success = t.Wait(2000);
            if (ID == null || !success) Logger.Log("Containerize", "Did not manage to spin up container.", Logger.LogLevel.ERROR);
            return ID;
        }   

        public async Task<bool> IsContainerRunning(string id, string image)  
        {  
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

        public async Task RemoveStoppedContainers()
        {
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