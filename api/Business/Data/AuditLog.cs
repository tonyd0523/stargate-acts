// Persistent audit record written by LoggingBehavior for every MediatR request.
// IsException = false for successes, true for unhandled exceptions.
// OperationCanceledException is intentionally excluded — cancellation is not an error.
// Message stores the serialized request payload, so sensitive fields should be
// annotated with [JsonIgnore] on the request class to prevent them appearing here.
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations.Schema;

namespace StargateAPI.Business.Data
{
    /// <summary>
    /// A record of an API request logged by the pipeline behavior.
    /// </summary>
    [Table("AuditLog")]
    public class AuditLog
    {
        /// <summary>Unique log entry ID.</summary>
        public int Id { get; set; }

        /// <summary>UTC timestamp when the log entry was created.</summary>
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        /// <summary>Log message describing the request or error.</summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>True if this entry represents an exception.</summary>
        public bool IsException { get; set; }
    }

    public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedOnAdd();
        }
    }
}
