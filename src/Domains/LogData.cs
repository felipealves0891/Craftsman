using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Craftsman.Domain;

public class LogData : IDisposable
{
    private readonly Stopwatch _stopwatch;

    public string Version { get; private set; }
    public string RequestMethod { get; private set; }
    public string RequestPath { get; private set; }
    public IDictionary<string, string> RequestQuery { get; private set; }
    public object RequestBody { get; private set; }
    
    public string Status { get; private set; } = "SUCCESS";
    public object? ResponseBody { get; private set; }
    public long EleapsedTime { get; private set; }
    public IDictionary<string, IDictionary<string, object>> Steps  { get; private set; }

    public object? Error { get; private set; }    

    public LogData(string method, string path, IDictionary<string, string> query, object body)
    {
        _stopwatch = Stopwatch.StartNew();
        RequestBody = body;
        RequestPath = path;
        RequestMethod = method;
        RequestQuery = query;
        Steps = new Dictionary<string, IDictionary<string, object>>();
        Version = Assembly.GetExecutingAssembly()?.GetName()?.Version?.ToString() ?? "0.0.0.0";
    }

    public IDictionary<string, object> AddStep(string stepName)
    {
        Steps[stepName] = new Dictionary<string, object>();
        return Steps[stepName];
    }

    public void FinishLogWithSuccess(object response)
    {
        if(_stopwatch.IsRunning)
            _stopwatch.Stop();

        Status = "Success";
        ResponseBody = response;
        var log = JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        Console.WriteLine(log);
    }
    
    public void FinishLogWithError(Exception exception)
    {
        if(_stopwatch.IsRunning)
            _stopwatch.Stop();

        Status = "Error";
        Error = MapError(exception);    
        var log = JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        Console.WriteLine(log);
    }

    private object MapError(Exception exception)
    {
        return new
        {
            Message = exception.Message,
            StackTrace = exception.StackTrace,
            InnerException = exception.InnerException is not null ? MapError(exception.InnerException) : null 
        };
    }

    public void Dispose()
    {
        FinishLogWithSuccess(null!);
    }
}