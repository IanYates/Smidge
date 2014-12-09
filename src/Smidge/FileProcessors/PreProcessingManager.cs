﻿using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Smidge.Models;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.Runtime;
using Microsoft.Framework.OptionsModel;
using Smidge.FileProcessors;

namespace Smidge
{
    public sealed class PreProcessingManager
    {
        private FileSystemHelper _fileSystemHelper;
        private IHasher _hasher;

        public PreProcessingManager(FileSystemHelper fileSystemHelper, IHasher hasher)
        {
            _hasher = hasher;
            _fileSystemHelper = fileSystemHelper;
        }

        /// <summary>
        /// If the current asset/request requires minification, this will check the cache for its existence, if it doesn't
        /// exist, it will minify it and store the cache file. Lastly, it sets the file path for the JavaScript file.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public async Task ProcessAndCacheFileAsync(IWebFile file)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));
            if (file.Pipeline == null) throw new ArgumentNullException("\{nameof(file)}.Pipeline");

            switch (file.DependencyType)
            {
                case WebFileType.Js:
                    await ProcessJsFile(file);
                    break;
                case WebFileType.Css:
                    await ProcessCssFile(file);
                    break;
            }
        }

        private async Task ProcessCssFile(IWebFile file)
        {
            await ProcessFile(file, ".css");
        }

        private async Task ProcessJsFile(IWebFile file)
        {
            await ProcessFile(file, ".js");
        }

        private async Task ProcessFile(IWebFile file, string extension)
        {
            //check if it's in cache
            
            //TODO: If we make the hash as part of the last write time of the file, then the hash will be different
            // which means it will be a new cached file which means we can have auto-changing of files. Versioning
            // will still be manual but that would just be up to the client cache, not the server cache!

            var hashName = _hasher.Hash(file.FilePath) + extension;
            var cacheDir = _fileSystemHelper.CurrentCacheFolder;
            var cacheFile = Path.Combine(cacheDir, hashName);

            Directory.CreateDirectory(cacheDir);

            if (!File.Exists(cacheFile))
            {
                var filePath = _fileSystemHelper.MapPath(file.FilePath);
                var contents = await _fileSystemHelper.ReadContentsAsync(filePath);

                var processed = await file.Pipeline.ProcessAsync(new FileProcessContext(contents, file.FilePath));

                //save it to the cache path
                await _fileSystemHelper.WriteContentsAsync(cacheFile, processed);
            }
        }

    }
}