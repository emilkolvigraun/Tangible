from tangible_client import KafkaClient
import json, asyncio

client = KafkaClient('192.168.1.237:9092', 'main.group')

loop = asyncio.new_event_loop()
asyncio.set_event_loop(loop)

loop.run_until_complete(client.subscribe('Tangible.request.1'))
loop.run_forever()