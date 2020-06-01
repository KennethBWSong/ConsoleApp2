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
using Microsoft.Azure.Management.Authorization;
using Microsoft.Azure.Management.Authorization.Models;
using Microsoft.Azure.Management.Storage;
using Microsoft.Azure.Management.Network.Fluent.Models;
using System.Runtime.InteropServices;

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
            if (!CheckManagedIdentity())
                _client.WebApps.Update(_rgName, _appName, new SitePatchResource { Identity = new ManagedServiceIdentity(ManagedServiceIdentityType.SystemAssigned)});
        }

        public string GetManagedIdentityPrincipalId()
        {
            if (CheckManagedIdentity())
                return _client.WebApps.Get(_rgName, _appName).Identity.PrincipalId;
            else return "Error. Managed Identity not enabled.";
        }

        public bool CheckManagedIdentity()
        {
            if (_client.WebApps.Get(_rgName, _appName).Identity.Type == ManagedServiceIdentityType.SystemAssigned) return true;
            else return false;
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

        public void SetAppSettings(string settingName, string setting)
        {
            List<Microsoft.Azure.Management.WebSites.Models.NameValuePair> appSettings = new List<Microsoft.Azure.Management.WebSites.Models.NameValuePair>(
                new Microsoft.Azure.Management.WebSites.Models.NameValuePair[]
                {
                    new Microsoft.Azure.Management.WebSites.Models.NameValuePair() { Name = settingName, Value = setting }
                }
            );
            _client.WebApps.Update(_rgName, _appName, new SitePatchResource { SiteConfig = new SiteConfig { AppSettings = appSettings } });
        }

        public bool CheckAppSettings(string setting)
        {
            StringDictionary s = _client.WebApps.ListApplicationSettings(_rgName, _appName);
            foreach (var item in s.Properties)
            {
                if (item.Value.Equals(setting)) return true;
            }
            return false;
        }

        // Question: whether to add check on connection string type
        public bool CheckConnectionString(string connectionString)
        {
            ConnectionStringDictionary connectionStrings = _client.WebApps.ListConnectionStrings(_rgName, _appName);
            foreach (var item in connectionStrings.Properties)
            {
                if (item.Value.Value.Equals(connectionString)) return true;
            }
            return false;
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
            if (_type.Equals("SECRET"))
            {
                Console.WriteLine("Cannot be called when using secret.");
                return;
            }
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
        public AzureMySQL(string accessToken, string subscriptionId, string rgName, string serverName, string dbName, string userName, string password, string language)
        {
            _accessToken = accessToken;
            _subscriptionId = subscriptionId;
            _rgName = rgName;
            _serverName = serverName;
            _dbName = dbName;
            _userName = userName;
            _password = password;
            _language = language;
            _tokenCredentials = new TokenCredentials(accessToken);
            _client = new RestClient("https://management.azure.com");
            _type = "SECRET";
            SetConnectionString();
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

        public string GetConnectionString()
        {
            return _connectionString;
        }

        private void SetConnectionString()
        {
            switch (_language)
            {
                case "ADO.NET":
                    if (_type.Equals("SECRET")) _connectionString = "Server=" + _serverName + ".mysql.database.azure.com;Port=3306;Database=" + _dbName + ";Uid=" + _userName + ";Pwd=" + _password + ";SslMode=Preferred;";
                    else if (_type.Equals("MSI")) _connectionString = "Server=" + _serverName + ".mysql.database.azure.com;Port=3306;Database=" + _dbName + ";SslMode=Preferred;";
                    break;
            }
        }

        private string _accessToken;
        private TokenCredentials _tokenCredentials;
        private string _subscriptionId;
        private string _rgName;
        private string _serverName;
        private string _dbName;
        string _userName;
        string _password;
        string _language;
        string _type;
        RestClient _client;
        string _connectionString;
    }

    public class KeyVault
    {
        public KeyVault(string accessToken, string subscriptionId, string rgName, string keyVaultName)
        {
            _accessToken = accessToken;
            _subscriptionId = subscriptionId;
            _rgName = rgName;
            _keyVaultName = keyVaultName;
            _tokenCredentials = new TokenCredentials(accessToken);
            _client = new AuthorizationManagementClient(_tokenCredentials);
            _client.SubscriptionId = _subscriptionId;
        }

        public void AssignRoleToPricipalId(string principalId)
        {
            string roleId = "acdd72a7-3385-48ef-bd42-f606fba81ae7";
            string scope = "subscriptions/" + _subscriptionId + "/resourceGroups/" + _rgName + "/providers/Microsoft.KeyVault/vaults/" + _keyVaultName;
            _client.RoleAssignments.Create(scope, Guid.NewGuid().ToString(), new RoleAssignmentCreateParameters(scope + "/providers/Microsoft.Authorization/roleDefinitions/" + roleId, principalId));
        }

        public bool GetRoleAssignment(string principalId)
        {
            string scope = "subscriptions/" + _subscriptionId + "/resourceGroups/" + _rgName + "/providers/Microsoft.KeyVault/vaults/" + _keyVaultName;
            foreach (var item in _client.RoleAssignments.ListForScope(scope))
            {
                if (item.PrincipalId.Equals(principalId)) return true;
            }
            return false;
        }

        private string _accessToken;
        private TokenCredentials _tokenCredentials;
        private string _subscriptionId;
        private string _rgName;
        private string _keyVaultName;
        private AuthorizationManagementClient _client;
    }

    public class Storage
    {
        //TODO: Check the role definitions
        public Storage(string accessToken, string subscriptionId, string rgName, string storageAccount, string accountKey)
        {
            _accessToken = accessToken;
            _subscriptionId = subscriptionId;
            _rgName = rgName;
            _storageAccount = storageAccount;
            _accountKey = accountKey;
            _type = "SECRET";
            _tokenCredentials = new TokenCredentials(accessToken);
        }

        public Storage(string accessToken, string subscriptionId, string rgName, string storageAccount, string containerName, int storageType)
        {
            _accessToken = accessToken;
            _subscriptionId = subscriptionId;
            _rgName = rgName;
            _storageAccount = storageAccount;
            _storageType = (storageTypes)storageType;
            _containerName = containerName;
            _type = "MSI";
            _tokenCredentials = new TokenCredentials(accessToken);
            _client = new AuthorizationManagementClient(_tokenCredentials);
            _client.SubscriptionId = _subscriptionId;
        }

        public string GetEndpoint()
        {
            // According to https://docs.microsoft.com/en-us/azure/storage/common/storage-auth-aad-msi#net-code-example-create-a-block-blob
            string endpoint = "https://" + _storageAccount + ".";
            switch (_storageType)
            {
                case storageTypes.Blob:
                    endpoint += "blob";
                    break;
                case storageTypes.Queue:
                    endpoint += "queue";
                    break;
                case storageTypes.Table:
                    endpoint += "table";
                    break;
            }
            endpoint += ".core.windows.net/" + _containerName;
            return endpoint;
        }

        public string GetConnectionString()
        {
            if (_type.Equals("SECRET"))
                return "DefaultEndpointsProtocol=https;AccountName=" + _storageAccount + ";AccountKey=" + _accountKey + ";EndpointSuffix=core.windows.net";
            else
                return "Error";
        }

        public void SetManagedIdentity(string principalId)
        {
            string roleId = "c12c1c16-33a1-487b-954d-41c89c60f349";
            string scope = "subscriptions/" + _subscriptionId + "/resourceGroups/" + _rgName + "/providers/Microsoft.Storage/storageAccounts/" + _storageAccount;
            _client.RoleAssignments.Create(scope, Guid.NewGuid().ToString(), new RoleAssignmentCreateParameters(scope + "/providers/Microsoft.Authorization/roleDefinitions/" + roleId, principalId));
        }

        public bool GetRoleAssignment(string principalId)
        {
            string scope = "subscriptions/" + _subscriptionId + "/resourceGroups/" + _rgName + "/providers/Microsoft.Storage/storageAccounts/" + _storageAccount;
            foreach (var item in _client.RoleAssignments.ListForScope(scope))
            {
                if (item.PrincipalId.Equals(principalId)) return true;
            }
            return false;
        }

        private string _accessToken;
        private TokenCredentials _tokenCredentials;
        private string _subscriptionId;
        private string _rgName;
        private string _storageAccount;
        private string _accountKey;
        private string _containerName;
        enum storageTypes {Blob = 0, Table = 1, Queue = 2};
        private storageTypes _storageType;
        string _type;
        private AuthorizationManagementClient _client;
    }

    class Program
    {
        static void Main(string[] args)
        {
            string accessToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6IkN0VHVoTUptRDVNN0RMZHpEMnYyeDNRS1NSWSIsImtpZCI6IkN0VHVoTUptRDVNN0RMZHpEMnYyeDNRS1NSWSJ9.eyJhdWQiOiJodHRwczovL21hbmFnZW1lbnQuY29yZS53aW5kb3dzLm5ldC8iLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC83MmY5ODhiZi04NmYxLTQxYWYtOTFhYi0yZDdjZDAxMWRiNDcvIiwiaWF0IjoxNTkwOTg1NDg4LCJuYmYiOjE1OTA5ODU0ODgsImV4cCI6MTU5MDk4OTM4OCwiX2NsYWltX25hbWVzIjp7Imdyb3VwcyI6InNyYzEifSwiX2NsYWltX3NvdXJjZXMiOnsic3JjMSI6eyJlbmRwb2ludCI6Imh0dHBzOi8vZ3JhcGgud2luZG93cy5uZXQvNzJmOTg4YmYtODZmMS00MWFmLTkxYWItMmQ3Y2QwMTFkYjQ3L3VzZXJzLzNiM2FhYmI2LWVkMWYtNDAyZS1hMTkzLTIwYmIyNjliOGYzNi9nZXRNZW1iZXJPYmplY3RzIn19LCJhY3IiOiIxIiwiYWlvIjoiQVZRQXEvOFBBQUFBR1VxR2huQVhJSk9iMUZqZkZhWVJmdCtLM00wdkQzTE83Y3R5REc0eTNzVkZyRGNMaVJOS1BQZ20vNG1WeDBRTTFpK0dRUll0R0NCdy91N2dzQWFhbGJhcmVLZGFhSzFWY28zVlBEWnhZbXc9IiwiYW1yIjpbIndpYSIsIm1mYSJdLCJhcHBpZCI6IjdmNTlhNzczLTJlYWYtNDI5Yy1hMDU5LTUwZmM1YmIyOGI0NCIsImFwcGlkYWNyIjoiMiIsImRldmljZWlkIjoiNjM4ZTdkMTgtNTEwYi00ZjUwLWIzMDgtYzNiYWVhZTFhNDdjIiwiZmFtaWx5X25hbWUiOiJTb25nIiwiZ2l2ZW5fbmFtZSI6IkJvd2VuIiwiaXBhZGRyIjoiMTY3LjIyMC4yNTUuMCIsIm5hbWUiOiJCb3dlbiBTb25nIiwib2lkIjoiM2IzYWFiYjYtZWQxZi00MDJlLWExOTMtMjBiYjI2OWI4ZjM2Iiwib25wcmVtX3NpZCI6IlMtMS01LTIxLTIxNDY3NzMwODUtOTAzMzYzMjg1LTcxOTM0NDcwNy0yNjExNjcxIiwicHVpZCI6IjEwMDMyMDAwQThCNTJBNkQiLCJyaCI6IjAuQVFFQXY0ajVjdkdHcjBHUnF5MTgwQkhiUjNPbldYLXZMcHhDb0ZsUV9GdXlpMFFhQUpnLiIsInNjcCI6InVzZXJfaW1wZXJzb25hdGlvbiIsInN1YiI6IjYwQW5jTzQtMXRfeFMyYmFLQnZvemI3UDdlTGVJU092amFPRkIxVHUyVVEiLCJ0aWQiOiI3MmY5ODhiZi04NmYxLTQxYWYtOTFhYi0yZDdjZDAxMWRiNDciLCJ1bmlxdWVfbmFtZSI6ImJvd3NvbmdAbWljcm9zb2Z0LmNvbSIsInVwbiI6ImJvd3NvbmdAbWljcm9zb2Z0LmNvbSIsInV0aSI6IkNvZDRNZXk3OVV1bHRBVDZvalV0QUEiLCJ2ZXIiOiIxLjAifQ.UySLRwEwaG-aePZRcyy7ZODesV7-9mela9Vja3__MeMoAn8k-RbT9ONo6ouMDNXjLU1o2ROe2sUHOFLr0Bmp--QQBkFRAprQ12W81g2kOg6LU9a86T519EMv8Y5lW90SxQ8I1bcJYdVMx1_TFDrh8uWLkm6yUxXqoMzku1Bisy-N6hh6VHtI4Y36BYedzjrDfCldRkR1hRLannKhG4UxRIdL9D00HhzHHsyB6NEBor0QHSkW84TbepuBA91aklw6u3_1_l_HIuO8R98L6_hDMUzUxtdVOT_BQXEN7vjBSD6h80InzmKfvj_fy19dpiknNWd9tLhVidARFnC5KxzXlA";
            TokenCredentials token = new TokenCredentials(accessToken);
            string subscriptionId = "faab228d-df7a-4086-991e-e81c4659d41a";
            string aadGuid = "3b3aabb6-ed1f-402e-a193-20bb269b8f36";
            string rgName = "bwsonggroup";

            //-----------------------------------------------------------
            // Webapp + SQL: Connection via AAD
            //-----------------------------------------------------------
            //AppService app = new AppService(accessToken, subscriptionId, rgName, "bwsongapp");
            //AzureSQL sql = new AzureSQL(accessToken, subscriptionId, aadGuid, rgName, "bwsongsql", "bwsongdb", "ADO.NET");
            ////Console.WriteLine("webapp ip: " + app.GetOutboundIp());
            //app.SetManagedIdentity();
            //sql.SetAADAdmin();
            //sql.SetFirewallRule(app.GetOutboundIp(), app.GetOutboundIp());
            ////Console.WriteLine("Connection String: " + sql.GetConnectionString());
            //app.SetConnectionString(sql.GetConnectionString());

            //----------------------------------------------------------
            // Webapp + SQL: Validation via AAD
            //----------------------------------------------------------
            //AppService app = new AppService(accessToken, subscriptionId, rgName, "bwsongapp");
            //AzureSQL sql = new AzureSQL(accessToken, subscriptionId, aadGuid, rgName, "bwsongsql", "bwsongdb", "ADO.NET");
            //foreach (string item in app.GetOutboundIps())
            //    Console.WriteLine(item);
            //Console.WriteLine(sql.CheckIpIsInFirewallRule(app.GetOutboundIps()) && app.CheckConnectionString(sql.GetConnectionString()));

            //----------------------------------------------------------
            // Spring + MySQL: Connection via Service Binding
            //----------------------------------------------------------
            //SpringCloud sc = new SpringCloud(accessToken, subscriptionId, rgName, "bwsongsc", "bwsongscapp");
            //AzureMySQL mySql = new AzureMySQL(accessToken, subscriptionId, rgName, "bwsongmysql", "sys", "bwsong", "Dong@258", "ADO.NET");
            //sc.SetSericeBinding(mySql.GetSericeProperties());
            //mySql.SetFirewallRule();

            //----------------------------------------------------------
            // Spring + MySQL: Validation via Service Binding
            //----------------------------------------------------------
            //SpringCloud sc = new SpringCloud(accessToken, subscriptionId, rgName, "bwsongsc", "bwsongscapp");
            //AzureMySQL mySql = new AzureMySQL(accessToken, subscriptionId, rgName, "bwsongmysql", "sys", "bwsong", "Dong@258");
            //Console.WriteLine(sc.CheckSericeBinding(mySql.GetSericeProperties()) && mySql.CheckFirewallRule());

            //----------------------------------------------------------
            // Webapp + MySQL: Connection via Secret
            //----------------------------------------------------------
            //AppService app = new AppService(accessToken, subscriptionId, rgName, "bwsongapp");
            //AzureMySQL mySql = new AzureMySQL(accessToken, subscriptionId, rgName, "bwsongmysql", "sys", "bwsong", "Dong@258", "ADO.NET");
            //app.SetConnectionString(mySql.GetConnectionString());
            //mySql.SetFirewallRule();

            //----------------------------------------------------------
            // Webapp + MySQL: Validation via Secret
            //----------------------------------------------------------
            //AppService app = new AppService(accessToken, subscriptionId, rgName, "bwsongapp");
            //AzureMySQL mySql = new AzureMySQL(accessToken, subscriptionId, rgName, "bwsongmysql", "sys", "bwsong", "Dong@258", "ADO.NET");
            //Console.WriteLine(mySql.CheckFirewallRule() && app.CheckConnectionString(mySql.GetConnectionString()));

            //----------------------------------------------------------
            // Webapp + KeyVault: Connection via MSI
            //----------------------------------------------------------
            //AppService app = new AppService(accessToken, subscriptionId, rgName, "bwsongapp");
            //KeyVault key = new KeyVault(accessToken, subscriptionId, rgName, "bwsongkeyvault");
            //app.SetManagedIdentity();
            //key.AssignRoleToPricipalId(app.GetManagedIdentityPrincipalId());

            //----------------------------------------------------------
            // Webapp + KeyVault: Validation via MSI
            //----------------------------------------------------------
            //AppService app = new AppService(accessToken, subscriptionId, rgName, "bwsongapp");
            //KeyVault key = new KeyVault(accessToken, subscriptionId, rgName, "bwsongkeyvault");
            //Console.WriteLine(app.CheckManagedIdentity() && key.GetRoleAssignment(app.GetManagedIdentityPrincipalId()));

            //----------------------------------------------------------
            // Webapp + Storage: Connection via MSI
            //----------------------------------------------------------
            //AppService app = new AppService(accessToken, subscriptionId, rgName, "bwsongapp");
            //Storage storage = new Storage(accessToken, subscriptionId, rgName, "bwsongstorage", "bwsongstoragecontainer", 0);
            //app.SetAppSettings("Cupertino", storage.GetEndpoint());
            //storage.SetManagedIdentity(app.GetManagedIdentityPrincipalId());

            //----------------------------------------------------------
            // Webapp + Storage: Validation via MSI
            //----------------------------------------------------------
            AppService app = new AppService(accessToken, subscriptionId, rgName, "bwsongapp");
            Storage storage = new Storage(accessToken, subscriptionId, rgName, "bwsongstorage", "bwsongstoragecontainer", 0);
            Console.WriteLine(app.CheckManagedIdentity() && storage.GetRoleAssignment(app.GetManagedIdentityPrincipalId()) && app.CheckAppSettings(storage.GetEndpoint()));
        }
    }
}
