﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

#pragma warning disable 1998

namespace Aardwolf
{
    public sealed class JsonResponse : StatusResponse, IHttpResponseAction
    {
        readonly object _value;

        public JsonResponse(int statusCode, string statusDescription, object value)
            : base(statusCode, statusDescription)
        {
            _value = value;
        }

        public override async Task Execute(IHttpRequestResponseContext context)
        {
            SetStatus(context);
            context.Response.ContentType = "application/json; charset=utf-8";
            //context.Response.ContentEncoding = UTF8.WithoutBOM;

            using (context.Response.OutputStream)
            {
                var tw = new StreamWriter(context.Response.OutputStream, UTF8.WithoutBOM);
                Json.Serializer.Serialize(tw, _value);
            }
        }
    }
}
