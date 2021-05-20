using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OfficeDevPnP.Core;

using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using System.Security.Cryptography.X509Certificates;
using System.Configuration;
using Microsoft.SharePoint.Client;
using System.Net;

namespace Microsoft_AppOnlyAD
{
    public static class Function1
    {
        [FunctionName("Function1")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

        
           // string appOnlyId = ConfigurationManager.AppSettings["AppOnlyID"];
           // string appOnlySecret = ConfigurationManager.AppSettings["AppOnlySecret"];

            // parse query parameter  
            log.LogInformation("C# HTTP trigger function processed a request.");

            // // parse query parameter  
            string title = req.Query["title"];
            string nameFR = req.Query["spacenamefr"];
            string owner1 = req.Query["owner1"];
            string owner2 = req.Query["owner2"];
            string owner3 = req.Query["owner3"];
            string description = req.Query["description"];
            string template = req.Query["template"];
            string descriptionFr = req.Query["descriptionFr"];
            string business = req.Query["business"];
            string requester_name = req.Query["requester_name"];
            string requester_email = req.Query["requester_email"];

            // // Get request body  
            //dynamic data = await req.Content.ReadAsAsync<object>();
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            dynamic data = JsonConvert.DeserializeObject(requestBody);

            // // Set name to query string or body data  
            title = title ?? data?.name.title;
            nameFR = nameFR ?? data?.name.nameFR;
            owner1 = owner1 ?? data?.name.owner1;
            owner2 = owner2 ?? data?.name.owner2;
            owner3 = owner3 ?? data?.name.owner3;
            description = description ?? data?.name.description;
            template = template ?? data?.name.template;
            descriptionFr = descriptionFr ?? data?.name.descriptionFr;
            business = business ?? data?.name.business;
            requester_name = requester_name ?? data?.name.requester_name;
            requester_email = requester_email ?? data?.name.requester_email;

            log.LogInformation("get info" + title);

          //  string name = req.Query["name"];

           // string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
           // dynamic data = JsonConvert.DeserializeObject(requestBody);
            //name = name ?? data?.name;

           
           //using (var cc = new AuthenticationManager().GetAzureADAppOnlyAuthenticatedContext(siteUrl, "1d4139b1-fd16-4f06-8ac6-7b9ac7b58864", "tbssctdev.onmicrosoft.com", @"C:\test.pfx", "password123"))

            using (var cc = new OfficeDevPnP.Core.AuthenticationManager().GetAzureADAppOnlyAuthenticatedContext(siteURL, "", "", KeyVaultAccess.GetKeyVaultCertificate("", "")))
            {
                cc.Load(cc.Web, p => p.Title);
                cc.Load(cc.Web, p => p.Description);

                cc.ExecuteQuery();
                Console.WriteLine(cc.Web.Title);
                Console.WriteLine(cc.Web.Description);

           


                //ClientContext ctx = new OfficeDevPnP.Core.AuthenticationManager().GetAppOnlyAuthenticatedContext(siteURL, appOnlyId, appOnlySecret);
                log.LogInformation("get context");

                Web web = cc.Web;
                List list = cc.Web.Lists.GetByTitle("Space Requests");
                log.LogInformation("get list");

                ListItemCreationInformation oListItemCreationInformation = new ListItemCreationInformation();
                ListItem oItem = list.AddItem(oListItemCreationInformation);

                User userTest = web.EnsureUser(owner1);
                User userTest2 = web.EnsureUser(owner2);
                

                cc.Load(userTest);
                cc.ExecuteQuery();
                   log.LogInformation("get user");
                owner1 = userTest.Id.ToString() + ";#" + userTest.LoginName.ToString();
                cc.Load(userTest2);
                cc.ExecuteQuery();
                owner2 = userTest2.Id.ToString() + ";#" + userTest2.LoginName.ToString();
                if (owner3 != "")
                {
                    User userTest3 = web.EnsureUser(owner3);
                    cc.Load(userTest3);
                    cc.ExecuteQuery();
                    owner3 = userTest3.Id.ToString() + ";#" + userTest3.LoginName.ToString();
                }

                oItem["Space_x0020_Name"] = title;
                oItem["Space_x0020_Name_x0020_FR"] = nameFR;
                oItem["Owner1"] = owner1 + ";#" + owner2 + ";#" + owner3;
                oItem["Space_x0020_Description_x0020__x"] = description;
                oItem["Template_x0020_Title"] = template;
                oItem["Space_x0020_Description_x0020__x0"] = descriptionFr;
                oItem["Team_x0020_Purpose_x0020_and_x00"] = business;
                oItem["Business_x0020_Justification"] = business;
                oItem["Requester_x0020_Name"] = requester_name;
                oItem["Requester_x0020_email"] = requester_email;
                oItem["_Status"] = "Submitted";
                oItem.Update();
                cc.ExecuteQuery();
 
                CamlQuery camlQuery = new CamlQuery();
                camlQuery.ViewXml = string.Format(@"
                <View>
                    <Query>
                        <Where>
                            <Eq>
                                <FieldRef Name='Space_x0020_Name' />
                                <Value Type='Text'>{0}</Value>
                            </Eq>
                        </Where>
                    </Query>
                    <ViewFields>
                        <FieldRef Name='ID'/>
                        <FieldRef Name='Space_x0020_Name'/>
                    </ViewFields>
                    <RowLimit>1</RowLimit>
                </View>", title);

                ListItemCollection collListItemID = list.GetItems(camlQuery);
                cc.Load(collListItemID);
                cc.ExecuteQuery();

                int requestID = 0;

                foreach (ListItem oListItem in collListItemID)
                {
                    log.LogInformation(oListItem["Space_x0020_Name"].ToString());
                    requestID = oListItem.Id;

                }
                ListItem collListItem = list.GetItemById(requestID);
                // changes some fields 	
                collListItem["SharePoint_x0020_Site_x0020_URL"] = "https://.sharepoint.com/teams/1000" + requestID;
                collListItem.Update();
                // executes the update of the list item on SharePoint	
                cc.ExecuteQuery();

                //req.CreateResponse(HttpStatusCode.InternalServerError, "Create item successfully ");

                //  }  
                //return null;

                string responseMessage = "Create item successfully ";

               return new OkObjectResult(responseMessage);
            
            }
        }
    }

    //internal static X509Certificate2 GetKeyVaultCertificate(string keyvaultName, string name)
    //{
    //    var serviceTokenProvider = new AzureServiceTokenProvider();
    //    var keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(serviceTokenProvider.KeyVaultTokenCallback));

    //    // Getting the certificate
    //    var secret = keyVaultClient.GetSecretAsync("https://" + keyvaultName + ".vault.azure.net/", name);

    //    // Returning the certificate
    //    return new X509Certificate2(Convert.FromBase64String(secret.Result.Value));
    //}

    class KeyVaultAccess
    {

        internal static X509Certificate2 GetKeyVaultCertificate(string keyvaultName, string name)
        {
            var serviceTokenProvider = new AzureServiceTokenProvider();
            var keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(serviceTokenProvider.KeyVaultTokenCallback));

            // Getting the certificate
            var secret = keyVaultClient.GetSecretAsync("https://" + keyvaultName + ".vault.azure.net/", name);

            // Returning the certificate
            return new X509Certificate2(Convert.FromBase64String(secret.Result.Value));
        }
    }
}
