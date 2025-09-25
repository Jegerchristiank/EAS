namespace EsgAsAService.Web.Services;

public sealed class ToastService
{
    private readonly List<ToastMessage> _messages = new();

    public event EventHandler? OnChanged;

    public IReadOnlyList<ToastMessage> Messages => _messages;

    public void ShowInfo(string title, string message) => Add(new ToastMessage("info", title, message));
    public void ShowSuccess(string title, string message) => Add(new ToastMessage("success", title, message));
    public void ShowWarning(string title, string message) => Add(new ToastMessage("warning", title, message));
    public void ShowError(string title, string message) => Add(new ToastMessage("danger", title, message));

    public void Dismiss(ToastMessage toast)
    {
        if (_messages.Remove(toast))
        {
            OnChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void Add(ToastMessage message)
    {
        _messages.Add(message);
        if (_messages.Count > 5)
        {
            _messages.RemoveAt(0);
        }
        OnChanged?.Invoke(this, EventArgs.Empty);
    }
}

public sealed record ToastMessage(string Variant, string Title, string Message, DateTimeOffset CreatedAt)
{
    public ToastMessage(string Variant, string Title, string Message)
        : this(Variant, Title, Message, DateTimeOffset.UtcNow)
    {
    }
}
