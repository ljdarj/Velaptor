﻿// <copyright file="ContentSource.cs" company="KinsonDigital">
// Copyright (c) KinsonDigital. All rights reserved.
// </copyright>

#pragma warning disable CA1303 // Do not pass literals as localized parameters
namespace Raptor.Content
{
    using System;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using Raptor.Exceptions;

    /// <summary>
    /// Manages the content source.
    /// </summary>
    public class ContentSource : IContentSource
    {
        private static readonly string BaseDir = $@"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}\";
        private readonly IDirectory directory;
        private string contentRootDirectory = @$"{BaseDir}Content\";
        private string graphicsDirName = "Graphics";
        private string soundsDirName = "Sounds";
        private string atlasDirName = "AtlasData";

        /// <summary>
        /// Initializes a new instance of the <see cref="ContentSource"/> class.
        /// </summary>
        /// <param name="directory">Manages directories.</param>
        public ContentSource(IDirectory directory) => this.directory = directory;

        /// <inheritdoc/>
        public string ContentRootDirectory
        {
            get => this.contentRootDirectory;
            set
            {
                value = string.IsNullOrEmpty(value) ? BaseDir : value;

                // If the value ends with a backslash, leave as is, else add one
                value = value.EndsWith('\\') ? value : $@"{value}\";

                this.contentRootDirectory = $@"{value}Content\";
            }
        }

        /// <inheritdoc/>
        public string GraphicsDirectoryName
        {
            get => this.graphicsDirName;
            set
            {
                if (string.IsNullOrEmpty(value))
                    throw new Exception($"The '{nameof(GraphicsDirectoryName)}' must not be null or empty.");

                // NOTE: No localization required
#pragma warning disable CA1307 // Specify StringComparison
                value = value.Replace("\\", string.Empty);
#pragma warning restore CA1307 // Specify StringComparison

                this.graphicsDirName = value;
            }
        }

        /// <inheritdoc/>
        public string SoundsDirectoryName
        {
            get => this.soundsDirName;
            set
            {
                if (string.IsNullOrEmpty(value))
                    throw new Exception($"The '{nameof(SoundsDirectoryName)}' must not be null or empty.");

                // NOTE: No localization required
#pragma warning disable CA1307 // Specify StringComparison
                value = value.Replace("\\", string.Empty);
#pragma warning restore CA1307 // Specify StringComparison

                this.soundsDirName = value;
            }
        }

        /// <inheritdoc/>
        public string AtlasDirectoryName
        {
            get => this.atlasDirName;
            set
            {
                if (string.IsNullOrEmpty(value))
                    throw new Exception($"The '{nameof(AtlasDirectoryName)}' must not be null or empty.");

                // NOTE: No localization required
#pragma warning disable CA1307 // Specify StringComparison
                value = value.Replace("\\", string.Empty);
#pragma warning restore CA1307 // Specify StringComparison

                this.atlasDirName = value;
            }
        }

        /// <inheritdoc/>
        public string GetGraphicsPath() => $@"{this.contentRootDirectory}{this.graphicsDirName}\";

        /// <inheritdoc/>
        public string GetSoundsPath() => $@"{this.contentRootDirectory}{this.soundsDirName}\";

        /// <inheritdoc/>
        public string GetAtlasPath() => $@"{this.contentRootDirectory}{this.atlasDirName}\";

        /// <inheritdoc/>
        public string GetContentPath(ContentType contentType, string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new StringNullOrEmptyException();

            // If the name ends with a '\', throw and exception
            if (name.EndsWith('\\'))
                throw new ArgumentException($"The '{name}' cannot end with folder.  It must end with a file name with or without the extension.");

            // If the name has an extension, remove it
            if (Path.HasExtension(name))
            {
                // NOTE: No localization required
#pragma warning disable CA1307 // Specify StringComparison
                name = $@"{Path.GetDirectoryName(name)}\{Path.GetFileNameWithoutExtension(name)}".Replace(@"\", string.Empty);
#pragma warning restore CA1307 // Specify StringComparison
            }

            var filePath = string.Empty;

            switch (contentType)
            {
                case ContentType.Graphics:
                    filePath = $@"{GetGraphicsPath()}{name}";
                    break;
                case ContentType.Sounds:
                    filePath = $@"{GetSoundsPath()}{name}";
                    break;
                case ContentType.Atlas:
                    filePath = $@"{GetAtlasPath()}{name}";
                    break;
            }

            var directory = Path.GetDirectoryName(filePath);
            var fileNameNoExt = Path.GetFileNameWithoutExtension(filePath).ToUpperInvariant();

            // Check if there are any files that match the name
            var files = this.directory.GetFiles(directory)
                .Where(f => Path.GetFileNameWithoutExtension(f).ToUpperInvariant() == fileNameNoExt).ToArray();

            if (files.Length <= 0)
                throw new Exception($"The content item '{Path.GetFileNameWithoutExtension(filePath)}' does not exist.");

            if (files.Length > 1)
            {
                var exceptionMsg = new StringBuilder();
                exceptionMsg.AppendLine("Multiple items match the content item name.");
                exceptionMsg.AppendLine("The content item name must be unique and the file extension is not taken into account.");

                // Add the items to the exception message
                foreach (var file in files)
                {
                    exceptionMsg.AppendLine($"\t{Path.GetFileName(file)}");
                }

                throw new Exception(exceptionMsg.ToString());
            }

            return files[0];
        }
    }
}