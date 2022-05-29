using System;

namespace Notes.WebAPI.Domain
{
    public class Note
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Body { get; set; }
        public DateTime Created { get; set; } = DateTime.Now;
    }
}