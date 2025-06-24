# Dispersed-System-Proof-of-Concept
Hello and welcome, this is a proof-of-concept for a dispersed system architecture to support an online game. Using microservices, various communication forms such as TCP, REST api, and RabbitMQ for messaging, we have aimed to create a system architecture resistant to network partitions while remaining accesible to the theoretical player. The 'game' itself is just a simple game room clients can connect to and communicate messages and virtual location to other players.

This was made as part of the second semester of my Software Development bachelor, as the exam project in a course regarding System Integration and Large Systems.

On this picture you can see the intended structure of the system:
![DispersedSystem_Picture](https://github.com/user-attachments/assets/dae724bd-b224-44f1-92c5-db4afc9746dd)

# How To
While intended to run on some sort of orchestrated server, to run it locally:
1. Run an instance of RabbitMQ on your machine
2. Open an instance of each microservice, with the dockerfiles in each corresponding folder, in the following order:
    1. LoginService
    2. SessionService
    3. HealthService
    4. ChatRelay
    5. ChatService
    6. RoomRegistry
    7. RoomServer
3. Finally, open an instance of the client, register an account, and log in with that account
4. (Optionally) Open more instances of the client, register them, and log in to interact with multiple players
