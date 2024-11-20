using System.Text.Json;

namespace RedMaple.Orchestrator.Infrastructure
{
    public abstract class DocumentStore<T>
    {
        private List<T>? _items;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);
        private readonly static JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions() { WriteIndented = true };

        protected abstract string SaveFilePath { get; }

        protected async Task<List<T>> LoadAsync()
        {
            await _semaphore.WaitAsync();

            try
            { 
                if (_items is null)
                {
                    try
                    {
                        var json = await File.ReadAllTextAsync(SaveFilePath);
                        _items = JsonSerializer.Deserialize<List<T>>(json) ?? new();
                    }
                    catch (Exception) 
                    {
                        try
                        {
                            var pending = SaveFilePath + ".pending";
                            var json = await File.ReadAllTextAsync(pending);
                            _items = JsonSerializer.Deserialize<List<T>>(json) ?? new();
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
                if(_items is null)
                {
                    return new();
                }
                return new List<T>(_items);
            }
            finally
            {
                _semaphore.Release();
            }
        }
        protected async Task CommitAsync(List<T> items)
        {
            await _semaphore.WaitAsync();
            try
            {
                _items = items;
            }
            finally
            {
                ScheduleCommitToDisk();
                _semaphore.Release();
            }
        }

        private Thread? _commitToDiskThread;
        private readonly ManualResetEventSlim _wakeCommitToDiskEvent = new(false);
        private void ScheduleCommitToDisk()
        {
            if(_commitToDiskThread is null)
            {
                _commitToDiskThread = new Thread(async () =>
                {
                    while (true)
                    {
                        try
                        {
                            Thread.Sleep(TimeSpan.FromSeconds(1));
                            await CommitToDiskAsync();
                            _wakeCommitToDiskEvent.Wait();
                            _wakeCommitToDiskEvent.Reset();
                        }
                        catch { }
                    }
                })
                {
                    IsBackground = true,
                    Name = "ScheduleCommitToDisk_" + typeof(T).Name
                };
                _commitToDiskThread.Start();
            }
            else
            {
                _wakeCommitToDiskEvent.Set();
            }
        }

        private async Task CommitToDiskAsync()
        {
            var pending = SaveFilePath + ".pending";

            await _semaphore.WaitAsync();
            try
            {
                var json = JsonSerializer.Serialize(_items, _jsonSerializerOptions);
                await File.WriteAllTextAsync(pending, json);
                File.Copy(pending, SaveFilePath, true);
                File.Delete(pending);
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
