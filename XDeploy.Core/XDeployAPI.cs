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

        public async Task<string> GetAppDetailsAsync(string id) => await GETRequestAsync("/App?id=" + id);

        public async Task<string> UploadFileIfNotExistsAsync(string id, string baseDirectory, string fullFilePath)
        {
            var contentLocation = fullFilePath.Replace(baseDirectory, string.Empty).Replace("%5C", "\\").TrimStart('\\');
            var checksum = Cryptography.SHA256CheckSum(fullFilePath);

            //Check if file already exists
            var request = (HttpWebRequest)WebRequest.Create(_endpoint + "/HasFile?id=" + id);
            request.Method = "GET";
            request.Headers[HttpRequestHeader.Authorization] = _authHeaderValue;
            request.Headers[HttpRequestHeader.ContentLocation] = contentLocation;
            request.Headers["X-SHA256"] = checksum;
            var response = (HttpWebResponse)await request.GetResponseAsync();
            bool exists = false;
            using (var reader = new StreamReader(response.GetResponseStream()))
            {
                exists = JsonConvert.DeserializeObject<bool>(reader.ReadToEnd());
            }
            if (!exists)
            {
                var request2 = (HttpWebRequest)WebRequest.Create(_endpoint + "/UploadFile?id=" + id);
                request2.Method = "POST";
                request2.Headers[HttpRequestHeader.Authorization] = _authHeaderValue;
                request2.Headers[HttpRequestHeader.ContentLocation] = contentLocation;
                request2.Headers["X-SHA256"] = checksum;
                //request2.Headers[Httprequest2Header.ContentLength] = bytes.Length.ToString();
                using (var stream = await request2.GetRequestStreamAsync())
                {
                    var bytes = File.ReadAllBytes(fullFilePath);
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Close();
                }

                using (var reader = new StreamReader((await request2.GetResponseAsync()).GetResponseStream()))
                {
                    return reader.ReadToEnd();
                }
            }
            else
            {
                return "Exists";
            }
        }

        private async Task<T> GETAsync<T>(string path) => JsonConvert.DeserializeObject<T>(await GETRequestAsync(path));

        public async Task<Tree> GetRemoteTreeAsync(string id) => JsonConvert.DeserializeObject<Tree>(await GETRequestAsync("/RemoteTree?id=" + id));

        public async Task<byte[]> DownloadFileAsync(string id, string relativePath)
        {
            var request = (HttpWebRequest)WebRequest.Create(_endpoint + "/DownloadFile?id=" + id);
            request.Method = "GET";
            request.Headers[HttpRequestHeader.Authorization] = _authHeaderValue;
            request.Headers[HttpRequestHeader.ContentLocation] = relativePath;
            var response = (HttpWebResponse)await request.GetResponseAsync();
            using (var reader = new BinaryReader(response.GetResponseStream()))
            {
                return reader.ReadBytes(Convert.ToInt32(response.Headers[HttpRequestHeader.ContentLength]));
            }
        }

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
