using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
//using System.Web.Http.Description;
using Microsoft.Bot.Connector;
//using Newtonsoft.Json;
using Microsoft.Azure.Management.ResourceManager.Models;
using Microsoft.Rest;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Azure.Management.ResourceManager;
using Microsoft.Azure.Management.Storage.Models;
using Microsoft.Azure.Management.Storage;
using System.Configuration;

namespace MyComplexBot
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        private static string subscriptionId = ConfigurationManager.AppSettings["SubscriptionId"];
        private static string resourceGroup = ConfigurationManager.AppSettings["ResourceGroupName"];
        private static string location = ConfigurationManager.AppSettings["Location"];
        private static string storageName = ConfigurationManager.AppSettings["Storage"];
        private static string clientKey = ConfigurationManager.AppSettings["ClientKey"];
        private static string clientPassword = ConfigurationManager.AppSettings["ClientPassword"];
        private static string tenantId = ConfigurationManager.AppSettings["TenantId"];
        static string accessToken;
        static DateTime expiresOn;

        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public HttpResponseMessage Post([FromBody]Activity activity)
        {
            if (activity.Type == ActivityTypes.Message)
            {
                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                int length = (activity.Text ?? string.Empty).Length;
                string message = activity.Text.ToLower();
                var token = GetAccessToken();
                var credential = new TokenCredentials(accessToken);

                // return our reply to the user
                if (message.Contains("hello") || message.Contains("hi"))
                {
                    connector.Conversations.ReplyToActivity(activity.CreateReply("Hi Sanjay"));
                }
                else if (message.Contains("create") &&  message.Contains("storage"))
                {
                    connector.Conversations.ReplyToActivity(activity.CreateReply("Creating storage with name ..."+ storageName));
                    var stResult = CreateStorageAccount(credential, resourceGroup, subscriptionId, location,storageName);
                    connector.Conversations.ReplyToActivity(activity.CreateReply("Storage with name "+ storageName +" created Succeeded"));
                }
                else if (message.Contains("delete") && message.Contains("storage"))
                {
                    connector.Conversations.ReplyToActivity(activity.CreateReply("Deleting storage with name ..." + storageName));
                    DeleteStorageAccount(credential, resourceGroup, subscriptionId, storageName);
                    connector.Conversations.ReplyToActivity(activity.CreateReply("Storage with name " + storageName + " created Succeeded"));
                }
                else
                {
                    connector.Conversations.ReplyToActivity(activity.CreateReply("I didnt understand the command"));

                }

            }
            else
            {
                HandleSystemMessage(activity);
            }
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        private Activity HandleSystemMessage(Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
              
              
               
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
                // Handle conversation state changes, like members being added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
                // Not available in all channels
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (message.Type == ActivityTypes.Typing)
            {
                // Handle knowing tha the user is typing
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }

            return null;
        }
              
        private static string GetAccessToken()
        {
            var cc = new ClientCredential(clientKey, clientPassword);
            var context = new AuthenticationContext("https://login.windows.net/" + tenantId);


            if (string.IsNullOrEmpty(accessToken) || expiresOn < DateTime.Now)
            {
                var result = context.AcquireTokenAsync("https://management.azure.com/", cc).Result;
                expiresOn = result.ExpiresOn.LocalDateTime;
                accessToken =result.AccessToken;
                if (result == null)
                {
                    throw new InvalidOperationException("Could not get the token");
                }
            }
           
            return accessToken;
        }

        public static StorageAccount CreateStorageAccount(TokenCredentials credential, string groupName, string subscriptionId,
                    string location, string storageName)
        {
            var storageManagementClient = new StorageManagementClient(credential)
            { SubscriptionId = subscriptionId };
            return  storageManagementClient.StorageAccounts.Create(
              groupName,
              storageName,
              new StorageAccountCreateParameters()
              {
                  Sku = new Microsoft.Azure.Management.Storage.Models.Sku()
                  { Name = SkuName.StandardLRS },
                  Kind = Kind.Storage,
                  Location = location
              }
            );
        }

        public static void DeleteStorageAccount(TokenCredentials credential, string groupName, string subscriptionId, string storageName)
        {
            var storageManagementClient = new StorageManagementClient(credential)
            { SubscriptionId = subscriptionId };
            storageManagementClient.StorageAccounts.Delete(groupName, storageName);
        }

    }
}