using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.IO;
using System.Configuration;
using Microsoft.Rest.Azure;
using Microsoft.Rest;
using Microsoft.Azure.Management.ResourceManager;
using Microsoft.Azure.Management.ResourceManager.Models;
using System.Data.SqlClient;
using Microsoft.Azure.Management.WebSites;
using Microsoft.Azure.Management.WebSites.Models;
using Microsoft.Azure;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Azure.Common;
using Microsoft.Azure.Management.Dns;
using Microsoft.Azure.Management.Dns.Models;
using System.Runtime.Remoting.Contexts;
using System.Net;
using ManagedServiceIdentity = Microsoft.Azure.Management.WebSites.Models.ManagedServiceIdentity;
using ManagedServiceIdentityType = Microsoft.Azure.Management.WebSites.Models.ManagedServiceIdentityType;
using Microsoft.Azure.Management.Sql;
using Microsoft.Azure.Management.Sql.Models;
using Microsoft.Azure.Management.AppPlatform;
using RestSharp;
using Microsoft.Azure.Management.AppPlatform.Models;
using NetTools;
using Microsoft.Azure.Management.Network.Fluent.Models;
using MySql.Data.MySqlClient.Memcached;

namespace ConsoleApp2
{
    class Program
    {
        static string serverIp = "167.220.255.0";
        static async Task Main(string[] args)
        {
            string accessToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6IkN0VHVoTUptRDVNN0RMZHpEMnYyeDNRS1NSWSIsImtpZCI6IkN0VHVoTUptRDVNN0RMZHpEMnYyeDNRS1NSWSJ9.eyJhdWQiOiJodHRwczovL21hbmFnZW1lbnQuY29yZS53aW5kb3dzLm5ldC8iLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC83MmY5ODhiZi04NmYxLTQxYWYtOTFhYi0yZDdjZDAxMWRiNDcvIiwiaWF0IjoxNTkwNjUyMTA5LCJuYmYiOjE1OTA2NTIxMDksImV4cCI6MTU5MDY1NjAwOSwiX2NsYWltX25hbWVzIjp7Imdyb3VwcyI6InNyYzEifSwiX2NsYWltX3NvdXJjZXMiOnsic3JjMSI6eyJlbmRwb2ludCI6Imh0dHBzOi8vZ3JhcGgud2luZG93cy5uZXQvNzJmOTg4YmYtODZmMS00MWFmLTkxYWItMmQ3Y2QwMTFkYjQ3L3VzZXJzLzNiM2FhYmI2LWVkMWYtNDAyZS1hMTkzLTIwYmIyNjliOGYzNi9nZXRNZW1iZXJPYmplY3RzIn19LCJhY3IiOiIxIiwiYWlvIjoiQVZRQXEvOFBBQUFBNFpFYmxYeFBiWWRwSW1CczB5M0RVeEtrVWdPbjcrRExORGhQTXpHTEVtV0RqTzgyNGpXTnkyelgvUWMzRFBIczAxNld2Y2ozOVkza0prK1UwcTVRU0pYZFl6NUYvSGVpNCs5Ym1IcWdkNkU9IiwiYW1yIjpbIndpYSIsIm1mYSJdLCJhcHBpZCI6IjdmNTlhNzczLTJlYWYtNDI5Yy1hMDU5LTUwZmM1YmIyOGI0NCIsImFwcGlkYWNyIjoiMiIsImRldmljZWlkIjoiNjM4ZTdkMTgtNTEwYi00ZjUwLWIzMDgtYzNiYWVhZTFhNDdjIiwiZmFtaWx5X25hbWUiOiJTb25nIiwiZ2l2ZW5fbmFtZSI6IkJvd2VuIiwiaXBhZGRyIjoiMTY3LjIyMC4yNTUuMCIsIm5hbWUiOiJCb3dlbiBTb25nIiwib2lkIjoiM2IzYWFiYjYtZWQxZi00MDJlLWExOTMtMjBiYjI2OWI4ZjM2Iiwib25wcmVtX3NpZCI6IlMtMS01LTIxLTIxNDY3NzMwODUtOTAzMzYzMjg1LTcxOTM0NDcwNy0yNjExNjcxIiwicHVpZCI6IjEwMDMyMDAwQThCNTJBNkQiLCJyaCI6IjAuQVFFQXY0ajVjdkdHcjBHUnF5MTgwQkhiUjNPbldYLXZMcHhDb0ZsUV9GdXlpMFFhQUpnLiIsInNjcCI6InVzZXJfaW1wZXJzb25hdGlvbiIsInN1YiI6IjYwQW5jTzQtMXRfeFMyYmFLQnZvemI3UDdlTGVJU092amFPRkIxVHUyVVEiLCJ0aWQiOiI3MmY5ODhiZi04NmYxLTQxYWYtOTFhYi0yZDdjZDAxMWRiNDciLCJ1bmlxdWVfbmFtZSI6ImJvd3NvbmdAbWljcm9zb2Z0LmNvbSIsInVwbiI6ImJvd3NvbmdAbWljcm9zb2Z0LmNvbSIsInV0aSI6IlVzVjVLQWE4MkVlNGV5YWNmRDhWQUEiLCJ2ZXIiOiIxLjAifQ.mO1hH3p6gHP4mi-pqH7CIkyKq-eUQnUc2r4SavPY22OFmW_w9dcFmvOR5z8gseOpsFz6Nd0bplWVVfRGjPhEkwKBGmnvTcgB5cv5QVi7vUUQXeDYBywB50_8qmNta5eHu9Wrc3j4_r-qNYNmG5BIF0bAMdtRE-GIp-lNRObuwDebdMEc5AddHzfaeKoF1wcqCaXBEuI93IPjur_fBJQkY2olO44wNWs9mza3KOv7vlcxark_11nVkV66Z955nZQ0Wuo6G-b09XirkqJEYDbhMPQD2PsTF1K9UfQtclGOEEpsIhZl8r3dnBz_h8dDZFVsv99DMZRgXz_AqBio_URpmw";
            TokenCredentials token = new TokenCredentials(accessToken);
            string conStr = @"Server=tcp:bwsongsql.database.windows.net,1433; Database=bwsongdb;";
            string SubscriptionId = "faab228d-df7a-4086-991e-e81c4659d41a";
            string aadGuid = "3b3aabb6-ed1f-402e-a193-20bb269b8f36";
            string appName = "bwsongapp";
            string rgName = "bwsonggroup";
            string sqlName = "bwsongsql";

            //ConnectSQLApp(token, conStr, SubscriptionId, aadGuid, appName, rgName, sqlName);
            //Console.WriteLine(ValidationSQLApp(token, conStr, SubscriptionId, aadGuid, appName, rgName, sqlName));
            //ConnectMySQLSC(token, conStr, SubscriptionId, aadGuid, "bwsongsc", "bwsongscapp", "bwsonggroup", "bwsongmysql", "sys", "bowsong", "Dong@258", accessToken);
            Console.WriteLine(ValidationMySQLSC(token, conStr, SubscriptionId, aadGuid, "bwsongsc", "bwsongscapp", "bwsonggroup", "bwsongmysql", "sys", "bowsong", "Dong@258", accessToken));
        }

