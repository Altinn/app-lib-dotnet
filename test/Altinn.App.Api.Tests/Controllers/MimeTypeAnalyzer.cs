﻿using Altinn.App.Core.Features.FileAnalyzis;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using MimeDetective;

namespace Altinn.App.Api.Tests.Controllers
{
    // TODO: Move to separate project?
    public class MimeTypeAnalyzer : IFileAnalyzer
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public MimeTypeAnalyzer(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;

            // Allown synchronous IO access for the usage of MimeDetective
            // which does not have async methods. This on a per request basis.
            var syncIOFeature = _httpContextAccessor.HttpContext?.Features.Get<IHttpBodyControlFeature>();
            if (syncIOFeature != null)
            {
                syncIOFeature.AllowSynchronousIO = true;
            }
        }

        public async Task<IEnumerable<FileAnalyzeResult>> Analyze(Stream stream)
        {
            // TODO: Move to service registration as singleton
            var Inspector = new ContentInspectorBuilder()
            {
                Definitions = MimeDetective.Definitions.Default.All(),
                MatchEvaluatorOptions = new MimeDetective.Engine.DefinitionMatchEvaluatorOptions() 
                { 
                    Include_Matches_Complete = true,
                    Include_Matches_Failed = false,
                    Include_Matches_Partial = true,
                    Include_Segments_Prefix = true,
                    Include_Segments_Strings = true
                }}
            .Build();

            var results = Inspector.Inspect(stream);
            
            // We only care for the 100% match anything else is considered unsafe.
            var match = results.OrderByDescending(match => match.Points).FirstOrDefault(match => match.Percentage == 1);

            var fileAnalyzeResult = new FileAnalyzeResult();
            if (match != null)
            {                
                fileAnalyzeResult.Extensions = match.Definition.File.Extensions.ToList();
                fileAnalyzeResult.MimeType = match.Definition.File.MimeType;
            }
            
            return new List<FileAnalyzeResult>() { fileAnalyzeResult };
        }
    }
}