using System.Collections.Concurrent;
using System.Net;
using Microsoft.Extensions.Logging;

namespace Safarni.Infrastructure.Travel;

/// <summary>
/// Lightweight resilience for the external travel providers without adding a
/// new package dependency. Requests are retried only for network failures,
/// timeouts, HTTP 408/429 and 5xx responses. Repeated transient failures open
/// a short in-memory circuit so one unhealthy provider cannot continuously
/// consume every itinerary worker slot.
/// </summary>
internal static class ExternalHttpResilience
{
    private const int MaximumAttempts = 3;
    private static readonly TimeSpan MaximumRetryDelay =
        TimeSpan.FromSeconds(30);
    private static readonly TimeSpan CircuitOpenDuration =
        TimeSpan.FromSeconds(30);
    private static readonly ConcurrentDictionary<string, CircuitState>
        Circuits = new(StringComparer.OrdinalIgnoreCase);

    public static async Task<HttpResponseMessage> SendAsync(
        HttpClient client,
        Func<HttpRequestMessage> requestFactory,
        ILogger logger,
        string provider,
        string operation,
        CancellationToken cancellationToken)
    {
        var circuit = Circuits.GetOrAdd(
            provider,
            static _ => new CircuitState());

        if (!circuit.CanExecute(DateTime.UtcNow, out var retryAfter))
        {
            throw new HttpRequestException(
                $"The {provider} circuit is temporarily open. " +
                $"Retry after approximately {Math.Ceiling(retryAfter.TotalSeconds)} seconds.");
        }

        Exception? lastException = null;

        for (var attempt = 1; attempt <= MaximumAttempts; attempt++)
        {
            try
            {
                using var request = requestFactory();
                var response = await client.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);

                if (!IsTransient(response.StatusCode))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        circuit.ReportSuccess();
                    }

                    return response;
                }

                if (attempt == MaximumAttempts)
                {
                    circuit.ReportFailure(DateTime.UtcNow);
                    return response;
                }

                var delay = GetDelay(response, attempt);
                logger.LogWarning(
                    "{Provider} returned HTTP {StatusCode} during {Operation}. Retrying attempt {NextAttempt} after {DelaySeconds} seconds.",
                    provider,
                    response.StatusCode,
                    operation,
                    attempt + 1,
                    delay.TotalSeconds);

                response.Dispose();
                await Task.Delay(delay, cancellationToken);
            }
            catch (OperationCanceledException exception)
                when (!cancellationToken.IsCancellationRequested)
            {
                lastException = exception;

                if (attempt == MaximumAttempts)
                {
                    circuit.ReportFailure(DateTime.UtcNow);
                    throw;
                }

                var delay = GetDefaultDelay(attempt);
                logger.LogWarning(
                    "{Provider} timed out during {Operation}. Retrying attempt {NextAttempt} after {DelaySeconds} seconds.",
                    provider,
                    operation,
                    attempt + 1,
                    delay.TotalSeconds);
                await Task.Delay(delay, cancellationToken);
            }
            catch (HttpRequestException exception)
            {
                lastException = exception;

                if (attempt == MaximumAttempts)
                {
                    circuit.ReportFailure(DateTime.UtcNow);
                    throw;
                }

                var delay = GetDefaultDelay(attempt);
                logger.LogWarning(
                    exception,
                    "{Provider} network request failed during {Operation}. Retrying attempt {NextAttempt} after {DelaySeconds} seconds.",
                    provider,
                    operation,
                    attempt + 1,
                    delay.TotalSeconds);
                await Task.Delay(delay, cancellationToken);
            }
        }

        circuit.ReportFailure(DateTime.UtcNow);
        throw new HttpRequestException(
            $"{provider} request failed during {operation}.",
            lastException);
    }

    private static bool IsTransient(HttpStatusCode statusCode)
    {
        var numeric = (int)statusCode;
        return statusCode is HttpStatusCode.RequestTimeout or
                   HttpStatusCode.TooManyRequests ||
               numeric >= 500;
    }

    private static TimeSpan GetDelay(
        HttpResponseMessage response,
        int attempt)
    {
        var retryAfter = response.Headers.RetryAfter;
        var delay = retryAfter?.Delta;

        if (delay is null && retryAfter?.Date is { } date)
        {
            delay = date - DateTimeOffset.UtcNow;
        }

        var effectiveDelay =
            delay is null || delay <= TimeSpan.Zero
                ? GetDefaultDelay(attempt)
                : delay.Value;

        return effectiveDelay > MaximumRetryDelay
            ? MaximumRetryDelay
            : effectiveDelay;
    }

    private static TimeSpan GetDefaultDelay(int attempt)
    {
        return TimeSpan.FromSeconds(Math.Pow(2, attempt - 1));
    }

    private sealed class CircuitState
    {
        private readonly object _sync = new();
        private int _consecutiveFailures;
        private DateTime? _openUntil;

        public bool CanExecute(DateTime now, out TimeSpan retryAfter)
        {
            lock (_sync)
            {
                if (_openUntil is not { } openUntil)
                {
                    retryAfter = TimeSpan.Zero;
                    return true;
                }

                if (openUntil <= now)
                {
                    _openUntil = null;
                    _consecutiveFailures = 0;
                    retryAfter = TimeSpan.Zero;
                    return true;
                }

                retryAfter = openUntil - now;
                return false;
            }
        }

        public void ReportSuccess()
        {
            lock (_sync)
            {
                _consecutiveFailures = 0;
                _openUntil = null;
            }
        }

        public void ReportFailure(DateTime now)
        {
            lock (_sync)
            {
                _consecutiveFailures++;
                if (_consecutiveFailures >= 5)
                {
                    _openUntil = now.Add(CircuitOpenDuration);
                }
            }
        }
    }
}
