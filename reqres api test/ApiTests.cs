using NUnit.Framework;
using RestSharp;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using System.IO;
using System.Collections.Generic;

namespace ApiTests
{
    [TestFixture]
    public class RegresTests
    {
        private RestClient client;
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

        // Helper สำหรับส่ง request และ log response
        private JObject ExecuteRequest(string endpoint, Method method, object body = null, int expectedStatusCode = 200)
        {
            var request = new RestRequest(endpoint, method);
            AddApiKey(request);

            if (body != null)
                request.AddJsonBody(body);

            var response = client.Execute(request);

            if ((int)response.StatusCode != expectedStatusCode)
            {
                Assert.Fail($"Request to {endpoint} failed. StatusCode: {response.StatusCode}, Response: {response.Content}");
            }

            if (string.IsNullOrEmpty(response.Content))
                return new JObject();

            return JObject.Parse(response.Content);
        }

        // Helper validate JSON schema
        private void ValidateJsonSchema(JObject json, string schemaFileName)
        {
            string basePath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string schemaPath = Path.Combine(basePath, "Schemas", schemaFileName);

            if (!File.Exists(schemaPath))
                Assert.Fail($"Schema file not found: {schemaPath}");

            string schemaJson = File.ReadAllText(schemaPath);
            JSchema schema = JSchema.Parse(schemaJson);

            bool valid = json.IsValid(schema, out IList<string> errors);
            Assert.That(valid, Is.True, "Response does not match schema: " + string.Join(", ", errors));
        }

        [Test, Order(1)]
        public void TC_001_CreateUser()
        {
            var newUser = new
            {
                name = (string)config.NewUser.name,
                job = (string)config.NewUser.job
            };

            var content = ExecuteRequest("users", Method.Post, newUser, 201);
            ValidateJsonSchema(content, "UserCreateSchema.json");
        }

        [Test, Order(2)]
        public void TC_002_GetSingleUser()
        {
            var userId = (string)config.UserId;
            var content = ExecuteRequest($"users/{userId}", Method.Get, null, 200);
            ValidateJsonSchema(content, "UserGetSchema.json");
        }

        [Test, Order(3)]
        public void TC_003_UpdateUser()
        {
            var userId = (string)config.UserId;
            var updatedUser = new
            {
                name = (string)config.UpdatedUser.name,
                job = (string)config.UpdatedUser.job
            };

            var content = ExecuteRequest($"users/{userId}", Method.Put, updatedUser, 200);
            ValidateJsonSchema(content, "UserUpdateSchema.json");
        }

        [Test, Order(4)]
        public void TC_004_DeleteUser()
        {
            var userId = (string)config.UserId;
            var content = ExecuteRequest($"users/{userId}", Method.Delete, null, 204);

            // DELETE อาจไม่มี content
            Assert.That(content.Count, Is.EqualTo(0), "Delete response should be empty");
        }
    }
}
