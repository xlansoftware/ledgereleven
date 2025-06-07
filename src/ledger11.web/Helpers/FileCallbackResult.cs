using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

public class FileCallbackResult : FileResult
{
    private readonly Func<Stream, ActionContext, Task> _callback;

    public FileCallbackResult(string contentType, Func<Stream, ActionContext, Task> callback)
        : base(contentType)
    {
        _callback = callback ?? throw new ArgumentNullException(nameof(callback));
    }

    public override async Task ExecuteResultAsync(ActionContext context)
    {
        var response = context.HttpContext.Response;
        response.ContentType = ContentType.ToString();

        if (!string.IsNullOrEmpty(FileDownloadName))
        {
            var headerValue = new ContentDispositionHeaderValue("attachment")
            {
                FileNameStar = FileDownloadName
            };
            response.Headers[HeaderNames.ContentDisposition] = headerValue.ToString();
        }

        await _callback(response.Body, context);
    }
}
