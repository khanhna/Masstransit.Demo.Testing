using System;

namespace Library.Contract
{
    public interface IBookAddedGlobalEvent
    {
        public Guid BookId { get; }
        public DateTime TimeStamp { get; }

        public string Isbn { get; }
        public string Title { get; }
    }
}