        // Connect SQL and webapp
        static void ConnectSQLApp(TokenCredentials token, string conStr, string SubscriptionId, string aadGuid, string appName, string rgName, string sqlName)
        {
            Console.WriteLine("Enable Webapp Managed Identity.");
            //var loggingHandler = new LoggingHandler(new HttpClientHandler());
            // Create webclient and assign subscription id
            WebSiteManagementClient webClient = new WebSiteManagementClient(token);
            webClient.SubscriptionId = SubscriptionId;
            // Create identity and update webapp with identity
            var send = new SitePatchResource
            {
                Identity = new ManagedServiceIdentity(ManagedServiceIdentityType.SystemAssigned)
            };
            webClient.WebApps.Update(rgName, appName, send);
            // Get webapp outbound ip
            string ip = webClient.WebApps.Get(rgName, appName).OutboundIpAddresses.Split(',')[0];
            // Set webapp connectionstring
            var connectionString = new ConnectionStringDictionary
            {
                Properties = new Dictionary<string, ConnStringValueTypePair>
                {
                    {"Cupertino_"+appName+"_ConnectionString", new ConnStringValueTypePair{Value = conStr, Type = ConnectionStringType.SQLServer} }
                }
            };
            webClient.WebApps.UpdateConnectionStrings(rgName, appName, connectionString);

            Console.WriteLine("Add SQL Admin and Set Firewall Rules.");
            // Create sqlclient and assign subscription id
            SqlManagementClient sqlClient = new SqlManagementClient(token);
            sqlClient.SubscriptionId = SubscriptionId;
            // Get GUID of aad user and set sql aad admin
            sqlClient.ServerAzureADAdministrators.CreateOrUpdate(rgName, sqlName, new ServerAzureADAdministrator("Admin", new Guid(aadGuid)));
            // Set Firewall Rule for server to access sql
            sqlClient.FirewallRules.CreateOrUpdate(rgName, sqlName, "server", new FirewallRule(serverIp, serverIp));
            // Set Firewall Rule for webapp to access sql
            sqlClient.FirewallRules.CreateOrUpdate(rgName, sqlName, appName, new FirewallRule(ip, ip));

            Console.WriteLine("Add User to SQL Database.");
            using (SqlConnection connection = new SqlConnection(conStr))
            {
                connection.AccessToken = (new Microsoft.Azure.Services.AppAuthentication.AzureServiceTokenProvider()).GetAccessTokenAsync("https://database.windows.net/").Result;
                connection.Open();
                Console.WriteLine("Connected");
                SqlCommand command = new SqlCommand("CREATE USER [bwsongapp] FROM EXTERNAL PROVIDER;ALTER ROLE db_datareader ADD MEMBER [bwsongapp]; ALTER ROLE db_datawriter ADD MEMBER [bwsongapp]; ALTER ROLE db_ddladmin ADD MEMBER [bwsongapp];", connection);
                command.ExecuteNonQuery();
                Console.WriteLine("Added");
            }
        }

