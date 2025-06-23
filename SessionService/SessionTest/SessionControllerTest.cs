using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SessionService;
using SessionService.Controllers;
using SessionService.Interfaces;
using SessionService.Models;
using System.Security.Claims;

namespace SessionTest
{
    [TestClass]
    public class SessionControllerTest
    {
        private SessionController sessionController;

        private Mock<HttpMessageHandler> mockHandler;
        private Mock<HttpContext> mockContext;
        private Mock<HttpRequest> mockRequest;

        private Mock<ISessionDatabase> mockDatabase;
        private Dictionary<string, Session> mockUsers;
        private Dictionary<string, Dictionary<string, Session>> mockRooms;

        [TestInitialize]
        public void Initialize()
        {
#if DEBUG
            DevelopEnvLoader.Load("develop.env");
#endif

            mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Loose);

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

            sessionController = new SessionController(mockDatabase.Object, null, null, mockHandler.Object);

            mockContext = new Mock<HttpContext>();
            mockRequest = new Mock<HttpRequest>();
            mockContext.Setup(x => x.Request).Returns(mockRequest.Object);

            sessionController.ControllerContext = new ControllerContext() { HttpContext = mockContext.Object };
        }

        #region StartSession Tests
        [TestMethod]
        public void StartSessionSuccess()
        {
            //Arrange
            string username = "TestJoe";
            string timeNow = DateTime.Now.ToString();
            string expectedToBeContainedInId = username + timeNow;
            UsernameDto usernameDto = new UsernameDto() { Username = username };

            //Act
            var response = sessionController.StartSession(usernameDto);
            var responseObject = response as CreatedResult;

            //Assert
            Assert.IsNotNull(responseObject);
            Assert.IsInstanceOfType(response, typeof(CreatedResult));
            Assert.IsTrue(responseObject.Value.ToString().Contains(expectedToBeContainedInId));
        }

        [TestMethod]
        public void StartSessionUsernameSessionConflict()
        {
            //Arrange
            string username1 = "TestJoe";
            UsernameDto usernameDto1 = new UsernameDto() { Username = username1 };
            string username2 = "TestJoe";
            UsernameDto usernameDto2 = new UsernameDto() { Username = username2 };
            string expectedObject = "Username session conflict.";

            //Act
            sessionController.StartSession(usernameDto1);
            var response = sessionController.StartSession(usernameDto2);

            //Assert
            AssertObjectResponse<ConflictObjectResult>(response, 409, expectedObject);
        }

        #endregion

        #region EndSession Tests
        [TestMethod]
        public void EndSessionSuccess()
        {
            //Arrange
            string username = "TestJoe";
            UsernameDto usernameDto = new UsernameDto() { Username = username };
            int expectedStatusCode = 200;

            //Act
            sessionController.StartSession(usernameDto);
            var response = sessionController.EndSession(usernameDto);

            //Assert
            AssertStatusResponse<OkResult>(response, expectedStatusCode);
        }

        [TestMethod]
        public void EndSessionUserNotFound()
        {
            //Arrange
            string username = "TestJoe";
            UsernameDto usernameDto = new UsernameDto() { Username = username };
            int expectedStatusCode = 404;
            string expectedValue = "User not found.";

            //Act
            var response = sessionController.EndSession(usernameDto);

            //Assert
            AssertObjectResponse<NotFoundObjectResult>(response, expectedStatusCode, expectedValue);
        }

        #endregion

