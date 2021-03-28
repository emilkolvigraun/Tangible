import tangible_client, asyncio, time

loop = asyncio.new_event_loop()
asyncio.set_event_loop(loop)

client = tangible_client.Client(brokers="192.168.1.237:9092,192.168.1.237:9093", user="test-user", return_topic="MyApplication")

# client.send_many_write(loop, 2, 10000)

def get_time_ms():
    return time.time()*1000

amount = 100
send = 0

frequency = 1 #hz
update_interval = (1/frequency)*1000

t0 = time.time()*1000
while send < amount:
    t1 = get_time_ms() - t0
    t2 = get_time_ms()
    if ( t1 >= update_interval):
        with open("send.txt", "a") as f:
            f.write(str(get_time_ms()) + "," + str(send)+"\n")

        client.write(loop, 2, send)
        send+=1
        t0 = get_time_ms()-(get_time_ms()-t2)

# for i in range(10000):
#     client.write(loop, 2, i)
    # client.subscribe(loop, 2)
    # client.read(loop, 2)

# client.listen(loop, ResponseHandler())