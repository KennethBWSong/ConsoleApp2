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
using NetTools;
using Microsoft.Azure.Management.AppPlatform.Models;

namespace ConsoleApp2
{
    class Program
    {
        static string serverIp = "167.220.255.0";
        static void Main(string[] args)
        {
            string accessToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6IkN0VHVoTUptRDVNN0RMZHpEMnYyeDNRS1NSWSIsImtpZCI6IkN0VHVoTUptRDVNN0RMZHpEMnYyeDNRS1NSWSJ9.eyJhdWQiOiJodHRwczovL21hbmFnZW1lbnQuY29yZS53aW5kb3dzLm5ldC8iLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC83MmY5ODhiZi04NmYxLTQxYWYtOTFhYi0yZDdjZDAxMWRiNDcvIiwiaWF0IjoxNTkwNjM0NDIzLCJuYmYiOjE1OTA2MzQ0MjMsImV4cCI6MTU5MDYzODMyMywiX2NsYWltX25hbWVzIjp7Imdyb3VwcyI6InNyYzEifSwiX2NsYWltX3NvdXJjZXMiOnsic3JjMSI6eyJlbmRwb2ludCI6Imh0dHBzOi8vZ3JhcGgud2luZG93cy5uZXQvNzJmOTg4YmYtODZmMS00MWFmLTkxYWItMmQ3Y2QwMTFkYjQ3L3VzZXJzLzNiM2FhYmI2LWVkMWYtNDAyZS1hMTkzLTIwYmIyNjliOGYzNi9nZXRNZW1iZXJPYmplY3RzIn19LCJhY3IiOiIxIiwiYWlvIjoiQVZRQXEvOFBBQUFBakw0NkFUZUZEVm5zTXIrYlZMNnJpU0VHWWV2aEpmYU5aM3hWOXBoVytTVi9ocmsvMUEzV1dqcFc1S09vczBlclV2aUkxSktnaFBFbVpQVkExM3VNYU5DY1A2UG5QMEVhVllNUUI5Y3RMT1k9IiwiYW1yIjpbIndpYSIsIm1mYSJdLCJhcHBpZCI6IjdmNTlhNzczLTJlYWYtNDI5Yy1hMDU5LTUwZmM1YmIyOGI0NCIsImFwcGlkYWNyIjoiMiIsImRldmljZWlkIjoiNjM4ZTdkMTgtNTEwYi00ZjUwLWIzMDgtYzNiYWVhZTFhNDdjIiwiZmFtaWx5X25hbWUiOiJTb25nIiwiZ2l2ZW5fbmFtZSI6IkJvd2VuIiwiaXBhZGRyIjoiMTY3LjIyMC4yNTUuMCIsIm5hbWUiOiJCb3dlbiBTb25nIiwib2lkIjoiM2IzYWFiYjYtZWQxZi00MDJlLWExOTMtMjBiYjI2OWI4ZjM2Iiwib25wcmVtX3NpZCI6IlMtMS01LTIxLTIxNDY3NzMwODUtOTAzMzYzMjg1LTcxOTM0NDcwNy0yNjExNjcxIiwicHVpZCI6IjEwMDMyMDAwQThCNTJBNkQiLCJyaCI6IjAuQVFFQXY0ajVjdkdHcjBHUnF5MTgwQkhiUjNPbldYLXZMcHhDb0ZsUV9GdXlpMFFhQUpnLiIsInNjcCI6InVzZXJfaW1wZXJzb25hdGlvbiIsInN1YiI6IjYwQW5jTzQtMXRfeFMyYmFLQnZvemI3UDdlTGVJU092amFPRkIxVHUyVVEiLCJ0aWQiOiI3MmY5ODhiZi04NmYxLTQxYWYtOTFhYi0yZDdjZDAxMWRiNDciLCJ1bmlxdWVfbmFtZSI6ImJvd3NvbmdAbWljcm9zb2Z0LmNvbSIsInVwbiI6ImJvd3NvbmdAbWljcm9zb2Z0LmNvbSIsInV0aSI6ImdhMnFGUURvZjBDWmM0OS14WDBiQUEiLCJ2ZXIiOiIxLjAifQ.x4aacUCEH41l0xgI2byRNulIwfkOO9RlP3B5gE-1fqKbPLgdJwmZLyty4mEpDIJEikjlNOcGlREtJsdJVhEEMvcltrAMo4K3kLdQO_TSSZ46DBZWu-DSmiARJxEkveBG4HiVEgIbsVy2sOwEvRfNM7htLBObiuBFnGAq1G-TVHHO4S_nZCxsA1PyS-2OsnnBVM1YZtA0RkY4C-LC2DRrhAyWWBkmJaO486olRNzACKjMwOqVSbubQ6QYiQD8BAd664OVlOHTdhFiVPCAhTXS3i5MSNvTBdxwliBkQJTnB-jnl2IieK1-0sh-nYBzTHHJ-4iYhMBlkUIHrjyq8M9_1A";
            TokenCredentials token = new TokenCredentials(accessToken);
            string conStr = @"Server=tcp:bwsongsql.database.windows.net,1433; Database=bwsongdb;";
            string SubscriptionId = "faab228d-df7a-4086-991e-e81c4659d41a";
            string aadGuid = "3b3aabb6-ed1f-402e-a193-20bb269b8f36";
            string appName = "bwsongapp";
            string rgName = "bwsonggroup";
            string sqlName = "bwsongsql";

            //ConnectSQLApp(token, conStr, SubscriptionId, aadGuid, appName, rgName, sqlName);
            //Console.WriteLine(ValidationSQLApp(token, conStr, SubscriptionId, aadGuid, appName, rgName, sqlName));
            ConnectMySQLSC(token, conStr, SubscriptionId, aadGuid, "bwsongsc", "bwsongscapp", "bwsonggroup", "bwsongmysql", "mysql", "bowsong", "Dong@258");
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
        static void ConnectMySQLSC(TokenCredentials token, string conStr, string SubscriptionId, string aadGuid, string scName, string scAppName, string rgName, string mySqlName, string mySqlDbName, string username, string password)
        {
            AppPlatformManagementClient scClient = new AppPlatformManagementClient(token);
            scClient.SubscriptionId = SubscriptionId;
            BindingResource bindingResource = new BindingResource
            {
                Properties = new BindingResourceProperties
                {
                    ResourceId = "/subscriptions/" + SubscriptionId + "resourceGroups/" + rgName + "/providers/Microsoft.DBforMySQL/servers/" + mySqlName,
                    Key = "abcd",
                    BindingParameters = new Dictionary<string, object>
                    {
                        {"Database name", mySqlDbName},
                        {"username", username},
                        {"Password", password }
                    }
                }
            };
            scClient.Bindings.CreateOrUpdate(rgName, scName, scAppName, "cupertino", bindingResource);
        }
    }
}