        #region CheckSessionId Tests
        [TestMethod]
        public void CheckSessionIdSuccess()
        {
            //Arrange
            int expectedStatusCode = 200;
            string username = "Test";
            UsernameDto usernameDto = new UsernameDto() { Username = username };

            //Act
            var response1 = sessionController.StartSession(usernameDto);
            var response1Object = response1 as CreatedResult;
            string id = response1Object.Value.ToString();

            mockRequest.Setup(x => x.Headers["Authorization"]).Returns("Bearer valid_token");
            ClaimsIdentity claimsIdentity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.NameIdentifier, id)
            });
            ClaimsPrincipal claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
            mockContext.Setup(x => x.User).Returns(claimsPrincipal);

            var response2 = sessionController.CheckSessionId();

            //Assert
            AssertStatusResponse<OkResult>(response2, expectedStatusCode);
        }

        [TestMethod]
        public void CheckSessionIdUserNotFoundNoNameClaim()
        {
            //Arrange
            int expectedStatusCode = 404;
            string expectedValue = "User not found.";
            string username = "Test";
            UsernameDto usernameDto = new UsernameDto() { Username = username };

            //Act
            var response1 = sessionController.StartSession(usernameDto);
            var response1Object = response1 as CreatedResult;
            string id = response1Object.Value.ToString();

            mockRequest.Setup(x => x.Headers["Authorization"]).Returns("Bearer valid_token");
            ClaimsIdentity claimsIdentity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, id)
            });
            ClaimsPrincipal claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
            mockContext.Setup(x => x.User).Returns(claimsPrincipal);

            var response2 = sessionController.CheckSessionId();

            //Assert
            AssertObjectResponse<NotFoundObjectResult>(response2, expectedStatusCode, expectedValue);
        }

        [TestMethod]
        public void CheckSessionIdIdNotFoundNoNameIdentifierClaim()
        {
            //Arrange
            int expectedStatusCode = 404;
            string expectedValue = "Id not found.";
            string username = "Test";
            UsernameDto usernameDto = new UsernameDto() { Username = username };

            //Act
            var response1 = sessionController.StartSession(usernameDto);
            var response1Object = response1 as CreatedResult;
            string id = response1Object.Value.ToString();

            mockRequest.Setup(x => x.Headers["Authorization"]).Returns("Bearer valid_token");
            ClaimsIdentity claimsIdentity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, username)
            });
            ClaimsPrincipal claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
            mockContext.Setup(x => x.User).Returns(claimsPrincipal);

            var response2 = sessionController.CheckSessionId();

            //Assert
            AssertObjectResponse<NotFoundObjectResult>(response2, expectedStatusCode, expectedValue);
        }

        [TestMethod]
        public void CheckSessionIdIdNotFoundIdNotInDatabase()
        {
            //Arrange
            int expectedStatusCode = 404;
            string expectedValue = "Id not found.";
            string username = "Test";
            UsernameDto usernameDto = new UsernameDto() { Username = username };

            //Act
            mockRequest.Setup(x => x.Headers["Authorization"]).Returns("Bearer valid_token");
            ClaimsIdentity claimsIdentity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.NameIdentifier, "valid_id")
            });
            ClaimsPrincipal claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
            mockContext.Setup(x => x.User).Returns(claimsPrincipal);

            var response2 = sessionController.CheckSessionId();

            //Assert
            AssertObjectResponse<NotFoundObjectResult>(response2, expectedStatusCode, expectedValue);
        }

        #endregion

        #region IsUserOnline Tests
        [TestMethod]
        public void IsUserOnlineSuccess()
        {
            //Arrange
            int expectedStatusCode = 200;
            string username = "Test";
            UsernameDto usernameDto = new UsernameDto() { Username = username };

            //Act
            sessionController.StartSession(usernameDto);
            var response = sessionController.IsUserOnline(usernameDto);

            //Assert
            AssertStatusResponse<OkResult>(response, expectedStatusCode);
        }

        [TestMethod]
        public void IsUserOnlineUserNotFound()
        {
            //Arrange
            int expectedStatusCode = 404;
            string expectedValue = "User not found.";
            string username = "Test";
            UsernameDto usernameDto = new UsernameDto() { Username = username };

            //Act
            var response = sessionController.IsUserOnline(usernameDto);

            //Assert
            AssertObjectResponse<NotFoundObjectResult>(response, expectedStatusCode, expectedValue);
        }

        #endregion

        #region GetUserState Tests
        [TestMethod]
        public void GetUserStateSuccess()
        {
            //Arrange
            int expectedStatusCode = 200;
            string username = "Test";
            UsernameDto usernameDto = new UsernameDto() { Username = username };
            StateDto expectedStateDto = new StateDto() { Username = username, RoomName = "", SessionId = "" };

            //Act
            var response1 = sessionController.StartSession(usernameDto);
            var startResponse = response1 as CreatedResult;
            string id = startResponse.Value.ToString();
            expectedStateDto.SessionId = id;

            var response2 = sessionController.GetUserState(username);
            var object2 = response2 as OkObjectResult;
            var actualStateDto = object2.Value as StateDto;

            //Assert
            AssertObjectResponse<OkObjectResult>(response2, expectedStatusCode);
            Assert.IsNotNull(actualStateDto);
            Assert.AreEqual(expectedStateDto.RoomName, actualStateDto.RoomName);
            Assert.AreEqual(expectedStateDto.Username, actualStateDto.Username);
            Assert.AreEqual(expectedStateDto.SessionId, actualStateDto.SessionId);
        }

        [TestMethod]
        public void GetUserStateUserNotFound()
        {
            //Arrange
            int expectedStatusCode = 404;
            string expectedValue = "User not found.";
            string username = "Test";

            //Act
            var response = sessionController.GetUserState(username);

            //Assert
            AssertObjectResponse<NotFoundObjectResult>(response, expectedStatusCode, expectedValue);
        }

        #endregion

        #region GetUsersInRoom Tests
        [TestMethod]
        public void GetUsersInRoomSuccess()
        {
            //Arrange
            int expectedStatusCode = 200;
            string room = "TestRoom";
            string username1 = "Test1";
            UsernameDto usernameDto1 = new UsernameDto() { Username = username1 };
            string username2 = "Test2";
            UsernameDto usernameDto2 = new UsernameDto() { Username = username2 };
            string username3 = "Test3";
            UsernameDto usernameDto3 = new UsernameDto() { Username = username3 };
            List<UsernameDto> expectedValue = [usernameDto1, usernameDto2, usernameDto3];

            //Act
            sessionController.StartSession(usernameDto1);
            sessionController.StartSession(usernameDto2);
            sessionController.StartSession(usernameDto3);
            mockDatabase.Object.UpdateSession(username1, new Session() { Username = username1, RoomName = room, SessionId = mockUsers[username1].SessionId });
            mockDatabase.Object.UpdateSession(username2, new Session() { Username = username2, RoomName = room, SessionId = mockUsers[username2].SessionId });
            mockDatabase.Object.UpdateSession(username3, new Session() { Username = username3, RoomName = room, SessionId = mockUsers[username3].SessionId });

            var response = sessionController.GetUsersInRoom(room);
            var responseObject = response as OkObjectResult;
            var actualCollection = responseObject.Value as List<UsernameDto>;

            //Assert
            AssertObjectResponse<OkObjectResult>(response, expectedStatusCode);
            Assert.IsNotNull(actualCollection);
            for (int i = 0; i < actualCollection.Count; i++)
            {
                Assert.AreEqual(expectedValue[i].Username, actualCollection[i].Username);
            }
        }

        [TestMethod]
        public void GetUsersInRoomRoomNotFound()
        {
            //Arrange
            int expectedStatusCode = 404;
            string expectedValue = "Room not found.";
            string room = "TestRoom";
            string username1 = "Test1";

            //Act
            var response = sessionController.GetUsersInRoom(room);

            //Assert
            AssertObjectResponse<NotFoundObjectResult>(response, expectedStatusCode, expectedValue);
        }

        #endregion

        #region GetUserRoom Tests
        [TestMethod]
        public void GetUserRoomSuccess()
        {
            //Arrange
            int expectedStatusCode = 200;
            string username = "TestName";
            UsernameDto usernameDto = new UsernameDto() { Username = username };
            string roomName = "TestRoom";
            RoomDto expectedValue = new RoomDto() { RoomName = roomName };

            //Act
            var startResponse = sessionController.StartSession(usernameDto);
            bool updateResponse = mockDatabase.Object.UpdateSession(username, new Session() { Username = username, RoomName = roomName, SessionId = mockUsers[username].SessionId });
            var response = sessionController.GetUserRoom(username);
            var responseObject = response as OkObjectResult;
            RoomDto actualValue = responseObject.Value as RoomDto;

            //Assert
            AssertObjectResponse<OkObjectResult>(response, expectedStatusCode);
            Assert.IsNotNull(actualValue);
            Assert.AreEqual(expectedValue.RoomName, actualValue.RoomName);
        }

        [TestMethod]
        public void GetUserRoomUserNotFound()
        {
            //Arrange
            int expectedStatusCode = 404;
            string expectedValue = "User not found.";
            string username = "TestName";
            string roomName = "TestRoom";

            //Act
            var response = sessionController.GetUserRoom(username);

            //Assert
            AssertObjectResponse<NotFoundObjectResult>(response, expectedStatusCode, expectedValue);
        }

        #endregion

        #region GetUsersInRoomWithUser
        [TestMethod]
        public void GetUsersInRoomWithUserSuccess()
        {
            //Arrange
            int expectedStatusCode = 200;
            string username1 = "TestName1";
            string username2 = "TestName2";
            string username3 = "TestName3";
            string room = "TestRoom";
            UsernameDto usernameDto1 = new UsernameDto() { Username = username1 };
            UsernameDto usernameDto2 = new UsernameDto() { Username = username2 };
            UsernameDto usernameDto3 = new UsernameDto() { Username = username3 };
            List<UsernameDto> expectedValue = new List<UsernameDto>() { usernameDto2, usernameDto3 };

            //Act
            sessionController.StartSession(usernameDto1);
            sessionController.StartSession(usernameDto2);
            sessionController.StartSession(usernameDto3);
            mockDatabase.Object.UpdateSession(username1, new Session() { Username = username1, RoomName = room, SessionId = mockUsers[username1].SessionId });
            mockDatabase.Object.UpdateSession(username2, new Session() { Username = username2, RoomName = room, SessionId = mockUsers[username2].SessionId });
            mockDatabase.Object.UpdateSession(username3, new Session() { Username = username3, RoomName = room, SessionId = mockUsers[username3].SessionId });
            var response = sessionController.GetUsersInRoomWithUser(username1);
            var responseObject = response as OkObjectResult;
            var actualValue = responseObject.Value as List<UsernameDto>;

            //Assert
            AssertObjectResponse<OkObjectResult>(response, expectedStatusCode);
            Assert.IsNotNull(actualValue);
            for (int i = 0; i < actualValue.Count; i++)
            {
                Assert.AreEqual(expectedValue[i].Username, actualValue[i].Username);
            }
        }

        [TestMethod]
        public void GetUsersInRoomWithUserUserNotFound()
        {
            //Arrange
            int expectedStatusCode = 404;
            string expectedValue = "User not found.";
            string username = "TestName";

            //Act
            var response = sessionController.GetUsersInRoomWithUser(username);

            //Assert
            AssertObjectResponse<NotFoundObjectResult>(response, expectedStatusCode, expectedValue);

        }

        #endregion

        private void AssertObjectResponse<T>(ActionResult actual, int expectedStatusCode, object? expectedValue = null) where T : ObjectResult
        {
            Assert.IsNotNull(actual);
            T? objectResult = actual as T;
            Assert.IsNotNull(objectResult);
            Assert.AreEqual(expectedStatusCode, objectResult.StatusCode);

            if (expectedValue != null)
                Assert.AreEqual(expectedValue, objectResult.Value);
        }

        private void AssertStatusResponse<T>(ActionResult actual, int expectedStatusCode) where T : StatusCodeResult
        {
            Assert.IsNotNull(actual);
            T? statusResult = actual as T;
            Assert.IsNotNull(statusResult);
            Assert.AreEqual(expectedStatusCode, statusResult.StatusCode);
        }

    }
}