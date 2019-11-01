using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net;
using System.IO;
using Newtonsoft.Json;
using XDeploy.Core.IO;

namespace XDeploy.Core
{
    /// <summary>
    /// Represents an XDeploy API implementation.
    /// </summary>
    public class XDeployAPI
    {
        private readonly string _endpoint;
        private readonly string _authHeaderValue;

        public XDeployAPI(string endpoint, string email, string apiKey)
        {
            _authHeaderValue = "Basic " + Cryptography.Base64Encode(string.Join(':', email, apiKey));
            _endpoint = endpoint + "/api";
        }


        public async Task<bool> ValidateCredentialsAsync() => await POSTRequestAsync("/ValidateCredentials");

        public async Task<Tree> GetRemoteTree(string id)
        {
            var res = await GETRequestAsync("/TreeForApp?id=" + id);
            return JsonConvert.DeserializeObject<Tree>(res);
        }

        public async Task<string> GetAppDetailsAsync(string id) => await GETRequestAsync("/App?id=" + id);

        private async Task<string> GETRequestAsync(string path)
        {
            var request = (HttpWebRequest)WebRequest.Create(_endpoint + path);
            request.Method = "GET";
            request.Headers[HttpRequestHeader.Authorization] = _authHeaderValue;
            var response = (HttpWebResponse)await request.GetResponseAsync();
            using (var reader = new StreamReader(response.GetResponseStream()))
            {
                return reader.ReadToEnd();
            }
        }

        private async Task<bool> POSTRequestAsync(string path)
        {
            var request = (HttpWebRequest)WebRequest.Create(_endpoint + path);
            request.Method = "POST";
            request.Headers[HttpRequestHeader.Authorization] = _authHeaderValue;
            var response = (HttpWebResponse)await request.GetResponseAsync();
            if (response.StatusCode == HttpStatusCode.OK)
                return true;

            return false;
        }
    }
}
