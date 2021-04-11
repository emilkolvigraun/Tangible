import sys,json


if __name__ == "__main__":
    N = int(sys.argv[1])
    for i in range(N):
        base_settings = {
            "Host": "192.168.1.211",
            "Port": 5000,
            "ID": "TcpNode0",
            "RDFPath": "C:\\Model\\Is\\Located\\Somewhere\\RDF.ttl",
            "RequestTopic": "tangible.requests.1",
            "BroadcastTopic": "tangible.cluster.1",
            "DockerRemoteHost": "npipe://./pipe/docker_engine",
            "DriverHostName": "192.168.1.211",
            "DriverPortRangeStart": 8000,
            "DriverPortRangeEnd": 8100,
            "Optional":{
                "Heartbeat_MS":2000,
                "Timeout_MS":1000,
                "WaitBeforeStartConsumer_MS":8000,
                "BatchSize":100
            },
            "TcpNodes": list(),
            "Testing": {
                "DieAsFollower_MS": -1,
                "DieAsLeader_MS": -1,
                "RunHIE": True,
                "TestReceiverHost": "192.168.1.211",
                "Frequency_Hz":1,
                "TestReceiverPort": 4000
            }
        }
        base_settings['Port'] = 5000+i
        base_settings['Optional']['Heartbeat_MS'] = 1250*N
        base_settings['Optional']['Timeout_MS'] = int(base_settings['Optional']['Heartbeat_MS']*.75)
        base_settings['ID'] = 'TcpNode'+str(i)
        base_settings['DriverPortRangeStart'] = 8000+(i*10)
        base_settings['DriverPortRangeEnd'] = base_settings['DriverPortRangeStart']+10
        for j in range(int(sys.argv[1])):
            if j==i: continue
            base_settings['TcpNodes'].append({
                "Host":base_settings['Host'],
                "Port":5000+j,
                "ID":'TcpNode'+str(j)
            })
        with open('TcpNode'+str(i)+'.json', 'w') as f:
            f.write(json.dumps(base_settings))