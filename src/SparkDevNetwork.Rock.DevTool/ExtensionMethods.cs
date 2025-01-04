using System.IO.Abstractions;
using System.Net;
using System.Security.Cryptography;

namespace SparkDevNetwork.Rock.DevTool;

static class ExtensionMethods
{
    /// <summary>
    /// Downloads the URI into the specified stream while providing progress
    /// feedback.
    /// </summary>
    /// <param name="client">The <see cref="HttpClient"/> to execute the operation on.</param>
    /// <param name="requestUri">The URI to be downloaded.</param>
    /// <param name="destination">The <see cref="Stream"/> to store the downloaded data into.</param>
    /// <param name="progress">The callback for progress reporting as a value between <c>0</c> and <c>1</c>.</param>
    /// <param name="cancellationToken">The token monitored for cancellation requests.</param>
    /// <returns>A <see cref="Task"/> that indicates when the operation has completed.</returns>
    public static async Task DownloadAsync( this HttpClient client, string requestUri, Stream destination, Action<float> progress, CancellationToken cancellationToken = default )
    {
        using var response = await client.GetAsync( requestUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken ).ConfigureAwait( false );

        if ( response.StatusCode == HttpStatusCode.NotFound )
        {
            throw new Exception( $"Unable to download {requestUri}, item not found." );
        }
        else if ( response.StatusCode != HttpStatusCode.OK )
        {
            throw new Exception( $"Unable to download {requestUri}, status code = {response.StatusCode}." );
        }

        var contentLength = response.Content.Headers.ContentLength;

        using var readStream = await response.Content.ReadAsStreamAsync( cancellationToken );

        if ( !contentLength.HasValue )
        {
            await readStream.CopyToAsync( destination, cancellationToken );
        }
        else
        {
            var buffer = new byte[81920];
            long totalBytesRead = 0;
            int bytesRead;

            while ( ( bytesRead = await readStream.ReadAsync( buffer, cancellationToken ).ConfigureAwait( false ) ) != 0 )
            {
                await destination.WriteAsync( buffer.AsMemory( 0, bytesRead ), cancellationToken ).ConfigureAwait( false );
                totalBytesRead += bytesRead;
                progress( ( float ) totalBytesRead / contentLength.Value );
            }
        }

        progress( 1 );
    }

    /// <summary>
    /// Reads the file into the specified stream while providing progress
    /// feedback.
    /// </summary>
    /// <param name="path">TThe path to the file to read.</param>
    /// <param name="destination">The <see cref="Stream"/> to store the downloaded data into.</param>
    /// <param name="progress">The callback for progress reporting as a value between <c>0</c> and <c>1</c>.</param>
    /// <param name="cancellationToken">The token monitored for cancellation requests.</param>
    /// <returns>A <see cref="Task"/> that indicates when the operation has completed.</returns>
    public static async Task ReadFileAsync( this IFile file, string requestUri, Stream destination, Action<float> progress, CancellationToken cancellationToken = default )
    {
        using var readStream = file.OpenRead( requestUri );
        var totalSize = file.FileSystem.FileInfo.New( requestUri ).Length;

        var buffer = new byte[81920];
        long totalBytesRead = 0;
        int bytesRead;

        while ( ( bytesRead = await readStream.ReadAsync( buffer, cancellationToken ).ConfigureAwait( false ) ) != 0 )
        {
            await destination.WriteAsync( buffer.AsMemory( 0, bytesRead ), cancellationToken ).ConfigureAwait( false );
            totalBytesRead += bytesRead;
            progress( ( float ) totalBytesRead / totalSize );
        }

        progress( 1 );
    }

    /// <summary>
    /// Calculates the SHA1 hash of a files contents and returns that hash as a
    /// hexadecimal string without spacing or "0x" prefix.
    /// </summary>
    /// <param name="file">The object that provides access to the file system.
    /// <param name="filename">The path to the filename to read.</param>
    /// <returns>A SHA1 hash in hexadecimal notation.</returns>
    public static string CalculateHexHash( this IFile file, string filename )
    {
        using var stream = file.OpenRead( filename );

        return stream.CalculateHexHash();
    }

    /// <summary>
    /// Calculates the SHA1 hash of a stream and returns that hash as a
    /// hexadecimal string without spacing or "0x" prefix.
    /// </summary>
    /// <param name="stream">The stream to read.</param>
    /// <returns>A SHA1 hash in hexadecimal notation.</returns>
    public static string CalculateHexHash( this Stream stream )
    {
        using var sha1 = SHA1.Create();
        var hash = sha1.ComputeHash( stream );

        return Convert.ToHexString( hash );
    }

    /// <summary>
    /// Checks if the specified directory is empty. This will also return
    /// <c>true</c> if the directory does not exist.
    /// </summary>
    /// <param name="directory">The directory object that handles file system access.</param>
    /// <param name="path">The path to check.</param>
    /// <returns><c>true</c> if the directory does not exist or has no files or directories.</returns>
    public static bool IsEmpty( this IDirectory directory, string path )
    {
        if ( !directory.Exists( path ) )
        {
            return true;
        }

        return !directory.EnumerateFiles( path ).Any() && !directory.EnumerateDirectories( path ).Any();
    }

    /// <summary>
    /// Normalize the path separators in the string to match the one used
    /// by the native operating system.
    /// </summary>
    /// <param name="pathObject">The path object that handles file system access.</param>
    /// <param name="path">The path to be normalized.</param>
    /// <returns>The normalized version of the path.</returns>
    public static string NormalizePathSeperator( this IPath pathObject, string path )
    {
        if ( pathObject.DirectorySeparatorChar == '/' )
        {
            return path.Replace( '\\', '/' );
        }
        else
        {
            return path.Replace( '/', pathObject.DirectorySeparatorChar );
        }
    }

    /// <summary>
    /// Returns a string that can be used to display to the individual. This
    /// converts the path to be relative to the current directory and also
    /// normalizes the directory separators.
    /// </summary>
    /// <param name="pathObject">The path object that handles file system access.</param>
    /// <param name="path">The path to make friendly.</param>
    /// <returns>A new string that represents the friendly path.</returns>
    public static string GetFriendlyPath( this IPath pathObject, string path )
    {
        var currentDirectory = pathObject.FileSystem.Directory.GetCurrentDirectory();
        var relativePath = pathObject.GetRelativePath( currentDirectory, path );

        return pathObject.NormalizePathSeperator( relativePath );
    }

    /// <summary>
    /// Creates a string that represents the Guid in the format used by Visual
    /// Studio Solution files.
    /// </summary>
    /// <param name="guid">The guid to be converted.</param>
    /// <returns>A new string that represents the guid.</returns>
    public static string ToSolution( this Guid guid )
    {
        return "{" + guid.ToString().ToUpper() + "}";
    }
}
