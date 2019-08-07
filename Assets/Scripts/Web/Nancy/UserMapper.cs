/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using Nancy;
using Nancy.Authentication.Forms;
using Simulator.Database;
using Simulator.Database.Services;
using System;
using System.Security.Claims;
using System.Security.Principal;

namespace Simulator.Web
{
    public class UserMapper : IUserMapper
    {
        private static ISessionService sessionService = new SessionService();
        private static IUserService userService = new UserService();

        public ClaimsPrincipal GetUserFromIdentifier(Guid identifier, NancyContext context)
        {
            var session = sessionService.Get(identifier.ToString());
            if (session == null)
            {
                // We cannot find a session for the current user
                return null;
            }

            var user = userService.Get(session.Username);
            if (user == null)
            {
                return null;
            }

            GenericIdentity identity = new GenericIdentity(user.Username);
            identity.AddClaim(new Claim(ClaimTypes.PrimarySid, identifier.ToString()));
            return new ClaimsPrincipal(identity);
        }

        public static void RemoveUserSession(string identifier)
        {
            sessionService.Remove(identifier);
        }

        public static void RegisterUserSession(Guid identifier, string username)
        {
            sessionService.Add(new SessionModel()
            {
                Cookie = identifier.ToString(),
                Username = username,
                Expire = DateTime.UtcNow.AddSeconds(Config.sessionTimeout)
            });
        }
    }
}
