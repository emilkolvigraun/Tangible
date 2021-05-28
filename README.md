## Tangible
# An Orchestration Platform for the HPL in BOSs

Tangible executes read, write and subscribe requests in a Hardware Interaction Environment facilitated with Docker. It spins up drivers (as persistant processors) on-demand, based on a resource in the Docker registry. Note that such driver adheres to a framework and merely requires the implementation of an interface. The current version utilizes the one found here:

```
 https://hub.docker.com/repository/docker/emilkolvigraun/tangible-test-driver
```

The current version acts as a standalone EXE, but requires an installation of [Docker](https://docs.docker.com/docker-for-windows/install/). Furthermore, emphasises the evaluation and sorts the ESB out of the loop. A demonstration can be run by following the description in the **Demonstration** section.


* `KafkaCluster` showcases the facilitation of a [Confluent Kafka Cluster](https://github.com/confluentinc/confluent-kafka-dotnet).
* `TangibleNode` contains the implementation of a Tangible Node.
* `Tools` contains:
    * `TestReceiver` used in the evaluation/demonstration.
    * `TestDriver` which showcases the implementation of the driver-framework and interface.
    * `TestApp` *deprecated*


## Demonstration

To run the demonstrations build two projects:
```
..\TangibleNode> dotnet publish
..\Tools\TestReceiver> dotnet publish
```

Tangible will autpomatically pull the Docker image from the registry.

`spawn_cluster.cmd` executes a cluster of N nodes and creates a TestReceiver to demonstrate the delivery of requests. This automatically generates a set of configuration files and removes them again. The nodes will automatically discover each other and requests are load balanced. If you close a follower node, the leader will resolve it. If you close a leader node, the remaining followers will perform a leader electerion to resolve new authority. A single node runs as a sleeper and if aware of the other (broken) nodes, activates `CandidateResolve`. Run the cluster as such:

```
...\Tangible>spawn_cluster.cmd 3 192.168.163.17
...\Tangible>spawn_cluster.cmd <NUMBER OF NODES> <LOCAL-IPV4-ADDRESS>
```

`single_node.cmd` runs just a single node, with outset in a provided JSON formatted configuration file. An example of such configuration file can be found in `TangibleNode\settings\single_node.json`.

```
...\Tangible>single_node.cmd \TangibleNode\settings\single_node.json
...\Tangible>single_node.cmd <PATH TO FILE>
```

## Dependencies

The following is a presentation of the required runtime DLLs.

* `Docker.Dotnet`
* `Newtonsoft.Json`
* `Confluent.Kafka`
* `System.Diagnostics.PerformanceCounter`
* `System.Security.Cryptography.ProtectedData`
* `System.Security.Permissions`

