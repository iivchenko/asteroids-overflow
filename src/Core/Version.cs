﻿using Microsoft.Extensions.FileProviders;
using System.Reflection;
using System.IO;

namespace Core
{
    public static class Version
    {
        public static string Current
        {
            get
            {
                var assembly = Assembly.GetExecutingAssembly();
                var embeddedProvider = new EmbeddedFileProvider(assembly, "Core");
                using (var stream = embeddedProvider.GetFileInfo(".version").CreateReadStream())
                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadLine();
                }
            }
        }
    }
}