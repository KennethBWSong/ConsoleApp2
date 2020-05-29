using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Rest.Azure;
using Microsoft.Rest;
using Microsoft.Azure.Management.ResourceManager;
using Microsoft.Azure.Management.WebSites;
using Microsoft.Azure.Management.WebSites.Models;
using Microsoft.Azure.Management.Dns;
using System.Net;
using ManagedServiceIdentity = Microsoft.Azure.Management.WebSites.Models.ManagedServiceIdentity;
using ManagedServiceIdentityType = Microsoft.Azure.Management.WebSites.Models.ManagedServiceIdentityType;
using Microsoft.Azure.Management.Sql;
using Microsoft.Azure.Management.Sql.Models;
using Microsoft.Azure.Management.AppPlatform;
using RestSharp;
using Microsoft.Azure.Management.AppPlatform.Models;
using NetTools;

namespace ConsoleApp2
{
    public class AppService
    {
        public AppService(string accessToken, string subscriptionId, string rgName, string appName)
        {
            _accessToken = string.Copy(accessToken);
            _subscriptionId = string.Copy(subscriptionId);
            _rgName = string.Copy(rgName);
            _appName = string.Copy(appName);
            _tokenCredentials = new TokenCredentials(accessToken);
            _client = new WebSiteManagementClient(_tokenCredentials);
            _client.SubscriptionId = _subscriptionId;
        }

        public void SetManagedIdentity()
        {
            _client.WebApps.Update(_rgName, _appName, new SitePatchResource { Identity = new ManagedServiceIdentity(ManagedServiceIdentityType.SystemAssigned)});
        }

        public string GetOutboundIp()
        {
            return _client.WebApps.Get(_rgName, _appName).OutboundIpAddresses.Split(',')[0];
        }

        public string[] GetOutboundIps()
        {
            return _client.WebApps.Get(_rgName, _appName).OutboundIpAddresses.Split(',');
        }

        public void SetConnectionString(string connectionString)
        {
            var _connectionString = new ConnectionStringDictionary
            {
                Properties = new Dictionary<string, ConnStringValueTypePair>
                {
                    {"Cupertino_" + _appName, new ConnStringValueTypePair{Value = connectionString, Type = ConnectionStringType.SQLServer} }
                }
            };
            _client.WebApps.UpdateConnectionStrings(_rgName, _appName, _connectionString);
        }

        private string _accessToken;
        private TokenCredentials _tokenCredentials;
        private string _subscriptionId;
        private string _appName;
        private string _rgName;
        private WebSiteManagementClient _client;
    }

    public class AzureSQL
    {
        public AzureSQL(string accessToken, string subscriptionId, string aadGuid, string rgName, string serverName, string dbName, string language)
        {
            // For MSI
            _accessToken = accessToken;
            _subscriptionId = subscriptionId;
            _aadGuid = aadGuid;
            _rgName = rgName;
            _serverName = serverName;
            _dbName = dbName;
            _language = language;
            _type = "MSI";
            _tokenCredentials = new TokenCredentials(accessToken);
            _client = new SqlManagementClient(_tokenCredentials);
            _client.SubscriptionId = _subscriptionId;
            SetConnectionString();
        }

        public AzureSQL(string accessToken, string subscriptionId, string aadGuid, string rgName, string serverName, string dbName, string userName, string password, string language)
        {
            // For SECRET
            _accessToken = accessToken;
            _subscriptionId = subscriptionId;
            _aadGuid = aadGuid;
            _rgName = rgName;
            _serverName = serverName;
            _dbName = dbName;
            _userName = userName;
            _password = password;
            _language = language;
            _type = "SECRET";
            _tokenCredentials = new TokenCredentials(accessToken);
            _client = new SqlManagementClient(_tokenCredentials);
            _client.SubscriptionId = _subscriptionId;
            SetConnectionString();
        }

        public void SetAADAdmin()
        {
            _client.ServerAzureADAdministrators.CreateOrUpdate(_rgName, _serverName, new ServerAzureADAdministrator("Admin", new Guid(_aadGuid)));
        }

        public void SetFirewallRule(string startIp, string endIp)
        {
            _client.FirewallRules.CreateOrUpdate(_rgName, _serverName, "Cupertino", new FirewallRule(startIp, endIp));
        }

