﻿using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DropNet.Authenticators;
using DropNet.Exceptions;
using DropNet.Extensions;
using DropNet.Helpers;
using DropNet.Models;
using RestSharp;
using RestSharp.Deserializers;

#if MONOTOUCH
using System.Security.Cryptography;
#endif

namespace DropNet
{
    public partial class DropNetClient
    {
        private const string ApiBaseUrl = "https://api.dropbox.com";
        private const string ApiContentBaseUrl = "https://api-content.dropbox.com";
        private const string Version = "1";

        private UserLogin _userLogin;

        /// <summary>
        /// Contains the Users Token and Secret
        /// </summary>
        public UserLogin UserLogin
        {
            get { return _userLogin; }
            set
            {
                _userLogin = value;
                SetAuthProviders();
            }
        }
        
        /// <summary>
        /// To use Dropbox API in sandbox mode (app folder access) set to true
        /// </summary>
        public bool UseSandbox { get; set; }

        private const string SandboxRoot = "sandbox";
        private const string DropboxRoot = "dropbox";

        protected readonly string _apiKey;
        protected readonly string _appsecret;

        private RestClient _restClient;
        private RestClient _restClientContent;
        protected RequestHelper _requestHelper;

        /// <summary>
        /// Gets the directory root for the requests (full or sandbox mode)
        /// </summary>
        string Root
        {
            get { return UseSandbox ? SandboxRoot : DropboxRoot; }
        }
        
        /// <summary>
        /// Default Constructor for the DropboxClient
        /// </summary>
        /// <param name="apiKey">The Api Key to use for the Dropbox Requests</param>
        /// <param name="appSecret">The Api Secret to use for the Dropbox Requests</param>
        public DropNetClient(string apiKey, string appSecret)
        {
            _apiKey = apiKey;
            _appsecret = appSecret;

            LoadClient();
        }

        /// <summary>
        /// Creates an instance of the DropNetClient given an API Key/Secret and a User Token/Secret
        /// </summary>
        /// <param name="apiKey">The Api Key to use for the Dropbox Requests</param>
        /// <param name="appSecret">The Api Secret to use for the Dropbox Requests</param>
        /// <param name="userToken">The User authentication token</param>
        /// <param name="userSecret">The Users matching secret</param>
        public DropNetClient(string apiKey, string appSecret, string userToken, string userSecret)
        {
            _apiKey = apiKey;
            _appsecret = appSecret;

            LoadClient();

            UserLogin = new UserLogin { Token = userToken, Secret = userSecret };
        }

        private void LoadClient()
        {
            _restClient = new RestClient(ApiBaseUrl);
            _restClient.ClearHandlers();
            _restClient.AddHandler("*", new JsonDeserializer());

            _restClientContent = new RestClient(ApiContentBaseUrl);
            _restClientContent.ClearHandlers();
            _restClientContent.AddHandler("*", new JsonDeserializer());

            _requestHelper = new RequestHelper(Version);

            //Default to full access
            UseSandbox = false;
        }

        /// <summary>
        /// Helper Method to Build up the Url to authorize a Token/Secret
        /// </summary>
        /// <param name="callback"></param>
        /// <returns></returns>
        public string BuildAuthorizeUrl(string callback = null)
        {
            return BuildAuthorizeUrl(UserLogin, callback);
        }

        /// <summary>
        /// Helper Method to Build up the Url to authorize a Token/Secret
        /// </summary>
        /// <param name="userLogin"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public string BuildAuthorizeUrl(UserLogin userLogin, string callback = null)
        {
            if (userLogin == null)
            {
                throw new ArgumentNullException("userLogin");
            }

            //Go 1-Liner!
            return string.Format("https://www.dropbox.com/1/oauth/authorize?oauth_token={0}{1}", userLogin.Token,
                (string.IsNullOrEmpty(callback) ? string.Empty : "&oauth_callback=" + callback));
        }

		
#if MONOTOUCH
		
		/// <summary>
		/// Gets the token from URL.
		/// This is used in the OpenUrl of AppDelegate to retrive the token
		/// </summary>
		/// <param name='url'></param>
		/// <returns>
		/// The token from the URL.
		/// </returns>
		public UserLogin GetTokenFromUrl(string url)
		{
			Uri u = new Uri(url);
			
			return GetUserLoginFromParams(u.Query.Replace("?", ""));
			
		}

		
		/// <summary>
		/// Fallback method to use the web browser.
		/// </summary>
		/// <param name="userLogin"></param>
		/// <param name="callback"></param>
		/// <returns></returns>
		public string BuildWebAuthorizeUrlIOS()
		{
			
			
			//Go 1-Liner!
			return string.Format(
				"https://www.dropbox.com/1/connect?k={0}&s={1}&dca=1&easl=1", _apiKey, GetSValue(_appsecret));
		}
		
		/// <summary>
		/// Builds the app authorize URL for iOS. Tries to load the DropBox App if it's available.
		/// </summary>
		/// <returns>
		/// the URL to call. Check it with UIApplication.SharedApplication.CanOpenUrl(new NSUrl(url))
		/// </returns>
		public string BuildAppAuthorizeUrlIOS()
		{
			
			
			//Go 1-Liner!
			return string.Format(
				"dbapi-1://1/connect?k={0}&s={1}&dca=1&easl=1", _apiKey, GetSValue(_appsecret));
		}
		
