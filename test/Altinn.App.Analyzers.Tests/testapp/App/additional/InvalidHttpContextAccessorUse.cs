using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Altinn.App.Core.Features;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.AspNetCore.Http;

namespace Altinn.App.Models.logic;

internal sealed class ProcessTaskStart : IProcessTaskStart
{
    private readonly HttpContext _httpContext;
    private readonly ClaimsPrincipal _user;

    public ProcessTaskStart(IHttpContextAccessor httpContextAccessor)
    {
        _httpContext = httpContextAccessor.HttpContext;
        _user = _httpContext.User;
    }

    public Task Start(string taskId, Instance instance, Dictionary<string, string> prefill)
    {
        return Task.CompletedTask;
    }
}
