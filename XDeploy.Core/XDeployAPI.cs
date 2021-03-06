﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net;
using System.IO;
using Newtonsoft.Json;
using XDeploy.Core.IO;
using System.Linq;
using FileInfo = XDeploy.Core.IO.FileInfo;

namespace XDeploy.Core
{
    /// <summary>
    /// Represents an XDeploy API implementation.
    /// </summary>
    public class XDeployAPI
    {
        private readonly string _endpoint;
        private readonly string _authHeaderValue;
        private readonly IWebProxy _proxy;

        /// <summary>
        /// Initializes a new instance of the <see cref="XDeployAPI"/> class.
        /// </summary>
        public XDeployAPI(StartupConfig config)
        {
            _authHeaderValue = "Basic " + Cryptography.Base64Encode(string.Join(':', config.Email, config.APIKey));
            _endpoint = config.Endpoint + "/api";
            _proxy = config.Proxy;
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
        /// Locks an application by terminating all active client update jobs.
        /// </summary>
        public async Task LockApplicationAsync(ApplicationInfo app) => await LockApplicationAsync(app.ID);

        /// <summary>
        /// Locks an application by terminating all active client update jobs.
        /// </summary>
        public async Task LockApplicationAsync(string appID) => await POSTSimpleAsync("/LockApplication?id=" + appID);

        /// <summary>
        /// Unlocks an application by notifying all active clients that there is an update available.
        /// </summary>
        public async Task UnlockApplicationAsync(ApplicationInfo app) => await UnlockApplicationAsync(app.ID);

        /// <summary>
        /// Unlocks an application by notifying all active clients that there is an update available.
        /// </summary>
        public async Task UnlockApplicationAsync(string appID) => await POSTSimpleAsync("/UnlockApplication?id=" + appID);

        /// <summary>
        /// Uses a list of <see cref="IODifference"/>s to clear files and folders that may have been removed locally.
        /// </summary>
        public async Task DoCleanupAsync(ApplicationInfo app, IEnumerable<IODifference> differences) => await DoCleanupAsync(app.ID, differences);

        /// <summary>
        /// Uses a list of <see cref="IODifference"/>s to clear files and folders that may have been removed locally.
        /// </summary>
        public async Task DoCleanupAsync(string appID, IEnumerable<IODifference> differences) => await POSTRequestAsync<string>("/Cleanup?id=" + appID, differences);

        /// <summary>
        /// Checks if the server already has a specific file and in case it does not, it uploads it.
        /// </summary>
        public async Task<string> UploadFileIfNotExistsAsync(ApplicationInfo app, string relativeLocation, string checksum, byte[] fileBytes)
        {
            var contentLocation = relativeLocation.Replace("%5C", "\\").TrimStart('\\');
            if (!await HasFileAsync(app.ID, contentLocation, checksum))
            {
                var request = (HttpWebRequest)WebRequest.Create(_endpoint + "/UploadFile?id=" + app.ID);
                request.Method = "POST";
                request.Headers[HttpRequestHeader.Authorization] = _authHeaderValue;
                request.Headers[HttpRequestHeader.ContentLocation] = contentLocation;
                request.Headers["X-SHA256"] = checksum;
                request.Proxy = _proxy;
                //request2.Headers[Httprequest2Header.ContentLength] = bytes.Length.ToString();
                using (var stream = await request.GetRequestStreamAsync())
                {
                    stream.Write(fileBytes, 0, fileBytes.Length);
                    stream.Close();
                }
                using (var reader = new StreamReader((await request.GetResponseAsync()).GetResponseStream()))
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
        public async Task<string> UploadFileIfNotExistsAsync(string appID, int jobid, string baseDirectory, string fullFilePath) => await Task.Run(async () => 
        {
            var contentLocation = fullFilePath.Replace(baseDirectory, string.Empty).Replace("%5C", "\\").TrimStart('\\');
            var checksum = Cryptography.SHA256CheckSum(fullFilePath);
            if (!await HasFileAsync(appID, contentLocation, checksum))
            {
                var request = (HttpWebRequest)WebRequest.Create(_endpoint + "/UploadFile?id=" + appID);
                request.Method = "POST";
                request.Headers[HttpRequestHeader.Authorization] = _authHeaderValue;
                request.Headers[HttpRequestHeader.ContentLocation] = contentLocation;
                request.Headers["X-SHA256"] = checksum;
                request.Proxy = _proxy;
                //request2.Headers[Httprequest2Header.ContentLength] = bytes.Length.ToString();
                using (var stream = await request.GetRequestStreamAsync())
                {
                    var bytes = File.ReadAllBytes(fullFilePath);
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Close();
                }
                using (var reader = new StreamReader((await request.GetResponseAsync()).GetResponseStream()))
                {
                    return reader.ReadToEnd();
                }
            }
            else
            {
                return "Exists";
            }
        });

        private async Task<bool> HasFileAsync(string appID, string relativeLocation, string checksum)
        {
            var request = (HttpWebRequest)WebRequest.Create(_endpoint + "/HasFile?id=" + appID);
            request.Method = "GET";
            request.Headers[HttpRequestHeader.Authorization] = _authHeaderValue;
            request.Headers[HttpRequestHeader.ContentLocation] = relativeLocation;
            request.Headers["X-SHA256"] = checksum;
            request.Proxy = _proxy;
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
        public async Task<IEnumerable<FileInfo>> GetRemoteFilesAsync(ApplicationInfo app) => await GetRemoteFilesAsync(app.ID);

        /// <summary>
        /// Gets the remote tree of an application.
        /// </summary>
        public async Task<IEnumerable<FileInfo>> GetRemoteFilesAsync(string appID) => JsonConvert.DeserializeObject<IEnumerable<FileInfo>>(await GETRequestAsync("/RemoteFiles?id=" + appID));

        /// <summary>
        /// Downloads an application file's bytes.
        /// </summary>
        public async Task<byte[]> DownloadFileBytesAsync(ApplicationInfo app, string relativePath) => await DownloadFileBytesAsync(app.ID, relativePath);

        /// <summary>
        /// Downloads an application file's bytes.
        /// </summary>
        public async Task<byte[]> DownloadFileBytesAsync(string appID, string relativePath) => await Task.Run(async() => 
        {
            var request = (HttpWebRequest)WebRequest.Create(_endpoint + "/DownloadFile?id=" + appID);
            request.Method = "GET";
            request.Headers[HttpRequestHeader.Authorization] = _authHeaderValue;
            request.Headers[HttpRequestHeader.ContentLocation] = relativePath;
            request.Proxy = _proxy;
            var response = (HttpWebResponse)await request.GetResponseAsync();
            using (var reader = new BinaryReader(response.GetResponseStream()))
            {
                return reader.ReadBytes(Convert.ToInt32(response.Headers[HttpRequestHeader.ContentLength]));
            }
        });

        private async Task<string> GETRequestAsync(string path)
        {
            var request = (HttpWebRequest)WebRequest.Create(_endpoint + path);
            request.Method = "GET";
            request.Headers[HttpRequestHeader.Authorization] = _authHeaderValue;
            if (_proxy != null)
            {
                request.Proxy = _proxy;
            }
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
            request.Proxy = _proxy;
            var response = (HttpWebResponse)await request.GetResponseAsync();
        }

        private async Task<T> POSTRequestAsync<T>(string path, object content = null)
        {
            var request = (HttpWebRequest)WebRequest.Create(_endpoint + path);
            request.Method = "POST";
            request.Headers[HttpRequestHeader.Authorization] = _authHeaderValue;
            request.Headers[HttpRequestHeader.ContentType] = "application/json";
            request.Proxy = _proxy;
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
