using Moq;
using RabbitMQ.Client;
using SessionService;
using SessionService.Interfaces;
using SessionService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using System.Threading.Tasks;
using Testcontainers.RabbitMq;

namespace SessionTest
{
    [TestClass]
    public class ConsumerTest
    {

        private RabbitMqContainer rabbitContainer;

        private Mock<ISessionDatabase> mockDatabase;
        private Dictionary<string, Session> mockUsers;
        private Dictionary<string, Dictionary<string, Session>> mockRooms;

        private ManualResetEventSlim messageConsumedEvent;

        private Consumer consumer;

        private string queueName;

        private IConnection publishConnection;
        private IModel publishChannel;

        [TestInitialize]
        public async Task InitializeAsync()
        {
#if DEBUG
            DevelopEnvLoader.Load("develop.env");
#endif

            messageConsumedEvent = new ManualResetEventSlim(false);

            int hostPort = 60500;
            int amqpPort = 5672;

            rabbitContainer = new RabbitMqBuilder()
                .WithImage("rabbitmq:3.11")
            .WithPortBinding(hostPort, amqpPort)
            .Build();

            await rabbitContainer.StartAsync();

            string rabbitHost = rabbitContainer.Hostname;
            ushort rabbitPort = rabbitContainer.GetMappedPublicPort(amqpPort);

            #region Database mocking
            mockUsers = new Dictionary<string, Session>();
            mockRooms = new Dictionary<string, Dictionary<string, Session>>();
            mockDatabase = new Mock<ISessionDatabase>();
            mockDatabase.Setup(db => db.AddSession(It.IsAny<Session>()))
                .Returns<Session>(session =>
                {
                    if (!mockUsers.ContainsKey(session.Username))
                    {
                        mockUsers.Add(session.Username, session);
                        if (!string.IsNullOrWhiteSpace(session.RoomName))
                        {
                            if (!mockRooms.ContainsKey(session.RoomName))
                                mockRooms.Add(session.RoomName, new Dictionary<string, Session>());
                            mockRooms[session.RoomName].Add(session.Username, session);
                        }
                        return true;
                    }
                    else
                        return false;
                });
            mockDatabase.Setup(db => db.RemoveSession(It.IsAny<string>()))
                .Returns<string>(username =>
                {
                    if (!mockUsers.ContainsKey(username))
                        return false;
                    else
                    {
                        Session user = mockUsers[username];
                        if (!string.IsNullOrWhiteSpace(user.RoomName) && mockRooms.ContainsKey(user.RoomName))
                            mockRooms[user.RoomName].Remove(user.Username);
                        mockUsers.Remove(username);
                        return true;
                    }
                });
            mockDatabase.Setup(db => db.UpdateSession(It.IsAny<string>(), It.IsAny<Session>()))
                .Returns((string username, Session newSession) =>
                {
                    if (!mockUsers.ContainsKey(username))
                        return false;
                    Session user = mockUsers[username];
                    string newRoom = newSession.RoomName;
                    string oldRoom = user.RoomName;
                    mockUsers[username] = newSession;
                    if (newRoom != oldRoom)
                    {
                        if (!string.IsNullOrWhiteSpace(oldRoom) && mockRooms.ContainsKey(oldRoom))
                        {
                            mockRooms[oldRoom].Remove(username);
                            if (mockRooms[oldRoom].Count <= 0)
                                mockRooms.Remove(oldRoom);
                        }
                        if (!string.IsNullOrWhiteSpace(newRoom))
                        {
                            if (!mockRooms.ContainsKey(newRoom))
                                mockRooms.Add(newRoom, new Dictionary<string, Session>());
                            mockRooms[newRoom].Add(username, mockUsers[username]);
                        }
                    }
                    messageConsumedEvent.Set();
                    return true;
                });
            mockDatabase.Setup(db => db.UpdateRoom(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string username, string roomName) =>
                {
                    if (!mockUsers.ContainsKey(username))
                        return false;
                    Session user = mockUsers[username];
                    string newRoom = roomName;
                    string oldRoom = user.RoomName;
                    if (newRoom != oldRoom)
                    {
                        mockUsers[username].RoomName = roomName;
                        if (!string.IsNullOrWhiteSpace(oldRoom) && mockRooms.ContainsKey(oldRoom))
                        {
                            mockRooms[oldRoom].Remove(username);
                            if (mockRooms[oldRoom].Count <= 0)
                                mockRooms.Remove(oldRoom);
                        }
                        if (!string.IsNullOrWhiteSpace(newRoom))
                        {
                            if (!mockRooms.ContainsKey(newRoom))
                                mockRooms.Add(newRoom, new Dictionary<string, Session>());
                            mockRooms[newRoom].Add(username, mockUsers[username]);
                        }
                    }
                    messageConsumedEvent.Set();
                    return true;
                });
            mockDatabase.Setup(db => db.CheckSessionId(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string username, string sessionId) =>
                {
                    if (!mockUsers.ContainsKey(username))
                        return false;
                    Session user = mockUsers[username];
                    return user.SessionId == sessionId;
                });
            mockDatabase.Setup(db => db.GetSessionByUsername(It.IsAny<string>()))
                .Returns<string>(username =>
                {
                    if (!mockUsers.ContainsKey(username))
                        return null;
                    else
                        return mockUsers[username];
                });
            mockDatabase.Setup(db => db.GetSessionsInRoomWithUser(It.IsAny<string>()))
                .Returns<string>(username =>
                {
                    if (!mockUsers.ContainsKey(username))
                        return null;
                    string roomName = mockUsers[username].RoomName;

                    if (!mockRooms.ContainsKey(roomName))
                        return null;
                    Dictionary<string, Session> room = mockRooms[roomName];
                    return room.Values.ToList();
                });
            mockDatabase.Setup(db => db.GetSessionsByRoom(It.IsAny<string>()))
                .Returns<string>(roomName =>
                {
                    if (!mockRooms.ContainsKey(roomName))
                        return null;
                    Dictionary<string, Session> room = mockRooms[roomName];
                    return room.Values.ToList();
                });

            #endregion

            queueName = "TestQueue";

            consumer = new Consumer(rabbitHost, hostPort, queueName, mockDatabase.Object);
            CreateTestPublisher(rabbitHost, hostPort, queueName);
        }

