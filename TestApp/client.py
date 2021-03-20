import tangible_client, asyncio

loop = asyncio.new_event_loop()
asyncio.set_event_loop(loop)

client = tangible_client.Client(brokers="192.168.1.237:9092", user="test-user", return_topic="MyApplication")

client.subscribe(loop, 2)
# client.listen(loop)