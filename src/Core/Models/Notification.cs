using System;
using Gearbox.Core.Types;
#pragma warning disable CS8618, CS9264

namespace Gearbox.Core.Models
{
    public class Notification
    {
        public Notification()
        {

        }
        public Notification(string title, string message) : this()
        {
            Title = title;
            Message = message;
        }

        public string Title { get; set; }
        public string Message { get; set; }
        public NotificationType Type { get; set; }
        public TimeSpan? Expiration { get; set; }
        public Action? OnClick { get; set; }
        public Action? OnClose { get; set; }
    }
}
