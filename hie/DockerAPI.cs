using Docker.DotNet;
using Docker.DotNet.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Node
{
    // https://github.com/dotnet/Docker.DotNet

    class DockerAPI 
    {
        // The Docker Client object, which is used to perform 
        // almost any Docker related operations
        private readonly DockerClient _client;

        public DockerAPI()
        { 
            // Retrieve the correct URI, depending on OS
            Uri DockerURI = GetDockerURI();

            // Initialize the DockerClient object
            _client = new DockerClientConfiguration(DockerURI).CreateClient();
        }

        /// <summary>
        ///  
        /// </summary> 
        /// <returns></returns>
        public async void PullImage(string _image, string _version) 
        {
            try
            {
                await _client.Images.CreateImageAsync(new ImagesCreateParameters
                {
                    FromImage = _image,
                    Tag = _version
                },
                        new AuthConfig(),
                        new Progress<JSONMessage>());
            } catch (Exception) 
            { 
            }
        }    

        /// <summary>
        ///  
        /// </summary> 
        public async Task<IList<ContainerListResponse>> GetContainersInformation()  
        {  
            // Return the first "limit" containers
            return await _client.Containers.ListContainersAsync(new ContainersListParameters()); 
        }   

        /// <summary>
        /// The Docker client needs a URI to connect with the daemon. 
        /// <para>By default, the Docker daemon listens on:</para>  
        /// <para>npipe://./pipe/docker_engine : Windows</para>
        /// <para>unix:/var/run/docker.sock    : Linux</para>
        /// </summary>  
        /// <returns>Uri-object representing Docker URI</returns>     
        private Uri GetDockerURI()  
        { 
            // Does the OS equal Windows?
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return new Uri("npipe://./pipe/docker_engine");
            }

            // Or does the OS equal Linux?
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return new Uri("unix:/var/run/docker.sock");
            } 

            // If neither, throw an exception because then we cannot run.
            throw new Exception("The OS does not appear to be Windows or Linux.");
        } 

        public void AttachContainer(object ID) 
        {
            var config = new ContainerAttachParameters
            {  
                Stream = true,
                Stderr = false,
                Stdin = false,
                Stdout = true    
            };

            var buffer = new byte[1024];
            using (var stream = _client.Containers.AttachContainerAsync((string)ID, false, config, default).GetAwaiter().GetResult())
            {
                var result = stream.ReadOutputAsync(buffer, 0, buffer.Length, default).GetAwaiter().GetResult();
                do
                { 
                    //Console.ReadLine();
                    Console.Write(Encoding.UTF8.GetString(buffer, 0, result.Count));
                }
                while (!result.EOF);
            }
        } 

        public async Task<string> StartContainer(string _image, string[] _env)
        {
            var response = await _client.Containers.CreateContainerAsync(new CreateContainerParameters
            { 
                Image = _image, 
                ArgsEscaped = false,
                AttachStderr = false, 
                AttachStdin = false,
                AttachStdout = true,  
                Hostname = "localhost",
                Env = _env, 
                HostConfig = new HostConfig { NetworkMode = "host" },

            });      

            await _client.Containers.StartContainerAsync(response.ID, null);

            return response.ID;
        }   

        public async void StopContainer(string _id)
        {
            await _client.Containers.KillContainerAsync(_id, new ContainerKillParameters());
        }

        public async void RemoveContainer(string _id)
        {
            await _client.Containers.RemoveContainerAsync(_id, new ContainerRemoveParameters());
        }

        public async void RemoveImage(string _image)
        {
            await _client.Images.DeleteImageAsync(_image, new ImageDeleteParameters());
        }

        public async void Prune()
        { 
            await _client.Images.PruneImagesAsync();
            await _client.Containers.PruneContainersAsync();
        }
    }
}
