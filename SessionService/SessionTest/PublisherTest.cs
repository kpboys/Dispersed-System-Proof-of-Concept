using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SessionService;
using SessionService.Interfaces;
using SessionService.Models;
using System.Text;
using System.Text.Json;
using Testcontainers.RabbitMq;

namespace SessionTest
{
    [TestClass]
    public class PublisherTest
    {

        private RabbitMqContainer rabbitContainer;

        private Mock<ISessionDatabase> mockDatabase;
        private Dictionary<string, Session> mockUsers;
        private Dictionary<string, Dictionary<string, Session>> mockRooms;

        private byte[] latestMessage;

        private ManualResetEventSlim messageConsumedEvent;

        private string exName;

        private Publisher publisher;

        private IConnection? subscriberConnection;
        private IModel? subscriberChannel;

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
                        if (mockRooms.ContainsKey(session.RoomName))
                            mockRooms[session.RoomName].Add(session.Username, session);
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

            exName = "ExchangeTest";

            publisher = new Publisher(rabbitHost, hostPort, exName);
            TestSubscriber(rabbitHost, hostPort, exName);
        }

        [TestCleanup]
        public async Task TestCleanup()
        {
            await rabbitContainer.StopAsync();
        }

        private void TestSubscriber(string hostname, int hostPort, string exchangeName)
        {
            var factory = new ConnectionFactory { HostName = hostname };
            factory.Uri = new Uri($"amqp://rabbitmq:rabbitmq@{hostname}:{hostPort}");
            subscriberConnection = factory.CreateConnection();
            subscriberChannel = subscriberConnection.CreateModel();

            subscriberChannel.ExchangeDeclare(exchange: exchangeName, type: ExchangeType.Fanout);

            var queueName = subscriberChannel.QueueDeclare().QueueName;
            subscriberChannel.QueueBind(queue: queueName, exchange: exchangeName, routingKey: string.Empty);

            var consumer = new EventingBasicConsumer(subscriberChannel);
            consumer.Received += OnTestConsume;
            subscriberChannel.BasicConsume(queue: queueName, autoAck: true, consumer: consumer);
        }

        private void OnTestConsume(object? model, BasicDeliverEventArgs ea)
        {
            var body = ea.Body.ToArray();
            latestMessage = body;
            messageConsumedEvent.Set();
        }

        [TestMethod]
        public void PublisherTestSingleMessageSuccess()
        {
            StatusDto state = new StatusDto { Username = "TestUser", IsAlive = false };

            publisher.Publish(state);

            messageConsumedEvent.Wait(TimeSpan.FromSeconds(10));

            string jsonString = Encoding.UTF8.GetString(latestMessage);
            StatusDto? actualState = JsonSerializer.Deserialize<StatusDto>(jsonString);

            Assert.IsNotNull(actualState);
            Assert.AreEqual("TestUser", actualState.Username);
            Assert.AreEqual(false, actualState.IsAlive);
        }

    }
}
