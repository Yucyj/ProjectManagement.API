using System;

namespace ProjectManagement.API.DTOs
{
    public class CommentDto
    {
        public int Id { get; set; }
        public int ChangeRequestId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
    }

    public class CreateCommentDto
    {
        public int ChangeRequestId { get; set; }
        public string Text { get; set; } = string.Empty;
    }
}
