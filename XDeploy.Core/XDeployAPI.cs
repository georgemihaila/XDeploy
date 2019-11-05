using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net;
using System.IO;
using Newtonsoft.Json;
using XDeploy.Core.IO;
using System.Linq;

namespace XDeploy.Core
{
    /// <summary>
    /// Represents an XDeploy API implementation.
    /// </summary>
    public class XDeployAPI
    {
        private readonly string _endpoint;
        private readonly string _authHeaderValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="XDeployAPI"/> class.
        /// </summary>
        public XDeployAPI(StartupConfig config)
        {
            _authHeaderValue = "Basic " + Cryptography.Base64Encode(string.Join(':', config.Email, config.APIKey));
            _endpoint = config.Endpoint + "/api";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="XDeployAPI"/> class.
        /// </summary>
        /// <param name="endpoint">The endpoint address.</param>
        /// <param name="email">The email.</param>
        /// <param name="apiKey">The API key.</param>
        public XDeployAPI(string endpoint, string email, string apiKey)
        {
            _authHeaderValue = "Basic " + Cryptography.Base64Encode(string.Join(':', email, apiKey));
            _endpoint = endpoint + "/api";
        }

        /// <summary>
        /// Returns a result indicating whether a user's credentials are valid.
        /// </summary>
        public async Task<bool> ValidateCredentialsAsync() => await POSTRequestAsync<bool>("/ValidateCredentials");

        /// <summary>
        /// Gets an application's details.
        /// </summary>
        public async Task<string> GetAppDetailsAsync(ApplicationInfo app) => await GetAppDetailsAsync(app.ID);

        /// <summary>
        /// Gets an application's details.
        /// </summary>
        public async Task<string> GetAppDetailsAsync(string appID) => await GETRequestAsync("/App?id=" + appID);

        /// <summary>
        /// Attempts to create a deployment job based on an expected file list and returns the job ID, in case the request is successful.
        /// </summary>
        public async Task<int> CreateDeploymentJobAsync(ApplicationInfo app, IEnumerable<ExpectedFileInfo> expected) => await CreateDeploymentJobAsync(app.ID, expected);

        /// <summary>
        /// Attempts to create a deployment job based on an expected file list and returns the job ID, in case the request is successful.
        /// </summary>
        public async Task<int> CreateDeploymentJobAsync(string appID, IEnumerable<ExpectedFileInfo> expected) => await POSTRequestAsync<int>("/CreateDeploymentJob?id=" + appID, expected);

        /// <summary>
        /// Attempts to delete a deployment job based on its ID.
        /// </summary>
        public async Task DeleteDeploymentJobAsync(ApplicationInfo app, int jobid) => await DeleteDeploymentJobAsync(app.ID, jobid);

        /// <summary>
        /// Attempts to delete a deployment job based on its ID.
        /// </summary>
        public async Task DeleteDeploymentJobAsync(string appID, int jobid) => await POSTSimpleAsync("/DeleteDeploymentJob?id=" + appID + "&jobid=" + jobid);

        /// <summary>
        /// Checks if the server already has a specific file and in case it does not, it uploads it.
        /// </summary>
        public async Task<string> UploadFileIfNotExistsAsync(ApplicationInfo app, int jobid, string relativeLocation, string checksum, byte[] fileBytes)
        {
            var contentLocation = relativeLocation.Replace("%5C", "\\").TrimStart('\\');
            if (!await HasFileAsync(app.ID, contentLocation, checksum))
            {
                var request2 = (HttpWebRequest)WebRequest.Create(_endpoint + "/UploadFile?id=" + app.ID + "&jobid=" + jobid);
                request2.Method = "POST";
                request2.Headers[HttpRequestHeader.Authorization] = _authHeaderValue;
                request2.Headers[HttpRequestHeader.ContentLocation] = contentLocation;
                request2.Headers["X-SHA256"] = checksum;
                //request2.Headers[Httprequest2Header.ContentLength] = bytes.Length.ToString();
                using (var stream = await request2.GetRequestStreamAsync())
                {
                    stream.Write(fileBytes, 0, fileBytes.Length);
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

        /// <summary>
        /// Checks if the server already has a specific file and in case it does not, it uploads it.
        /// </summary>
        public async Task<string> UploadFileIfNotExistsAsync(ApplicationInfo app, int jobid, string fullFilePath) => await UploadFileIfNotExistsAsync(app.ID, jobid, app.Location, fullFilePath);

        /// <summary>
        /// Checks if the server already has a specific file and in case it does not, it uploads it.
        /// </summary>
        public async Task<string> UploadFileIfNotExistsAsync(string appID, int jobid, string baseDirectory, string fullFilePath)
        {
            var contentLocation = fullFilePath.Replace(baseDirectory, string.Empty).Replace("%5C", "\\").TrimStart('\\');
            var checksum = Cryptography.SHA256CheckSum(fullFilePath);
            if (!await HasFileAsync(appID, contentLocation, checksum))
            {
                var request2 = (HttpWebRequest)WebRequest.Create(_endpoint + "/UploadFile?id=" + appID + "&jobid=" + jobid);
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

        private async Task<bool> HasFileAsync(string appID, string relativeLocation, string checksum)
        {
            var request = (HttpWebRequest)WebRequest.Create(_endpoint + "/HasFile?id=" + appID);
            request.Method = "GET";
            request.Headers[HttpRequestHeader.Authorization] = _authHeaderValue;
            request.Headers[HttpRequestHeader.ContentLocation] = relativeLocation;
            request.Headers["X-SHA256"] = checksum;
            var response = (HttpWebResponse)await request.GetResponseAsync();
            using (var reader = new StreamReader(response.GetResponseStream()))
            {
                return JsonConvert.DeserializeObject<bool>(reader.ReadToEnd());
            }
        }

        private async Task<T> GETAsync<T>(string path) => JsonConvert.DeserializeObject<T>(await GETRequestAsync(path));

        /// <summary>
        /// Gets the remote tree of an application.
        /// </summary>
        public async Task<Tree> GetRemoteTreeAsync(ApplicationInfo app) => await GetRemoteTreeAsync(app.ID);

        /// <summary>
        /// Gets the remote tree of an application.
        /// </summary>
        public async Task<Tree> GetRemoteTreeAsync(string appID) => JsonConvert.DeserializeObject<Tree>(await GETRequestAsync("/RemoteTree?id=" + appID));

        /// <summary>
        /// Downloads an application file's bytes.
        /// </summary>
        public async Task<byte[]> DownloadFileBytesAsync(ApplicationInfo app, string relativePath) => await DownloadFileBytesAsync(app.ID, relativePath);

        /// <summary>
        /// Downloads an application file's bytes.
        /// </summary>
        public async Task<byte[]> DownloadFileBytesAsync(string appID, string relativePath)
        {
            var request = (HttpWebRequest)WebRequest.Create(_endpoint + "/DownloadFile?id=" + appID);
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

        private async Task POSTSimpleAsync(string path)
        {
            var request = (HttpWebRequest)WebRequest.Create(_endpoint + path);
            request.Method = "POST";
            request.Headers[HttpRequestHeader.Authorization] = _authHeaderValue;
            var response = (HttpWebResponse)await request.GetResponseAsync();
        }

        private async Task<T> POSTRequestAsync<T>(string path, object content = null)
        {
            var request = (HttpWebRequest)WebRequest.Create(_endpoint + path);
            request.Method = "POST";
            request.Headers[HttpRequestHeader.Authorization] = _authHeaderValue;
            request.Headers[HttpRequestHeader.ContentType] = "application/json";
            if (content != null)
            {
                using (var stream = await request.GetRequestStreamAsync())
                {
                    var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(content));
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Close();
                }
            }
            var response = (HttpWebResponse)await request.GetResponseAsync();
            using (var reader = new StreamReader(response.GetResponseStream()))
            {
                return JsonConvert.DeserializeObject<T>(reader.ReadToEnd());
            }
        }
    }
}
