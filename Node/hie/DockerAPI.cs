using Docker.DotNet;
using System;
using System.Collections.Generic;
using Docker.DotNet.Models;
 
namespace Node 
{
    class DockerAPI 
    {
        DockerClient _client;

        DockerAPI()
        {
            _client = new DockerClientConfiguration(
                new Uri(Params.DOCKER_HOST_NAME))
                .CreateClient();
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