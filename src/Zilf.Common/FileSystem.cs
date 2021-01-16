/* Copyright 2010-2018 Jesse McGrew
 * 
 * This file is part of ZILF.
 * 
 * ZILF is free software: you can redistribute it and/or modify it
 * under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * ZILF is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with ZILF.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Zilf.Common
{
    /// <summary>
    /// Exposes a logical file system to the compiler and interpreter.
    /// </summary>
    public interface IFileSystem
    {
        /// <summary>
        /// Tests whether a file currently exists.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <returns><see langword="true"/> if the file currently exists; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// This method does not test whether the file can be read or written.
        /// The file may also be created or deleted by another process after this method is called.
        /// </remarks>
        bool Exists(string path);

        /// <summary>
        /// Opens a file in read-only mode.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <returns>A stream.</returns>
        /// <exception cref="FileNotFoundException">The file does not exist.</exception>
        /// <exception cref="IOException">The file could not be opened.</exception>
        Stream OpenForReading(string path);

        /// <summary>
        /// Opens a file in write-only mode.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <returns>A stream.</returns>
        /// <exception cref="NotSupportedException">The file system does not support writing.</exception>
        /// <exception cref="IOException">The file could not be opened.</exception>
        /// <remarks>If the file already exists, it will be truncated.</remarks>
        Stream OpenForWriting(string path);

        /// <summary>
        /// Opens a file in write mode with the ability to seek and re-read data that has already been written.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <returns>A stream.</returns>
        /// <exception cref="NotSupportedException">The file system does not support writing.</exception>
        /// <exception cref="IOException">The file could not be opened.</exception>
        /// <remarks>If the file already exists, it will be truncated.</remarks>
        Stream OpenForWritingAndReading(string path);

        /// <summary>
        /// Gets the contents of a readable file as a byte array.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <returns>A byte array containing the file contents.</returns>
        /// <exception cref="FileNotFoundException">The file does not exist.</exception>
        /// <exception cref="IOException">The file could not be read.</exception>
        byte[] GetBytes(string path)
        {
            using var input = OpenForReading(path);
            using var output = new MemoryStream();
            input.CopyTo(output);
            return output.ToArray();
        }

        /// <summary>
        /// Asynchronously gets the contents of a readable file as a byte array, using a specified cancellation token.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
        /// <returns>A task that represents the asynchronous read operation.</returns>
        /// <exception cref="FileNotFoundException">The file does not exist.</exception>
        /// <exception cref="IOException">The file could not be read.</exception>
        async Task<byte[]> GetBytesAsync(string path, CancellationToken cancellationToken = default)
        {
            var input = OpenForReading(path);
            await using var _ = input.ConfigureAwait(false);

            using var output = new MemoryStream();
            await input.CopyToAsync(output, cancellationToken).ConfigureAwait(false);

            return output.ToArray();
        }

        /// <summary>
        /// Gets the contents of a readable file as a string, using UTF-8 encoding.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <returns>A string containing the file contents.</returns>
        /// <exception cref="FileNotFoundException">The file does not exist.</exception>
        /// <exception cref="IOException">The file could not be read.</exception>
        string GetText(string path)
        {
            using var input = OpenForReading(path);
            using var rdr = new StreamReader(input, Encoding.UTF8);
            return rdr.ReadToEnd();
        }

        /// <summary>
        /// Asynchronously gets the contents of a readable file as a string, using UTF-8 encoding.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <returns>A task that represents the asynchronous read operation.</returns>
        /// <exception cref="FileNotFoundException">The file does not exist.</exception>
        /// <exception cref="IOException">The file could not be read.</exception>
        async Task<string> GetTextAsync(string path)
        {
            var input = OpenForReading(path);
            await using var _ = input.ConfigureAwait(false);

            using var rdr = new StreamReader(input, Encoding.UTF8);
            return await rdr.ReadToEndAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Sets the contents of a writable file from a string, using UTF-8 encoding.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <param name="content">A string containing the desired contents of the file.</param>
        /// <exception cref="NotSupportedException">The file system does not support writing.</exception>
        /// <exception cref="IOException">The file could not be written.</exception>
        void SetText(string path, string content)
        {
            using var output = OpenForWriting(path);
            using var wtr = new StreamWriter(output, Encoding.UTF8);
            wtr.Write(content);
        }

        /// <summary>
        /// Asynchronously sets the contents of a writable file from a string, using UTF-8 encoding.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <param name="content">A string containing the desired contents of the file.</param>
        /// <returns>A task representing the asynchronous write operation.</returns>
        /// <exception cref="NotSupportedException">The file system does not support writing.</exception>
        /// <exception cref="IOException">The file could not be written.</exception>
        async Task SetTextAsync(string path, string content)
        {
            var output = OpenForWriting(path);
            await using var _ = output.ConfigureAwait(false);

            var wtr = new StreamWriter(output, Encoding.UTF8);
            await using var __ = wtr.ConfigureAwait(false);

            await wtr.WriteAsync(content).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// A minimal implementation of <see cref="IFileSystem"/> representing an empty, read-only file system.
    /// </summary>
    public sealed class NullFileSystem : IFileSystem
    {
        public static NullFileSystem Instance { get; } = new NullFileSystem();

        private NullFileSystem() { }

        public bool Exists(string path) => false;

        public Stream OpenForReading(string path) => throw new FileNotFoundException(nameof(NullFileSystem), path);

        public Stream OpenForWriting(string path) => throw new NotSupportedException($"{nameof(NullFileSystem)} is read-only");

        public Stream OpenForWritingAndReading(string path) => throw new NotSupportedException($"{nameof(NullFileSystem)} is read-only");
    }

    /// <summary>
    /// An implementation of <see cref="IFileSystem"/> providing read-write access to the local file system.
    /// </summary>
    public sealed class PhysicalFileSystem : IFileSystem
    {
        public static PhysicalFileSystem Instance { get; } = new PhysicalFileSystem();

        private PhysicalFileSystem() { }

        public bool Exists(string path) => File.Exists(path);

        public Stream OpenForReading(string path) => new FileStream(path, FileMode.Open, FileAccess.Read);

        public Stream OpenForWriting(string path) => new FileStream(path, FileMode.Create, FileAccess.Write);

        public Stream OpenForWritingAndReading(string path) => new FileStream(path, FileMode.Create, FileAccess.ReadWrite);
    }

    /// <summary>
    /// An implementation of <see cref="IFileSystem"/> providing read-write access to temporary storage in memory.
    /// </summary>
    public sealed class InMemoryFileSystem : IFileSystem
    {
        private readonly Dictionary<string, MemoryStream> writtenFiles = new();
        private readonly Dictionary<string, byte[]> readableFiles = new();

        public IEnumerable<string> Paths => readableFiles.Keys.Concat(writtenFiles.Keys);

        public static InMemoryFileSystem Of(string path, string content)
        {
            var result = new InMemoryFileSystem();
            result.SetText(path, content);
            return result;
        }

        private bool TryGetReadableFile(string path, [NotNullWhen(true)] out byte[]? bytes)
        {
            if (readableFiles.TryGetValue(path, out bytes))
                return true;

            if (!writtenFiles.TryGetValue(path, out var stream))
                return false;

            if (stream.CanWrite)
            {
                // can't read it while it's still open for writing
                return false;
            }

            bytes = stream.ToArray();
            readableFiles.Add(path, bytes);
            writtenFiles.Remove(path);
            return true;
        }

        public void Clear()
        {
            readableFiles.Clear();
            writtenFiles.Clear();
        }

        public byte[] GetBytes(string path) =>
            TryGetReadableFile(path, out var bytes) ? bytes : throw new FileNotFoundException("File not readable", path);

        public string GetText(string path) => Encoding.UTF8.GetString(GetBytes(path));

        public void SetBytes(string path, byte[] content)
        {
            readableFiles[path] = content;
            writtenFiles.Remove(path);
        }

        public void SetText(string path, string content) => SetBytes(path, Encoding.UTF8.GetBytes(content));

        public bool Exists(string path) => readableFiles.ContainsKey(path) || writtenFiles.ContainsKey(path);

        public Stream OpenForReading(string path) => new MemoryStream(GetBytes(path), false);

        public Stream OpenForWriting(string path)
        {
            var result = new MemoryStream();
            writtenFiles[path] = result;
            readableFiles.Remove(path);
            return result;
        }

        public Stream OpenForWritingAndReading(string path) => OpenForWriting(path);
    }

    /// <summary>
    /// An <see cref="IFileSystem"/> combinator that provides read-write access to a first file system, with
    /// reads of missing files falling back to a second file system.
    /// </summary>
    public sealed class OverlayFileSystem : IFileSystem
    {
        private readonly IFileSystem first, second;

        public OverlayFileSystem(IFileSystem first, IFileSystem second)
        {
            this.first = first;
            this.second = second;
        }

        public bool Exists(string path) => first.Exists(path) || second.Exists(path);

        public Stream OpenForReading(string path) => first.Exists(path) ? first.OpenForReading(path) : second.OpenForReading(path);

        public Stream OpenForWriting(string path) => first.OpenForWriting(path);

        public Stream OpenForWritingAndReading(string path) => first.OpenForWritingAndReading(path);
    }

    /// <summary>
    /// An <see cref="IFileSystem"/> combinator that provides a flattened, read-only view of specified
    /// subdirectories in an underlying file system.
    /// </summary>
    public sealed class LimitedFileSystem : IFileSystem
    {
        private readonly IFileSystem underlyingFileSystem;
        private readonly string[] searchDirs;

        private readonly Dictionary<string, string?> cachedPaths = new() { [""] = null, ["."] = null, [".."] = null };

        public LimitedFileSystem(string[] searchDirs)
            : this(PhysicalFileSystem.Instance, searchDirs)
        {
        }

        public LimitedFileSystem(IFileSystem underlyingFileSystem, string[] searchDirs)
        {
            this.underlyingFileSystem = underlyingFileSystem;
            this.searchDirs = Array.ConvertAll(searchDirs, Path.GetFullPath);
        }

        private string? GetFullPath(string path)
        {
            if (cachedPaths.TryGetValue(path, out var fullPath))
                return fullPath;

            if (path.Contains(Path.DirectorySeparatorChar) || path.Contains(Path.AltDirectorySeparatorChar))
                return null;

            var filename = Path.GetFileName(path);

            foreach (var d in searchDirs)
            {
                var candidate = Path.Combine(d, filename);

                if (underlyingFileSystem.Exists(candidate))
                {
                    cachedPaths.Add(path, candidate);
                    return candidate;
                }
            }

            cachedPaths.Add(path, null);
            return null;
        }

        public bool Exists(string path)
        {
            var fullPath = GetFullPath(path);
            return fullPath != null && underlyingFileSystem.Exists(fullPath);
        }

        public Stream OpenForReading(string path)
        {
            var fullPath = GetFullPath(path);

            if (fullPath == null)
                throw new FileNotFoundException("Path not allowed for reading");

            return underlyingFileSystem.OpenForReading(fullPath);
        }

        public Stream OpenForWriting(string path) => throw new NotSupportedException($"{nameof(LimitedFileSystem)} is read-only");

        public Stream OpenForWritingAndReading(string path) => throw new NotSupportedException($"{nameof(LimitedFileSystem)} is read-only");
    }
}
