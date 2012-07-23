﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DropNet.Tests
{
    [TestClass]
    public class UserTests1
    {
        readonly DropNetClient _client;

        public UserTests1()
        {
            _client = new DropNetClient(TestVariables.ApiKey, TestVariables.ApiSecret);
        }

        [TestMethod]
        public void Test_CanGetRequestToken()
        {
            var response = _client.GetToken();

            Assert.IsNotNull(response);
            Assert.IsNotNull(response.Token);
            Assert.IsNotNull(response.Secret);
        }

        [TestMethod]
        public void Test_CanBuildAutorizeUrl()
        {
            var authorizeUrl = _client.BuildAuthorizeUrl(new Models.UserLogin
                                                        {
                                                            Secret = TestVariables.Secret,
                                                            Token = TestVariables.Token
                                                        });

            Assert.IsNotNull(authorizeUrl);
        }

        [TestMethod]
        public void Test_BuildAutorizeUrl_ThrowNullException()
        {
            try
            {
                _client.BuildAuthorizeUrl();

                Assert.Fail();
            }
            catch (ArgumentNullException ane)
            {
                Assert.IsNotNull(ane);
            }
            catch (Exception)
            {
                Assert.Fail();
            }
        }

        [TestMethod]
        public void Can_Get_AccountInfo()
        {
            _client.UserLogin = new Models.UserLogin { Token = TestVariables.Token, Secret = TestVariables.Secret };

            var accountInfo = _client.AccountInfo();

            Assert.IsNotNull(accountInfo);
            Assert.IsNotNull(accountInfo.display_name);
            Assert.IsNotNull(accountInfo.uid);
        }

    }
}
