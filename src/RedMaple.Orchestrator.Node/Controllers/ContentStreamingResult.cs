
using System.Collections.Concurrent;
using System.Text;
using System.Threading.Tasks.Dataflow;

namespace RedMaple.Orchestrator.Node.Controllers
{
    public class ContentStreamingResult : ActionResult, IProgress<string>
    {
        private readonly BufferBlock<string> _pipe = new BufferBlock<string>();
        private readonly CancellationToken _cancellationToken;

        public CancellationTokenSource CancellationTokenSource { get; } = new();

        public ContentStreamingResult(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
        }

        public override async Task ExecuteResultAsync(ActionContext context)
        {
            var httpContext = context.HttpContext;
            httpContext.Response.ContentType = "text/plain";

            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(CancellationTokenSource.Token, _cancellationToken);
                await using var writer = new StreamWriter(httpContext.Response.Body, Encoding.UTF8);
                while (!cts.IsCancellationRequested)
                {
                    var line = await _pipe.ReceiveAsync(cts.Token);
                    await writer.WriteLineAsync(line);
                    await writer.FlushAsync();
                }
            }
            catch (OperationCanceledException) { }
            await httpContext.Response.CompleteAsync();
        }

        public void Report(string value)
        {
            _pipe.Post(value);
        }
    }
}
