﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SunokoLibrary.Web.GooglePlus
{
    using SunokoLibrary.Web.GooglePlus.Primitive;

    public class PlatformClientBuilder : IPlatformClientBuilder
    {
        public PlatformClientBuilder(string email, string name, string iconUrl, int accountIndex, CookieContainer cookies, IApiAccessor accessor)
        {
            Email = email;
            Name = name;
            IconUrl = iconUrl;
            AccountIndex = accountIndex;
            _accessor = accessor;
            _cookies = cookies;
        }
        IApiAccessor _accessor;
        CookieContainer _cookies;

        public int AccountIndex { get; private set; }
        public string Name { get; private set; }
        public string Email { get; private set; }
        public string IconUrl { get; private set; }
        public Task<PlatformClient> Build()
        { return PlatformClient.Factory.Create(_cookies, AccountIndex, _accessor); }
    }
}