        public bool CheckIpIsInFirewallRule(string[] ips)
        {
            IEnumerable<FirewallRule> firewallRules = _client.FirewallRules.ListByServer(_rgName, _serverName);
            foreach (FirewallRule firewallRule in firewallRules)
            {
                if (firewallRule.StartIpAddress.Equals(firewallRule.EndIpAddress))
                {
                    foreach (string ip in ips)
                    {
                        if (ip.Equals(firewallRule.StartIpAddress)) return true;
                    }
                }
                else
                {
                    var ipRange = IPAddressRange.Parse(firewallRule.StartIpAddress + '/' + firewallRule.EndIpAddress);
                    foreach (string ip in ips)
                    {
                        if (ipRange.Contains(IPAddress.Parse(ip))) return true;
                    }
                }
            }
            return false;
        }

        private void SetConnectionString()
        {
            switch (_language)
            {
                case "ADO.NET":
                    if (_type.Equals("MSI")) _connectionString = "Server=tcp:" + _serverName + ".database.windows.net,1433; Database=" + _dbName;
                    else if (_type.Equals("SECRET")) _connectionString = "Server=tcp:" + _serverName + ".database.windows.net,1433; Database=" + _dbName + "User ID=" + _userName + "Password=" + _password + ";MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
                    break;
            }
        }

        public string GetConnectionString()
        {
            return _connectionString;
        }

        private string _accessToken;
        private TokenCredentials _tokenCredentials;
        private string _subscriptionId;
        private string _aadGuid;
        private string _rgName;
        private string _serverName;
        private string _dbName;
        private string _userName;
        private string _password;
        private string _language;
        string _type;
        SqlManagementClient _client;
        private string _connectionString;
    }

    public class SpringCloud
    {
        public SpringCloud(string accessToken, string subscriptionId, string rgName, string scName, string scAppName)
        {
            _accessToken = accessToken;
            _subscriptionId = subscriptionId;
            _rgName = rgName;
            _scName = scName;
            _scAppName = scAppName;
            _tokenCredentials = new TokenCredentials(accessToken);
            _client = new AppPlatformManagementClient(_tokenCredentials);
            _client.SubscriptionId = _subscriptionId;
        }

        public void SetSericeBinding(BindingResourceProperties serviceProperties)
        {
            _client.Bindings.CreateOrUpdate(_rgName, _scName, _scAppName, "cupertino", new BindingResource {Properties = serviceProperties });
        }

        public bool CheckSericeBinding(BindingResourceProperties serviceProperties)
        {
            IPage<BindingResource> allBindings = _client.Bindings.List(_rgName, _scName, _scAppName);
            foreach (BindingResource bindingItem in allBindings)
            {
                if (bindingItem.Properties.ResourceId.Equals(serviceProperties.ResourceId))
                {
                    foreach (var item in bindingItem.Properties.BindingParameters)
                    {
                        if (!serviceProperties.BindingParameters[item.Key].Equals(item.Value)) return false;
                    }
                    return true;
                }
            }
            return false;
        }

        private string _accessToken;
        private TokenCredentials _tokenCredentials;
        private string _subscriptionId;
        private string _rgName;
        private string _scName;
        private string _scAppName;
        AppPlatformManagementClient _client;
    }

    public class AzureMySQL
    {
        public AzureMySQL(string accessToken, string subscriptionId, string rgName, string serverName, string dbName, string userName, string password)
        {
            _accessToken = accessToken;
            _subscriptionId = subscriptionId;
            _rgName = rgName;
            _serverName = serverName;
            _dbName = dbName;
            _userName = userName;
            _password = password;
            _tokenCredentials = new TokenCredentials(accessToken);
            _client = new RestClient("https://management.azure.com");
        }

        public void SetFirewallRule()
        {
            RestRequest request = new RestRequest("subscriptions/" + _subscriptionId + "/resourceGroups/" + _rgName + "/providers/Microsoft.DBforMySQL/servers/" + _serverName + "/firewallRules/" + "Cupertino" + "?api-version=2017-12-01", Method.PUT);
            request.AddHeader("Content-type", "application/json");
            request.AddParameter("application/json", "{\"properties\": {\"startIpAddress\": \"0.0.0.0\",\"endIpAddress\": \"0.0.0.0\"}}", RestSharp.ParameterType.RequestBody);
            request.AddHeader("Authorization", "Bearer " + _accessToken);
            string returnedStr = _client.Execute(request).Content;
            Console.WriteLine(returnedStr);
        }

