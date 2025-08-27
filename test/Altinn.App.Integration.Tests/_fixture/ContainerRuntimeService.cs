// Requires NuGet: Docker.DotNet (>= 3.125.15) or similar
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace Altinn.App.Integration.Tests;

public static class ContainerRuntimeService
{
    private static DockerClient? _client;
    private static string? _cachedHostIp; // process-lifetime cache
    private static readonly SemaphoreSlim _lock = new(1, 1);

    // You can change this to "alpine:3" if you prefer; busybox is tiny & multi-arch.
    private const string ProbeImage = "busybox:1.36";

    /// <summary>
    /// Returns an IPv4 address that containers can use to reach the host.
    /// </summary>
    public static async Task<string> GetHostIP(CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrEmpty(_cachedHostIp))
            return _cachedHostIp;

        await _lock.WaitAsync(cancellationToken);
        try
        {
            _client ??= await CreateDockerClient(cancellationToken);
            await EnsureImage(ProbeImage, cancellationToken);

            // One-shot probe container that prints a single IPv4 and exits.
            // Strategy:
            // 1) Try common internal hostnames (Docker Desktop, Podman, Rancher Desktop).
            // 2) Fallback: parse default gateway from /proc/net/route (works on Linux engines).
            var probeScript = """
                set -e
                try_host() {
                    # use ping to avoid depending on getent; parse 'PING name (A.B.C.D)'
                    ip=$(ping -c1 -W1 "$1" 2>/dev/null | sed -n 's/^PING [^(]*(\([0-9.]*\)).*/\1/p')
                    if [ -n "$ip" ]; then echo "$ip"; return 0; fi
                    return 1
                }
                for n in host.docker.internal host.containers.internal host.rancher-desktop.internal; do
                    try_host "$n" && exit 0
                done
                # default route (Gateway hex in column 3 of /proc/net/route on the 0.0.0.0 row)
                g=$(awk '$2=="00000000" {print $3}' /proc/net/route | head -n1)
                if [ -n "$g" ]; then
                    printf "%d.%d.%d.%d\n" 0x${g:6:2} 0x${g:4:2} 0x${g:2:2} 0x${g:0:2}
                fi
                """.ReplaceLineEndings("\n");

            // Create container
            var create = await _client.Containers.CreateContainerAsync(
                new CreateContainerParameters
                {
                    Image = ProbeImage,
                    Tty = false, // disable TTY to allow proper multiplexed log reading
                    Cmd = ["sh", "-lc", probeScript],
                    // default network is fine for a simple, portable probe
                    HostConfig = new HostConfig
                    {
                        AutoRemove = false, // we'll remove explicitly after reading logs
                    },
                },
                cancellationToken
            );

            string id = create.ID;
            try
            {
                await _client.Containers.StartContainerAsync(id, null, cancellationToken);
                // Wait for it to finish
                await _client.Containers.WaitContainerAsync(id, cancellationToken);

                // Grab logs (stdout)
                using var logs = await _client.Containers.GetContainerLogsAsync(
                    id,
                    false,
                    new ContainerLogsParameters
                    {
                        ShowStdout = true,
                        ShowStderr = true,
                        Timestamps = false,
                    },
                    cancellationToken
                );

                var (stdout, stderr) = await logs.ReadOutputToEndAsync(cancellationToken);
                var output = stdout + stderr;

                var ip = ExtractFirstIPv4(output);
                if (ip is null)
                    throw new InvalidOperationException($"Unable to determine host IPv4 from probe: {output}");

                _cachedHostIp = ip;
                return ip;
            }
            finally
            {
                try
                {
                    await _client.Containers.RemoveContainerAsync(
                        id,
                        new ContainerRemoveParameters { Force = true },
                        cancellationToken
                    );
                }
                catch
                { /* ignore */
                }
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    // --- helpers -------------------------------------------------------------

    private static string? ExtractFirstIPv4(string text)
    {
        var m = Regex.Match(text, @"\b(?:(?:25[0-5]|2[0-4]\d|[01]?\d?\d)(?:\.|$)){4}\b");
        return m.Success ? m.Value : null;
    }

    private static async Task EnsureImage(string image, CancellationToken cancellationToken)
    {
        Assert.NotNull(_client);
        try
        {
            await _client.Images.InspectImageAsync(image, cancellationToken); // present → return
            return;
        }
        catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // not present → pull
        }

        var parts = image.Split(':', 2);
        var fromImage = parts[0];
        var tag = parts.Length > 1 ? parts[1] : "latest";

        await _client.Images.CreateImageAsync(
            new ImagesCreateParameters { FromImage = fromImage, Tag = tag },
            authConfig: null,
            progress: new Progress<JSONMessage>(_ => { }),
            cancellationToken: cancellationToken
        );
    }

    private static async Task<DockerClient> CreateDockerClient(CancellationToken cancellationToken)
    {
        var endpoints = CandidateDockerApiEndpoints(cancellationToken);
        List<Exception> errors = new();

        await foreach (var uri in endpoints)
        {
            try
            {
                var cfg = new DockerClientConfiguration(uri);
                var client = cfg.CreateClient();
                // quick ping to validate
                await client.System.PingAsync();
                return client;
            }
            catch (Exception ex)
            {
                errors.Add(new InvalidOperationException($"Failed connecting to {uri}", ex));
            }
        }

        throw new AggregateException(
            "No Docker-compatible API socket found. Ensure Docker or Podman (Docker API) is running.",
            errors
        );
    }

    private static async IAsyncEnumerable<Uri> CandidateDockerApiEndpoints(
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        // 1) Respect DOCKER_HOST if set
        var env = Environment.GetEnvironmentVariable("DOCKER_HOST");
        if (!string.IsNullOrWhiteSpace(env) && Uri.TryCreate(env, UriKind.Absolute, out var fromEnv))
            yield return fromEnv;

        // 2) Platform-specific defaults
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            yield return new Uri("npipe://./pipe/docker_engine");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // podman-mac-helper: forwards Docker API at /var/run/docker.sock
            var ddCompat = "/var/run/docker.sock"; // /private/var/run/docker.sock also works
            if (File.Exists(ddCompat))
                yield return new Uri($"unix://{ddCompat}");

            // Docker Desktop’s user socket (sometimes present)
            var userSock = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".docker",
                "run",
                "docker.sock"
            );
            if (File.Exists(userSock))
                yield return new Uri($"unix://{userSock}");

            // Ask podman for the exact host-side socket path
            var fromCli = await TryReadPodmanSocketPath(cancellationToken);
            if (!string.IsNullOrWhiteSpace(fromCli) && File.Exists(fromCli))
                yield return new Uri($"unix://{fromCli}");

            yield break;
        }
        else
        {
            // Docker Engine / Docker Desktop on Linux
            yield return new Uri("unix:///var/run/docker.sock");

            // Podman (rootless, socket-activated)
            var uid = await GetUnixUid(cancellationToken);
            if (uid != null)
                yield return new Uri($"unix:///run/user/{uid}/podman/podman.sock");

            // Podman (rootful)
            yield return new Uri("unix:///run/podman/podman.sock");
        }
    }

    private static async Task<uint?> GetUnixUid(CancellationToken cancellationToken)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return null;

        await foreach (var line in File.ReadLinesAsync("/proc/self/status", cancellationToken))
        {
            // Format: "Uid:\t<real>\t<effective>\t<saved>\t<fs>"
            if (line.StartsWith("Uid:"))
            {
                var parts = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                // parts[1] = real UID
                if (parts.Length >= 2 && uint.TryParse(parts[1], out var real))
                    return real;
            }
        }

        throw new InvalidOperationException("Could not determine UID");
    }

    private static async Task<string?> TryReadPodmanSocketPath(CancellationToken cancellationToken)
    {
        var result = await new Command(
            "podman",
            "machine inspect --format {{.ConnectionInfo.PodmanSocket.Path}}",
            WorkingDirectory: ModuleInitializer.GetSolutionDirectory(),
            ThrowOnNonZero: false,
            CancellationToken: cancellationToken
        );
        if (!result.IsSuccess)
            return null;
        var stdout = result.StdOut.Trim();
        return string.IsNullOrWhiteSpace(stdout) ? null : stdout;
    }
}
