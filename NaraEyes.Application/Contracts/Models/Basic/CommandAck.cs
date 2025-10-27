namespace NaraEyes.Application.Contracts.Models.Basic
{
    public sealed class CommandAck
    {
        public Guid CommandId { get; set; }
        public bool Accepted { get; set; }
        public string? Message { get; set; }
        
    }
    public sealed class CommandAck<T>
    {
        public Guid CommandId { get; set; }
        public bool Accepted { get; set; }
        public string? Message { get; set; }
        public T payload { get; set; }

    }
}