		private string GetSValue(string tokenSecret)
		{
			using (var cp = new SHA1CryptoServiceProvider())
			{
				byte[] buffer = System.Text.Encoding.ASCII.GetBytes(tokenSecret);
				string hash = BitConverter.ToString(cp.ComputeHash(buffer));
				hash = hash.Replace("-", "");
				return hash.Substring(hash.Length -8);
				
			}
		}
		
		
#endif

#if !WINDOWS_PHONE && !WINRT && !SILVERLIGHT
        private T Execute<T>(ApiType apiType, IRestRequest request) where T : new()
        {
            IRestResponse<T> response;
            if (apiType == ApiType.Base)
            {
                response = _restClient.Execute<T>(request);

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    throw new DropboxException(response);
                }
            }
            else
            {
                response = _restClientContent.Execute<T>(request);

                if (response.StatusCode != HttpStatusCode.OK && response.StatusCode != HttpStatusCode.PartialContent)
                {
                    throw new DropboxException(response);
                }
            }

            return response.Data;
        }

        private IRestResponse Execute(ApiType apiType, IRestRequest request)
        {
            IRestResponse response;
            if (apiType == ApiType.Base)
            {
                response = _restClient.Execute(request);

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    throw new DropboxException(response);
                }
            }
            else
            {
                response = _restClientContent.Execute(request);

				if (response.StatusCode != HttpStatusCode.OK && response.StatusCode != HttpStatusCode.PartialContent)
                {
                    throw new DropboxException(response);
                }
            }

            return response;
        }
#endif

        protected void ExecuteAsync(ApiType apiType, IRestRequest request, Action<IRestResponse> success, Action<DropboxException> failure)
        {
#if WINDOWS_PHONE
            //check for network connection
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                //do nothing
                failure(new DropboxException
                {
                    StatusCode = System.Net.HttpStatusCode.BadGateway
                });
                return;
            }
#endif
            if (apiType == ApiType.Base)
            {
                _restClient.ExecuteAsync(request, (response, asynchandle) =>
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        failure(new DropboxException(response));
                    }
                    else
                    {
                        success(response);
                    }
                });
            }
            else
            {
                _restClientContent.ExecuteAsync(request, (response, asynchandle) =>
                {
					if (response.StatusCode != HttpStatusCode.OK && response.StatusCode != HttpStatusCode.PartialContent)
                    {
                        failure(new DropboxException(response));
                    }
                    else
                    {
                        success(response);
                    }
                });
            }
        }

        protected void ExecuteAsync<T>(ApiType apiType, IRestRequest request, Action<T> success, Action<DropboxException> failure) where T : new()
        {
#if WINDOWS_PHONE
            //check for network connection
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                //do nothing
                failure(new DropboxException
                {
                    StatusCode = System.Net.HttpStatusCode.BadGateway
                });
                return;
            }
#endif
            if (apiType == ApiType.Base)
            {
                _restClient.ExecuteAsync<T>(request, (response, asynchandle) =>
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        failure(new DropboxException(response));
                    }
                    else
                    {
                        success(response.Data);
                    }
                });
            }
            else
            {
                _restClientContent.ExecuteAsync<T>(request, (response, asynchandle) =>
                {
					if (response.StatusCode != HttpStatusCode.OK && response.StatusCode != HttpStatusCode.PartialContent)
                    {
                        failure(new DropboxException(response));
                    }
                    else
                    {
                        success(response.Data);
                    }
                });
            }
        }

#if !WINRT

        private Task<T> ExecuteTask<T>(ApiType apiType, IRestRequest request, CancellationToken token = default(CancellationToken)) where T : new()
        {
            if (apiType == ApiType.Base)
            {
                return _restClient.ExecuteTask<T>(request, token);
            }
            else
            {
                return _restClientContent.ExecuteTask<T>(request, token);
            }
        }

		private Task<IRestResponse> ExecuteTask(ApiType apiType, IRestRequest request, CancellationToken token = default(CancellationToken))
        {
            if (apiType == ApiType.Base)
            {
                return _restClient.ExecuteTask(request, token);
            }
            else
            {
                return _restClientContent.ExecuteTask(request, token);
            }
        }

#endif

        protected UserLogin GetUserLoginFromParams(string parms)
        {
            var userLogin = new UserLogin();

            //TODO - Make this not suck
            var parameters = parms.Split('&');

            foreach (var parameter in parameters)
            {
                var keyVal = parameter.Split ('=');
                switch (keyVal[0]) {
                case "uid":
                    userLogin.Uid = keyVal[1];
                    break;
                case "oauth_token_secret":
                    userLogin.Secret = keyVal[1];
                    break;
                case "oauth_token":
                    userLogin.Token = keyVal[1];
                    break;
                }
            }

            return userLogin;
        }

        private void SetAuthProviders()
        {
            if (UserLogin != null)
            {
                //Set the OauthAuthenticator only when the UserLogin property changes
                _restClientContent.Authenticator = new OAuthAuthenticator(_restClientContent.BaseUrl, _apiKey, _appsecret, UserLogin.Token, UserLogin.Secret);
                _restClient.Authenticator = new OAuthAuthenticator(_restClient.BaseUrl, _apiKey, _appsecret, UserLogin.Token, UserLogin.Secret);
            }
        }

        protected enum ApiType
        {
            Base,
            Content
        }
    }
}