        public void SetFirewallRule(string startIp, string endIp)
        {
            RestRequest request = new RestRequest("subscriptions/" + _subscriptionId + "/resourceGroups/" + _rgName + "/providers/Microsoft.DBforMySQL/servers/" + _serverName + "/firewallRules/" + "Cupertino" + "?api-version=2017-12-01", Method.PUT);
            request.AddHeader("Content-type", "application/json");
            request.AddParameter("application/json", "{\"properties\": {\"startIpAddress\": \"" + startIp + "\",\"endIpAddress\": \"" + endIp + "\"}}", RestSharp.ParameterType.RequestBody);
            request.AddHeader("Authorization", "Bearer " + _accessToken);
            string returnedStr = _client.Execute(request).Content;
            //Console.WriteLine(returnedStr);
        }
        
        public bool CheckFirewallRule()
        {
            RestRequest request = new RestRequest("subscriptions/" + _subscriptionId + "/resourceGroups/" + _rgName + "/providers/Microsoft.DBforMySQL/servers/" + _serverName + "/firewallRules?api-version=2017-12-01", Method.GET);
            request.AddHeader("Authorization", "Bearer " + _accessToken);
            string returnedStr = _client.Execute(request).Content;
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

        public BindingResourceProperties GetSericeProperties()
        {
            BindingResourceProperties serviceProperties = new BindingResourceProperties
            {
                ResourceId = "/subscriptions/" + _subscriptionId + "/resourceGroups/" + _rgName + "/providers/Microsoft.DBforMySQL/servers/" + _serverName,
                Key = "test",
                BindingParameters = new Dictionary<string, object>
                    {
                        {"Database name", _dbName},
                        {"username", _userName},
                        {"Password", _password}
                    }
            };
            return serviceProperties;
        }

        private string _accessToken;
        private TokenCredentials _tokenCredentials;
        private string _subscriptionId;
        private string _rgName;
        private string _serverName;
        private string _dbName;
        string _userName;
        string _password;
        RestClient _client;
    }

