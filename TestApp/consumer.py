import tangible_client
import json, asyncio, time

def get_time_ms():
    return time.time()*1000

class ResponseHandler(tangible_client.InterfaceResponseHandler):
    
    async def handle(self, message):
        msg = message.value.decode()
        obj = json.loads(msg)
        print(obj)
        {'T2': 1616869155185, 'T1': 1616869155168, 'Status': True, 'Value': '1', 'PointID': 'test-sensor-1'}
        with open("receive.txt", "a") as f:
            f.write(str(obj["T0"]) + ";" + str(obj["T1"]) + ";" + str(obj["T2"]) + ";" + str(get_time_ms())  + ";" + str(obj["Value"]) + ";" + str(obj["Heartbeat"]) + ";" + str(obj["Cluster"]) + ";" + str(obj["Jobs"]) + ";" + str(obj["Name"]) + "\n")
        

client = tangible_client.KafkaClient('192.168.1.237:9092,192.168.1.237:9093,192.168.1.237:9094', 'main.group')

loop = asyncio.new_event_loop()
asyncio.set_event_loop(loop)

loop.run_until_complete(client.subscribe('MyApplication', ResponseHandler()))
loop.run_forever()