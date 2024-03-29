#!/usr/bin/env python3.7.2
import asyncio, json, time
from datetime import datetime
from aiokafka import AIOKafkaProducer
from aiokafka import AIOKafkaConsumer
import random as rn

class InterfaceResponseHandler:
    async def handle(self, message):
        raise NotImplementedError

class ResponseHandler(InterfaceResponseHandler):
    
    """
        Default implementation of model.InterfaceResponseHandler
    """
    def __init__(self):
        self.times = {"sent":[], "received":[]}
        self.filename = "results_1"
        self.test = 2

    async def handle(self, message):
        print(message.value.decode())

class Client:
    def __init__(self, brokers:str, user:str, return_topic:str):
        self.kclient = KafkaClient(brokers, str(rn.randint(1000, 9999)))
        self.return_topic = return_topic
        self.user = user

    def lookup(self, loop):
        query = ""
        loop.run_until_complete(self.kclient.send_message('Tangible.request.1', query))

    def subscribe(self, loop, priority):
        query = '{"TypeOf":"SUBSCRIBE",'+'"User":"'+self.user+'","Priority":'+str(priority)+',"ReturnTopic":"'+self.return_topic+'","Location":{"ID":"MMMI","LocationOf":{"ID":"FLOOR0","LocationOf":null,"HasPoint":null},"HasPoint":null},"Value":null}'
        loop.run_until_complete(self.kclient.send_message('Tangible.request.1', query))
        print("send request:", query)

    def read(self, loop, priority):
        query = '{"TypeOf":"READ",'+'"User":"'+self.user+'","Priority":'+str(priority)+',"ReturnTopic":"'+self.return_topic+'","Location":{"ID":"MMMI","LocationOf":{"ID":"FLOOR0","LocationOf":null,"HasPoint":null},"HasPoint":null},"Value":null}'
        loop.run_until_complete(self.kclient.send_message('Tangible.request.1', query))
        print("send request:", query)

    def send_many_write(self, loop, priority, amount):
        messages = []
        for i in range(amount):
            query = '{"TypeOf":"WRITE",'+'"User":"'+self.user+'","Priority":'+str(priority)+',"ReturnTopic":"'+self.return_topic+'","Location":{"ID":"MMMI","LocationOf":{"ID":"FLOOR0","LocationOf":null,"HasPoint":null},"HasPoint":null},"Value":"'+str(i)+'"}'
            messages.append(query)
        loop.run_until_complete(self.kclient.send_messages('Tangible.request.1', messages))
            


    def write(self, loop, priority, value):
        # query = '{"TypeOf":"WRITE",'+'"User":"'+self.user+'","Priority":'+str(priority)+',"ReturnTopic":"'+self.return_topic+'","Location":{"ID":"MMMI","LocationOf":{"ID":"FLOOR0","LocationOf":null,"HasPoint":null},"HasPoint":null},"Value":"'+str(value)+'"}'        
        query = '{"ID":null,"TypeOf":"WRITE","Action":"WRITE","User":"'+self.user+'","Priority":'+str(priority)+',"ReturnTopic":"'+self.return_topic+'","Location":{"ID":"MMMI","LocationOf":{"ID":"ROOM-xyz","LocationOf":null,"HasPoint":{"ID":"sensor-xyz"}},"HasPoint":null},"Value":"'+str(value)+'"}'
        loop.run_until_complete(self.kclient.send_message('Tangible.request.1', query))
        print("send request:", query)


    def listen(self, loop, rsh=ResponseHandler()):
        loop.run_until_complete(self.kclient.subscribe(self.return_topic, rsh))
        loop.run_forever()



class Admin:
    def __init__(self, brokers: list):
        print("not yet implemented")

class Producer:
    def __init__(self, broker:str):
        loop = asyncio.get_event_loop()
        self.producer = AIOKafkaProducer(loop=loop, bootstrap_servers=broker)
    
    async def send_message(self, topic, message):
        await self.producer.start()
        try:
            await self.producer.send_and_wait(topic, message.encode('utf-8'))
        finally:
            await self.producer.stop()

    async def send_messages(self, topic, messages:list):
        await self.producer.start()
        try:
            for msg in messages:
                await self.producer.send_and_wait(topic, msg.encode('utf-8'))
        finally:
            await self.producer.stop()

class Consumer: 
    def __init__(self, group:str, host):
        self.host   = host
        self.group = group
        self.loop = asyncio.get_event_loop()
        self.consumers = []
        self.topics = []
    
    async def subscribe(self, topics, response_handler):
        consumer = AIOKafkaConsumer(
        topics,
        loop=self.loop, bootstrap_servers=self.host,
        group_id=self.group, auto_offset_reset='earliest')

        await consumer.start()
        try:
            async for msg in consumer:
                await response_handler.handle(msg)
        finally:
            await consumer.stop()

class KafkaClient:
    def __init__(self, broker:str, group:str):
        self.group = group
        self.broker = broker

    async def send_message(self, topic:str, message:str):
        producer = Producer(self.broker)
        await producer.send_message(topic, message)

    async def subscribe(self, topic:str, response_handler=ResponseHandler()):
        consumer = Consumer(self.group, self.broker)
        await consumer.subscribe(topic, response_handler) 

    async def send_messages(self, topic:str, messages:list):
        for message in messages:
            await self.send_message(topic, message)
