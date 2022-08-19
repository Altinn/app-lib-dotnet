﻿using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace Altinn.App.Common.Tests
{
    public static class EmbeddedResource
    {
        public async static Task<string> LoadDataAsString(string resourceName)
        {
            var resourceStream = LoadDataAsStream(resourceName);

            using var reader = new StreamReader(resourceStream);
            string text = await reader.ReadToEndAsync();

            return text;
        }

        public static Stream LoadDataAsStream(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            Stream? resourceStream = assembly.GetManifestResourceStream(resourceName);

            if (resourceStream == null)
            {
                throw new InvalidOperationException($"Unable to find resource {resourceName} embedded in assembly {assembly.FullName}.");
            }

            resourceStream.Seek(0, SeekOrigin.Begin);

            return resourceStream;
        }
    }
}