        // Validate connection between sql and webapp
        static bool ValidationSQLApp(TokenCredentials token, string conStr, string SubscriptionId, string aadGuid, string appName, string rgName, string sqlName)
        {
            Console.WriteLine("Check SQL Firewall Rule");
            // Create sqlclient and assign subscription id
            SqlManagementClient sqlClient = new SqlManagementClient(token);
            sqlClient.SubscriptionId = SubscriptionId;
            // Create webclient and assign subscription id
            WebSiteManagementClient webClient = new WebSiteManagementClient(token);
            webClient.SubscriptionId = SubscriptionId;
            // Get webapp outbound ips
            string[] ips = webClient.WebApps.Get(rgName, appName).OutboundIpAddresses.Split(',');
            // Get Firewall Rule for server to access sql
            IEnumerable<FirewallRule> firewallRules = sqlClient.FirewallRules.ListByServer(rgName, sqlName);
            // Check whether webapp outbound ips falls in the firewall rule
            bool isInFirewall = false;
            foreach (FirewallRule firewallRule in firewallRules)
            {
                if (firewallRule.StartIpAddress.Equals(firewallRule.EndIpAddress))
                {
                    foreach (string ip in ips)
                    {
                        if (ip.Equals(firewallRule.StartIpAddress))
                        {
                            isInFirewall = true;
                            break;
                        }
                    }
                    if (isInFirewall) break;
                }
                else
                {
                    var ipRange = IPAddressRange.Parse(firewallRule.StartIpAddress + '/' + firewallRule.EndIpAddress);
                    foreach (string ip in ips)
                    {
                        if (ipRange.Contains(IPAddress.Parse(ip)))
                        {
                            isInFirewall = true;
                            break;
                        }
                    }
                    if (isInFirewall) break;
                }
            }
            return isInFirewall;
        }

