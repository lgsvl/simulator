using System.Collections.Generic;
using System.Threading.Tasks;
using Docker.DotNet.Models;
using System;
using System.IO;
using System.Threading;

namespace Docker.DotNet
{
    public interface IImageOperations
    {
        /// <summary>
        /// Retrieves a list of the images on the server.
        /// </summary>
        /// <param name="parameters">Specifics of how to perform the operation.</param>
        /// <param name="cancellationToken">When triggered, the operation will stop at the next available time, if possible.</param>
        /// <remarks>
        /// The equivalent commands in the Docker CLI are <c>docker images</c> and <c>docker image ls</c>.
        /// </remarks>
        /// <exception cref="ArgumentNullException">One or more of the inputs was <see langword="null"/>.</exception>
        /// <exception cref="DockerApiException">The input is invalid or the daemon experienced an error.</exception>
        /// <exception cref="HttpRequestException">The request failed due to an underlying issue such as network connectivity, DNS failure, server certificate validation or timeout.</exception>
        Task<IList<ImagesListResponse>> ListImagesAsync(ImagesListParameters parameters, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Builds an image from a tar archive that contains a Dockerfile.
        /// </summary>
        /// <param name="parameters">Specifics of how to perform the operation.</param>
        /// <param name="cancellationToken">When triggered, the operation will stop at the next available time, if possible.</param>
        /// <remarks>
        /// The Dockerfile specifies how the image is built from the tar archive. It is typically in the
        /// archive's root, but can be at a different path or have a different name by setting the <c>dockerfile</c>
        /// parameter. See the Dockerfile reference for more information.
        /// <br/>
        /// The Docker daemon performs a preliminary validation of the Dockerfile before starting the build,
        /// and returns an error if the syntax is incorrect. After that, each instruction is run one-by-one until
        /// the ID of the new image is output.
        /// <br/>
        /// The build is canceled if the client drops the connection by quitting or being killed.
        /// <br/>
        /// The equivalent commands in the Docker CLI are <c>docker build</c> and <c>docker image build</c>.
        /// </remarks>
        /// <exception cref="ArgumentNullException">One or more of the inputs was <see langword="null"/>.</exception>
        /// <exception cref="DockerApiException">The input is invalid or the daemon experienced an error.</exception>
        /// <exception cref="HttpRequestException">The request failed due to an underlying issue such as network connectivity, DNS failure, server certificate validation or timeout.</exception>
        Task<Stream> BuildImageFromDockerfileAsync(Stream contents, ImageBuildParameters parameters, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Creates an image by either pulling it from a registry or importing it.
        /// </summary>
        /// <param name="parameters">Specifics of how to perform the operation.</param>
        /// <param name="authConfig">Information for authenticating with the registry.</param>
        /// <param name="progress">Provides a delegate the receives progress updates while the operation is underway.</param>
        /// <param name="cancellationToken">When triggered, the operation will stop at the next available time, if possible.</param>
        /// <remarks>
        /// The equivalent commands in the Docker CLI are <c>docker pull</c> and <c>docker image pull</c>.
        /// <exception cref="ArgumentNullException">One or more of the inputs was <see langword="null"/>.</exception>
        /// <exception cref="DockerApiException">The repository does not exist, the repository only allows read access for the
        /// current auth status, the input is invalid, or the daemon experienced an error.</exception>
        /// </remarks>
        /// <exception cref="HttpRequestException">The request failed due to an underlying issue such as network connectivity, DNS failure, server certificate validation or timeout.</exception>
        Task CreateImageAsync(ImagesCreateParameters parameters, AuthConfig authConfig, IProgress<JSONMessage> progress, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Creates an image by either pulling it from a registry or importing it.
        /// </summary>
        /// <param name="parameters">Specifics of how to perform the operation.</param>
        /// <param name="authConfig">Information for authenticating with the registry.</param>
        /// <param name="headers">Additional headers to include in the HTTP request to the registry.</param>
        /// <param name="progress">Provides a delegate the receives progress updates while the operation is underway.</param>
        /// <param name="cancellationToken">When triggered, the operation will stop at the next available time, if possible.</param>
        /// <remarks>
        /// The equivalent commands in the Docker CLI are <c>docker pull</c> and <c>docker image pull</c>.
        /// <exception cref="ArgumentNullException">One or more of the inputs was <see langword="null"/>.</exception>
        /// <exception cref="DockerApiException">The repository does not exist, the repository only allows read access for the
        /// current auth status, the input is invalid, or the daemon experienced an error.</exception>
        /// </remarks>
        /// <exception cref="HttpRequestException">The request failed due to an underlying issue such as network connectivity, DNS failure, server certificate validation or timeout.</exception>
        Task CreateImageAsync(ImagesCreateParameters parameters, AuthConfig authConfig, IDictionary<string, string> headers, IProgress<JSONMessage> progress, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Creates an image by importing it from a stream.
        /// </summary>
        /// <param name="parameters">Specifics of how to perform the operation.</param>
        /// <param name="imageStream">A readable stream that contains the image to import.</param>
        /// <param name="authConfig">Information for authenticating with the registry.</param>
        /// <param name="progress">Provides a delegate the receives progress updates while the operation is underway.</param>
        /// <param name="cancellationToken">When triggered, the operation will stop at the next available time, if possible.</param>
        /// <remarks>
        /// The equivalent commands in the Docker CLI are <c>docker pull</c>, <c>docker image pull</c>, and <c>docker import</c>.
        /// <exception cref="ArgumentNullException">One or more of the inputs was <see langword="null"/>.</exception>
        /// <exception cref="DockerApiException">The repository does not exist, the repository only allows read access for the
        /// current auth status, the input is invalid, or the daemon experienced an error.</exception>
        /// </remarks>
        /// <exception cref="HttpRequestException">The request failed due to an underlying issue such as network connectivity, DNS failure, server certificate validation or timeout.</exception>
        Task CreateImageAsync(ImagesCreateParameters parameters, Stream imageStream, AuthConfig authConfig, IProgress<JSONMessage> progress, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Creates an image by importing it from a stream.
        /// </summary>
        /// <param name="parameters">Specifics of how to perform the operation.</param>
        /// <param name="imageStream">A readable stream that contains the image to import.</param>
        /// <param name="authConfig">Information for authenticating with the registry.</param>
        /// <param name="headers">Additional headers to include in the HTTP request to the registry.</param>
        /// <param name="progress">Provides a delegate the receives progress updates while the operation is underway.</param>
        /// <param name="cancellationToken">When triggered, the operation will stop at the next available time, if possible.</param>
        /// <remarks>
        /// The equivalent commands in the Docker CLI are <c>docker pull</c>, <c>docker image pull</c>, and <c>docker import</c>.
        /// <exception cref="ArgumentNullException">One or more of the inputs was <see langword="null"/>.</exception>
        /// <exception cref="DockerApiException">The repository does not exist, the repository only allows read access for the
        /// current auth status, the input is invalid, or the daemon experienced an error.</exception>
        /// </remarks>
        /// <exception cref="HttpRequestException">The request failed due to an underlying issue such as network connectivity, DNS failure, server certificate validation or timeout.</exception>
        Task CreateImageAsync(ImagesCreateParameters parameters, Stream imageStream, AuthConfig authConfig, IDictionary<string, string> headers, IProgress<JSONMessage> progress, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Retrieves low-level information about an image.
        /// </summary>
        /// <param name="name">An image name or ID.</param>
        /// <param name="cancellationToken">When triggered, the operation will stop at the next available time, if possible.</param>
        /// <remarks>
        /// The equivalent commands in the Docker CLI are <c>docker inspect</c> and <c>docker image inspect</c>.
        /// </remarks>
        /// <exception cref="ArgumentNullException">One or more of the inputs was <see langword="null"/>.</exception>
        /// <exception cref="DockerImageNotFoundException">No such image was found.</exception>
        /// <exception cref="DockerApiException">The input is invalid or the daemon experienced an error.</exception>
        /// <exception cref="HttpRequestException">The request failed due to an underlying issue such as network connectivity, DNS failure, server certificate validation or timeout.</exception>
        Task<ImageInspectResponse> InspectImageAsync(string name, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Gets the "history" (parent layers) of an image.
        /// </summary>
        /// <param name="name">An image name or ID.</param>
        /// <param name="cancellationToken">When triggered, the operation will stop at the next available time, if possible.</param>
        /// <remarks>
        /// The equivalent commands in the Docker CLI are <c>docker history</c> and <c>docker image history</c>.
        /// </remarks>
        /// <exception cref="ArgumentNullException">One or more of the inputs was <see langword="null"/>.</exception>
        /// <exception cref="DockerImageNotFoundException">No such image was found.</exception>
        /// <exception cref="DockerApiException">The input is invalid or the daemon experienced an error.</exception>
        /// <exception cref="HttpRequestException">The request failed due to an underlying issue such as network connectivity, DNS failure, server certificate validation or timeout.</exception>
        Task<IList<ImageHistoryResponse>> GetImageHistoryAsync(string name, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Pushes an image to a registry.
        /// </summary>
        /// <param name="name">An image name or ID.</param>
        /// <param name="parameters">Specifics of how to perform the operation.</param>
        /// <param name="authConfig">Information for authenticating with the registry.</param>
        /// <param name="progress">Provides a delegate the receives progress updates while the operation is underway.</param>
        /// <param name="cancellationToken">When triggered, the operation will stop at the next available time, if possible.</param>
        /// <remarks>
        /// If you wish to push an image on to a private registry, that image must already have a tag which
        /// references that registry. For example {registry.example.com/myimage:latest}.
        /// <br/>
        /// The push is cancelled if the HTTP connection is closed.
        /// <br/>
        /// The equivalent commands in the Docker CLI are <c>docker push</c> and <c>docker image push</c>.
        /// </remarks>
        /// <exception cref="ArgumentNullException">One or more of the inputs was <see langword="null"/>.</exception>
        /// <exception cref="DockerImageNotFoundException">No such image was found.</exception>
        /// <exception cref="DockerApiException">The input is invalid or the daemon experienced an error.</exception>
        /// <exception cref="HttpRequestException">The request failed due to an underlying issue such as network connectivity, DNS failure, server certificate validation or timeout.</exception>
        Task PushImageAsync(string name, ImagePushParameters parameters, AuthConfig authConfig, IProgress<JSONMessage> progress, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Tags an image so that it becomes part of a registry.
        /// </summary>
        /// <param name="name">An image name or ID.</param>
        /// <param name="parameters">Specifics of how to perform the operation.</param>
        /// <param name="cancellationToken">When triggered, the operation will stop at the next available time, if possible.</param>
        /// <remarks>
        /// The equivalent commands in the Docker CLI are <c>docker tag</c> and <c>docker image tag</c>.
        /// </remarks>
        /// <param name="name">Image name or id.</param>
        /// <exception cref="ArgumentNullException">One or more of the inputs was <see langword="null"/>.</exception>
        /// <exception cref="DockerImageNotFoundException">No such image was found.</exception>
        /// <exception cref="DockerApiException">There was a conflict, or the input is invalid, or the daemon experienced an error.</exception>
        /// <exception cref="HttpRequestException">The request failed due to an underlying issue such as network connectivity, DNS failure, server certificate validation or timeout.</exception>
        Task TagImageAsync(string name, ImageTagParameters parameters, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Removes an image, along with any untagged parent images that were referenced by that image.
        /// </summary>
        /// <param name="name">An image name or ID.</param>
        /// <param name="parameters">Specifics of how to perform the operation.</param>
        /// <param name="cancellationToken">When triggered, the operation will stop at the next available time, if possible.</param>
        /// <remarks>
        /// Images can't be removed if they have descendant images, are being used by a running container,
        /// or are being used by a build.
        /// <br/>
        /// The equivalent commands in the Docker CLI are <c>docker inspect</c> and <c>docker image inspect</c>.
        /// </remarks>
        /// <param name="name">Image name or id.</param>
        /// <exception cref="ArgumentNullException">One or more of the inputs was <see langword="null"/>.</exception>
        /// <exception cref="DockerImageNotFoundException">No such image was found.</exception>
        /// <exception cref="DockerApiException">There was a conflict, or the input is invalid, or the daemon experienced an error.</exception>
        /// <exception cref="HttpRequestException">The request failed due to an underlying issue such as network connectivity, DNS failure, server certificate validation or timeout.</exception>
        Task<IList<IDictionary<string, string>>> DeleteImageAsync(string name, ImageDeleteParameters parameters, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Searchs for an image on Docker Hub.
        /// </summary>
        /// <param name="parameters">Specifics of how to perform the operation.</param>
        /// <param name="cancellationToken">When triggered, the operation will stop at the next available time, if possible.</param>
        /// <remarks>
        /// The equivalent command in the Docker CLI is <c>docker search</c>.
        /// </remarks>
        /// <exception cref="ArgumentNullException">One or more of the inputs was <see langword="null"/>.</exception>
        /// <exception cref="DockerApiException">There was a conflict, or the input is invalid, or the daemon experienced an error.</exception>
        /// <exception cref="HttpRequestException">The request failed due to an underlying issue such as network connectivity, DNS failure, server certificate validation or timeout.</exception>
        Task<IList<ImageSearchResponse>> SearchImagesAsync(ImagesSearchParameters parameters, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Deletes unused images.
        /// </summary>
        /// <param name="parameters">Specifics of how to perform the operation.</param>
        /// <param name="cancellationToken">When triggered, the operation will stop at the next available time, if possible.</param>
        /// <remarks>
        /// The equivalent commands in the Docker CLI are <c>docker rmi</c> and <c>docker image rm</c>.
        /// </remarks>
        /// <exception cref="ArgumentNullException">One or more of the inputs was <see langword="null"/>.</exception>
        /// <exception cref="DockerApiException">There was a conflict, or the input is invalid, or the daemon experienced an error.</exception>
        /// <exception cref="HttpRequestException">The request failed due to an underlying issue such as network connectivity, DNS failure, server certificate validation or timeout.</exception>
        Task<ImagesPruneResponse> PruneImagesAsync(ImagesPruneParameters parameters = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Create a new image from a container.
        /// </summary>
        /// <param name="parameters">Specifics of how to perform the operation.</param>
        /// <param name="cancellationToken">When triggered, the operation will stop at the next available time, if possible.</param>
        /// <remarks>
        /// The equivalent command in the Docker CLI is <c>docker commit</c>.
        /// </remarks>
        /// <exception cref="ArgumentNullException">One or more of the inputs was <see langword="null"/>.</exception>
        /// <exception cref="DockerApiException">There was a conflict, or the input is invalid, or the daemon experienced an error.</exception>
        /// <exception cref="DockerContainerNotFoundException">No such container was found.</exception>
        /// <exception cref="HttpRequestException">The request failed due to an underlying issue such as network connectivity, DNS failure, server certificate validation or timeout.</exception>
        Task<CommitContainerChangesResponse> CommitContainerChangesAsync(CommitContainerChangesParameters parameters, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Exports an image and its associated metadata as a tarball.
        /// </summary>
        /// <param name="name">An image name or ID.</param>
        /// <param name="cancellationToken">When triggered, the operation will stop at the next available time, if possible.</param>
        /// <remarks>
        /// The equivalent command in the Docker CLI is <c>docker export</c>.
        /// </remarks>
        /// <param name="name">Image name or ID.</param>
        /// <seealso cref="SaveImagesAsync(string[], CancellationToken)"/>
        /// <exception cref="ArgumentNullException">One or more of the inputs was <see langword="null"/>.</exception>
        /// <exception cref="DockerApiException">There was a conflict, or the input is invalid, or the daemon experienced an error.</exception>
        /// <exception cref="DockerImageNotFoundException">No such image was found.</exception>
        /// <exception cref="HttpRequestException">The request failed due to an underlying issue such as network connectivity, DNS failure, server certificate validation or timeout.</exception>
        Task<Stream> SaveImageAsync(string name, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Exports multiple images and their associated metadata to a single tarball.
        /// </summary>
        /// <param name="names">An array of image names and IDs.</param>
        /// <param name="cancellationToken">When triggered, the operation will stop at the next available time, if possible.</param>
        /// <remarks>
        /// For each value of the <paramref name="names"/> parameter: if it is a specific name and tag (e.g. <c>ubuntu:latest</c>),
        /// then only that image (and its parents) are returned; if it is an image ID, similarly only that
        /// image (and its parents) are returned and there would be no names referenced in the 'repositories'
        /// file for this image ID.
        /// <br/>
        /// The equivalent command in the Docker CLI is <c>docker export</c>.
        /// </remarks>
        /// <param name="names">Image names to filter by.</param>
        /// <seealso cref="SaveImageAsync(string, CancellationToken)"/>
        /// <exception cref="ArgumentNullException">One or more of the inputs was <see langword="null"/>.</exception>
        /// <exception cref="DockerApiException">There was a conflict, or the input is invalid, or the daemon experienced an error.</exception>
        /// <exception cref="DockerImageNotFoundException">No such image was found.</exception>
        /// <exception cref="HttpRequestException">The request failed due to an underlying issue such as network connectivity, DNS failure, server certificate validation or timeout.</exception>
        Task<Stream> SaveImagesAsync(string[] names, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Loads a set of images and tags into a Docker repository.
        /// </summary>
        /// <param name="parameters">Specifics of how to perform the operation.</param>
        /// <param name="imageStream">A readable stream that contains the images and tags to import.</param>
        /// <param name="progress">Provides a delegate the receives progress updates while the operation is underway.</param>
        /// <param name="cancellationToken">When triggered, the operation will stop at the next available time, if possible.</param>
        /// <remarks>
        /// The equivalent command in the Docker CLI is <c>docker load</c>.
        /// </remarks>
        /// <exception cref="ArgumentNullException">One or more of the inputs was <see langword="null"/>.</exception>
        /// <exception cref="DockerApiException">There was a conflict, or the input is invalid, or the daemon experienced an error.</exception>
        /// <exception cref="HttpRequestException">The request failed due to an underlying issue such as network connectivity, DNS failure, server certificate validation or timeout.</exception>
        Task LoadImageAsync(ImageLoadParameters parameters, Stream imageStream, IProgress<JSONMessage> progress, CancellationToken cancellationToken = default(CancellationToken));
    }
}