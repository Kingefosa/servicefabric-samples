// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
// ------------------------------------------------------------

namespace Web.Service
{
    using System.Web.Http;
    using Microsoft.Owin;
    using Microsoft.Owin.FileSystems;
    using Microsoft.Owin.StaticFiles;
    using Owin;

    internal class Startup : IOwinAppBuilder
    {
        public void Configuration(IAppBuilder appBuilder)
        {
            var config = new HttpConfiguration();

            var physicalFileSystem = new PhysicalFileSystem(@".\wwwroot");
            var fileOptions = new FileServerOptions();

            fileOptions.EnableDefaultFiles = true;
            fileOptions.RequestPath = PathString.Empty;
            fileOptions.FileSystem = physicalFileSystem;
            fileOptions.DefaultFilesOptions.DefaultFileNames = new[] {"index.html"};
            fileOptions.StaticFileOptions.FileSystem = fileOptions.FileSystem = physicalFileSystem;
            fileOptions.StaticFileOptions.ServeUnknownFileTypes = true;


            FormatterConfig.ConfigureFormatters(config.Formatters);
            config.MapHttpAttributeRoutes();

            appBuilder.UseWebApi(config);
            appBuilder.UseFileServer(fileOptions);
        }
    }
}