        // Connect MySQL and Spring Cloud
        static void ConnectMySQLSC(TokenCredentials token, string conStr, string SubscriptionId, string aadGuid, string scName, string scAppName, string rgName, string mySqlName, string mySqlDbName, string username, string password, string accessToken)
        {
            //AppPlatformManagementClient scClient = new AppPlatformManagementClient(token);
            //scClient.SubscriptionId = SubscriptionId;
            //BindingResource bindingResource = new BindingResource
            //{
            //    Properties = new BindingResourceProperties
            //    {
            //        ResourceId = "/subscriptions/" + SubscriptionId + "/resourceGroups/" + rgName + "/providers/Microsoft.DBforMySQL/servers/" + mySqlName,
            //        Key = "abcd",
            //        BindingParameters = new Dictionary<string, object>
            //        {
            //            {"Database name", mySqlDbName},
            //            {"username", username},
            //            {"Password", password }
            //        }
            //    }
            //};
            //scClient.Bindings.CreateOrUpdate(rgName, scName, scAppName, "cupertino", bindingResource);
            RestClient rc = new RestClient("https://management.azure.com");
            RestRequest request = new RestRequest("subscriptions/faab228d-df7a-4086-991e-e81c4659d41a/resourceGroups/bwsonggroup/providers/Microsoft.DBforMySQL/servers/bwsongmysql/firewallRules/test?api-version=2017-12-01", Method.PUT);
            request.AddHeader("Content-type", "application/json");
            request.AddParameter("application/json", "{\"properties\": {\"startIpAddress\": \"0.0.0.0\",\"endIpAddress\": \"0.0.0.0\"}}", RestSharp.ParameterType.RequestBody);
            request.AddHeader("Authorization", "Bearer " + accessToken);
            string returnedStr = rc.Execute(request).Content;
            Console.WriteLine(returnedStr);
        }

        // Validate connection between MySQL and Spring Cloud
        static bool ValidationMySQLSC(TokenCredentials token, string conStr, string SubscriptionId, string aadGuid, string scName, string scAppName, string rgName, string mySqlName, string mySqlDbName, string username, string password, string accessToken)
        {
            AppPlatformManagementClient scClient = new AppPlatformManagementClient(token);
            scClient.SubscriptionId = SubscriptionId;
            IPage<BindingResource> bindings = scClient.Bindings.List(rgName, scName, scAppName);
            string resourceId = "/subscriptions/" + SubscriptionId + "/resourceGroups/" + rgName + "/providers/Microsoft.DBforMySQL/servers/" + mySqlName;
            IDictionary<string, object> bindingParameters = new Dictionary<string, object>
            {
                {"Database name", mySqlDbName},
                {"username", username},
                {"Password", password }
            };
            bool isConnected = false;
            foreach (BindingResource binding in bindings)
            {
                if (binding.Properties.ResourceId.Equals(resourceId))
                {
                    foreach (var item in binding.Properties.BindingParameters)
                    {
                        if (!bindingParameters[item.Key].Equals(item.Value)) return false;
                    }
                    isConnected = true;
                }
            }
            if (!isConnected) return false;
            RestClient rc = new RestClient("https://management.azure.com");
            RestRequest request = new RestRequest("subscriptions/faab228d-df7a-4086-991e-e81c4659d41a/resourceGroups/bwsonggroup/providers/Microsoft.DBforMySQL/servers/bwsongmysql/firewallRules?api-version=2017-12-01", Method.GET);
            request.AddHeader("Authorization", "Bearer " + accessToken);
            string returnedStr = rc.Execute(request).Content;
            Console.WriteLine(returnedStr);
            int i = 0;
            while (i < returnedStr.Length)
            {
                int tmp = returnedStr.IndexOf("startIpAddress", i);
                if (tmp == 0 || tmp == -1) return false;
                string startIp = returnedStr.Substring(tmp + 17, 7);
                string endIp = returnedStr.Substring(tmp + 42, 7);
                if (startIp.Equals("0.0.0.0") && endIp.Equals("0.0.0.0")) return true;
                else i = tmp + 1;
            }
            return false;
        }
    }
}