    class Program
    {
        static void Main(string[] args)
        {
            string accessToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6IkN0VHVoTUptRDVNN0RMZHpEMnYyeDNRS1NSWSIsImtpZCI6IkN0VHVoTUptRDVNN0RMZHpEMnYyeDNRS1NSWSJ9.eyJhdWQiOiJodHRwczovL21hbmFnZW1lbnQuY29yZS53aW5kb3dzLm5ldC8iLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC83MmY5ODhiZi04NmYxLTQxYWYtOTFhYi0yZDdjZDAxMWRiNDcvIiwiaWF0IjoxNTkwNzE4NjA2LCJuYmYiOjE1OTA3MTg2MDYsImV4cCI6MTU5MDcyMjUwNiwiX2NsYWltX25hbWVzIjp7Imdyb3VwcyI6InNyYzEifSwiX2NsYWltX3NvdXJjZXMiOnsic3JjMSI6eyJlbmRwb2ludCI6Imh0dHBzOi8vZ3JhcGgud2luZG93cy5uZXQvNzJmOTg4YmYtODZmMS00MWFmLTkxYWItMmQ3Y2QwMTFkYjQ3L3VzZXJzLzNiM2FhYmI2LWVkMWYtNDAyZS1hMTkzLTIwYmIyNjliOGYzNi9nZXRNZW1iZXJPYmplY3RzIn19LCJhY3IiOiIxIiwiYWlvIjoiQVZRQXEvOFBBQUFBNnptWnhnYUFvdGw4NGJFOU00bzJSUlQzMVd2bjM0UytBVVR5OE5nTW1CTnMxejlKQlJtazFSWmpsZzRwWW9WYTBtck5nT1d3UTZ6Wm5BN2ZhTm9LcEVUalRaNzVRNXRoT2lLTGk1TEtkRGM9IiwiYW1yIjpbIndpYSIsIm1mYSJdLCJhcHBpZCI6IjdmNTlhNzczLTJlYWYtNDI5Yy1hMDU5LTUwZmM1YmIyOGI0NCIsImFwcGlkYWNyIjoiMiIsImRldmljZWlkIjoiNjM4ZTdkMTgtNTEwYi00ZjUwLWIzMDgtYzNiYWVhZTFhNDdjIiwiZmFtaWx5X25hbWUiOiJTb25nIiwiZ2l2ZW5fbmFtZSI6IkJvd2VuIiwiaXBhZGRyIjoiMTY3LjIyMC4yNTUuMCIsIm5hbWUiOiJCb3dlbiBTb25nIiwib2lkIjoiM2IzYWFiYjYtZWQxZi00MDJlLWExOTMtMjBiYjI2OWI4ZjM2Iiwib25wcmVtX3NpZCI6IlMtMS01LTIxLTIxNDY3NzMwODUtOTAzMzYzMjg1LTcxOTM0NDcwNy0yNjExNjcxIiwicHVpZCI6IjEwMDMyMDAwQThCNTJBNkQiLCJyaCI6IjAuQVFFQXY0ajVjdkdHcjBHUnF5MTgwQkhiUjNPbldYLXZMcHhDb0ZsUV9GdXlpMFFhQUpnLiIsInNjcCI6InVzZXJfaW1wZXJzb25hdGlvbiIsInN1YiI6IjYwQW5jTzQtMXRfeFMyYmFLQnZvemI3UDdlTGVJU092amFPRkIxVHUyVVEiLCJ0aWQiOiI3MmY5ODhiZi04NmYxLTQxYWYtOTFhYi0yZDdjZDAxMWRiNDciLCJ1bmlxdWVfbmFtZSI6ImJvd3NvbmdAbWljcm9zb2Z0LmNvbSIsInVwbiI6ImJvd3NvbmdAbWljcm9zb2Z0LmNvbSIsInV0aSI6IlpHUFppN3pKSmtHbWtfM2p2czBMQUEiLCJ2ZXIiOiIxLjAifQ.ZkImgY2_bk6BmLGwoS8xDM8mE_8NlzHR4JBdLdpWb2o_5eB6VHdlKd5uQvVItOCKh96qbfryhdSYEoJR5O5pOfM8vPSfGb0lF-o2HN5ZjSZgJ8Ic385Q781l2_q7WuJe5APg9nPm-2TAujVzw_WJRlSdDmiwRxX_t5PoXQo32EzVAbBl4JhCWvMXIxL8Sms8tQlORfZ3zdY3yElOTfKuCV_gApAlVhJMHeSDEzLRkIh1yTG9hbUlzPva5gRXPnqhgJdozZ-oYswKE6UM2Lp2uz7jgFluAOVPR1-CVcFnGvK0_8fdwvPxeabQEhgdHJcyPiKt5BuTY8dT43AASxVUfA";
            TokenCredentials token = new TokenCredentials(accessToken);
            string subscriptionId = "faab228d-df7a-4086-991e-e81c4659d41a";
            string aadGuid = "3b3aabb6-ed1f-402e-a193-20bb269b8f36";
            string rgName = "bwsonggroup";

            //-----------------------------------------------------------
            // Webapp + SQL: Connection
            //-----------------------------------------------------------
            AppService app = new AppService(accessToken, subscriptionId, rgName, "bwsongapp");
            AzureSQL sql = new AzureSQL(accessToken, subscriptionId, aadGuid, rgName, "bwsongsql", "bwsongdb", "ADO.NET");
            //Console.WriteLine("webapp ip: " + app.GetOutboundIp());
            app.SetManagedIdentity();
            sql.SetAADAdmin();
            sql.SetFirewallRule(app.GetOutboundIp(), app.GetOutboundIp());
            //Console.WriteLine("Connection String: " + sql.GetConnectionString());
            app.SetConnectionString(sql.GetConnectionString());

            //----------------------------------------------------------
            // Webapp + SQL: Validation
            //----------------------------------------------------------
            //AppService app = new AppService(accessToken, subscriptionId, rgName, "bwsongapp");
            //AzureSQL sql = new AzureSQL(accessToken, subscriptionId, aadGuid, rgName, "bwsongsql", "bwsongdb", "ADO.NET");
            //foreach (string item in app.GetOutboundIps())
            //    Console.WriteLine(item);
            Console.WriteLine(sql.CheckIpIsInFirewallRule(app.GetOutboundIps()));

            //----------------------------------------------------------
            // Spring + MySQL: Connection
            //----------------------------------------------------------
            SpringCloud sc = new SpringCloud(accessToken, subscriptionId, rgName, "bwsongsc", "bwsongscapp");
            AzureMySQL mySql = new AzureMySQL(accessToken, subscriptionId, rgName, "bwsongmysql", "sys", "bwsong", "Dong@258");
            sc.SetSericeBinding(mySql.GetSericeProperties());
            mySql.SetFirewallRule();

            //----------------------------------------------------------
            // Spring + MySQL: Validation
            //----------------------------------------------------------
            //SpringCloud sc = new SpringCloud(accessToken, subscriptionId, rgName, "bwsongsc", "bwsongscapp");
            //AzureMySQL mySql = new AzureMySQL(accessToken, subscriptionId, rgName, "bwsongmysql", "sys", "bwsong", "Dong@258");
            Console.WriteLine(sc.CheckSericeBinding(mySql.GetSericeProperties()) && mySql.CheckFirewallRule());
        }
    }
}
