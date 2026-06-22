namespace IDESK.Core.Agent;

public class AgentService
{
    private readonly AgentClient _client = new();

    public async Task SendAsync(string text, Action<string> onChunk)
    {
        var config = LlmConfig.Load();
        if (string.IsNullOrEmpty(config.Key))
        {
            onChunk("请先在设置中配置 API Key");
            return;
        }

        try
        {
            await _client.SendMessageAsync(config.Provider, config.Url, config.Key, config.Model, text, onChunk);
        }
        catch (Exception ex)
        {
            onChunk($"请求失败：{ex.Message}");
        }
    }
}
