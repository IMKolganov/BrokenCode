using System;

namespace BrokenCode.Etc
{
    public class GetReportRequest
    {
        public Guid DomainId { get; set; }
        public int PageSize { get; set; }
        public int PageNumber { get; set; }
    }
}
