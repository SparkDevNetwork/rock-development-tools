using System.Net;

namespace SparkDevNetwork.Rock.Plugin.Tool;

static class ExtensionMethods
{
    /// <summary>
    /// Downloads the URI into the specified stream while providing progress
    /// feedback.
    /// </summary>
    /// <param name="client">The <see cref="HttpClient"/> to execute the operation on.</param>
    /// <param name="requestUri">The URI to be downloaded</param>
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
}
