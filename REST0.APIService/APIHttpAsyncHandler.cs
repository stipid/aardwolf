﻿using REST0.Definition;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Json;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

#pragma warning disable 1998

namespace REST0.APIService
{
    public sealed class APIHttpAsyncHandler : IHttpAsyncHandler, IInitializationTrait, IConfigurationTrait
    {
        ConfigurationDictionary localConfig;
        SHA1Hashed<JsonValue> _serviceConfig;

        public async Task Configure(IHttpAsyncHostHandlerContext hostContext, ConfigurationDictionary configValues)
        {
            // Configure gets called first.
            localConfig = configValues;
        }

        public async Task Initialize(IHttpAsyncHostHandlerContext context)
        {
            // Initialize gets called after Configure.
            await RefreshConfigData();

            // Let a background task refresh the config data every 10 seconds:
#pragma warning disable 4014
            Task.Run(async () =>
            {
                while (true)
                {
                    // Wait until the next even 10-second mark on the clock:
                    const long sec10 = TimeSpan.TicksPerSecond * 10;
                    var now = DateTime.UtcNow;
                    var next10 = new DateTime(((now.Ticks + sec10) / sec10) * sec10, DateTimeKind.Utc);
                    await Task.Delay(next10.Subtract(now));

                    // Refresh config data:
                    await RefreshConfigData();
                }
            });
#pragma warning restore 4014
        }

        async Task RefreshConfigData()
        {
            // Get the latest config data:
            var config = await FetchConfigData();
            if (config == null) return;

            _serviceConfig = config;
        }

        async Task<SHA1Hashed<JsonValue>> FetchConfigData()
        {
            string url, path;

            // Prefer to fetch over HTTP:
            if (localConfig.TryGetSingleValue("config.Url", out url))
            {
                Trace.WriteLine("Getting config data via HTTP");

                // Fire off a request now to our configuration server for our config data:
                try
                {
                    var req = HttpWebRequest.CreateHttp(url);
                    using (var rsp = await req.GetResponseAsync())
                    using (var rspstr = rsp.GetResponseStream())
                    using (var sha1 = new SHA1StreamReader(rspstr))
                        return new SHA1Hashed<JsonValue>(JsonValue.Load(sha1), sha1.GetHash());
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex.ToString());

                    // Fall back on loading a local file:
                    goto loadFile;
                }
            }

        loadFile:
            if (localConfig.TryGetSingleValue("config.Path", out path))
            {
                Trace.WriteLine("Getting config data via file");

                // Load the local JSON file:
                try
                {
                    using (var fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                    using (var sha1 = new SHA1StreamReader(fs))
                    using (var tr = new StreamReader(sha1, true))
                        return new SHA1Hashed<JsonValue>(JsonValue.Load(tr), sha1.GetHash());
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex.ToString());
                    return null;
                }
            }

            // If all else fails, complain:
            throw new Exception(String.Format("Either '{0}' or '{1}' configuration keys are required", "config.Url", "config.Path"));
        }

        /// <summary>
        /// Main logic.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task<IHttpResponseAction> Execute(IHttpRequestContext context)
        {
            // Capture the current service configuration values only once per connection in case they update during:
            var config = _serviceConfig;

            if (context.Request.Url.AbsolutePath == "/")
                return new RedirectResponse("/foo");

            return new JsonResponse(new JsonObject() {
                { "hash", config.HashHexString },
                { "config", config.Value }
            });
        }
    }
}