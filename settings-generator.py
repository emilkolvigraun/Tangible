import sys,json

if __name__ == "__main__":
    N = int(sys.argv[1])
    for i in range(N):
        base_settings = {
            "Host": str(sys.argv[2]),
            "Port": 5001,
            "ID": "DemoNode1",
            "RDFPath":"C:\\Model\\Is\\Located\\Somewhere\\RDF.ttl",
            "RequestTopic":"tangible.requests.1",
            "BroadcastTopic":"tangible.cluster.1",
            "DockerRemoteHost": "npipe://./pipe/docker_engine",
            "DriverHostName": str(sys.argv[2]),
            "DriverPortRangeStart": 8101,
            "DriverPortRangeEnd": 8201,
            "LogLevel": [
                "DEBUG",
                "COMMIT",
                "INFO",
                "WARN",
                "ERROR",
                "FATAL"
            ],
            "Members": [],
            "Testing": {
                "DieAsFollower_MS": -1,
                "DieAsLeader_MS": -1,
                "RunHIE": True,
                "CommitFrequency": 2000,
                "TestReceiverHost": str(sys.argv[2]),
                "TestReceiverPort": 4000
            },
            "Optional": {
                "ElectionTimeoutTangeStart_MS": 250,
                "ElectionTimeoutTangeEnd_MS": 500,
                "Heartbeat_MS": 3000,
                "MaxRetries": 4,
                "Timeout_MS": 700,
                "BatchSize": 10,
                "WaitBeforeStartConsumer_MS": 6000,
                "WaitBeforeStart_MS": 0
            }
        }
        base_settings['Port'] = 5000+i
        base_settings['Optional']['Heartbeat_MS'] = 1250*N
        base_settings['Optional']['Timeout_MS'] = int(base_settings['Optional']['Heartbeat_MS']*.75)
        base_settings['ID'] = 'DemoNode'+str(i)
        base_settings['DriverPortRangeStart'] = 8000+(i*10)
        base_settings['DriverPortRangeEnd'] = base_settings['DriverPortRangeStart']+10
        for j in range(int(sys.argv[1])):
            if j==i: continue
            base_settings['Members'].append({
                "Host":str(sys.argv[2]),
                "Port":5000+j,
                "ID":'DemoNode'+str(j)
            })
        with open('DemoNode'+str(i)+'.json', 'w') as f:
            f.write(json.dumps(base_settings))