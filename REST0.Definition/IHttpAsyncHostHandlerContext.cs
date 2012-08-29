﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace REST0.Definition
{
    public interface IHttpAsyncHostHandlerContext
    {
        IHttpAsyncHost Host { get; }
        IHttpAsyncHandler Handler { get; }
    }
}