        private void CreateTestPublisher(string hostName, int hostPort, string queueName)
        {
            var factory = new ConnectionFactory { HostName = hostName };
            factory.Uri = new Uri($"amqp://rabbitmq:rabbitmq@{hostName}:{hostPort}");
            publishConnection = factory.CreateConnection();
            publishChannel = publishConnection.CreateModel();

            publishChannel.QueueDeclare(queue: queueName, durable: false, exclusive: false, autoDelete: false, arguments: null);
        }

        private void TestPublish(UserRoomDto state)
        {
            string jsonString = JsonSerializer.Serialize(state);
            byte[] body = Encoding.UTF8.GetBytes(jsonString);
            publishChannel.BasicPublish(exchange: string.Empty, routingKey: queueName, basicProperties: null, body: body);
        }

        [TestCleanup]
        public async Task TestCleanup()
        {
            await rabbitContainer.StopAsync();
        }

        [TestMethod]
        public void UpdateSessionNewNameSuccess()
        {
            //Arrange
            Session oldSession = new Session { Username = "TestName", RoomName = "OldRoom", SessionId = "Valid_Id" };
            UserRoomDto newState = new UserRoomDto { Username = "TestName", RoomName = "NewRoom", };
            mockDatabase.Object.AddSession(oldSession);

            //Act
            TestPublish(newState);
            messageConsumedEvent.Wait(TimeSpan.FromSeconds(10));
            Session newSession = mockUsers[newState.Username];

            //Assert
            Assert.IsNotNull(newSession);
            Assert.AreEqual("TestName", newSession.Username);
            Assert.AreEqual("NewRoom", newSession.RoomName);
        }

    }
}
