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
using System.Drawing;

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

        public void EnableManagedIdentity()
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

        public void Config(string[] config)
        {
            if (config == null) return;
            if (config.Length != 3)
            {
                Console.WriteLine("Error, the length of the array is wrong.");
                return;
            }
            if (config[0].Equals("ConnectionString")) SetConnectionString(config[1], config[2]);
            else SetAppSettings(config[0], config[1]);
        }

        public bool Validate(string[] config)
        {
            if (config == null) return true;
            if (config.Length != 3)
            {
                Console.WriteLine("Error, the length of the array is wrong.");
                return false;
            }
            if (!CheckManagedIdentity()) return false;
            if (config[0].Equals("ConnectionString")) return CheckConnectionString(config[1]);
            else return CheckAppSettings(config[1]);
        }

        private bool CheckManagedIdentity()
        {
            if (_client.WebApps.Get(_rgName, _appName).Identity != null && _client.WebApps.Get(_rgName, _appName).Identity.Type == ManagedServiceIdentityType.SystemAssigned) return true;
            else return false;
        }

        private void SetConnectionString(string connectionString, string serviceType)
        {
            ConnectionStringType type;
            switch (serviceType)
            {
                case "SQLServer":
                    type = ConnectionStringType.SQLServer;
                    break;
                case "MySQL":
                    type = ConnectionStringType.MySql;
                    break;
                default:
                    type = ConnectionStringType.Custom;
                    break;
            }
            var _connectionString = new ConnectionStringDictionary
            {
                Properties = new Dictionary<string, ConnStringValueTypePair>
                {
                    {"Cupertino_" + _appName, new ConnStringValueTypePair{Value = connectionString, Type = type} }
                }
            };
            _client.WebApps.UpdateConnectionStrings(_rgName, _appName, _connectionString);
        }

        private void SetAppSettings(string settingName, string setting)
        {
            List<Microsoft.Azure.Management.WebSites.Models.NameValuePair> appSettings = new List<Microsoft.Azure.Management.WebSites.Models.NameValuePair>(
                new Microsoft.Azure.Management.WebSites.Models.NameValuePair[]
                {
                    new Microsoft.Azure.Management.WebSites.Models.NameValuePair() { Name = settingName, Value = setting }
                }
            );
            _client.WebApps.Update(_rgName, _appName, new SitePatchResource { SiteConfig = new SiteConfig { AppSettings = appSettings } });
        }

        private bool CheckAppSettings(string setting)
        {
            StringDictionary s = _client.WebApps.ListApplicationSettings(_rgName, _appName);
            foreach (var item in s.Properties)
            {
                if (item.Value.Equals(setting)) return true;
            }
            return false;
        }

        private bool CheckConnectionString(string connectionString)
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
        }

        public string[] Auth(string principalId)
        {
            SetFirewallRule();
            if (_type.Equals("MSI")) SetAADAdmin();
            string[] res = { "ConnectionString", GetConnectionString(),"SQLServer" };
            return res;
        }

        public string[] GetInfo()
        {
            string[] res = { "ConnectionString", GetConnectionString(), "SQLServer" };
            return res;
        }

        public bool Validate(string principalId)
        {
            return CheckFirewallRule();
        }

        private void SetAADAdmin()
        {
            if (_type.Equals("SECRET"))
            {
                Console.WriteLine("Cannot be called when using secret.");
                return;
            }
            _client.ServerAzureADAdministrators.CreateOrUpdate(_rgName, _serverName, new ServerAzureADAdministrator("Admin", new Guid(_aadGuid)));
        }

        private void SetFirewallRule()
        {
            _client.FirewallRules.CreateOrUpdate(_rgName, _serverName, "Cupertino", new FirewallRule("0.0.0.0", "0.0.0.0"));
        }

        private bool CheckFirewallRule()
        {
            IEnumerable<FirewallRule> firewallRules = _client.FirewallRules.ListByServer(_rgName, _serverName);
            foreach (FirewallRule firewallRule in firewallRules)
            {
                if (firewallRule.StartIpAddress.Equals("0.0.0.0") && firewallRule.EndIpAddress.Equals("0.0.0.0")) return true;
            }
            return false;
        }

        private string GetConnectionString()
        {
            string _connectionString = null;
            switch (_language)
            {
                case "ADO.NET":
                    if (_type.Equals("MSI")) _connectionString = "Server=tcp:" + _serverName + ".database.windows.net,1433; Database=" + _dbName;
                    else if (_type.Equals("SECRET")) _connectionString = "Server=tcp:" + _serverName + ".database.windows.net,1433; Database=" + _dbName + ";User ID=" + _userName + ";Password=" + _password + ";MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
                    break;
            }
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

        public string[] Auth(string principalId)
        {
            AssignRoleToPricipalId(principalId);
            return null;
        }

        public bool Validate(string principalId)
        {
            return GetRoleAssignment(principalId);
        }

        public string[] GetInfo()
        {
            return null;
        }

        private void AssignRoleToPricipalId(string principalId)
        {
            if (GetRoleAssignment(principalId))
            {
                Console.WriteLine("Role Already Assigned!");
                return;
            }
            string roleId = "acdd72a7-3385-48ef-bd42-f606fba81ae7";
            string scope = "subscriptions/" + _subscriptionId + "/resourceGroups/" + _rgName + "/providers/Microsoft.KeyVault/vaults/" + _keyVaultName;
            _client.RoleAssignments.Create(scope, Guid.NewGuid().ToString(), new RoleAssignmentCreateParameters(scope + "/providers/Microsoft.Authorization/roleDefinitions/" + roleId, principalId));
        }

        private bool GetRoleAssignment(string principalId)
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

        public string[] Auth(string principalId)
        {
            if (_type.Equals("MSI"))
            {
                SetManagedIdentity(principalId);
                string[] res = { "Storage_Endpoint", GetEndpoint(), "Custom" };
                return res;
            }
            else if (_type.Equals("SECRET"))
            {
                string[] res = { "ConnectionString", GetConnectionString(), "Custom" };
                return res;
            }
            return null;
        }

        public bool Validate(string principalId)
        {
            return GetRoleAssignment(principalId);
        }

        public string[] GetInfo()
        {
            if (_type.Equals("MSI"))
            {
                string[] res = { "Storage_Endpoint", GetEndpoint(), "Custom" };
                return res;
            }
            else if (_type.Equals("SECRET"))
            {
                string[] res = { "ConnectionString", GetConnectionString(), "Custom" };
                return res;
            }
            return null;
        }

        private string GetEndpoint()
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

        private string GetConnectionString()
        {
            if (_type.Equals("SECRET"))
                return "DefaultEndpointsProtocol=https;AccountName=" + _storageAccount + ";AccountKey=" + _accountKey + ";EndpointSuffix=core.windows.net";
            else
                return "Error";
        }

        private void SetManagedIdentity(string principalId)
        {
            if (GetRoleAssignment(principalId)) return;
            string roleId = "c12c1c16-33a1-487b-954d-41c89c60f349";
            string scope = "subscriptions/" + _subscriptionId + "/resourceGroups/" + _rgName + "/providers/Microsoft.Storage/storageAccounts/" + _storageAccount;
            _client.RoleAssignments.Create(scope, Guid.NewGuid().ToString(), new RoleAssignmentCreateParameters(scope + "/providers/Microsoft.Authorization/roleDefinitions/" + roleId, principalId));
        }

        private bool GetRoleAssignment(string principalId)
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
            string accessToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6IkN0VHVoTUptRDVNN0RMZHpEMnYyeDNRS1NSWSIsImtpZCI6IkN0VHVoTUptRDVNN0RMZHpEMnYyeDNRS1NSWSJ9.eyJhdWQiOiJodHRwczovL21hbmFnZW1lbnQuY29yZS53aW5kb3dzLm5ldC8iLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC83MmY5ODhiZi04NmYxLTQxYWYtOTFhYi0yZDdjZDAxMWRiNDcvIiwiaWF0IjoxNTkxMDAwNzk0LCJuYmYiOjE1OTEwMDA3OTQsImV4cCI6MTU5MTAwNDY5NCwiX2NsYWltX25hbWVzIjp7Imdyb3VwcyI6InNyYzEifSwiX2NsYWltX3NvdXJjZXMiOnsic3JjMSI6eyJlbmRwb2ludCI6Imh0dHBzOi8vZ3JhcGgud2luZG93cy5uZXQvNzJmOTg4YmYtODZmMS00MWFmLTkxYWItMmQ3Y2QwMTFkYjQ3L3VzZXJzLzNiM2FhYmI2LWVkMWYtNDAyZS1hMTkzLTIwYmIyNjliOGYzNi9nZXRNZW1iZXJPYmplY3RzIn19LCJhY3IiOiIxIiwiYWlvIjoiQVZRQXEvOFBBQUFBYnhXNFI1dkVTWFlaSEcwZmxjVVMzSko3VVVtc2tWdG56aE4xU0FuMWRZSlZLZm9MY3FQdUNWVXdaN3VhV3VTSUY4dnBLSGE4SWc3dTRCK1BUQksxVmJYN2VMUDdIWGVpaEY4QVdNbmE1b1U9IiwiYW1yIjpbIndpYSIsIm1mYSJdLCJhcHBpZCI6IjdmNTlhNzczLTJlYWYtNDI5Yy1hMDU5LTUwZmM1YmIyOGI0NCIsImFwcGlkYWNyIjoiMiIsImRldmljZWlkIjoiNjM4ZTdkMTgtNTEwYi00ZjUwLWIzMDgtYzNiYWVhZTFhNDdjIiwiZmFtaWx5X25hbWUiOiJTb25nIiwiZ2l2ZW5fbmFtZSI6IkJvd2VuIiwiaXBhZGRyIjoiMTY3LjIyMC4yNTUuMCIsIm5hbWUiOiJCb3dlbiBTb25nIiwib2lkIjoiM2IzYWFiYjYtZWQxZi00MDJlLWExOTMtMjBiYjI2OWI4ZjM2Iiwib25wcmVtX3NpZCI6IlMtMS01LTIxLTIxNDY3NzMwODUtOTAzMzYzMjg1LTcxOTM0NDcwNy0yNjExNjcxIiwicHVpZCI6IjEwMDMyMDAwQThCNTJBNkQiLCJyaCI6IjAuQVFFQXY0ajVjdkdHcjBHUnF5MTgwQkhiUjNPbldYLXZMcHhDb0ZsUV9GdXlpMFFhQUpnLiIsInNjcCI6InVzZXJfaW1wZXJzb25hdGlvbiIsInN1YiI6IjYwQW5jTzQtMXRfeFMyYmFLQnZvemI3UDdlTGVJU092amFPRkIxVHUyVVEiLCJ0aWQiOiI3MmY5ODhiZi04NmYxLTQxYWYtOTFhYi0yZDdjZDAxMWRiNDciLCJ1bmlxdWVfbmFtZSI6ImJvd3NvbmdAbWljcm9zb2Z0LmNvbSIsInVwbiI6ImJvd3NvbmdAbWljcm9zb2Z0LmNvbSIsInV0aSI6IldkUXlTeFJXa1VDRldaT0xDeU13QUEiLCJ2ZXIiOiIxLjAifQ.XpeZF56HQJQb6ARy9LQr0pRAZRskgT7n13l_OT_EgEyPnhYEd1Wnqda9YjvCshBHNBUi8xh6BgIe-3V4JbUPU6TI_ZdObJyRs1X3V3sqWE2G-8Yn6Zs_YPoO2bTId__xIw-lAgwHeQvPsiQi1FkzgfbB7daFHkmmF9kHB_NE297GU5BDFx14iW5_cr8cotwxszwpfpgbo0ZklwO9fILrvnzYOrH7v-X2eMGHgpqhRQmAx_7i1n0uGt3uc3ydEmmi1iH0ExYgYWt_8ruL8SDIUu3_FtkNnyP5914s5jLBoiJJGRkayktR-WXwtv5yqHNxw8tCPljDTdO0RGhNmO6s5w";
            TokenCredentials token = new TokenCredentials(accessToken);
            string subscriptionId = "faab228d-df7a-4086-991e-e81c4659d41a";
            string aadGuid = "3b3aabb6-ed1f-402e-a193-20bb269b8f36";
            string rgName = "bwsonggroup";

            //-----------------------------------------------------------
            // Webapp + SQL: AAD
            //-----------------------------------------------------------
            //AppService app = new AppService(accessToken, subscriptionId, rgName, "bwsongapp");
            //AzureSQL sql = new AzureSQL(accessToken, subscriptionId, aadGuid, rgName, "bwsongsql", "bwsongdb", "ADO.NET");
            //// Connect
            //app.EnableManagedIdentity();
            //app.Config(sql.Auth(app.GetManagedIdentityPrincipalId()));
            //// Validate
            //Console.WriteLine(sql.Validate(app.GetManagedIdentityPrincipalId()) && app.Validate(sql.GetInfo()));

            //----------------------------------------------------------
            // Webapp + SQL: Secret
            //----------------------------------------------------------
            //AppService app = new AppService(accessToken, subscriptionId, rgName, "bwsongapp");
            //AzureSQL sql = new AzureSQL(accessToken, subscriptionId, aadGuid, rgName, "bwsongsql", "bwsongdb", "bwsong", "Dong@258", "ADO.NET");
            //// Connect
            //app.Config(sql.Auth(app.GetManagedIdentityPrincipalId()));
            //// Validation
            //Console.WriteLine(sql.Validate(app.GetManagedIdentityPrincipalId()) && app.Validate(sql.GetInfo()));

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
            // Webapp + KeyVault: AAD
            //----------------------------------------------------------
            //AppService app = new AppService(accessToken, subscriptionId, rgName, "bwsongapp");
            //KeyVault key = new KeyVault(accessToken, subscriptionId, rgName, "bwsongkeyvault");
            //// Connect
            //app.EnableManagedIdentity();
            //app.Config(key.Auth(app.GetManagedIdentityPrincipalId()));
            //// Validate
            //Console.WriteLine(key.Validate(app.GetManagedIdentityPrincipalId()) && app.Validate(key.GetInfo()));

            //----------------------------------------------------------
            // Webapp + Storage: AAD
            //----------------------------------------------------------
            AppService app = new AppService(accessToken, subscriptionId, rgName, "bwsongapp");
            Storage storage = new Storage(accessToken, subscriptionId, rgName, "bwsongstorage", "bwsongstoragecontainer", 0);
            // Connect
            app.EnableManagedIdentity();
            app.Config(storage.Auth(app.GetManagedIdentityPrincipalId()));
            // Validate
            Console.WriteLine(storage.Validate(app.GetManagedIdentityPrincipalId()) && app.Validate(storage.GetInfo()));
        }
    }
}
