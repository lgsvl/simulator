/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using Nancy;
using Nancy.Authentication.Forms;
using Nancy.ErrorHandling;
using Nancy.Security;
using SimpleJSON;
using Simulator.Database;
using Simulator.Database.Services;
using System;
using System.Collections.Generic;
using System.Net.Http;
using UnityEngine;

namespace Simulator.Web.Modules
{
    public class UserRequest
    {
        public string username { get; set; }
        public string secretKey { get; set; }
        public DateTime? expire { get; set; }
        public string settings { get; set; }

        public UserModel ToModel()
        {
            return new UserModel()
            {
                Username = username,
                SecretKey = secretKey,
                Settings = settings,
            };
        }
    }

    public class UserResponse
    {
        public string Username;
        public string Settings;

        public static UserResponse Create(UserModel user)
        {
            return new UserResponse()
            {
                Username = user.Username,
                Settings = user.Settings
            };
        }
    }

    public class UsersModule : NancyModule
    {
        public UsersModule(IUserService userService) : base("users")
        {
            Get("/", x =>
            {
                Debug.Log($"Getting current user");
                try
                {
                    this.RequiresAuthentication();

                    string currentUsername = this.Context.CurrentUser.Identity.Name;
                    UserModel userModel = userService.Get(currentUsername);
                    SIM.InitUser(userModel.Username);
                    return UserResponse.Create(userModel);
                }
                catch (RouteExecutionEarlyExitException ex)
                {
                    return Response.AsJson(new { error = $"User is not authorized: {ex.Message}", cloudUrl = Config.CloudUrl }, Nancy.HttpStatusCode.Unauthorized);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    return Response.AsJson(new { error = $"Failed to get user: {ex.Message}" }, Nancy.HttpStatusCode.InternalServerError);
                }
            });

            Put("/{token}", async x =>
            {
                Debug.Log($"Updating user with token");
                try
                {
                    var client = new HttpClient();
                    client.DefaultRequestHeaders.Add("Accept", "application/json");

                    var postData = new [] { new KeyValuePair<string, string>("token", x.token) };
                    var formContent = new FormUrlEncodedContent(postData);

                    var response = await client.PutAsync(Config.CloudUrl + "/users/token", formContent);
                    var content = await response.Content.ReadAsStringAsync();

                    var json = JSONNode.Parse(content);
                    UserModel userModel = new UserModel()
                    {
                        Username = json["username"].Value,
                        SecretKey = json["secretKey"].Value,
                        Settings = json["settings"].Value
                    };
                    userService.AddOrUpdate(userModel);
                    SIM.InitUser(userModel.Username);

                    var guid = Guid.NewGuid();
                    UserMapper.RegisterUserSession(guid, userModel.Username);
                    return this.LoginWithoutRedirect(guid, DateTime.UtcNow.AddSeconds(Config.sessionTimeout));
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    return Response.AsJson(new { error = $"Failed to add/update user by token: {x.token}, {ex.Message}" }, Nancy.HttpStatusCode.InternalServerError);
                }
            });
        }
    }
}
