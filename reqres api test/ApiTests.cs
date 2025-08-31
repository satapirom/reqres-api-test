using NUnit.Framework;
using RestSharp;
using Newtonsoft.Json.Linq;
using System.IO;

namespace ApiTests
{
    [TestFixture]
    public class ReqresApiTests
    {
        private RestClient client;
        private string createdUserId;
        private dynamic config; 

        [SetUp]
        public void Setup()
        {
            string json = File.ReadAllText("configData.json");
            config = JObject.Parse(json);

            client = new RestClient((string)config.BaseUrl);
        }

        [TearDown]
        public void TearDown()
        {
            client?.Dispose();
        }

        private void AddApiKey(RestRequest request)
        {
            request.AddHeader("x-api-key", (string)config.ApiKey);
        }

        [Test, Order(1)]
        public void TC01_CreateUser()
        {
            var newUser = new
            {
                name = (string)config.NewUser.name,
                job = (string)config.NewUser.job
            };

            var request = new RestRequest("users", Method.Post);
            AddApiKey(request);
            request.AddJsonBody(newUser);

            var response = client.Execute(request);
            Assert.That((int)response.StatusCode, Is.EqualTo(201));

            var content = JObject.Parse(response.Content);
            Assert.That(content["id"], Is.Not.Null);
            Assert.That(content["createdAt"], Is.Not.Null);

            createdUserId = content["id"].ToString();
        }

        [Test, Order(2)]
        public void TC02_GetSingleUser()
        {
            var userId = (string)config.UserId;
            var request = new RestRequest($"users/{userId}", Method.Get);
            AddApiKey(request);

            var response = client.Execute(request);
            Assert.That((int)response.StatusCode, Is.EqualTo(200));

            var content = JObject.Parse(response.Content);
            Assert.That(content["data"], Is.Not.Null);
            Assert.That((int)content["data"]["id"], Is.EqualTo(int.Parse(userId)));
        }

        [Test, Order(3)]
        public void TC03_UpdateUser()
        {
            var userId = createdUserId ?? (string)config.DefaultUserId;
            var updatedUser = new
            {
                name = (string)config.UpdatedUser.name,
                job = (string)config.UpdatedUser.job
            };

            var request = new RestRequest($"users/{userId}", Method.Put);
            AddApiKey(request);
            request.AddJsonBody(updatedUser);

            var response = client.Execute(request);
            Assert.That((int)response.StatusCode, Is.EqualTo(200));

            var content = JObject.Parse(response.Content);
            Assert.That(content["updatedAt"], Is.Not.Null);
        }

        [Test, Order(4)]
        public void TC04_DeleteUser()
        {
            var userId = createdUserId ?? (string)config.DefaultUserId;
            var request = new RestRequest($"users/{userId}", Method.Delete);
            AddApiKey(request);

            var response = client.Execute(request);
            Assert.That((int)response.StatusCode, Is.EqualTo(204));
            Assert.That(response.Content, Is.Null.Or.Empty);
        }
    }
}
